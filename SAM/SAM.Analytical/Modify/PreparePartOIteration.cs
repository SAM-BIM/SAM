// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Prepares a model for one Approved Document O base iteration: settles the ventilation route the
        /// assessment is being made over, decides from that route whether the Approved Document F airflows
        /// belong on the model, carries them where they do, reports how the model's authored opening
        /// behaviour compares with the stage, and states the scenarios the results are attributed to.
        /// <para>
        /// <b>This is the whole preparation, in the library.</b>
        /// <c>SAMAnalytical.PreparePartOIteration</c> reads Grasshopper parameters and raises Grasshopper
        /// messages; every decision it used to make inline is made here, so the decisions are testable
        /// without a Grasshopper assembly and the component and the tests cannot drift apart.
        /// </para>
        ///
        /// <para><b>The order of the four gates, and why it is that order</b></para>
        /// <list type="number">
        /// <item>
        /// <b>The route the assessment states.</b> <see cref="Enums.PartOVentilationMode"/>, resolved from
        /// what the assessed zones state and from nothing else. Settled first because it is the fact every
        /// later decision depends on, and because reaching the airflow application without it is the defect
        /// this whole design closes.
        /// </item>
        /// <item>
        /// <b>The route the iteration is defined over.</b> An iteration is not neutral about how the
        /// dwelling is ventilated - its operating assumptions go into the permanent scenario key - so a
        /// stage and a route that disagree produce a true simulation filed under a false claim.
        /// </item>
        /// <item>
        /// <b>Whether the Part F airflows belong on the model</b>, which on the MVHR route additionally
        /// needs the Approved Document F condition the stage runs at.
        /// </item>
        /// <item>
        /// <b>How the authored openings compare with the stage.</b> Reported, never acted on.
        /// </item>
        /// </list>
        ///
        /// <para>
        /// <b>The route is stated, never inferred, and never written.</b> No <c>SAM_System</c>,
        /// <c>SystemTemplate</c> or <c>InternalCondition.VentilationSystemTypeName</c> on the model is read
        /// to decide what is simulated - they are metadata that may be stale, may predate the assessment,
        /// and may describe a different design stage. Nor is any of them mutated to force a route: that
        /// would put the decision straight back into the metadata it was taken out of, and would make the
        /// model on disk a lie about the building. See <c>documentation/PartO-ARCHITECTURE.md</c>.
        /// </para>
        /// <para>
        /// <b>The model is never rewritten to fit the stage.</b> An <c>OpeningRestriction</c> is authored
        /// building data and <c>PartOOpeningProperties.Schedule</c> is derived from it, so resetting a
        /// restriction to match the stage's assumption deletes the aperture's <c>PartO_DayOpen_HH_HH</c>
        /// availability schedule from the model that reaches TAS. The disagreement is reported instead.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// The model to prepare. <b>Not modified</b> - a prepared copy is returned on the result.
        /// </param>
        /// <param name="partOIteration">
        /// The base iteration being prepared - <c>BasePassive</c> (1a, MVHR) or
        /// <c>BaseNaturalVentilation</c> (1b). These are alternatives, not successive stages.
        /// </param>
        /// <param name="zones">
        /// The zones to assess, already resolved against <paramref name="analyticalModel"/>. Null means
        /// every zone the model carries; an empty sequence means the caller named zones and none of them
        /// resolved. Either way, no assessed zone means no stated route, which refuses.
        /// </param>
        /// <param name="dictionary_VentilationStrategy">
        /// The Part O ventilation route each assessed zone states, by zone guid - <c>NV</c> /
        /// <c>NaturalVentilation</c>, or <c>MVHR</c> / <c>MVRE</c>. Required, never defaulted.
        /// </param>
        public static PartOIterationPreparation PreparePartOIteration(this AnalyticalModel analyticalModel, PartOIteration partOIteration, IEnumerable<Zone> zones, Dictionary<Guid, string> dictionary_VentilationStrategy)
        {
            PartOIterationPreparation result = new();

            if (analyticalModel == null)
            {
                result.Refusal = "No analytical model was supplied, so there is nothing to prepare.";

                return result;
            }

            //Resolved off the SUPPLIED model rather than the applied one, because the route has to be known
            //before anything is applied. The airflow application never adds, removes or re-guids a zone -
            //it only replaces spaces - so the two sets are the same set, and the scenarios below are still
            //stated over zone objects taken from the model that will be simulated.
            List<Zone> zones_Assessed = [];
            if (zones == null)
            {
                zones_Assessed = analyticalModel.GetZones() ?? [];
            }
            else
            {
                foreach (Zone zone in zones)
                {
                    if (zone != null)
                    {
                        zones_Assessed.Add(zone);
                    }
                }
            }

            // ---- 1. The route the assessment states -----------------------------------------------------

            result.VentilationMode = Query.PartOVentilationMode(zones_Assessed, dictionary_VentilationStrategy, out string refusal_VentilationMode);

            result.AirflowApplication = Query.PartOPartFAirflowApplication(result.VentilationMode, out string diagnostic_Airflow);

            if (result.VentilationMode == Enums.PartOVentilationMode.Undefined)
            {
                //Refused BEFORE anything is applied, and with no model on the result. Continuing here is
                //materially different from continuing past an opening-restriction disagreement: that one
                //only mislabels which assumption a true result was obtained under, while this one would put
                //continuous mechanical supply and extract into a dwelling that may have none.
                result.Refusal = refusal_VentilationMode;

                return result;
            }

            // ---- 2. The route the iteration is defined over ---------------------------------------------

            PartOVentilationMode partOVentilationMode_Iteration = partOIteration.PartOIterationVentilationMode(out string refusal_Iteration);

            if (partOVentilationMode_Iteration == Enums.PartOVentilationMode.Undefined)
            {
                result.Refusal = refusal_Iteration;

                return result;
            }

            if (partOVentilationMode_Iteration != result.VentilationMode)
            {
                //Not a formality. The iteration's operating assumptions are part of the derived
                //OverheatingScenario.Key, so this pairing would not merely be confusing - it would mint a
                //permanent identity asserting something false about the building, and every result ever
                //attributed to that key would carry the assertion with it.
                result.Refusal = string.Format(
                    "The {0} iteration is the base configuration for the {1} route, but the assessed zones state {2}. These are alternative base configurations of the same dwelling, not successive stages, so one of the two statements is wrong. Assess a {2} dwelling at {3}. Preparing it here would attribute a true {2} result to a scenario whose permanent identity asserts the {1} route's operating assumptions.",
                    partOIteration,
                    partOVentilationMode_Iteration,
                    result.VentilationMode,
                    result.VentilationMode == Enums.PartOVentilationMode.NaturalVentilation ? PartOIteration.BaseNaturalVentilation : PartOIteration.BasePassive);

                return result;
            }

            // ---- 3. The Approved Document F airflows ----------------------------------------------------

            AnalyticalModel analyticalModel_Applied;

            if (result.AirflowApplication == PartOPartFAirflowApplication.SkipNaturalVentilation)
            {
                //A copy, so the "not modified, a prepared copy is returned" contract holds on this path too
                //and a caller comparing the two objects sees the same thing either way. Nothing is written
                //to it: absence of PartFSpaceData is not an error here, because there are no continuous
                //mechanical rates to look for.
                analyticalModel_Applied = new AnalyticalModel(analyticalModel);

                result.Notes.Add(diagnostic_Airflow);
                result.Warnings.Add(diagnostic_Airflow);
            }
            else
            {
                //The join between the two documents: which Approved Document F condition this stage runs at.
                //Asked only on the MVHR route, because it is the only route with a continuous mechanical
                //rate to have a condition for. A stage with no settled condition refuses rather than being
                //simulated at whichever rate happens to be handy.
                PartFOperatingMode? partFOperatingMode = partOIteration.PartOIterationOperatingMode(out string refusal_OperatingMode);
                if (!partFOperatingMode.HasValue)
                {
                    result.Refusal = refusal_OperatingMode;

                    return result;
                }

                analyticalModel_Applied = analyticalModel.ApplyPartFVentilationRates(partFOperatingMode.Value, out List<string> refusals_Airflow, out List<string> notes_Airflow);

                result.Refusals.AddRange(refusals_Airflow);
                result.Notes.AddRange(notes_Airflow);

                if (analyticalModel_Applied == null)
                {
                    result.Refusal = "No Part F rates could be applied, so there is nothing to simulate. Run a Part F component first.";

                    return result;
                }

                // ---- 4. The Base MVHR design realization ------------------------------------------------

                //This is what makes the stage's own assertion true. BasePassive states "Mechanical
                //Ventilation At Design Rate = True" and that claim is inside the permanent scenario key -
                //but until now nothing made a simulation obey it. Applying the rates above put the
                //requirement onto each space's internal condition; only the chain below turns that
                //requirement into air the simulation actually moves.
                //
                //It runs HERE, on the MVHR branch, and nowhere else. A Part F calculation on its own
                //creates requirements and nothing more. The trigger is the Base MVHR operating scenario
                //asking for design-rate operation, never the presence of an airflow on a space.
                analyticalModel_Applied = PrepareBaseMVHR(analyticalModel_Applied, result);

                if (result.Refusal != null)
                {
                    return result;
                }
            }

            // ---- 5. The authored openings, reported ------------------------------------------------------

            //The join between the stage's "Openings Restricted" assumption and the opening behaviour the
            //model carries. The model is REPORTED ON, never rewritten to fit the stage.
            result.OpeningCompatibility = analyticalModel_Applied.PartOIterationOpeningCompatibility(partOIteration, out string summary_Openings, out List<string> evidence_Openings);

            if (evidence_Openings != null)
            {
                result.Notes.AddRange(evidence_Openings);
            }

            if (summary_Openings != null)
            {
                result.Notes.Add(summary_Openings);
                result.Warnings.Add(summary_Openings);
            }

            result.AnalyticalModel = analyticalModel_Applied;

            // ---- 6. The scenarios the results are attributed to ------------------------------------------

            //Stated over zone objects taken from the APPLIED model, so a zone guid on a scenario and a zone
            //guid in the model that will be simulated are the same identity.
            List<Zone> zones_Applied = analyticalModel_Applied.GetZones() ?? [];

            List<Zone> zones_Scenario = [];
            foreach (Zone zone in zones_Assessed)
            {
                zones_Scenario.Add(zones_Applied.Find(x => x != null && x.Guid == zone.Guid) ?? zone);
            }

            result.OverheatingScenarios.AddRange(Create.OverheatingScenarios(zones_Scenario, partOIteration, dictionary_VentilationStrategy, out List<string> refusals_Scenarios));

            result.Refusals.AddRange(refusals_Scenarios);

            return result;
        }

        /// <summary>
        /// Turns the applied Approved Document F requirement into a Base MVHR design the simulation can
        /// actually run: design terminals, one generic system and unit, a derived duty checked against the
        /// requirement, and the directional air movements that carry it into TAS.
        /// <para>
        /// <b>Every step refuses rather than half-completing.</b> A model that reached the export with
        /// terminals but no system, or a system but no air movements, would simulate a dwelling whose
        /// mechanical ventilation is partly there - and would do it successfully, which is the failure mode
        /// this whole design exists to prevent.
        /// </para>
        /// </summary>
        private static AnalyticalModel PrepareBaseMVHR(AnalyticalModel analyticalModel, PartOIterationPreparation result)
        {
            //ONE cluster instance, taken once and put back once. AnalyticalModel.AdjacencyCluster returns a
            //fresh copy on every read, so reading it twice would silently discard the first half of this.
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            // ---- Design terminals --------------------------------------------------------------------

            //Every space, not only the assessed zones' spaces: Modify.ApplyPartFVentilationRates is a
            //whole-model write and the route resolution refuses a mixed model, so the sized spaces and the
            //assessed spaces are the same set. Realizing a subset would leave the applied airflow of the
            //rest with no terminal behind it.
            List<VentilationTerminal> ventilationTerminals = adjacencyCluster.RealizePartFVentilationTerminals(null, out List<string> notes_Terminals, out List<string> refusals_Terminals);

            result.Notes.AddRange(notes_Terminals);

            if (refusals_Terminals.Count != 0)
            {
                result.Refusal = string.Join(" ", refusals_Terminals);

                return analyticalModel;
            }

            if (ventilationTerminals == null || ventilationTerminals.Count == 0)
            {
                result.Refusal = "The MVHR route was stated and the Approved Document F rates were applied, but no space carries a continuous requirement that could be realized as a design ventilation terminal, so there is no mechanical ventilation for this iteration to simulate. Run the Part F calculation before preparing the iteration.";

                return analyticalModel;
            }

            // ---- The generic system and unit ----------------------------------------------------------

            VentilationSystem ventilationSystem = adjacencyCluster.AddPartOBaseMVHRSystem(null, out AirHandlingUnit airHandlingUnit, out List<string> notes_System, out List<string> warnings_System, out List<string> refusals_System);

            result.Notes.AddRange(notes_System);
            result.Notes.AddRange(warnings_System);
            result.Warnings.AddRange(warnings_System);

            if (ventilationSystem == null || airHandlingUnit == null)
            {
                result.Refusal = refusals_System.Count != 0
                    ? string.Join(" ", refusals_System)
                    : "The Base MVHR ventilation system could not be established, so there is nothing to move the design airflow.";

                return analyticalModel;
            }

            // ---- The derived duty, checked against the requirement -------------------------------------

            bool reconciled = adjacencyCluster.ReconcileVentilationSystemDesignDuty(ventilationSystem, out List<string> notes_Duty, out List<string> warnings_Duty, out List<string> refusals_Duty);

            result.Notes.AddRange(notes_Duty);
            result.Warnings.AddRange(warnings_Duty);

            if (!reconciled)
            {
                result.Refusal = string.Join(" ", refusals_Duty);

                return analyticalModel;
            }

            adjacencyCluster.VentilationSystemDesignDuty(ventilationSystem, out double supplyDuty_Lps, out double extractDuty_Lps);

            // ---- The runtime realization ---------------------------------------------------------------

            //The dwelling the transfer air is routed over, settled ONCE and used by everything below.
            //
            //Wider than the system's served spaces on purpose. The ventilation system is related only to the
            //rooms carrying a design terminal, which is correct - it moves no air into an internal hall - but
            //that hall is the room the dwelling's transfer air crosses and divides at, and a network solved
            //without it reports a supplied bedroom and an extracted bathroom as having no connection. Narrower
            //than the model on purpose too: a communal corridor belongs to no dwelling and must never become a
            //shortcut between two of them. Query.PartFTransferAirSpaces asks the Part F calculation's own
            //dwelling rule where the boundary is.
            List<Space> spaces_Dwelling = adjacencyCluster.PartFTransferAirSpaces(adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem), out List<string> notes_Scope);

            result.Notes.AddRange(notes_Scope ?? []);

            //Stale movements first, whatever built them. Preparing the same model twice must produce the same
            //model, not two sets of air movements that a TBD would write as two sets of inter-zone air
            //movements - and a model arriving with its own system-template air movements on these rooms
            //would otherwise add its supply to this design's.
            RemoveBaseMVHRAirMovementObjects(adjacencyCluster, ventilationSystem, airHandlingUnit, spaces_Dwelling);

            //SCOPED to the system this iteration built. The model's own ventilation systems are left exactly
            //as authored and are not realized here: they may serve the same rooms, and walking them too would
            //ventilate every shared room twice.
            List<IAirMovementObject> airMovementObjects = adjacencyCluster.AddAirMovementObjects(analyticalModel.ProfileLibrary, ventilationSystem);

            if (airMovementObjects == null || airMovementObjects.Count == 0)
            {
                result.Refusal = string.Format("Ventilation system '{0}' was built with a design duty of {1:0.###} l/s supply and {2:0.###} l/s extract, but no air movement could be realized from it, so nothing would reach the simulation.", ventilationSystem.FullName, supplyDuty_Lps, extractDuty_Lps);

                return analyticalModel;
            }

            result.Notes.Add(string.Format(
                "Realized {0} air movement object(s) for the Base MVHR design-rate operating state: supply into each space from '{1}' and extract from each space back to it, each direction sized from that space's own design terminals.",
                airMovementObjects.Count,
                airHandlingUnit.Name));

            // ---- The internal transfer air that closes each room -----------------------------------------

            //A balanced heat recovery dwelling balances at the SYSTEM, so almost every room is individually
            //out of balance - and TAS refuses to simulate a zone whose inter-zone air movements do not
            //balance. The air that closes each room is transfer air, routed by the Approved Document F
            //airflow network over the model's own internal adjacencies. Nothing is invented: where the
            //network cannot route a room's net, this refuses rather than making a route up.
            List<SpaceAirMovement> spaceAirMovements_Transfer = adjacencyCluster.AddPartFTransferAirMovements(analyticalModel.ProfileLibrary, spaces_Dwelling, out List<string> notes_Transfer, out List<string> refusals_Transfer);

            result.Notes.AddRange(notes_Transfer);

            if (spaceAirMovements_Transfer == null || refusals_Transfer.Count != 0)
            {
                result.Refusal = refusals_Transfer.Count != 0
                    ? string.Join(" ", refusals_Transfer)
                    : "The dwelling's internal transfer air could not be established, so its rooms would not balance and TAS would refuse to simulate the model.";

                return analyticalModel;
            }

            // ---- Conservation, checked at every node ----------------------------------------------------

            //Checked over the DWELLING, not over the served spaces: a zero-terminal hall that passes air on
            //is a TAS zone carrying inter-zone air movements like any other, and one that gained more than it
            //passed on would be refused by TAS while every served room balanced perfectly.
            string refusal_Balance = RefuseUnbalancedAirMovement(adjacencyCluster, spaces_Dwelling, airHandlingUnit);

            if (refusal_Balance != null)
            {
                result.Refusal = refusal_Balance;

                return analyticalModel;
            }

            result.Notes.Add("Every space and the air handling unit balance: each passes on exactly what it receives, which is what TAS requires of a zone carrying inter-zone air movements.");

            result.VentilationTerminals.AddRange(ventilationTerminals);
            result.VentilationSystem = ventilationSystem;
            result.AirHandlingUnit = airHandlingUnit;
            result.DesignSupplyDuty_Lps = supplyDuty_Lps;
            result.DesignExtractDuty_Lps = extractDuty_Lps;

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        /// <summary>
        /// Checks that every node the system's air movements touch passes on exactly what it receives, and
        /// states what is wrong where one does not.
        ///
        /// <para>
        /// <b>Summed per node, never matched per route.</b> These movements form a directed network: the
        /// unit feeds several rooms, a room may draw from several rooms and pass air on to several more, and
        /// flows split and recombine along the way - a bedroom of the acceptance dwelling divides its supply
        /// between three rooms, and one of its ensuites draws from two. No movement has a partner.
        /// Conservation, and TAS, ask only that the sums agree at each zone.
        /// </para>
        /// <para>
        /// This is a check, not a correction: nothing is rescaled to make it pass. A model that fails it is
        /// one TAS would refuse with <c>Simulation Failed</c>, and saying so here is the difference between
        /// a refusal an engineer can act on and a simulation that reports success having produced nothing.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model the air movements are read from.</param>
        /// <param name="spaces">
        /// Every space of the dwelling, terminal or no terminal - the same scope the transfer air was routed
        /// over. An intermediate space left out of this would be a zone TAS refused while this reported the
        /// model balanced.
        /// </param>
        /// <param name="airHandlingUnit">The unit, which is a node of the network like any room.</param>
        /// <returns><b>Null</b> where every node balances; the refusal otherwise.</returns>
        private static string RefuseUnbalancedAirMovement(AdjacencyCluster adjacencyCluster, List<Space> spaces, AirHandlingUnit airHandlingUnit)
        {
            List<SpaceAirMovement> spaceAirMovements = [];

            void Collect(Core.IJSAMObject jSAMObject)
            {
                foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(jSAMObject) ?? [])
                {
                    if (spaceAirMovement != null && spaceAirMovements.Find(x => x.Guid == spaceAirMovement.Guid) == null)
                    {
                        spaceAirMovements.Add(spaceAirMovement);
                    }
                }
            }

            Collect(airHandlingUnit);

            foreach (Space space in spaces ?? [])
            {
                Collect(space);
            }

            Dictionary<Guid, double> dictionary_Residual = adjacencyCluster.AirMovementResidual(spaceAirMovements, [airHandlingUnit]);

            List<string> diagnostics = [];

            void Check(Core.SAMObject sAMObject)
            {
                if (sAMObject == null || !dictionary_Residual.TryGetValue(sAMObject.Guid, out double residual))
                {
                    return;
                }

                if (System.Math.Abs(residual) <= Query.AirMovementResidualTolerance)
                {
                    return;
                }

                diagnostics.Add(string.Format("{0} {1} {2:0.###} l/s", sAMObject.Name, residual > 0 ? "gains" : "loses", System.Math.Abs(residual) * 1000));
            }

            foreach (Space space in spaces ?? [])
            {
                Check(space);
            }

            Check(airHandlingUnit);

            if (diagnostics.Count == 0)
            {
                return null;
            }

            diagnostics.Sort(StringComparer.Ordinal);

            return string.Format("The air movements do not balance: {0}. TAS refuses to simulate a zone that gains air it never loses, so this model would fail rather than produce a result. The design terminal duties are authoritative and are not adjusted to close the difference - correct the design, or the internal adjacencies the transfer air is routed over.", string.Join(", ", diagnostics));
        }

        /// <summary>
        /// Removes the air movement objects belonging to this system and unit, so that re-preparing a model
        /// replaces them rather than adding a second set beside them.
        /// <para>
        /// Scoped to the system's own unit and to the dwelling it serves on purpose: an inter-zone air
        /// movement a modeller created by hand elsewhere in the building is not this iteration's to delete.
        /// </para>
        /// <para>
        /// The dwelling rather than the served spaces, because a transfer movement is related to the space it
        /// ARRIVES in - so the one arriving in a zero-terminal hall is related to that hall alone. Collecting
        /// only the served spaces would leave it behind, and re-preparing the model would write a second
        /// transfer beside it.
        /// </para>
        /// </summary>
        private static void RemoveBaseMVHRAirMovementObjects(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, AirHandlingUnit airHandlingUnit, List<Space> spaces)
        {
            List<Core.SAMObject> sAMObjects = [];

            void Collect(Core.IJSAMObject jSAMObject)
            {
                foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(jSAMObject) ?? [])
                {
                    if (spaceAirMovement != null && sAMObjects.Find(x => x.Guid == spaceAirMovement.Guid) == null)
                    {
                        sAMObjects.Add(spaceAirMovement);
                    }
                }
            }

            Collect(airHandlingUnit);

            foreach (Space space in spaces ?? adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
            {
                Collect(space);
            }

            foreach (AirHandlingUnitAirMovement airHandlingUnitAirMovement in adjacencyCluster.GetRelatedObjects<AirHandlingUnitAirMovement>(airHandlingUnit) ?? [])
            {
                if (airHandlingUnitAirMovement != null && sAMObjects.Find(x => x.Guid == airHandlingUnitAirMovement.Guid) == null)
                {
                    sAMObjects.Add(airHandlingUnitAirMovement);
                }
            }

            if (sAMObjects.Count != 0)
            {
                adjacencyCluster.Remove(sAMObjects);
            }
        }
    }
}
