// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Builds the <b>derived</b> model that simulates only <paramref name="spaces"/> as thermal zones,
        /// keeping the thermal boundaries and the solar context the selection needs - or refuses, saying
        /// why.
        ///
        /// <para><b>The geometry is not decided here</b></para>
        /// <para>
        /// The extraction and the whole panel classification are
        /// <see cref="AdjacencyCluster.Filter(IEnumerable{Space}, bool)"/>'s - the production authority the
        /// <c>SAMAnalytical.FilterBySpaces</c> component has always called, which already returns a new
        /// cluster (the source is never touched), carries space guids across unchanged, and classifies every
        /// panel by comparing what it is adjacent to in the DERIVED cluster against what it was adjacent to
        /// in the SOURCE:
        /// </para>
        /// <list type="bullet">
        /// <item><b>selected to selected</b> - two spaces in the derived cluster, so it is left as the ordinary internal partition it is.</item>
        /// <item><b>selected to excluded</b> - one space in the derived cluster but two in the source, so it is the isolation cut and is marked <c>PanelParameter.Adiabatic</c>.</item>
        /// <item><b>selected to outside</b> - one space in both, so it is genuinely external and is left exactly as it is: its type, construction, apertures, orientation and exposure all unchanged.</item>
        /// </list>
        /// <para>
        /// This method adds only what <c>Filter</c> has no way to know about, because it is a general
        /// geometric filter and these are questions about a <i>simulation</i>: the plant graph, the airflow
        /// network, the apertures on the cut, and the surrounding geometry that still casts shade.
        /// </para>
        ///
        /// <para><b>Fail closed on shared plant and on airflow crossing the cut</b></para>
        /// <para>
        /// Checked <b>before</b> anything is built. A ventilation system or air handling unit that also
        /// serves excluded spaces cannot be carried into an isolated model without either simulating the
        /// spaces it was supposed to leave out, or quietly keeping a whole unit's duty while dropping the
        /// branches that justified it. Proportionally splitting shared central plant is a different
        /// engineering problem and is deliberately not attempted here; the selection is refused instead. The
        /// dedicated per-dwelling MVHR the Part O workflow builds is unaffected - it serves exactly one
        /// dwelling, so it is never shared.
        /// </para>
        ///
        /// <para><b>The source model is never modified.</b></para>
        /// </summary>
        /// <param name="adjacencyCluster">The whole building. Read only.</param>
        /// <param name="spaces">The spaces to isolate. Matched by guid - never by name.</param>
        /// <returns>The isolated cluster, or the refusals. Never both.</returns>
        public static SpaceIsolation IsolateSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces)
        {
            List<string> refusals = [];
            List<string> notes = [];

            if (adjacencyCluster is null)
            {
                refusals.Add("There is no model to isolate a selection from.");

                return new SpaceIsolation(null, refusals, notes, 0, 0, 0);
            }

            // ---- The selection, by identity ------------------------------------------------------------
            //
            // Resolved against the source cluster rather than trusting the instances handed in, so a caller
            // holding a stale copy of a space cannot select something the model no longer contains. The set
            // is the membership authority for every test below: one hash lookup per question, never a scan
            // of the model per selected space, which is what keeps this linear on a 5,000 space building.

            HashSet<Guid> guids_Selected = [];
            List<Space> spaces_Selected = [];

            foreach (Space space in spaces ?? [])
            {
                if (space is null)
                {
                    continue;
                }

                Space space_Source = adjacencyCluster.GetObject<Space>(space.Guid);
                if (space_Source is null)
                {
                    continue;
                }

                if (guids_Selected.Add(space_Source.Guid))
                {
                    spaces_Selected.Add(space_Source);
                }
            }

            if (spaces_Selected.Count == 0)
            {
                refusals.Add("None of the selected dwellings resolved to a space in this model, so there is nothing to simulate in isolation.");

                return new SpaceIsolation(null, refusals, notes, 0, 0, 0);
            }

            // ---- Fail closed BEFORE building anything --------------------------------------------------

            refusals.AddRange(Refusals_VentilationScope(adjacencyCluster, guids_Selected));
            refusals.AddRange(Refusals_AirMovementScope(adjacencyCluster, guids_Selected));

            if (refusals.Count != 0)
            {
                return new SpaceIsolation(null, refusals, notes, 0, 0, 0);
            }

            // ---- The extraction: the existing authority, unchanged -------------------------------------

            AdjacencyCluster result = adjacencyCluster.Filter(spaces_Selected, true);
            if (result is null)
            {
                refusals.Add("The selected dwellings could not be extracted from the model.");

                return new SpaceIsolation(null, refusals, notes, 0, 0, 0);
            }

            // ---- Relations Filter does not rebuild ------------------------------------------------------
            //
            // Filter relates each carried object to the SPACE it was carried for, which is all a geometric
            // filter needs. A simulation needs the rest of the graph too - a terminal to its system, an air
            // movement to its unit - so every source relation BETWEEN TWO CARRIED OBJECTS is restored. Only
            // carried objects are walked, so this costs the isolated model and not the building.

            RestoreRelations(adjacencyCluster, result);

            // ---- The plant the selection depends on -----------------------------------------------------
            //
            // An air handling unit is related to no space at all - a ventilation system names it in
            // VentilationSystemParameter.SupplyUnitName - so nothing about it is reachable from a space and
            // Filter cannot carry it. Without this the isolated model would have systems with no unit, and
            // every duty and equipment selection downstream would silently read as absent.

            notes.AddRange(CarryAirHandlingUnits(adjacencyCluster, result));

            // ---- The apertures on the cut ---------------------------------------------------------------

            int count_Adiabatic = 0;
            int count_ApertureRemoved = 0;

            foreach (Panel panel in result.GetPanels() ?? [])
            {
                //The isolation cut specifically - the flag Filter just set - and NOT everything
                //Query.Adiabatic would report. That reports any zero thickness construction as adiabatic
                //too, and a surface that was already adiabatic in the source building is not this run's
                //cut: it keeps whatever apertures it had, exactly as it would in a whole building run.
                if (panel is null || !panel.TryGetValue(PanelParameter.Adiabatic, out bool adiabatic) || !adiabatic)
                {
                    continue;
                }

                count_Adiabatic++;

                List<Aperture> apertures = panel.Apertures;
                if (apertures is null || apertures.Count == 0)
                {
                    continue;
                }

                // The cut stands for an omitted conditioned neighbour, and TAS represents that by nulling
                // the surface's link (SAM_Tas Modify.UpdateAdiabatic sets tbdNullLink). An aperture left on
                // it has nothing to open onto: in the derived model the panel has one adjacent space, so the
                // conversion would export the opening as an EXTERNAL window and TAS would give the flat a
                // door to outside, with solar gain and outside air behind it, where a corridor used to be.
                // That is a fabricated boundary, so the aperture is removed from the DERIVED panel. The
                // source model still has its door.
                count_ApertureRemoved += apertures.Count;

                Panel panel_Cut = Create.Panel(panel);
                panel_Cut.RemoveApertures();

                result.AddObject(panel_Cut);
            }

            if (count_ApertureRemoved != 0)
            {
                notes.Add(string.Format("{0} aperture(s) on the isolation cut were removed from the derived model, because an opening onto an omitted space would be simulated as an opening to outside. The source model is unchanged.", count_ApertureRemoved));
            }

            // ---- The surrounding building, as shade and nothing else -----------------------------------

            int count_Shade = AddShadingContext(adjacencyCluster, result);

            notes.Add(string.Format(
                "Thermal model scope: ISOLATED. {0} space(s) simulated; {1} interface(s) to excluded spaces treated as adiabatic; {2} excluded external surface(s) retained as shading context.",
                spaces_Selected.Count,
                count_Adiabatic,
                count_Shade));

            return new SpaceIsolation(result, refusals, notes, count_Adiabatic, count_Shade, count_ApertureRemoved);
        }

        /// <summary>
        /// The same isolation over a whole <see cref="AnalyticalModel"/>, so the derived model keeps the
        /// material library, the profile library, the location and the model level parameters the
        /// simulation reads. The source model is never modified.
        /// </summary>
        public static SpaceIsolation IsolateSpaces(this AnalyticalModel analyticalModel, IEnumerable<Space> spaces)
        {
            if (analyticalModel is null)
            {
                return new SpaceIsolation(null, ["There is no model to isolate a selection from."], [], 0, 0, 0);
            }

            return IsolateSpaces(analyticalModel.AdjacencyCluster, spaces);
        }

        /// <summary>
        /// Every ventilation system and air handling unit that serves a selected space, checked for whether
        /// it <b>also</b> serves an excluded one.
        /// <para>
        /// One pass over the systems, each asking its own related spaces - the set decides membership, so no
        /// part of this scans the model per selected space.
        /// </para>
        /// </summary>
        private static List<string> Refusals_VentilationScope(AdjacencyCluster adjacencyCluster, HashSet<Guid> guids_Selected)
        {
            List<string> result = [];

            //Unit name -> whether any system on it serves a selected space, and the excluded spaces it also
            //serves. Built in the same pass as the per-system check so the units cost nothing extra.
            Dictionary<string, bool> dictionary_Selected = [];
            Dictionary<string, List<string>> dictionary_Excluded = [];

            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem is null)
                {
                    continue;
                }

                bool serves_Selected = false;
                List<string> names_Excluded = [];

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
                {
                    if (space is null)
                    {
                        continue;
                    }

                    if (guids_Selected.Contains(space.Guid))
                    {
                        serves_Selected = true;
                    }
                    else
                    {
                        names_Excluded.Add(space.Name);
                    }
                }

                if (serves_Selected && names_Excluded.Count != 0)
                {
                    result.Add(string.Format(
                        "The selected dwellings cannot be simulated in isolation because ventilation system '{0}' also serves {1} space(s) outside the isolation scope, including '{2}'. Select those dwellings as well, or run the whole building.",
                        ventilationSystem.FullName ?? ventilationSystem.Name,
                        names_Excluded.Count,
                        names_Excluded[0]));
                }

                foreach (string name_Unit in UnitNames(ventilationSystem))
                {
                    dictionary_Selected[name_Unit] = dictionary_Selected.TryGetValue(name_Unit, out bool selected) ? selected || serves_Selected : serves_Selected;

                    if (!dictionary_Excluded.TryGetValue(name_Unit, out List<string> names))
                    {
                        names = [];
                        dictionary_Excluded[name_Unit] = names;
                    }

                    names.AddRange(names_Excluded);
                }
            }

            //A unit whose systems are individually in scope but which, taken together, straddles the cut -
            //one unit serving one selected dwelling and one excluded one through two systems. The
            //per-system check above cannot see that; this does.
            foreach (KeyValuePair<string, bool> keyValuePair in dictionary_Selected)
            {
                if (!keyValuePair.Value)
                {
                    continue;
                }

                if (!dictionary_Excluded.TryGetValue(keyValuePair.Key, out List<string> names_Excluded) || names_Excluded.Count == 0)
                {
                    continue;
                }

                string refusal = string.Format(
                    "The selected dwellings cannot be simulated in isolation because air handling unit '{0}' also serves {1} space(s) outside the isolation scope, including '{2}'. Splitting a shared unit's duty is not something this can decide - select those dwellings as well, or run the whole building.",
                    keyValuePair.Key,
                    names_Excluded.Count,
                    names_Excluded[0]);

                if (!result.Contains(refusal))
                {
                    result.Add(refusal);
                }
            }

            return result;
        }

        /// <summary>
        /// Any air movement that would survive into the isolated model pointing at a space that is no longer
        /// there - a transfer path from an excluded room into a selected one, or the reverse.
        /// <para>
        /// Refused rather than dropped. A transfer path is part of how the dwelling was shown to meet
        /// Approved Document F; silently removing one would change the ventilation design while reporting
        /// the same compliance. For the dedicated per-dwelling MVHR the Part O workflow builds, every path
        /// is within one dwelling and this never fires.
        /// </para>
        /// </summary>
        private static List<string> Refusals_AirMovementScope(AdjacencyCluster adjacencyCluster, HashSet<Guid> guids_Selected)
        {
            List<string> result = [];

            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetObjects<SpaceAirMovement>() ?? [])
            {
                if (spaceAirMovement is null)
                {
                    continue;
                }

                bool touches_Selected = false;
                List<string> names_Excluded = [];

                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(spaceAirMovement) ?? [])
                {
                    if (space is null)
                    {
                        continue;
                    }

                    if (guids_Selected.Contains(space.Guid))
                    {
                        touches_Selected = true;
                    }
                    else
                    {
                        names_Excluded.Add(space.Name);
                    }
                }

                if (touches_Selected && names_Excluded.Count != 0)
                {
                    result.Add(string.Format(
                        "The selected dwellings cannot be simulated in isolation because air movement '{0}' crosses the isolation boundary - it connects a selected space to '{1}', which is outside the scope. Select that dwelling as well, or run the whole building.",
                        spaceAirMovement.Name,
                        names_Excluded[0]));
                }
            }

            return result;
        }

        /// <summary>
        /// Restores every source relation whose <b>both</b> ends were carried into the derived cluster.
        /// Walks only what was carried, so an isolated flat costs the flat.
        /// </summary>
        private static void RestoreRelations(AdjacencyCluster adjacencyCluster, AdjacencyCluster result)
        {
            List<IJSAMObject> objects = result.GetObjects<IJSAMObject>();
            if (objects is null)
            {
                return;
            }

            HashSet<Guid> guids_Carried = [];
            foreach (IJSAMObject @object in objects)
            {
                Guid guid = result.GetGuid(@object);
                if (guid != Guid.Empty)
                {
                    guids_Carried.Add(guid);
                }
            }

            foreach (IJSAMObject @object in objects)
            {
                Guid guid = result.GetGuid(@object);
                if (guid == Guid.Empty)
                {
                    continue;
                }

                foreach (IJSAMObject object_Related in adjacencyCluster.GetRelatedObjects(guid) ?? [])
                {
                    Guid guid_Related = adjacencyCluster.GetGuid(object_Related);
                    if (guid_Related == Guid.Empty || !guids_Carried.Contains(guid_Related))
                    {
                        continue;
                    }

                    result.AddRelation(guid, guid_Related);
                }
            }
        }

        /// <summary>
        /// Carries in the air handling units the derived model's ventilation systems name, and the plant
        /// side air movements that belong to them. A unit reaches its systems by NAME rather than by
        /// relation, which is why nothing space-shaped can find it.
        /// </summary>
        private static List<string> CarryAirHandlingUnits(AdjacencyCluster adjacencyCluster, AdjacencyCluster result)
        {
            List<string> notes = [];

            HashSet<string> names_Unit = [];
            foreach (VentilationSystem ventilationSystem in result.GetObjects<VentilationSystem>() ?? [])
            {
                foreach (string name_Unit in UnitNames(ventilationSystem))
                {
                    names_Unit.Add(name_Unit);
                }
            }

            if (names_Unit.Count == 0)
            {
                return notes;
            }

            int count = 0;

            foreach (AirHandlingUnit airHandlingUnit in adjacencyCluster.GetObjects<AirHandlingUnit>() ?? [])
            {
                if (airHandlingUnit is null || string.IsNullOrWhiteSpace(airHandlingUnit.Name) || !names_Unit.Contains(airHandlingUnit.Name))
                {
                    continue;
                }

                AirHandlingUnit airHandlingUnit_Result = Core.Query.Clone(airHandlingUnit);
                if (!result.AddObject(airHandlingUnit_Result))
                {
                    continue;
                }

                count++;

                //The unit's own air movements, and its links to whatever the flat already carried - the
                //terminals' air movements are already in, and this is what reconnects them to the unit.
                foreach (IJSAMObject object_Related in adjacencyCluster.GetRelatedObjects(airHandlingUnit) ?? [])
                {
                    if (object_Related is AirHandlingUnitAirMovement)
                    {
                        IJSAMObject object_Clone = Core.Query.Clone(object_Related);
                        result.AddObject(object_Clone);
                        result.AddRelation(airHandlingUnit_Result, object_Clone);

                        continue;
                    }

                    Guid guid_Related = adjacencyCluster.GetGuid(object_Related);
                    if (guid_Related != Guid.Empty && result.GetObject<IJSAMObject>(guid_Related) is not null)
                    {
                        result.AddRelation(result.GetGuid(airHandlingUnit_Result), guid_Related);
                    }
                }
            }

            if (count != 0)
            {
                notes.Add(string.Format("{0} air handling unit(s) serving only the selected dwellings were carried into the isolated model.", count));
            }

            return notes;
        }

        /// <summary>
        /// Adds the excluded building's <b>external</b> surfaces to the derived model as shading geometry -
        /// and nothing else.
        ///
        /// <para><b>What qualifies, using the definitions that already exist</b></para>
        /// <para>
        /// A source panel that was not carried is shading context when it is exposed to the sun
        /// (<see cref="Query.ExposedToSun(PanelType)"/> - the same test the gbXML export uses to decide the
        /// surface's own solar exposure) <b>and</b> it has at most one adjacent space, which is what
        /// <see cref="AdjacencyCluster.External(Panel)"/> means. So an excluded façade, roof or exposed
        /// floor comes across, and an existing shade in the source model - which has no adjacent space at
        /// all - keeps coming across, exactly as it did before.
        /// </para>
        /// <para>
        /// <b>An internal partition never does.</b> It has two adjacent spaces and is not exposed to the
        /// sun, and turning every panel of every excluded dwelling into a shade would build a huge model
        /// of surfaces buried inside a building where no sun reaches, which is both meaningless and the
        /// thing that would undo the speed this feature exists for.
        /// </para>
        /// <para>
        /// <b>No proximity culling.</b> Retaining a legitimate external surface that turns out to shade
        /// nothing costs a little conversion; guessing wrong about which surfaces matter changes the solar
        /// result. The reduction this feature delivers comes from not simulating thousands of thermal
        /// zones, not from pruning shade.
        /// </para>
        /// </summary>
        private static int AddShadingContext(AdjacencyCluster adjacencyCluster, AdjacencyCluster result)
        {
            int count = 0;

            foreach (Panel panel in adjacencyCluster.GetPanels() ?? [])
            {
                if (panel is null)
                {
                    continue;
                }

                //Already in the isolated model as a real boundary. Adding it again as shade would double
                //the surface and shade the flat with a copy of its own wall.
                if (result.GetObject<Panel>(panel.Guid) is not null)
                {
                    continue;
                }

                if (!Query.ExposedToSun(panel.PanelType))
                {
                    continue;
                }

                List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(panel);
                if (spaces is not null && spaces.Count > 1)
                {
                    continue;
                }

                //Stated as a shade rather than left as a spaceless external wall: the conversion decides
                //what a surface is from its type, and a wall belonging to no space is not a thing the TBD
                //model has a place for. Apertures go with it - a window in a shade is not a window.
                Panel panel_Shade = panel.PanelType == PanelType.Shade ? Create.Panel(panel) : Create.Panel(panel, PanelType.Shade);
                panel_Shade.RemoveApertures();

                if (result.AddObject(panel_Shade))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> UnitNames(VentilationSystem ventilationSystem)
        {
            List<string> result = [];

            if (ventilationSystem is null)
            {
                return result;
            }

            if (ventilationSystem.TryGetValue(VentilationSystemParameter.SupplyUnitName, out string name_Supply) && !string.IsNullOrWhiteSpace(name_Supply))
            {
                result.Add(name_Supply);
            }

            if (ventilationSystem.TryGetValue(VentilationSystemParameter.ExhaustUnitName, out string name_Exhaust) && !string.IsNullOrWhiteSpace(name_Exhaust) && !result.Contains(name_Exhaust))
            {
                result.Add(name_Exhaust);
            }

            return result;
        }
    }
}
