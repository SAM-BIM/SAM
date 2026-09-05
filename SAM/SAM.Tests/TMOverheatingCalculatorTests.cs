// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The TM52/TM59 overheating calculation, extracted from <c>SAM.Analytical.Tas.OverheatingCalculator</c>
    /// into <c>SAM.Analytical</c>.
    /// <para>
    /// <b>These tests exist because the calculation never needed TAS.</b> It reads two named hourly series
    /// off each space and produces TM5x results; living in the TAS assembly meant its coverage required a
    /// licensed TAS install for no architectural reason. Everything here runs without TAS.
    /// </para>
    /// <para>
    /// The fixture stores its hourly data <b>the way the real converter does</b> -
    /// <c>ParameterSet.Add(name, JsonArray)</c> then <c>Space.Add(parameterSet)</c>, exactly as
    /// <c>Analytical.Tas.Convert.ToSAM(TSD.ZoneData, …)</c> writes it - rather than by any test-only route
    /// that merely happens to satisfy the lookup.
    /// </para>
    /// </summary>
    public class TMOverheatingCalculatorTests
    {
        private const string key_Analytical_ResultantTemperature = "Resultant Temperature";

        private const string key_Analytical_OccupancySensibleGain = "Occupancy Sensible Gain";

        //What the TAS conversion writes. Note the difference from the analytical name above: "Occupant",
        //not "Occupancy". Reconciling the two is deliberately separate work.
        private const string key_Tas_OccupantSensibleGain = "Occupant Sensible Gain";

        // ------------------------------------------------------------------
        // Series keys
        // ------------------------------------------------------------------

        /// <summary>
        /// Out of the box the calculator reads the analytical vocabulary - the names
        /// <c>SpaceSimulationResultParameter</c> declares - with no caller configuration at all.
        /// </summary>
        [Fact]
        public void AnalyticalDefaultSeriesKeys_AreRead()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Equal(key_Analytical_ResultantTemperature, tMOverheatingCalculator.ResultantTemperatureSeriesKey);
            Assert.Equal(key_Analytical_OccupancySensibleGain, tMOverheatingCalculator.OccupancySensibleGainSeriesKey);

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
        }

        /// <summary>
        /// The same calculator reads TAS-written data when the TAS names are supplied - which is all the
        /// compatibility wrapper does. Note this model stores "Occupant Sensible Gain", so the analytical
        /// default would find nothing; the key really is doing the work.
        /// </summary>
        [Fact]
        public void TasLegacySeriesKeys_AreReadWhenSupplied()
        {
            AnalyticalModel analyticalModel = Model(key_Tas_OccupantSensibleGain);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            //The analytical default cannot see TAS's spelling.
            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            tMOverheatingCalculator.OccupancySensibleGainSeriesKey = key_Tas_OccupantSensibleGain;

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
        }

        /// <summary>
        /// <b>Series keys are instance state.</b> A wrapper selecting TAS's spelling must not change what
        /// any other calculation reads, concurrently or afterwards. Two calculators over the same model
        /// disagree, and each is right about its own configuration.
        /// </summary>
        [Fact]
        public void SeriesKeys_AreInstanceStateNotShared()
        {
            AnalyticalModel analyticalModel = Model(key_Tas_OccupantSensibleGain);

            TMOverheatingCalculator tMOverheatingCalculator_Tas = Calculator(analyticalModel);
            tMOverheatingCalculator_Tas.OccupancySensibleGainSeriesKey = key_Tas_OccupantSensibleGain;

            TMOverheatingCalculator tMOverheatingCalculator_Analytical = Calculator(analyticalModel);

            Assert.Single(tMOverheatingCalculator_Tas.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Empty(tMOverheatingCalculator_Analytical.Calculate_TM59(analyticalModel.GetSpaces()));

            //And back again - the first instance is unaffected by the second having been created and run.
            Assert.Single(tMOverheatingCalculator_Tas.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Equal(key_Analytical_OccupancySensibleGain, tMOverheatingCalculator_Analytical.OccupancySensibleGainSeriesKey);
        }

        // ------------------------------------------------------------------
        // Behaviour preserved, not improved
        // ------------------------------------------------------------------

        /// <summary>
        /// A space missing a required series still produces no assessment - and now says which series it
        /// was missing.
        /// <para>
        /// The absence of a diagnostic used to be pinned here as pre-existing behaviour, explicitly "poor,
        /// and improving it is separate work". This is that work: a room that vanishes from an overheating
        /// assessment for want of data is a hole in the assessment, and a caller totalling the criterion
        /// lists could not tell it from a room that was never asked about.
        /// </para>
        /// <para>
        /// Naming WHICH series matters because the two have different causes. The occupancy key is the one
        /// the analytical and TAS vocabularies disagree about, so a whole model missing only that one is a
        /// key the caller did not supply rather than a damaged results file.
        /// </para>
        /// </summary>
        [Fact]
        public void MissingRequiredSeries_ProducesNoAssessmentAndSaysWhichSeriesWasMissing()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain, resultantTemperature: false);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            string refusal = Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals);
            Assert.Contains("Bedroom 2_3", refusal);
            Assert.Contains(key_Analytical_ResultantTemperature, refusal);
        }

        /// <summary>
        /// <b>Characterization only - this is NOT the intended API contract.</b>
        /// <para>
        /// With both series present but no weather data on the model, the comfort-temperature lookup throws
        /// a <c>NullReferenceException</c> - and it does so inside
        /// <c>SAM.Weather.Query.RunningMeanDryBulbTemperatures</c>, not in the overheating calculation
        /// itself, so the fragility is in the weather layer and any fix reaches wider than TM59.
        /// <para>
        /// Pre-existing behaviour carried through this extraction unchanged, recorded so the refactor can
        /// be shown to be faithful - not a decision that throwing is correct. What should happen instead is
        /// undecided, and changing it is a separate behavioural change with its own regression.
        /// </para>
        /// </para>
        /// </summary>
        [Fact]
        public void NoWeatherData_ThrowsToday_PreExistingBehaviourNotAContract()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain, weatherData: false);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Throws<NullReferenceException>(() => tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
        }

        // ------------------------------------------------------------------
        // Comfort bounds
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>An hour without comfort bounds is not assessed, never assessed against zero.</b>
        /// <para>
        /// The comfort series is bounded by the weather year (365 days = 8760 hours), while a leap-year
        /// simulation supplies 8784 hourly values. <c>IndexedDoubles</c> reads a missing index as 0, so the
        /// last day would be assessed against a 0 °C comfort limit and manufacture exceedances. Here those
        /// hours carry 40 °C - under the old behaviour they add 24 false &gt;28 °C exceedances; with the
        /// guard they are simply outside the assessed year.
        /// </para>
        /// </summary>
        [Fact]
        public void HoursBeyondTheComfortYear_AreNotAssessedAgainstZeroComfortLimits()
        {
            double[] resultant = new double[8784];
            double[] occupancy = new double[8784];
            for (int i = 0; i < 8760; i++)
            {
                resultant[i] = 21.0;
                occupancy[i] = 80.0;
            }

            for (int i = 8760; i < 8784; i++)
            {
                resultant[i] = 40.0;
                occupancy[i] = 80.0;
            }

            Space space = new("Bedroom 2_3");

            ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");
            parameterSet.Add(key_Analytical_ResultantTemperature, Values(resultant));
            parameterSet.Add(key_Analytical_OccupancySensibleGain, Values(occupancy));
            space.Add(parameterSet);

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(space);

            AnalyticalModel analyticalModel = new("Three Flats", null, null, null, adjacencyCluster);
            analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            TM59ExtendedResult tM59ExtendedResult = Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            //The assessed series covers exactly the comfort year - the leap day's 24 hours were not
            //adopted with zero limits.
            Assert.Equal(8760, tM59ExtendedResult.GetAnnualHours());

            //And the 40 °C hours beyond it manufactured no exceedances.
            TM59CorridorExtendedResult tM59CorridorExtendedResult = Assert.IsType<TM59CorridorExtendedResult>(tM59ExtendedResult);
            Assert.Equal(0, tM59CorridorExtendedResult.GetHoursNumberExceeding28());
        }

        // ------------------------------------------------------------------
        // Hourly series integrity - a verdict is refused rather than clipped
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Two series of different lengths are REFUSED, not assessed over the range they share.</b>
        /// <para>
        /// This test previously asserted the opposite, and it was right about the half of it that still
        /// holds: <c>Collect</c> walks both arrays with one counter, so bounding the walk by the occupancy
        /// length while the resultant series is shorter would throw
        /// <c>ArgumentOutOfRangeException</c> out of <c>Calculate_TM59</c> and lose every space's
        /// assessment. That bound is still there.
        /// </para>
        /// <para>
        /// What it got wrong is what should then happen to the space. Assessing it over the shared hours
        /// produced a TM59 verdict for the room and reported it as the room's, when a fifth of the room's
        /// occupied hours had no temperature to be judged at. Which of two unequal series is the truncated
        /// one is not knowable, so neither can be trusted. The space is refused, with a reason, and every
        /// other space in the building is still assessed - the trade the shared-range walk was making,
        /// with the unassessable room now visible instead of silently reported.
        /// </para>
        /// </summary>
        [Fact]
        public void SeriesOfDifferentLengths_AreRefusedRatherThanAssessedOverTheSharedRange()
        {
            AnalyticalModel analyticalModel = Model(
                key_Analytical_OccupancySensibleGain,
                values_ResultantTemperature: [21.0, 24.5, 27.5, 29.0],
                values_OccupancySensibleGain: [0, 80.0, 80.0, 0, 80.0]);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            //No verdict at all - not a verdict over four of the five hours.
            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            //And it says so, naming the room and both lengths.
            string refusal = Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals);
            Assert.Contains("Bedroom 2_3", refusal);
            Assert.Contains("different lengths", refusal);

            //The identity too, so a caller keeping the room out of a pass need not parse the sentence.
            Assert.Equal(analyticalModel.GetSpaces()[0].Guid, Assert.Single(tMOverheatingCalculator.SpaceGuids_HourlySeriesRefused));
        }

        /// <summary>
        /// Equal lengths proceed, however short, and leave no refusal behind. This class is the calculation
        /// over whatever series it is given - see <c>TMOverheatingCalculator.HourCount_Expected</c> for why
        /// the length a series has to be is the caller's statement, not this class's assumption.
        /// </summary>
        [Fact]
        public void SeriesOfEqualLength_Proceed()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Empty(tMOverheatingCalculator.HourlySeriesRefusals);
        }

        /// <summary>An empty series is a results file that was not written for the room, not a room with no
        /// exceedances - so it is refused rather than assessed as zero hours.</summary>
        [Fact]
        public void AnEmptySeries_IsRefused()
        {
            AnalyticalModel analyticalModel = Model(
                key_Analytical_OccupancySensibleGain,
                values_ResultantTemperature: [],
                values_OccupancySensibleGain: []);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            Assert.Contains("EMPTY", Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals));
        }

        /// <summary>
        /// <b>The full-year requirement, and that it is opt-in.</b>
        /// <para>
        /// Both series are the same length here, so nothing is mismatched - the results file is simply
        /// short of the year the caller says it needs. Left unstated (the default 0) the same input is
        /// assessed, because a short run is legitimate for a caller that asked for one; stated, it is
        /// refused, because neither a pass nor a failure may be produced from part of a year. Approved
        /// Document O is the caller that states it - see <c>PartOTM59Assessment</c>.
        /// </para>
        /// </summary>
        [Fact]
        public void ASeriesShorterThanTheStatedYear_IsRefusedOnlyWhereTheYearIsStated()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain);

            //Unstated: assessed, exactly as every existing caller's four-hour fixture is.
            Assert.Single(Calculator(analyticalModel).Calculate_TM59(analyticalModel.GetSpaces()));

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);
            tMOverheatingCalculator.HourCount_Expected = 8760;

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));

            string refusal = Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals);
            Assert.Contains("only 4 of the 8760", refusal);
            Assert.Contains("partial year", refusal);
        }

        /// <summary>
        /// A series LONGER than the stated year is not short of anything and proceeds - the leap-year
        /// simulation's 8784 hours against a 365-day weather year. Its surplus hours are already excluded
        /// by the comfort-band guard, which
        /// <see cref="HoursBeyondTheComfortYear_AreNotAssessedAgainstZeroComfortLimits"/> pins; refusing the
        /// space as well would lose an assessment that is correct.
        /// </summary>
        [Fact]
        public void ASeriesLongerThanTheStatedYear_Proceeds()
        {
            AnalyticalModel analyticalModel = Model(key_Analytical_OccupancySensibleGain);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);
            tMOverheatingCalculator.HourCount_Expected = 4;

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Empty(tMOverheatingCalculator.HourlySeriesRefusals);
        }

        /// <summary>
        /// One unusable room does not cost the others their assessment - the reason the refusal is reported
        /// rather than thrown, and the property the shared-range walk was originally protecting.
        /// </summary>
        [Fact]
        public void ARefusedSpace_DoesNotLoseTheAssessmentOfEveryOtherSpace()
        {
            Space space_Good = new("Bedroom 1_1");
            ParameterSet parameterSet_Good = new("SAM.Analytical.Tas.dll");
            parameterSet_Good.Add(key_Analytical_ResultantTemperature, Values([21.0, 24.5, 27.5, 29.0]));
            parameterSet_Good.Add(key_Analytical_OccupancySensibleGain, Values([0, 80.0, 80.0, 0]));
            space_Good.Add(parameterSet_Good);

            Space space_Bad = new("Bedroom 2_3");
            ParameterSet parameterSet_Bad = new("SAM.Analytical.Tas.dll");
            parameterSet_Bad.Add(key_Analytical_ResultantTemperature, Values([21.0, 24.5]));
            parameterSet_Bad.Add(key_Analytical_OccupancySensibleGain, Values([0, 80.0, 80.0, 0]));
            space_Bad.Add(parameterSet_Bad);

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(space_Good);
            adjacencyCluster.AddObject(space_Bad);

            AnalyticalModel analyticalModel = new("Three Flats", null, null, null, adjacencyCluster);
            analyticalModel.SetValue(AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            TM59ExtendedResult tM59ExtendedResult = Assert.Single(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Equal("Bedroom 1_1", tM59ExtendedResult.Name);

            Assert.Contains("Bedroom 2_3", Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals));
        }

        /// <summary>
        /// Each calculation reports its own refusals and never an earlier call's - including a call that
        /// returns null before assessing anything.
        /// </summary>
        [Fact]
        public void TheRefusalRecord_BelongsToTheLastCalculation()
        {
            AnalyticalModel analyticalModel = Model(
                key_Analytical_OccupancySensibleGain,
                values_ResultantTemperature: [21.0, 24.5],
                values_OccupancySensibleGain: [0, 80.0, 80.0, 0]);

            TMOverheatingCalculator tMOverheatingCalculator = Calculator(analyticalModel);

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces()));
            Assert.Single(tMOverheatingCalculator.HourlySeriesRefusals);

            //A call that refuses before reaching any space clears the record rather than leaving the
            //previous run's reasons readable as this one's.
            Assert.Null(tMOverheatingCalculator.Calculate_TM59(null));
            Assert.Empty(tMOverheatingCalculator.HourlySeriesRefusals);
        }

        // ------------------------------------------------------------------
        // Fixture
        // ------------------------------------------------------------------

        /// <summary>
        /// A calculator with an explicit empty <c>TextMap</c>, so the space resolves to no TM59 application
        /// and the criterion selection is deterministic without depending on a shipped resource file being
        /// installed on the machine running the tests.
        /// </summary>
        private static TMOverheatingCalculator Calculator(AnalyticalModel analyticalModel)
        {
            //Core.Create: TextMap's constructors are internal to SAM.Core.
            return new TMOverheatingCalculator(analyticalModel) { TextMap = Core.Create.TextMap("TM59") };
        }

        /// <summary>
        /// One space carrying a short run of hourly values, stored exactly as the TSD converter stores
        /// them: a <c>JsonArray</c> added to a <c>ParameterSet</c> that is then added to the space.
        /// </summary>
        private static AnalyticalModel Model(string key_OccupancySensibleGain, bool resultantTemperature = true, bool weatherData = true, IEnumerable<double> values_ResultantTemperature = null, IEnumerable<double> values_OccupancySensibleGain = null)
        {
            Space space = new("Bedroom 2_3");

            ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");

            if (resultantTemperature)
            {
                parameterSet.Add(key_Analytical_ResultantTemperature, Values(values_ResultantTemperature ?? [21.0, 24.5, 27.5, 29.0]));
            }

            parameterSet.Add(key_OccupancySensibleGain, Values(values_OccupancySensibleGain ?? [0, 80.0, 80.0, 0]));

            space.Add(parameterSet);

            AdjacencyCluster adjacencyCluster = new();
            adjacencyCluster.AddObject(space);

            AnalyticalModel result = new("Three Flats", null, null, null, adjacencyCluster);

            if (weatherData)
            {
                result.SetValue(AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));
            }

            return result;
        }

        /// <summary>
        /// A full year of flat 20 C dry-bulb hours. The comfort band TM52/TM59 compares against is derived
        /// from a running mean of these, so the year has to be populated - a bare <c>WeatherYear(2018)</c>
        /// carries no days and the running mean throws. A constant year keeps the expected band trivial and
        /// the tests about the calculation rather than about the weather.
        /// </summary>
        private static WeatherYear WeatherYear()
        {
            WeatherYear result = new(2018);

            for (int day = 0; day < 365; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    result.Add(day, hour, new Dictionary<string, double> { { WeatherDataType.DryBulbTemperature.ToString(), 20.0 } });
                }
            }

            return result;
        }

        private static JsonArray Values(IEnumerable<double> values)
        {
            JsonArray result = [];

            foreach (double value in values)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
