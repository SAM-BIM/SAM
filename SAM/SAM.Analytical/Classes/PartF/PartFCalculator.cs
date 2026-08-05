// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    /// <summary>
    /// Sizes NEW dwelling ventilation flow rates following Approved Document F, Volume 1: Dwellings
    /// (2021 edition, for use in England, in effect from 15 June 2022), for a balanced
    /// mechanical ventilation with heat recovery arrangement.
    /// <para>
    /// Whole dwelling rates come from Table 1.3 and paragraph 1.24, including note 1 for a dwelling with
    /// exactly one habitable room; wet room minimum extract rates from Table 1.2; and supply is
    /// distributed in proportion to habitable room volume per paragraph 1.67.
    /// </para>
    /// <para>
    /// <b>Scope: new dwellings only.</b> Section 3 of the Approved Document, covering work on existing
    /// dwellings, is not implemented, and there is deliberately no new/existing switch. Only mechanical
    /// ventilation with heat recovery (paragraphs 1.67 to 1.73) is sized - natural ventilation with
    /// intermittent extract (Table 1.1) and continuous mechanical extract ventilation (paragraphs 1.60 to
    /// 1.66) are not implemented.
    /// </para>
    /// <para>
    /// Each rate exists at two conditions. The <b>continuous design</b> rate is the Approved Document F
    /// sizing condition, taken as the greatest of the bedroom or one-habitable-room rate, the floor area
    /// rate, and the total of the wet room minimums. The <b>setback</b> rate is a SAM reduced-operation
    /// convention obtained by scaling the continuous design rates, and never alters them.
    /// </para>
    /// <para>
    /// Use <see cref="Calculate(IEnumerable{Space})"/> with no argument to size the whole model as
    /// one dwelling, or <see cref="Calculate(string)"/> with a zone category name to size each zone
    /// in that category independently as its own flat or dwelling.
    /// </para>
    /// <para>
    /// Rooms are recognised through the shared semantic classification layer
    /// (<see cref="SpaceSemanticsResolver"/>), so Approved Document F, Approved Document O, CIBSE TM59
    /// and the SAM_UI internal condition mapping all agree on what a space is.
    /// </para>
    /// <para>
    /// <b>Local kitchen extract limitation.</b> Approved Document F paragraph 1.17a and Table 1.2
    /// require extract ventilation of at least 13 l/s from the room containing the cooking function,
    /// while paragraph 1.67 requires mechanical supply to each habitable room. A studio and an open plan
    /// living kitchen are both: Appendix A makes them habitable, because neither is <i>solely</i> a
    /// kitchen, and both contain the cooking function. SAM assigns one terminal role per space, so by
    /// deliberate design convention those rooms receive the supply role only.
    /// </para>
    /// <para>
    /// The limitation is specifically the absence of an explicitly modelled LOCAL kitchen or cooker
    /// extract, not the absence of general dwelling extract. A cooking space counts as having explicit
    /// local kitchen extract only where that space itself takes an extract terminal - a room classified
    /// as solely a kitchen, carrying the Table 1.2 kitchen rate. SAM has no cooker hood or other local
    /// extract concept: VentilationSystem carries supply and exhaust unit names for system assignment,
    /// not Part F terminal rates.
    /// </para>
    /// <para>
    /// Extract from a bathroom, ensuite, utility room or sanitary accommodation may balance the dwelling
    /// airflow, but it is not evidence of local kitchen extract and therefore does not suppress the
    /// ENGINEERING CHECK REQUIRED warning. A studio or open plan living kitchen raises that warning even
    /// where a bathroom or ensuite provides the dwelling's general extract. This component does not on
    /// its own demonstrate compliance with the Approved Document F local kitchen extract requirement.
    /// </para>
    /// </summary>
    public class PartFCalculator
    {
        private readonly PartFData partFData;

        public PartFCalculator(PartFData partFData)
        {
            this.partFData = partFData;
        }

        public AdjacencyCluster AdjacencyCluster { get; set; }

        private double? setbackFlowRateFactor = null;

        /// <summary>
        /// Setback operating factor used by this calculation: setback rate = continuous design rate x
        /// factor. Defaults to the rule set's own factor, which is 30% of continuous design unless the
        /// rule set says otherwise.
        /// <para>
        /// Setting this overrides the rule set for this calculator only, so a caller can offer the factor
        /// as an input without mutating the shared default rule set held by ActiveSetting. Only a factor
        /// greater than 0 and no greater than 1 is accepted; zero, a negative value, a value above 1, NaN
        /// and infinity are replaced by <see cref="PartFData.DefaultSetbackFlowRateFactor"/>.
        /// </para>
        /// <para>
        /// A SAM reduced-operation convention. It is applied only after every Approved Document F minimum
        /// has been established at the continuous design condition, and never changes a design rate.
        /// </para>
        /// </summary>
        public double SetbackFlowRateFactor
        {
            get
            {
                return setbackFlowRateFactor ?? partFData?.SetbackFlowRateFactor ?? PartFData.DefaultSetbackFlowRateFactor;
            }

            set
            {
                setbackFlowRateFactor = PartFData.IsValidSetbackFlowRateFactor(value) ? value : PartFData.DefaultSetbackFlowRateFactor;
            }
        }

        /// <summary>One entry per dwelling sized by the most recent calculation.</summary>
        public List<PartFDwellingResult> DwellingResults { get; private set; } = [];

        /// <summary>
        /// Conditions from the most recent calculation that need the engineer's attention. Messages
        /// are prefixed with the dwelling zone name where the calculation was grouped by zone.
        /// </summary>
        public List<string> Warnings { get; private set; } = [];

        /// <summary>Informational notes from the most recent calculation.</summary>
        public List<string> Remarks { get; private set; } = [];

        /// <summary>Spaces from the most recent calculation that matched no Part F room category.</summary>
        public List<string> UnclassifiedSpaceNames { get; private set; } = [];

        /// <summary>Classified spaces from the most recent calculation that received no flow rate.</summary>
        public List<string> UnassignedSpaceNames { get; private set; } = [];

        /// <summary>
        /// Zones of the selected category that were not sized because they are not dwellings, from the
        /// most recent calculation.
        /// </summary>
        public List<string> ExcludedZoneNames { get; private set; } = [];

        /// <summary>
        /// Whole dwelling ventilation rate [l/s] at the continuous design condition of the last dwelling
        /// sized. In single dwelling mode this is the rate for the whole model; in zoned mode use
        /// <see cref="DwellingResults"/>.
        /// </summary>
        public double? FinalSystemRate_Lps { get; private set; }

        /// <summary>
        /// Whole dwelling ventilation rate [l/s] at the setback operating condition of the last dwelling
        /// sized.
        /// </summary>
        public double? SetbackSystemRate_Lps { get; private set; }

        /// <summary>
        /// Sizes the supplied spaces, or the whole model when <paramref name="spaces"/> is null, as
        /// a single dwelling.
        /// </summary>
        public bool Calculate(IEnumerable<Space> spaces = null)
        {
            Reset();

            if (partFData is null || AdjacencyCluster is null)
            {
                return false;
            }

            AdjacencyCluster adjacencyCluster = new(AdjacencyCluster, deepClone: true);

            List<Space> spaces_Selected = Resolve(adjacencyCluster, spaces is null ? adjacencyCluster.GetSpaces() : [.. spaces]);

            CalculateDwelling(adjacencyCluster, null, spaces_Selected);

            AdjacencyCluster = adjacencyCluster;

            return true;
        }

        /// <summary>
        /// Sizes each dwelling zone of the named zone category independently as its own dwelling. When
        /// <paramref name="zoneCategoryName"/> is null or blank the whole model is sized as one
        /// dwelling, which is the correct behaviour for a single house and is not itself a problem.
        /// <para>
        /// A zone category on its own does not identify a dwelling: a shared corridor, a landlord area
        /// or a commercial unit can legitimately sit in the same category as the flats it serves. Zones
        /// are therefore filtered on <see cref="ZoneParameter.IsDwelling"/>:
        /// </para>
        /// <list type="bullet">
        /// <item>where any zone in the category carries the parameter, only zones explicitly set to true
        /// are sized, and zones carrying no value at all are reported;</item>
        /// <item>a zone explicitly set to false is never sized, whatever else the category contains;</item>
        /// <item>where no zone in the category carries the parameter, every zone is sized as before, so
        /// existing models keep working, and a warning recommends marking the dwellings explicitly.</item>
        /// </list>
        /// </summary>
        public bool Calculate(string zoneCategoryName)
        {
            if (string.IsNullOrWhiteSpace(zoneCategoryName))
            {
                return Calculate((IEnumerable<Space>)null);
            }

            Reset();

            if (partFData is null || AdjacencyCluster is null)
            {
                return false;
            }

            AdjacencyCluster adjacencyCluster = new(AdjacencyCluster, deepClone: true);

            List<Zone> zones = adjacencyCluster.GetZones();
            if (zones is null || zones.Count == 0)
            {
                Warnings.Add(string.Format("The model contains no zones, so zone category '{0}' could not be used. Add zones for the individual dwellings, or leave zoneCategoryName_ empty to size the whole model as one dwelling.", zoneCategoryName));
                AdjacencyCluster = adjacencyCluster;
                return true;
            }

            List<Zone> zones_Category = zones.FindAll(x => x.GetValue<string>(ZoneParameter.ZoneCategory) == zoneCategoryName);
            if (zones_Category.Count == 0)
            {
                Warnings.Add(NoMatchingZoneMessage(zones, zoneCategoryName));
                AdjacencyCluster = adjacencyCluster;
                return true;
            }

            List<Zone> zones_Selected = SelectDwellingZones(zones_Category, zoneCategoryName);
            if (zones_Selected.Count == 0)
            {
                AdjacencyCluster = adjacencyCluster;
                return true;
            }

            Dictionary<Guid, List<string>> dictionary_Zones = [];
            List<Tuple<Zone, List<Space>>> tuples_Zone = [];

            foreach (Zone zone in zones_Selected)
            {
                List<Space> spaces_Zone = Resolve(adjacencyCluster, adjacencyCluster.GetRelatedObjects<Space>(zone));

                tuples_Zone.Add(new Tuple<Zone, List<Space>>(zone, spaces_Zone));

                foreach (Space space in spaces_Zone)
                {
                    if (!dictionary_Zones.TryGetValue(space.Guid, out List<string> names))
                    {
                        names = [];
                        dictionary_Zones[space.Guid] = names;
                    }

                    names.Add(zone.Name);
                }
            }

            //A space in two selected dwelling zones is sized twice, and the second dwelling silently
            //overwrites the first.
            foreach (KeyValuePair<Guid, List<string>> keyValuePair in dictionary_Zones)
            {
                if (keyValuePair.Value.Count < 2)
                {
                    continue;
                }

                Space space = adjacencyCluster.GetObject<Space>(keyValuePair.Key);
                Warnings.Add(string.Format("Space '{0}' belongs to more than one dwelling zone ({1}). It has been sized once for each, and only the last result is kept. Each space should belong to exactly one dwelling zone.", space?.Name, string.Join(", ", keyValuePair.Value)));
            }

            List<string> names_Unzoned = [.. adjacencyCluster.GetSpaces().FindAll(x => !dictionary_Zones.ContainsKey(x.Guid)).ConvertAll(x => x.Name)];
            if (names_Unzoned.Count != 0)
            {
                Warnings.Add(string.Format("{0} space(s) do not belong to any dwelling zone of category '{1}' and were given no ventilation properties: {2}. Shared and landlord areas are expected here; any dwelling space in this list is missing from its flat.", names_Unzoned.Count, zoneCategoryName, string.Join(", ", names_Unzoned)));
            }

            foreach (Tuple<Zone, List<Space>> tuple in tuples_Zone)
            {
                if (tuple.Item2.Count == 0)
                {
                    Warnings.Add(string.Format("Dwelling zone '{0}' contains no spaces and was skipped.", tuple.Item1.Name));
                    continue;
                }

                CalculateDwelling(adjacencyCluster, tuple.Item1.Name, tuple.Item2);
            }

            AdjacencyCluster = adjacencyCluster;

            return true;
        }

        /// <summary>
        /// Applies the explicit dwelling filter to the zones of the selected category. See
        /// <see cref="Calculate(string)"/> for the rules; this reports what it excluded and why.
        /// </summary>
        private List<Zone> SelectDwellingZones(List<Zone> zones_Category, string zoneCategoryName)
        {
            List<Zone> zones_True = [];
            List<Zone> zones_False = [];
            List<Zone> zones_Unmarked = [];

            foreach (Zone zone in zones_Category)
            {
                //TryGetValue distinguishes "explicitly false" from "no value at all", which GetValue
                //cannot: both would come back as false, and a legacy model would then size nothing.
                if (zone.TryGetValue(ZoneParameter.IsDwelling, out bool isDwelling))
                {
                    (isDwelling ? zones_True : zones_False).Add(zone);
                }
                else
                {
                    zones_Unmarked.Add(zone);
                }
            }

            //No zone carries the parameter: preserve the previous category-based behaviour so existing
            //models keep working, but recommend marking the dwellings explicitly.
            if (zones_True.Count == 0 && zones_False.Count == 0)
            {
                Warnings.Add(string.Format("No zone in category '{0}' has an Is Dwelling parameter, so every zone in the category has been sized as a dwelling. Set Is Dwelling on each zone - true for a flat or house, false for a shared corridor, landlord area or commercial unit - so that non-dwelling zones are not sized as dwellings.", zoneCategoryName));
                return zones_Category;
            }

            if (zones_False.Count != 0)
            {
                ExcludedZoneNames.AddRange(zones_False.ConvertAll(x => x.Name));
                Remarks.Add(string.Format("{0} zone(s) in category '{1}' are marked Is Dwelling = false and were not sized as dwellings: {2}. Size shared and landlord areas to Approved Document F, Volume 2: Buildings other than dwellings.", zones_False.Count, zoneCategoryName, string.Join(", ", zones_False.ConvertAll(x => x.Name))));
            }

            //Mixed: some zones marked, some not. Only an explicit true is treated as a dwelling, and the
            //unmarked zones are reported rather than silently included or silently dropped.
            if (zones_Unmarked.Count != 0)
            {
                ExcludedZoneNames.AddRange(zones_Unmarked.ConvertAll(x => x.Name));
                Warnings.Add(string.Format("{0} zone(s) in category '{1}' have no Is Dwelling parameter while others do, so they were NOT sized as dwellings: {2}. Set Is Dwelling explicitly on every zone in the category.", zones_Unmarked.Count, zoneCategoryName, string.Join(", ", zones_Unmarked.ConvertAll(x => x.Name))));
            }

            if (zones_True.Count == 0)
            {
                Warnings.Add(string.Format("No zone in category '{0}' is marked Is Dwelling = true, so no dwelling was sized. Set Is Dwelling = true on each zone that represents a flat or house.", zoneCategoryName));
            }

            return zones_True;
        }

        private static string NoMatchingZoneMessage(List<Zone> zones, string zoneCategoryName)
        {
            List<string> categories = [.. zones.ConvertAll(x => x.GetValue<string>(ZoneParameter.ZoneCategory)).FindAll(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x)];

            string result = string.Format("No zone belongs to zone category '{0}', so no ventilation properties were assigned. ", zoneCategoryName);

            List<string> categories_Case = categories.FindAll(x => string.Equals(x, zoneCategoryName, StringComparison.OrdinalIgnoreCase));
            if (categories_Case.Count != 0)
            {
                return result + string.Format("Zone category names are case sensitive - did you mean '{0}'?", categories_Case[0]);
            }

            if (categories.Count == 0)
            {
                return result + "None of the zones in the model has a Zone Category set.";
            }

            return result + string.Format("Zone categories present in the model: {0}.", string.Join(", ", categories));
        }

        private void Reset()
        {
            DwellingResults = [];
            Warnings = [];
            Remarks = [];
            UnclassifiedSpaceNames = [];
            UnassignedSpaceNames = [];
            ExcludedZoneNames = [];
            FinalSystemRate_Lps = null;
            SetbackSystemRate_Lps = null;
        }

        private static List<Space> Resolve(AdjacencyCluster adjacencyCluster, List<Space> spaces)
        {
            List<Space> result = [];
            if (spaces is null)
            {
                return result;
            }

            foreach (Space space in spaces)
            {
                if (space is null)
                {
                    continue;
                }

                Space space_Cluster = adjacencyCluster.GetObject<Space>(space.Guid);
                if (space_Cluster is not null)
                {
                    result.Add(space_Cluster);
                }
            }

            return result;
        }

        private void CalculateDwelling(AdjacencyCluster adjacencyCluster, string dwellingName, List<Space> spaces)
        {
            double setbackFlowRateFactor = SetbackFlowRateFactor;

            PartFDwellingResult dwellingResult = new(dwellingName)
            {
                SpaceNames = [.. spaces.ConvertAll(x => x.Name)],
                SetbackFlowRateFactor = setbackFlowRateFactor,
            };

            DwellingResults.Add(dwellingResult);

            if (spaces.Count == 0)
            {
                Publish(dwellingResult);
                return;
            }

            List<Tuple<PartFCategory, Space>> tuples = [];
            foreach (Space space in spaces)
            {
                PartFCategory partFCategory = partFData.GetPartFCategory(space, out SpaceSemantics spaceSemantics);

                //Record the shared classification on the space whether or not Part F has a category for
                //it, so the mapping is visible in SAM_UI and reusable by Part O and TM59.
                if (spaceSemantics is not null)
                {
                    space.SetValue(SpaceParameter.SpaceSemantics, spaceSemantics);
                    adjacencyCluster.AddObject(space);
                }

                //A space positively identified as belonging to no dwelling is excluded from this
                //dwelling entirely rather than sized as part of it. Being unclassified is NOT such an
                //identification - an unrecognised name is a reporting problem, not evidence the space is
                //communal - so this only catches communal circulation and explicitly non-dwelling uses.
                if (spaceSemantics is not null && spaceSemantics.SpaceUse != SpaceUse.Undefined && !spaceSemantics.IsDwellingSpace)
                {
                    dwellingResult.NonDwellingSpaceNames.Add(space.Name);
                    continue;
                }

                if (partFCategory is null)
                {
                    dwellingResult.UnclassifiedSpaceNames.Add(space.Name);
                    continue;
                }

                space.SetValue(SpaceParameter.PartFSpaceData, new PartFSpaceData(partFCategory));
                adjacencyCluster.AddObject(space);

                tuples.Add(new Tuple<PartFCategory, Space>(partFCategory, space));
            }

            if (dwellingResult.NonDwellingSpaceNames.Count != 0)
            {
                dwellingResult.Warnings.Add(string.Format("{0} space(s) are not part of any dwelling and were excluded from this dwelling entirely - they do not count towards the bedroom count, the internal floor area or the flow rates: {1}. Move shared circulation and landlord areas out of the dwelling zone and size them to Approved Document F, Volume 2: Buildings other than dwellings.", dwellingResult.NonDwellingSpaceNames.Count, string.Join(", ", dwellingResult.NonDwellingSpaceNames)));
            }

            if (dwellingResult.UnclassifiedSpaceNames.Count != 0)
            {
                dwellingResult.Warnings.Add(string.Format("{0} space(s) could not be matched to a Part F room category and were left out of the dwelling entirely - they do not count towards the bedroom count, the internal floor area or the flow rates: {1}. Rename them, set a Space Use Override on them, or add a matching synonym to the space use text map.", dwellingResult.UnclassifiedSpaceNames.Count, string.Join(", ", dwellingResult.UnclassifiedSpaceNames)));
            }

            //Habitable room count. Appendix A (page 36): a habitable room is used for dwelling purposes
            //but is not SOLELY a kitchen, utility room, bathroom, cellar or sanitary accommodation. So a
            //studio and an open plan living kitchen are habitable, a room that is solely a kitchen is not,
            //and a bathroom, ensuite, utility room, sanitary accommodation, circulation space, store,
            //plant room or void never increases this count.
            List<Tuple<PartFCategory, Space>> tuples_Habitable = tuples.FindAll(x => x.Item1.PartFType == Enums.PartFType.Habitable);

            dwellingResult.HabitableRoomCount = tuples_Habitable.Count;
            dwellingResult.HabitableRoomNames = [.. tuples_Habitable.ConvertAll(x => x.Item2.Name)];

            //Table 1.3 (page 10): minimum whole dwelling rate by number of bedrooms. A studio counts as
            //one bedroom because it combines sleeping with living and cooking in a single room.
            dwellingResult.BedroomCount = tuples.FindAll(x => x.Item1.IsBedroom).Count;

            //Table 1.3 note 1 (page 10): "If the dwelling only has one habitable room, a minimum
            //ventilation rate of 13l/s should be used." This keys off the habitable ROOM count, not the
            //bedroom count, and where it applies it REPLACES the bedroom based rate - so a studio with a
            //separate bathroom is sized at 13 l/s, not the one bedroom figure of 19 l/s. Adding any second
            //habitable room (a separate living room, a study) takes the dwelling back onto Table 1.3.
            dwellingResult.OneHabitableRoomRuleApplied = dwellingResult.HabitableRoomCount == 1;

            double bedroomBaseRate = partFData.GetBedroomOrHabitableRate_Lps(dwellingResult.HabitableRoomCount, dwellingResult.BedroomCount);
            if (double.IsNaN(bedroomBaseRate))
            {
                dwellingResult.Warnings.Add("No whole dwelling ventilation rate table (Approved Document F Table 1.3) is loaded. The bedroom based minimum rate has been ignored and only the floor area based rate applied.");
                bedroomBaseRate = 0;
            }

            dwellingResult.BedroomBasedRate_Lps = bedroomBaseRate;
            dwellingResult.BedroomOrHabitableRate_Lps = bedroomBaseRate;

            if (dwellingResult.OneHabitableRoomRuleApplied)
            {
                dwellingResult.Remarks.Add(string.Format("The dwelling has exactly one habitable room ({0}), so Approved Document F Table 1.3 note 1 applies and the whole dwelling rate set by the rooms is {1:0.##} l/s rather than the one bedroom figure of Table 1.3. The floor area rate and the total of the wet room minimums still apply, so the continuous design rate may be higher.", string.Join(", ", dwellingResult.HabitableRoomNames), bedroomBaseRate));
            }

            if (dwellingResult.HabitableRoomCount == 0)
            {
                dwellingResult.Warnings.Add("No space was classified as a habitable room. Approved Document F paragraph 1.67 requires mechanical supply to each habitable room, so this dwelling has no supply provision, and Table 1.3 note 1 could not be assessed.");
            }

            if (dwellingResult.BedroomCount == 0 && dwellingResult.HabitableRoomCount != 1)
            {
                dwellingResult.Warnings.Add("No space was classified as a bedroom, so the dwelling has been sized at the one bedroom rate of Approved Document F Table 1.3, which gives no value below one bedroom. Check the room names if the dwelling does contain a bedroom.");
            }

            //Paragraph 1.17a (page 8) and Table 1.2 (page 10): kitchens need extract ventilation.
            List<Tuple<PartFCategory, Space>> tuples_Cooking = tuples.FindAll(x => x.Item1.IsCookingSpace);
            if (tuples_Cooking.Count == 0)
            {
                dwellingResult.Warnings.Add("The dwelling contains no kitchen, open plan living kitchen or studio. Approved Document F paragraph 1.17a requires extract ventilation from the kitchen, so check that the cooking space has been named in a way the space use text map recognises.");
            }

            //Paragraph 1.70 / Table 1.2 (pages 10 and 17): each wet room served by an extract
            //terminal must achieve at least its tabulated minimum high rate.
            List<Tuple<PartFCategory, Space>> tuples_Extract = tuples.FindAll(x =>
                x.Item1.PartFVentilationType == Enums.PartFVentilationType.extract &&
                x.Item1.IsTerminalSpace);

            //Local kitchen extract check.
            //
            //The engineering limitation is the absence of an explicitly modelled LOCAL kitchen or cooker
            //extract, NOT the absence of general dwelling extract. A cooking space is treated as having
            //explicit local kitchen extract only where that space itself takes an extract terminal - i.e.
            //a room classified as solely a kitchen, which carries the Table 1.2 kitchen rate. A studio or
            //an open plan living kitchen is a habitable supply space by SAM design convention, so it never
            //does, and SAM has no separate cooker hood or local extract concept to represent it.
            //
            //Extract from a bathroom, ensuite, utility room or sanitary accommodation may balance the
            //dwelling airflow, but it is NOT evidence of local kitchen extract, so it must not suppress
            //this warning. The warning therefore does not depend on tuples_Extract at all, and must not
            //claim the dwelling has no extract terminal - the separate paragraph 1.17 warning below covers
            //that case.
            List<string> names_CookingWithoutLocalExtract = [.. tuples_Cooking
                .FindAll(x => x.Item1.PartFVentilationType != Enums.PartFVentilationType.extract || !x.Item1.IsTerminalSpace)
                .ConvertAll(x => x.Item2.Name)];

            if (names_CookingWithoutLocalExtract.Count != 0)
            {
                dwellingResult.Warnings.Add(string.Format("ENGINEERING CHECK REQUIRED: This dwelling contains a cooking space, but no explicit local kitchen or cooker extract is represented. Extract from a bathroom, ensuite or other wet room may balance the dwelling airflow but does not demonstrate compliance with the local kitchen-extract requirement. Affected space(s): {0}. Approved Document F paragraph 1.17a and Table 1.2 require extract ventilation of at least 13 l/s from the room containing the cooking function. SAM assigns one terminal role per space and has no local kitchen or cooker extract concept, so this must be modelled and verified separately.", string.Join(", ", names_CookingWithoutLocalExtract)));
            }

            //Paragraph 1.24a (page 10): 0.3 l/s per m2 of internal floor area, all floors.
            List<Tuple<PartFCategory, Space>> tuples_Area = tuples.FindAll(x => x.Item1.IncludeInFloorAreaCheck);

            dwellingResult.FloorAreaExcludedSpaceNames = [.. tuples.FindAll(x => !x.Item1.IncludeInFloorAreaCheck).ConvertAll(x => x.Item2.Name)];
            if (dwellingResult.FloorAreaExcludedSpaceNames.Count != 0)
            {
                dwellingResult.Remarks.Add(string.Format("{0} space(s) were excluded from the Approved Document F paragraph 1.24a internal floor area because their category is not counted, normally voids and open-to-below areas: {1}.", dwellingResult.FloorAreaExcludedSpaceNames.Count, string.Join(", ", dwellingResult.FloorAreaExcludedSpaceNames)));
            }

            dwellingResult.InternalFloorArea_M2 = tuples_Area.ConvertAll(x => x.Item2.GetValue<double>(SpaceParameter.Area)).Sum();
            if (dwellingResult.InternalFloorArea_M2 <= 0)
            {
                dwellingResult.Warnings.Add("The internal floor area of the dwelling is zero, so the floor area based minimum rate of Approved Document F paragraph 1.24a could not be applied. Check that the spaces carry an Area parameter in m2.");
            }

            dwellingResult.AreaBasedRate_Lps = partFData.AreaRate_LpsPerM2 * dwellingResult.InternalFloorArea_M2;

            //Paragraph 1.24 (page 10): the rate must meet both conditions, so the greater applies.
            dwellingResult.WholeDwellingRate_Lps = System.Math.Max(bedroomBaseRate, dwellingResult.AreaBasedRate_Lps);

            dwellingResult.WetRoomMinimumTotal_Lps = tuples_Extract.ConvertAll(x => x.Item1.MinFlowRate_Lps ?? 0).Sum();

            //Paragraph 1.69 (page 16): the continuous design rate is the whole dwelling rate, but it can
            //never be lower than the sum of the wet room minimums. Every applicable minimum is established
            //here, at the continuous design condition, before the setback factor is applied:
            //
            //  ContinuousDesignRate = max(BedroomOrHabitableRate, AreaRate, TotalExtractMinimum)
            dwellingResult.ContinuousDesignSystemRate_Lps = System.Math.Max(dwellingResult.WholeDwellingRate_Lps, dwellingResult.WetRoomMinimumTotal_Lps);

            dwellingResult.SetbackSystemRate_Lps = dwellingResult.ContinuousDesignSystemRate_Lps * setbackFlowRateFactor;

            FinalSystemRate_Lps = dwellingResult.ContinuousDesignSystemRate_Lps;
            SetbackSystemRate_Lps = dwellingResult.SetbackSystemRate_Lps;

            //Paragraph 1.67 (page 16): each habitable room has mechanical supply, distributed in
            //proportion to the volume of each habitable room.
            List<Tuple<PartFCategory, Space>> tuples_Supply = tuples.FindAll(x =>
                x.Item1.PartFVentilationType == Enums.PartFVentilationType.supply &&
                x.Item1.ScaleSupplyWithVolume &&
                x.Item1.IsTerminalSpace);

            if (tuples_Supply.Count == 0)
            {
                dwellingResult.Warnings.Add("The dwelling has no space that takes a supply terminal, so no supply air was distributed and total supply does not balance total extract (Approved Document F paragraphs 1.67 and 1.69).");
            }

            double totalSupplyWeight = tuples_Supply.ConvertAll(x => x.Item2.GetValue<double>(SpaceParameter.Volume)).Sum();
            if (tuples_Supply.Count != 0 && totalSupplyWeight <= 0)
            {
                dwellingResult.Warnings.Add("The supply spaces have no volume, so the supply air could not be distributed in proportion to room volume (Approved Document F paragraph 1.67). Check that the spaces carry a Volume parameter in m3.");
            }

            foreach (Tuple<PartFCategory, Space> tuple in tuples_Supply)
            {
                if (totalSupplyWeight <= 0)
                {
                    continue;
                }

                double flowRate = dwellingResult.ContinuousDesignSystemRate_Lps * (tuple.Item2.GetValue<double>(SpaceParameter.Volume) / totalSupplyWeight);

                SetFlowRates(adjacencyCluster, tuple, flowRate, setbackFlowRateFactor);

                dwellingResult.TotalSupply_Lps += flowRate;
                dwellingResult.TotalSetbackSupply_Lps += flowRate * setbackFlowRateFactor;
            }

            if (tuples_Extract.Count == 0)
            {
                dwellingResult.Warnings.Add("The dwelling has no wet room that takes an extract terminal. Approved Document F paragraph 1.17 requires extract ventilation from kitchens, utility rooms, bathrooms and sanitary accommodation.");
            }

            double extraExtractNeeded = System.Math.Max(0, dwellingResult.ContinuousDesignSystemRate_Lps - dwellingResult.WetRoomMinimumTotal_Lps);

            List<Tuple<PartFCategory, Space>> tuples_Scaled = tuples_Extract.FindAll(x => x.Item1.ScaleExtractAboveMinimum);

            double totalExtractWeight = tuples_Scaled.ConvertAll(x => x.Item2.GetValue<double>(SpaceParameter.Volume)).Sum();
            if (tuples_Extract.Count != 0 && extraExtractNeeded > 0 && totalExtractWeight <= 0)
            {
                dwellingResult.Warnings.Add("The balance of extract above the wet room minimums could not be distributed because the extract spaces have no volume. Each wet room holds its Approved Document F Table 1.2 minimum only, so total extract is below the whole dwelling ventilation rate.");
            }

            foreach (Tuple<PartFCategory, Space> tuple in tuples_Extract)
            {
                //Every wet room extract terminal receives at least its Table 1.2 minimum, whether or
                //not it takes a share of the balance.
                double flowRate = tuple.Item1.MinFlowRate_Lps ?? 0;

                if (extraExtractNeeded > 0 && totalExtractWeight > 0 && tuple.Item1.ScaleExtractAboveMinimum)
                {
                    flowRate += extraExtractNeeded * (tuple.Item2.GetValue<double>(SpaceParameter.Volume) / totalExtractWeight);
                }

                SetFlowRates(adjacencyCluster, tuple, flowRate, setbackFlowRateFactor);

                dwellingResult.TotalExtract_Lps += flowRate;
                dwellingResult.TotalSetbackExtract_Lps += flowRate * setbackFlowRateFactor;
            }

            //Transfer, storage, plant and void spaces have no terminal, so they carry no flow rate.
            foreach (Tuple<PartFCategory, Space> tuple in tuples.FindAll(x => !x.Item1.IsTerminalSpace))
            {
                SetFlowRates(adjacencyCluster, tuple, 0, setbackFlowRateFactor);
            }

            foreach (Tuple<PartFCategory, Space> tuple in tuples)
            {
                if (tuple.Item2.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.ContinuousDesignFlowRate_Lps is null)
                {
                    dwellingResult.UnassignedSpaceNames.Add(tuple.Item2.Name);
                }
            }

            if (dwellingResult.UnassignedSpaceNames.Count != 0)
            {
                dwellingResult.Remarks.Add(string.Format("{0} classified space(s) received no ventilation flow rate because their category is neither a supply nor an extract terminal: {1}.", dwellingResult.UnassignedSpaceNames.Count, string.Join(", ", dwellingResult.UnassignedSpaceNames)));
            }

            Publish(dwellingResult);
        }

        private void Publish(PartFDwellingResult dwellingResult)
        {
            string prefix = string.IsNullOrWhiteSpace(dwellingResult.Name) ? string.Empty : dwellingResult.Name + ": ";

            Warnings.AddRange(dwellingResult.Warnings.ConvertAll(x => prefix + x));
            Remarks.AddRange(dwellingResult.Remarks.ConvertAll(x => prefix + x));
            UnclassifiedSpaceNames.AddRange(dwellingResult.UnclassifiedSpaceNames);
            UnassignedSpaceNames.AddRange(dwellingResult.UnassignedSpaceNames);
        }

        /// <summary>
        /// Writes the continuous design flow rate and the setback flow rate derived from it. The setback
        /// rate is only ever a scaling of an already-established continuous design rate, so no regulatory
        /// minimum can be bypassed by the reduced-operation factor.
        /// </summary>
        private static void SetFlowRates(AdjacencyCluster adjacencyCluster, Tuple<PartFCategory, Space> tuple, double continuousDesignFlowRate, double setbackFlowRateFactor)
        {
            Space space = tuple.Item2;

            PartFSpaceData partFSpaceData = new(tuple.Item1)
            {
                ContinuousDesignFlowRate_Lps = continuousDesignFlowRate,
                SetbackFlowRate_Lps = continuousDesignFlowRate * setbackFlowRateFactor,
            };

            space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);
            adjacencyCluster.AddObject(space);
        }
    }
}
