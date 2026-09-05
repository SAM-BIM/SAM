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

        private List<string> hourlySeriesRefusals = [];

        private List<System.Guid> spaceGuids_HourlySeriesRefused = [];

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
        /// How many hourly values each series must carry for a space to be assessed at all. <b>0 - the
        /// default - enforces nothing</b>, and every equal-length pair of series is assessed however short
        /// it is.
        ///
        /// <para><b>Why this is stated by the caller and not decided here</b></para>
        /// <para>
        /// This class is the TM52 and TM59 calculation over whatever series it is given, and the length a
        /// series has to be is a property of the RUN rather than of the calculation. A Grasshopper component
        /// handed four hours of a test model, or a TM52 assessment of a summer window, is doing something
        /// legitimate; refusing it because a year is 8760 hours long would break a calculation that is
        /// correct for its input.
        /// </para>
        /// <para>
        /// Approved Document O is the case where a full year IS the contract - its dynamic method assesses
        /// annual and summer criteria, and a verdict from part of a year is not the verdict the document
        /// asks for. <c>PartOTM59Assessment</c> therefore sets this from the WEATHER YEAR the results were
        /// produced against, which is the same authority the comfort band is derived from, rather than from
        /// a literal 8760: a year's hour count is whatever its weather data actually holds.
        /// </para>
        /// <para>
        /// <b>Shorter is refused; longer is not.</b> A series with fewer hours than the run needs is missing
        /// data. A series with more - a leap-year simulation's 8784 against a 365-day weather year - is not,
        /// and the surplus hours are already excluded by <see cref="Collect"/>, which refuses any hour the
        /// comfort band does not cover rather than assessing it against a 0 degC limit.
        /// </para>
        /// </summary>
        public int HourCount_Expected { get; set; } = 0;

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

        /// <summary>
        /// Spaces left out of the last calculation because their two hourly series could not be assessed
        /// together, one sentence each - a series absent, a series empty, or the two series of DIFFERENT
        /// LENGTHS.
        ///
        /// <para><b>What this replaces</b></para>
        /// <para>
        /// <see cref="Collect"/> walks both arrays with one counter and used to bound the walk by the
        /// shorter of the two. That stopped a truncated result throwing out of the whole run - which is
        /// worth keeping, and is kept - but it also meant a space whose resultant-temperature series ended
        /// early was assessed over the hours the two happened to share and its verdict reported as though it
        /// were a verdict about the space. Which of two unequal series is the truncated one is not knowable
        /// here, so neither can be trusted, and an overheating verdict measured over part of a room's hours
        /// is not a verdict about that room.
        /// </para>
        /// <para>
        /// So the space is refused instead: no result, and a reason. Nothing throws, and every other space
        /// in the building is still assessed - the same trade the shared-range walk was making, with the
        /// unassessable space now visible rather than silently reported.
        /// </para>
        ///
        /// <para><b>Length equality only, at this level</b></para>
        /// <para>
        /// Two equal-length series are assessed however long they are. This class is the TM52 and TM59
        /// calculation over whatever series it is given, and a caller assessing a deliberately short run -
        /// the Grasshopper components, a summer-only TM52 window - is doing something legitimate that
        /// nothing here should refuse. Whether a series is long enough to be a full year is a question about
        /// the RUN, and it is asked where a full year is actually the contract: Approved Document O requires
        /// one, and <c>PartOTM59Assessment</c> refuses a short series on the strength of the weather year
        /// the results were produced against.
        /// </para>
        /// <para>A copy, so a reporting layer cannot edit the record of what went unassessed.</para>
        /// </summary>
        public List<string> HourlySeriesRefusals => [.. hourlySeriesRefusals];

        /// <summary>
        /// The <see cref="Space.Guid"/> of every space named in <see cref="HourlySeriesRefusals"/>, in the
        /// same order.
        /// <para>
        /// Identities as well as prose, because a caller that has to keep a refused room OUT of a pass needs
        /// to name it rather than parse a sentence. <c>PartOTM59Assessment</c> maps these back to their
        /// design spaces and counts them as unassessed, which is what stops an Approved Document O run
        /// reporting a pass over the rooms whose data happened to survive.
        /// </para>
        /// </summary>
        public List<System.Guid> SpaceGuids_HourlySeriesRefused => [.. spaceGuids_HourlySeriesRefused];

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
            //The hourly-series refusals ARE cleared here, unlike the ventilation ones: TM52 reads the same
            //two series and so can produce these, whereas it selects no criterion and can produce none of
            //those. Each call therefore reports its own unusable series and never an earlier call's.
            ClearHourlySeriesRefusals();

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

            ClearHourlySeriesRefusals();

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

                //The applications come from the space the MODEL now holds - space_Temp - not from the space
                //instance the caller listed. Explicitly scoped assessments receive the map's retained
                //simulation-space instances, which predate RestoreDesignInternalConditions, so the caller's
                //`space` can still carry no internal condition while space_Temp carries the restored design
                //one. Classifying the stale instance would pick the wrong TM59 result type or corridor
                //fallback.
                List<TM59SpaceApplication> tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space_Temp?.InternalCondition);
                if (tM59SpaceApplications == null || tM59SpaceApplications.Count == 0)
                {
                    tM59SpaceApplications = tM59Manager.TM59SpaceApplications(space_Temp);
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
        /// Both hourly series, in a state they can be assessed together in - or false, with the reason on
        /// <see cref="HourlySeriesRefusals"/> and the space on
        /// <see cref="SpaceGuids_HourlySeriesRefused"/>.
        /// <para>
        /// Four states are refused: either series absent, either series empty, and the two series of
        /// different lengths. See <see cref="HourlySeriesRefusals"/> for why unequal lengths are a refusal
        /// rather than a shared-range assessment, and why length equality is all that is judged here.
        /// </para>
        /// <para>
        /// A refusal is REPORTED, never thrown. One space with unusable data must not cost every other
        /// space in the building its assessment - that was the reason the walk was bounded by the shorter
        /// series in the first place, and it still holds.
        /// </para>
        /// </summary>
        private bool TryGetHourlyValues(Space space, out JsonArray jsonArray_OccupancySensibleGain, out JsonArray jsonArray_ResultantTemperature)
        {
            jsonArray_ResultantTemperature = null;

            bool hasOccupancy = Core.Query.TryGetValue(space, OccupancySensibleGainSeriesKey, out jsonArray_OccupancySensibleGain)
                && jsonArray_OccupancySensibleGain != null;

            bool hasResultant = Core.Query.TryGetValue(space, ResultantTemperatureSeriesKey, out jsonArray_ResultantTemperature)
                && jsonArray_ResultantTemperature != null;

            if (!hasOccupancy || !hasResultant)
            {
                //Which one is missing, because "no result for this room" with no reason is what this
                //replaces and the two have different causes: the occupancy key is the one the analytical and
                //TAS vocabularies disagree about (see OccupancySensibleGainSeriesKey), so a whole model
                //missing only that one is a key that was not supplied rather than a damaged results file.
                RefuseHourlySeries(space, string.Format(
                    "Space '{0}' carries no {1} hourly series, so it could not be assessed and was left out.",
                    space?.Name ?? "?",
                    !hasOccupancy && !hasResultant
                        ? string.Format("'{0}' or '{1}'", OccupancySensibleGainSeriesKey, ResultantTemperatureSeriesKey)
                        : string.Format("'{0}'", hasOccupancy ? ResultantTemperatureSeriesKey : OccupancySensibleGainSeriesKey)));

                return false;
            }

            if (jsonArray_OccupancySensibleGain.Count == 0 || jsonArray_ResultantTemperature.Count == 0)
            {
                RefuseHourlySeries(space, string.Format(
                    "Space '{0}' carries an EMPTY hourly series ('{1}' has {2} values, '{3}' has {4}), so there is nothing to assess and it was left out. An empty series is a results file that was not written for this room, not a room with no exceedances.",
                    space?.Name ?? "?",
                    OccupancySensibleGainSeriesKey,
                    jsonArray_OccupancySensibleGain.Count,
                    ResultantTemperatureSeriesKey,
                    jsonArray_ResultantTemperature.Count));

                return false;
            }

            if (jsonArray_OccupancySensibleGain.Count != jsonArray_ResultantTemperature.Count)
            {
                RefuseHourlySeries(space, string.Format(
                    "Space '{0}' carries hourly series of different lengths ('{1}' has {2} values, '{3}' has {4}), so one of them is truncated and which is not knowable. It was refused rather than assessed over the {5} hours the two share, because an overheating verdict over part of a room's hours is not a verdict about that room.",
                    space?.Name ?? "?",
                    OccupancySensibleGainSeriesKey,
                    jsonArray_OccupancySensibleGain.Count,
                    ResultantTemperatureSeriesKey,
                    jsonArray_ResultantTemperature.Count,
                    System.Math.Min(jsonArray_OccupancySensibleGain.Count, jsonArray_ResultantTemperature.Count)));

                return false;
            }

            //Length agreed; is it enough of a year? Only where the caller said what "enough" is - see
            //HourCount_Expected. Both counts are equal here, so either may be compared.
            if (HourCount_Expected > 0 && jsonArray_ResultantTemperature.Count < HourCount_Expected)
            {
                RefuseHourlySeries(space, string.Format(
                    "Space '{0}' carries only {1} of the {2} hourly values this assessment requires, so it was refused rather than assessed over a partial year. Both of its series are this length, so nothing here is a mismatch - the results file itself is short, and neither a pass nor a failure may be produced from part of a year.",
                    space?.Name ?? "?",
                    jsonArray_ResultantTemperature.Count,
                    HourCount_Expected));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Empties the hourly-series record, so one calculation never reports another's refusals. Called at
        /// the top of both calculations, before either can fail.
        /// </summary>
        private void ClearHourlySeriesRefusals()
        {
            hourlySeriesRefusals = [];
            spaceGuids_HourlySeriesRefused = [];
        }

        /// <summary>Records one space's hourly-series refusal - the sentence and the identity together.</summary>
        private void RefuseHourlySeries(Space space, string refusal)
        {
            hourlySeriesRefusals.Add(refusal);

            if (space != null)
            {
                spaceGuids_HourlySeriesRefused.Add(space.Guid);
            }
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

            //The loop is bounded by the SHORTER of the two series, and both callers now REFUSE a space whose
            //series are of different lengths before reaching here - see TryGetHourlyValues and
            //HourlySeriesRefusals. So this bound is no longer what decides such a space's verdict; it is
            //kept as the defence it originally was, because the loop indexes both arrays by one counter and
            //a future caller reaching this private method without that check must still not throw out of
            //the whole TM52/TM59 run and lose every assessment in it.
            //
            //It is NOT a substitute for the refusal, and must not be relied on as one: silently assessing a
            //room over the hours two unequal series happen to share is the defect the refusal removes.
            int count = System.Math.Min(jsonArray_OccupancySensibleGain.Count, jsonArray_ResultantTemperature.Count);

            for (int i = 0; i < count; i++)
            {
                if (i < startHourOfYear || i > endHourOfYear)
                {
                    continue;
                }

                if (!Core.Query.TryConvert(jsonArray_ResultantTemperature[i], out double resultantTemperature) || double.IsNaN(resultantTemperature))
                {
                    continue;
                }

                //Both comfort bounds must EXIST for the hour. IndexedDoubles returns 0 for a missing index,
                //and the comfort series is bounded by the weather year (365 days = 8760 hours) - so a
                //leap-year simulation's extra 24 hours would otherwise be assessed against a 0 degC comfort
                //limit and manufacture exceedances. An hour with no comfort bounds is simply not assessed.
                if (!maxIndoorComfortTemperatures.TryGetValue(i, out double maxIndoorComfortTemperature) || !minIndoorComfortTemperatures.TryGetValue(i, out double minIndoorComfortTemperature))
                {
                    continue;
                }

                maxAcceptableTemperatures.Add(i, maxIndoorComfortTemperature);
                minAcceptableTemperatures.Add(i, minIndoorComfortTemperature);
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

            //Null wherever the ambient SAM install has no system type library - a clean CI runner, a headless
            //host, or any process whose ActiveSetting was never seeded from %APPDATA%\SAM. Both loops below
            //dereferenced it, so this threw a NullReferenceException out of the middle of an assessment rather
            //than falling through to the documented "NV" default.
            SystemTypeLibrary systemTypeLibrary = Query.DefaultSystemTypeLibrary();

            List<Zone> zones = systemTypeLibrary == null ? null : adjacencyCluster?.GetRelatedObjects<Zone>(space);
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
