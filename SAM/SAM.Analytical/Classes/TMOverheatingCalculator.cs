// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Weather;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// The CIBSE TM52 and TM59 overheating assessment, over simulated space data already stored on the
    /// model. <b>Analytical-domain code with no engine dependency of any kind.</b>
    /// <para>
    /// It was extracted from <c>SAM.Analytical.Tas.OverheatingCalculator</c>, which never called TAS: it
    /// read two named hourly series off each <c>Space</c> and produced <c>TM5x</c> results. Sitting
    /// in the TAS assembly meant its tests needed a licensed TAS install to run, for no reason other than
    /// where the file happened to live. That class remains, as a thin wrapper over this one, so every
    /// existing Grasshopper and user-interface caller is unaffected.
    /// </para>
    /// <para>
    /// <b>Named <c>TMOverheatingCalculator</c>, not <c>OverheatingCalculator</c>.</b> A type of the same
    /// name in a parent namespace shadows the child's in every file importing both - the mistake that broke
    /// seven call sites during the Part F work - and <c>SAM.Analytical.Tas</c> is nested inside
    /// <c>SAM.Analytical</c>. The name also matches the <c>TMResult</c> / <c>TMExtendedResult</c> hierarchy
    /// it produces.
    /// </para>
    /// <para>
    /// Approved Document O's dynamic method follows the TM59 methodology, so this is the calculation a Part
    /// O assessment rests on. It is TM59:2017 / current Approved Document O; nothing here anticipates a
    /// later edition.
    /// </para>
    /// </summary>
    public class TMOverheatingCalculator
    {
        private TextMap textMap = Query.DefaultInternalConditionTextMap_TM59();

        private List<string> ventilationStrategyRefusals = [];

        public TMOverheatingCalculator(AnalyticalModel analyticalModel)
        {
            AnalyticalModel = analyticalModel;
        }

        public TM52BuildingCategory TM52BuildingCategory { get; set; } = TM52BuildingCategory.CategoryII;

        public AnalyticalModel AnalyticalModel { get; set; } = null;

        /// <summary>
        /// The key under which each space's hourly operative-temperature series is stored in its
        /// <c>ParameterSet</c> - a key returning a <c>JsonArray</c> of simulated values.
        /// <para>
        /// Configurable and not a constant because the writing side chooses it: the analytical vocabulary
        /// and the TAS conversion do not agree on every key, and the assessment must read what was actually
        /// stored rather than what this assembly would have called it. Defaults to the analytical
        /// vocabulary; the TAS wrapper supplies TAS's. Two keys, both defaulted - deliberately not a
        /// general series-lookup framework.
        /// </para>
        /// </summary>
        public string ResultantTemperatureSeriesKey { get; set; } = Core.Query.Name(SpaceSimulationResultParameter.ResultantTemperature);

        /// <summary>
        /// The key for each space's hourly occupancy-gain series, used only to decide which hours were
        /// occupied.
        /// <para>
        /// <b>This is the key the two vocabularies disagree about.</b> The analytical vocabulary says
        /// "Occupancy Sensible Gain"; the TAS conversion writes "Occupant Sensible Gain". Reading the wrong
        /// one is silent - the space simply produces no assessment - so the key is supplied rather than
        /// assumed. Reconciling the two is deliberately left as separate work, and no stored data is
        /// migrated here.
        /// </para>
        /// </summary>
        public string OccupancySensibleGainSeriesKey { get; set; } = Core.Query.Name(SpaceSimulationResultParameter.OccupancySensibleGain);

        /// <summary>
        /// Where a result says it came from. <b>Provenance only</b> - it names no object, owns no result and
        /// takes no part in any scenario, equipment or result identity.
        /// </summary>
        public string Source
        {
            get
            {
                string result = AnalyticalModel?.Name;

                return string.IsNullOrWhiteSpace(result) ? SourceFallback : result;
            }
        }

        /// <summary>
        /// What <see cref="Source"/> reports where the model is unnamed. Settable so the TAS wrapper can
        /// keep stamping its own assembly name, as it always has.
        /// </summary>
        public string SourceFallback { get; set; } = Core.Query.Name(typeof(TMOverheatingCalculator).Assembly);

        /// <summary>
        /// Which ventilation strategy governs which space, as stated by <c>OverheatingScenario</c>. Where this
        /// is supplied it is <b>authoritative</b> for the TM59 criterion.
        /// <para>
        /// Supplied, not derived, and it replaces the derivation rather than seeding it. The space's internal
        /// condition, the zone-name lookup and the natural-ventilation default in
        /// <see cref="SystemTypeName"/> are all bypassed, and a space the map refuses produces <b>no
        /// assessment</b> with its reason in <see cref="VentilationStrategyRefusals"/> - never an assessment
        /// against a fallback criterion. A gap is visible; a number measured against the wrong TM59 rule is
        /// not.
        /// </para>
        /// <para>
        /// <b>Left null the old derivation applies, unchanged.</b> Every existing caller - the Grasshopper
        /// components, the user interface, <c>OverheatingCalculator</c> - keeps the behaviour it had, because
        /// none of them has a scenario to state yet. Making the fallback unreachable is a later step, once
        /// there is a path that always supplies one.
        /// </para>
        /// <para>
        /// <b>Held by reference, and live.</b> A caller that keeps adding scenarios after assigning this will
        /// change what the next <see cref="Calculate_TM59"/> decides. That is deliberate - a map is built up
        /// scenario by scenario and is not an identity - but it is the opposite of the copy-in discipline
        /// <c>OverheatingScenario</c> follows, so it is stated rather than left to be discovered.
        /// </para>
        /// </summary>
        public VentilationStrategyMap VentilationStrategyMap { get; set; } = null;

        /// <summary>
        /// Why spaces were left out of the last <see cref="Calculate_TM59"/> because
        /// <see cref="VentilationStrategyMap"/> refused them, one sentence each. A copy; replaced by every
        /// <see cref="Calculate_TM59"/> call, and empty where no map was supplied or nothing was refused.
        /// <para>
        /// The refusals are reported rather than thrown so that one unstated dwelling does not lose the
        /// assessment of every other dwelling in the building - but they are reported, which is the whole
        /// difference between this and the silent default it replaces.
        /// </para>
        /// <para>
        /// <b><see cref="Calculate_TM52"/> deliberately does not clear these.</b> TM52 selects no criterion and
        /// so can neither produce nor answer a ventilation refusal; clearing them would let a TM52 run erase a
        /// TM59 run's record of which dwellings went unassessed. They belong to the last TM59 call and nothing
        /// else touches them.
        /// </para>
        /// <para>
        /// A space listed twice is refused twice, because it was asked about twice - the same way the no-map
        /// path assesses it twice. De-duplicating only the refusals would misreport the input.
        /// </para>
        /// </summary>
        public List<string> VentilationStrategyRefusals => [.. ventilationStrategyRefusals];

        public TextMap TextMap
        {
            get
            {
                return textMap;
            }

            set
            {
                textMap = value;
            }
        }

        public List<TM52ExtendedResult> Calculate_TM52(IEnumerable<Space> spaces, int startHourOfYear = 2880, int endHourOfYear = 6528)
        {
            if (AnalyticalModel == null || spaces == null)
            {
                return null;
            }

            IndexedDoubles maxIndoorComfortTemperatures = GetMaxIndoorComfortTemperatures();
            IndexedDoubles minIndoorComfortTemperatures = GetMinIndoorComfortTemperatures();

            List<TM52ExtendedResult> result = [];
            foreach (Space space in spaces)
            {
                Space space_Temp = AnalyticalModel.GetSpaces()?.Find(x => x.Guid == space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }

                if (!TryGetHourlyValues(space_Temp, out JsonArray jArray_OccupancySensibleGain, out JsonArray jArray_ResultantTemperature))
                {
                    continue;
                }

                Collect(
                    jArray_OccupancySensibleGain,
                    jArray_ResultantTemperature,
                    maxIndoorComfortTemperatures,
                    minIndoorComfortTemperatures,
                    startHourOfYear,
                    endHourOfYear,
                    out HashSet<int> occupiedHourIndices,
                    out IndexedDoubles minAcceptableTemperatures,
                    out IndexedDoubles maxAcceptableTemperatures,
                    out IndexedDoubles operativeTemperatures);

                result.Add(new TM52ExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures));
            }

            return result;
        }

        public List<TM59ExtendedResult> Calculate_TM59(IEnumerable<Space> spaces)
        {
            //Cleared even where the call is about to fail, so a stale refusal from an earlier call can never
            //be read as belonging to this one.
            ventilationStrategyRefusals = [];

            if (AnalyticalModel == null || spaces == null || textMap == null)
            {
                return null;
            }

            TM59Manager tM59Manager = new(textMap);

            IndexedDoubles maxIndoorComfortTemperatures = GetMaxIndoorComfortTemperatures();
            IndexedDoubles minIndoorComfortTemperatures = GetMinIndoorComfortTemperatures();

            AdjacencyCluster adjacencyCluster = AnalyticalModel.AdjacencyCluster;

            List<TM59ExtendedResult> result = [];
            foreach (Space space in spaces)
            {
                Space space_Temp = adjacencyCluster?.GetSpaces()?.Find(x => x.Guid == space.Guid);
                if (space_Temp == null)
                {
                    continue;
                }

                if (!TryGetVentilationStrategy(adjacencyCluster, space_Temp, out string systemTypeName))
                {
                    //Refused, and the reason is recorded. No criterion applies, so no result is produced -
                    //deliberately, in place of the "NV" default that made this the wrong assessment rather
                    //than an absent one.
                    continue;
                }

                List<TM59SpaceApplication> tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space?.InternalCondition);
                if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
                {
                    tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space);
                }

                if (!TryGetHourlyValues(space_Temp, out JsonArray jArray_OccupancySensibleGain, out JsonArray jArray_ResultantTemperature))
                {
                    continue;
                }

                Collect(
                    jArray_OccupancySensibleGain,
                    jArray_ResultantTemperature,
                    maxIndoorComfortTemperatures,
                    minIndoorComfortTemperatures,
                    int.MinValue,
                    int.MaxValue,
                    out HashSet<int> occupiedHourIndices,
                    out IndexedDoubles minAcceptableTemperatures,
                    out IndexedDoubles maxAcceptableTemperatures,
                    out IndexedDoubles operativeTemperatures);

                TM59ExtendedResult tM59ExtendedResult;

                if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0 || (!string.IsNullOrWhiteSpace(systemTypeName) && systemTypeName.Equals("UV")))
                {
                    tM59ExtendedResult = new TM59CorridorExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures);
                }
                else if (!string.IsNullOrWhiteSpace(systemTypeName) && systemTypeName.Equals("NV"))
                {
                    tM59ExtendedResult = tM59SpaceApplications.Contains(TM59SpaceApplication.Sleeping)
                        ? new TM59NaturalVentilationBedroomExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures)
                        : new TM59NaturalVentilationExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications?.ToArray());
                }
                else
                {
                    tM59ExtendedResult = new TM59MechanicalVentilationExtendedResult(space_Temp.Name, Source, space.Guid.ToString(), TM52BuildingCategory, occupiedHourIndices, minAcceptableTemperatures, maxAcceptableTemperatures, operativeTemperatures, tM59SpaceApplications?.ToArray());
                }

                if (tM59ExtendedResult == null)
                {
                    continue;
                }

                result.Add(tM59ExtendedResult);
            }

            return result;
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            WeatherYear weatherYear = WeatherYear();

            List<double> values = weatherYear == null ? null : Query.MaxIndoorComfortTemperatures(weatherYear, TM52BuildingCategory);

            return values == null || values.Count == 0 ? null : new IndexedDoubles(values).Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            WeatherYear weatherYear = WeatherYear();

            List<double> values = weatherYear == null ? null : Query.MaxIndoorComfortTemperatures(weatherYear, TM52BuildingCategory, startDayIndex, endDayIndex);

            return values == null || values.Count == 0 ? null : new IndexedDoubles(values, startDayIndex).Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            WeatherYear weatherYear = WeatherYear();

            List<double> values = weatherYear == null ? null : Query.MinIndoorComfortTemperatures(weatherYear, TM52BuildingCategory);

            return values == null || values.Count == 0 ? null : new IndexedDoubles(values).Repeat(period, Period.Daily);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            WeatherYear weatherYear = WeatherYear();

            List<double> values = weatherYear == null ? null : Query.MinIndoorComfortTemperatures(weatherYear, TM52BuildingCategory, startDayIndex, endDayIndex);

            return values == null || values.Count == 0 ? null : new IndexedDoubles(values, startDayIndex).Repeat(period, Period.Daily);
        }

        // ------------------------------------------------------------------
        // Shared
        // ------------------------------------------------------------------

        private WeatherYear WeatherYear()
        {
            return AnalyticalModel != null && AnalyticalModel.TryGetValue(AnalyticalModelParameter.WeatherData, out WeatherData weatherData) && weatherData != null
                ? weatherData.WeatherYears?.FirstOrDefault()
                : null;
        }

        /// <summary>
        /// Both hourly series, or false where either is missing.
        /// <para>
        /// <b>A missing series produces no assessment for that space, silently.</b> That is the behaviour
        /// the extracted code had and it is preserved deliberately rather than improved here - changing it
        /// during a refactor would hide whether the refactor was faithful. A space that yields nothing is
        /// pinned by regression; giving it a diagnostic is separate work.
        /// </para>
        /// </summary>
        private bool TryGetHourlyValues(Space space, out JsonArray jsonArray_OccupancySensibleGain, out JsonArray jsonArray_ResultantTemperature)
        {
            jsonArray_ResultantTemperature = null;

            return Core.Query.TryGetValue(space, OccupancySensibleGainSeriesKey, out jsonArray_OccupancySensibleGain)
                && jsonArray_OccupancySensibleGain != null
                && Core.Query.TryGetValue(space, ResultantTemperatureSeriesKey, out jsonArray_ResultantTemperature)
                && jsonArray_ResultantTemperature != null;
        }

        /// <summary>
        /// Walks the hourly series once, collecting the operative temperature, the comfort band and which
        /// hours were occupied. An hour counts as occupied when the occupancy gain is above zero.
        /// </summary>
        private static void Collect(
            JsonArray jsonArray_OccupancySensibleGain,
            JsonArray jsonArray_ResultantTemperature,
            IndexedDoubles maxIndoorComfortTemperatures,
            IndexedDoubles minIndoorComfortTemperatures,
            int startHourOfYear,
            int endHourOfYear,
            out HashSet<int> occupiedHourIndices,
            out IndexedDoubles minAcceptableTemperatures,
            out IndexedDoubles maxAcceptableTemperatures,
            out IndexedDoubles operativeTemperatures)
        {
            occupiedHourIndices = [];
            minAcceptableTemperatures = new IndexedDoubles();
            maxAcceptableTemperatures = new IndexedDoubles();
            operativeTemperatures = new IndexedDoubles();

            for (int i = 0; i < jsonArray_OccupancySensibleGain.Count; i++)
            {
                if (i < startHourOfYear || i > endHourOfYear)
                {
                    continue;
                }

                if (!Core.Query.TryConvert(jsonArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                {
                    continue;
                }

                maxAcceptableTemperatures.Add(i, maxIndoorComfortTemperatures[i]);
                minAcceptableTemperatures.Add(i, minIndoorComfortTemperatures[i]);
                operativeTemperatures.Add(i, resultantTemperature);

                if (!Core.Query.TryConvert(jsonArray_OccupancySensibleGain[i], out double occupancySensibleGain) || double.IsNaN(occupancySensibleGain))
                {
                    continue;
                }

                if (occupancySensibleGain <= 0)
                {
                    continue;
                }

                occupiedHourIndices.Add(i);
            }
        }

        /// <summary>
        /// The ventilation strategy the TM59 criterion selection uses for a space: the scenario's, where
        /// <see cref="VentilationStrategyMap"/> was supplied, and otherwise the old derivation.
        /// <para>
        /// <b>The two paths do not blend.</b> With a map, a refusal is a refusal - it does not fall through to
        /// <see cref="SystemTypeName"/>, because falling through would restore exactly the defect the map
        /// exists to remove and would do it invisibly, at the one input where nothing was said.
        /// </para>
        /// </summary>
        /// <returns>False where the space must not be assessed at all.</returns>
        private bool TryGetVentilationStrategy(AdjacencyCluster adjacencyCluster, Space space, out string ventilationStrategy)
        {
            if (VentilationStrategyMap == null)
            {
                //No scenario stated. The pre-existing derivation, unchanged, for every caller that has none.
                ventilationStrategy = SystemTypeName(adjacencyCluster, space);

                return true;
            }

            VentilationStrategySelection ventilationStrategySelection = VentilationStrategyMap.Selection(space);

            if (!ventilationStrategySelection.IsSelected)
            {
                ventilationStrategyRefusals.Add(ventilationStrategySelection.Reason);
                ventilationStrategy = null;

                return false;
            }

            ventilationStrategy = ventilationStrategySelection.VentilationStrategy;

            return true;
        }

        /// <summary>
        /// The ventilation system type governing a space as it was derived <b>before a scenario could state
        /// one</b>: the space's own internal condition first, then a system type whose name matches one of the
        /// space's zones, and "NV" where nothing says otherwise.
        /// <para>
        /// <b>Superseded, and kept only for callers with no scenario.</b> Every step of it is unsound as a way
        /// of choosing an Approved Document O criterion. The zone-name lookup makes a dwelling's assessment
        /// turn on whether somebody named a zone after a library entry, and the default silently assesses an
        /// MVRE dwelling as naturally ventilated. Supplying a <see cref="VentilationStrategyMap"/> bypasses
        /// this method entirely; it remains reachable because the Grasshopper and user-interface callers have
        /// no scenario to state yet, and removing it would change their behaviour without giving them a way to
        /// state the right one.
        /// </para>
        /// </summary>
        private static string SystemTypeName(AdjacencyCluster adjacencyCluster, Space space)
        {
            string result = space?.InternalCondition?.GetSystemTypeName<VentilationSystemType>()?.ToUpper();
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }

            SystemTypeLibrary systemTypeLibrary = Query.DefaultSystemTypeLibrary();

            List<Zone> zones = adjacencyCluster?.GetRelatedObjects<Zone>(space);
            if (zones != null)
            {
                foreach (Zone zone in zones)
                {
                    VentilationSystemType ventilationSystemType = systemTypeLibrary.GetSystemTypes<VentilationSystemType>(zone.Name, TextComparisonType.Equals, true)?.FirstOrDefault();
                    if (ventilationSystemType != null)
                    {
                        return ventilationSystemType.Name.ToUpper().Trim();
                    }
                }

                foreach (Zone zone in zones)
                {
                    VentilationSystemType ventilationSystemType = systemTypeLibrary.GetSystemTypes<VentilationSystemType>(zone.Name, TextComparisonType.StartsWith, false)?.FirstOrDefault();
                    if (ventilationSystemType != null)
                    {
                        return ventilationSystemType.Name.ToUpper().Trim();
                    }
                }
            }

            return "NV";
        }
    }
}
