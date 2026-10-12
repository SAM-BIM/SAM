// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The units of <see cref="AnalyticalModelSimulationResult"/> consumptions and peak loads (SAM_Tas#92, option A): the
    /// <see cref="AnalyticalModelSimulationResultParameter.ResultUnitsVersion"/> marker, the producer rule for results
    /// written without it, and DesignExplorer reading through <see cref="Analytical.Query.TryGetConsumptionOrPeakLoad"/>.
    /// The numbers are the licensed SAM_Tas#92 measurements.
    /// </summary>
    public class ResultUnitsTests
    {
        // SAM_Tas#92: Systems Demo TSD copy, as SAM_Tas stored it before the correction (Wh, W).
        private const double DemoHeatingWh = 15_227_840.66370964;
        private const double DemoCoolingWh = 2_978_532.5259896517;
        private const double DemoPeakHeatingW = 38_727.40625;

        // SAM_Tas#92: the SAM-generated Part O model, cooling (PR7a measured 2 523.75 kWh).
        private const double PartOCoolingWh = 2_523_751.8729965687;

        private const double PeakCoolingW = 21_456.875;
        private const double FloorArea = 1_234.5;
        private const double Volume = 3_703.5;

        private static readonly AnalyticalModelSimulationResultParameter[] ConsumptionsAndPeaks =
        [
            AnalyticalModelSimulationResultParameter.ConsumptionHeating,
            AnalyticalModelSimulationResultParameter.ConsumptionCooling,
            AnalyticalModelSimulationResultParameter.PeakHeatingLoad,
            AnalyticalModelSimulationResultParameter.PeakCoolingLoad,
        ];

        // ---- the marker -----------------------------------------------------------------------------------------

        [Fact]
        public void Marker_IsAppended_AndExistingOrdinalsAndStoredNamesAreUnchanged()
        {
            (AnalyticalModelSimulationResultParameter Member, int Ordinal, string Name)[] expected =
            [
                (AnalyticalModelSimulationResultParameter.ConsumptionHeating, 0, "Consumption Heating"),
                (AnalyticalModelSimulationResultParameter.PeakHeatingLoad, 1, "Peak Heating Load"),
                (AnalyticalModelSimulationResultParameter.PeakHeatingHour, 2, "Peak Heating Hour"),
                (AnalyticalModelSimulationResultParameter.ConsumptionCooling, 3, "Consumption Cooling"),
                (AnalyticalModelSimulationResultParameter.PeakCoolingLoad, 4, "Peak Cooling Load"),
                (AnalyticalModelSimulationResultParameter.PeakCoolingHour, 5, "Peak Cooling Hour"),
                (AnalyticalModelSimulationResultParameter.FloorArea, 6, "Floor Area"),
                (AnalyticalModelSimulationResultParameter.Volume, 7, "Volume"),
                (AnalyticalModelSimulationResultParameter.ResultUnitsVersion, 8, "Result Units Version"),
            ];

            Assert.Equal(expected.Length, Enum.GetValues(typeof(AnalyticalModelSimulationResultParameter)).Length);
            foreach ((AnalyticalModelSimulationResultParameter member, int ordinal, string name) in expected)
            {
                Assert.Equal(ordinal, (int)member);
                Assert.Equal(name, Core.Attributes.ParameterProperties.Get(member).Name);
            }

            Assert.Equal(1, AnalyticalModelSimulationResult.CurrentResultUnitsVersion);
            Assert.Equal("SAM.Analytical.Tas", AnalyticalModelSimulationResult.TasSource);
            Assert.Equal("SAM.Analytical.OpenStudio", AnalyticalModelSimulationResult.OpenStudioSource);
        }

        [Fact]
        public void SetResultUnitsVersion_StampsVersionOne_AndConvertsNothing()
        {
            AnalyticalModelSimulationResult result = Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh / 1000, DemoCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000);

            Assert.True(result.SetResultUnitsVersion());

            Assert.True(result.TryGetValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion, out int version));
            Assert.Equal(1, version);
            Assert.Equal(DemoHeatingWh / 1000, result.GetValue<double>(AnalyticalModelSimulationResultParameter.ConsumptionHeating));
            Assert.False(Analytical.Modify.SetResultUnitsVersion(null));
        }

        // ---- the reader rule ------------------------------------------------------------------------------------

        [Theory]
        [InlineData("SAM.Analytical.Tas", false, ResultUnitsState.LegacyTas)]
        [InlineData("SAM.Analytical.Tas", true, ResultUnitsState.Marked)]
        [InlineData("SAM.Analytical.OpenStudio", false, ResultUnitsState.LegacyOpenStudio)]
        [InlineData("SAM.Analytical.OpenStudio", true, ResultUnitsState.Marked)]
        [InlineData("Some.Other.Producer", true, ResultUnitsState.Marked)]
        [InlineData("Some.Other.Producer", false, ResultUnitsState.UnknownProducer)]
        [InlineData("Tas", false, ResultUnitsState.UnknownProducer)]
        [InlineData("sam.analytical.tas", false, ResultUnitsState.UnknownProducer)]
        [InlineData("SAM.Analytical.Tas ", false, ResultUnitsState.UnknownProducer)]
        [InlineData("SAM.Analytical.Tas.TPD", false, ResultUnitsState.UnknownProducer)]
        [InlineData("", false, ResultUnitsState.UnknownProducer)]
        [InlineData(null, false, ResultUnitsState.UnknownProducer)]
        public void ResultUnitsState_IsDecidedByTheMarkerThenTheExactProducer(string source, bool marked, ResultUnitsState expected)
        {
            AnalyticalModelSimulationResult result = Result(source, 1, 1, 1, 1);
            if (marked)
            {
                result.SetResultUnitsVersion();
            }

            Assert.Equal(expected, result.ResultUnitsState());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-1)]
        [InlineData(1.5)]
        [InlineData(double.NaN)]
        [InlineData("1.0")]
        [InlineData("one")]
        [InlineData("")]
        [InlineData(true)]
        public void ResultUnitsState_AnUnknownMarkerVersionIsNeverGuessed_EvenOnATasOrOpenStudioResult(object marker)
        {
            foreach (string source in new[] { AnalyticalModelSimulationResult.TasSource, AnalyticalModelSimulationResult.OpenStudioSource })
            {
                AnalyticalModelSimulationResult result = Result(source, DemoHeatingWh, DemoCoolingWh, DemoPeakHeatingW, PeakCoolingW);
                SetRawMarker(result, marker);

                Assert.Equal(ResultUnitsState.UnsupportedVersion, result.ResultUnitsState());
                foreach (AnalyticalModelSimulationResultParameter parameter in ConsumptionsAndPeaks)
                {
                    Assert.False(result.TryGetConsumptionOrPeakLoad(parameter, out double value));
                    Assert.True(double.IsNaN(value));
                }
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1.0)]
        [InlineData("1")]
        [InlineData(" 1 ")]
        public void ResultUnitsState_VersionOneIsReadWhateverNumberTypeItCameBackAs(object marker)
        {
            AnalyticalModelSimulationResult result = Result(AnalyticalModelSimulationResult.TasSource, 1, 1, 1, 1);
            SetRawMarker(result, marker);

            Assert.Equal(ResultUnitsState.Marked, result.ResultUnitsState());
        }

        [Fact]
        public void ResultUnitsState_OfNoResult_IsUndefined()
        {
            Assert.Equal(ResultUnitsState.Undefined, Analytical.Query.ResultUnitsState(null));
            Assert.False(Analytical.Query.TryGetConsumptionOrPeakLoad(null, AnalyticalModelSimulationResultParameter.ConsumptionHeating, out _));
        }

        [Fact]
        public void LegacyTas_IsConvertedFromWhAndW_ExactlyOnce_OnTheRecordedNumbers()
        {
            AnalyticalModelSimulationResult legacy = Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW);

            Assert.Equal(15_227.84066370964, legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating), 9);
            Assert.Equal(2_523.7518729965687, legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionCooling), 9);
            Assert.Equal(38.72740625, legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakHeatingLoad), 12);
            Assert.Equal(21.456875, legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakCoolingLoad), 12);
            Assert.Equal(15_227.84, Core.Query.Round(legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating), 0.01));
            Assert.Equal(2_523.75, Core.Query.Round(legacy.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionCooling), 0.01));

            // Reading converts the value it returns, never the result: the stored Wh stay, and no marker is added.
            Assert.Equal(DemoHeatingWh, legacy.GetValue<double>(AnalyticalModelSimulationResultParameter.ConsumptionHeating));
            Assert.False(legacy.HasValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion));
            Assert.Equal(ResultUnitsState.LegacyTas, legacy.ResultUnitsState());
        }

        [Fact]
        public void MarkedTas_WrittenAsTheCorrectedConverterWritesIt_ReadsBitIdenticalToLegacyTas()
        {
            AnalyticalModelSimulationResult legacy = Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh, DemoCoolingWh, DemoPeakHeatingW, PeakCoolingW);
            AnalyticalModelSimulationResult marked = Marked(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh / 1000, DemoCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000);

            foreach (AnalyticalModelSimulationResultParameter parameter in ConsumptionsAndPeaks)
            {
                Assert.True(legacy.TryGetConsumptionOrPeakLoad(parameter, out double fromLegacy));
                Assert.True(marked.TryGetConsumptionOrPeakLoad(parameter, out double fromMarked));
                Assert.Equal(BitConverter.DoubleToInt64Bits(fromLegacy), BitConverter.DoubleToInt64Bits(fromMarked));

                // Not divided a second time.
                Assert.Equal(marked.GetValue<double>(parameter), fromMarked);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OpenStudio_IsAlwaysKilo_WithOrWithoutTheMarker(bool marked)
        {
            AnalyticalModelSimulationResult result = Result(AnalyticalModelSimulationResult.OpenStudioSource, 15_227.84, 2_978.53, 38.73, 21.46);
            if (marked)
            {
                result.SetResultUnitsVersion();
            }

            Assert.Equal(15_227.84, result.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating));
            Assert.Equal(2_978.53, result.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionCooling));
            Assert.Equal(38.73, result.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakHeatingLoad));
            Assert.Equal(21.46, result.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakCoolingLoad));
        }

        [Fact]
        public void UnknownProducer_WithoutTheMarker_IsNotRead()
        {
            AnalyticalModelSimulationResult result = Result("Some.Other.Producer", DemoHeatingWh, DemoCoolingWh, DemoPeakHeatingW, PeakCoolingW);

            foreach (AnalyticalModelSimulationResultParameter parameter in ConsumptionsAndPeaks)
            {
                Assert.False(result.TryGetConsumptionOrPeakLoad(parameter, out double value));
                Assert.True(double.IsNaN(value));
            }
        }

        [Fact]
        public void OnlyTheFourConsumptionAndPeakParameters_AreRead_AndAMissingValueIsNotZero()
        {
            AnalyticalModelSimulationResult result = new AnalyticalModelSimulationResult("Model", AnalyticalModelSimulationResult.TasSource, "model.tsd");
            result.SetValue(AnalyticalModelSimulationResultParameter.FloorArea, FloorArea);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingHour, 205);

            Assert.False(result.TryGetConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.FloorArea, out _));
            Assert.False(result.TryGetConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakHeatingHour, out _));
            Assert.False(result.TryGetConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ResultUnitsVersion, out _));
            Assert.False(result.TryGetConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating, out double missing));
            Assert.True(double.IsNaN(missing));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TheMarkerAndTheProducer_SurviveAJsonRoundTrip_InsideAModel(bool marked)
        {
            AnalyticalModelSimulationResult result = marked
                ? Marked(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh / 1000, DemoCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000)
                : Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh, DemoCoolingWh, DemoPeakHeatingW, PeakCoolingW);

            AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), "Model");
            Assert.True(analyticalModel.AddResult(result));

            AnalyticalModel reloaded = new AnalyticalModel(analyticalModel.ToJsonObject());
            AnalyticalModelSimulationResult reloadedResult = reloaded.GetAnalyticalModelSimulationResults().Single();

            Assert.Equal(marked ? ResultUnitsState.Marked : ResultUnitsState.LegacyTas, reloadedResult.ResultUnitsState());
            Assert.Equal(15_227.84066370964, reloadedResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.ConsumptionHeating), 9);
            Assert.Equal(38.72740625, reloadedResult.ConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter.PeakHeatingLoad), 12);
        }

        // ---- DesignExplorer -------------------------------------------------------------------------------------

        [Fact]
        public void DesignExplorer_LegacyAndMarkedTasResults_GiveTheSameRows_AsBeforeTheCorrection()
        {
            List<Tuple<string, object>> legacy = DesignExplorerRows(Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW));
            List<Tuple<string, object>> marked = DesignExplorerRows(Marked(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000));

            Dictionary<string, object> expected = PreCorrectionRows(DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW);

            Assert.Equal(expected.Keys, legacy.Select(x => x.Item1));
            Assert.Equal(expected.Keys, marked.Select(x => x.Item1));
            foreach (Tuple<string, object> row in legacy)
            {
                Assert.Equal(expected[row.Item1], row.Item2);
            }

            foreach (Tuple<string, object> row in marked)
            {
                Assert.Equal(expected[row.Item1], row.Item2);
            }

            // The recorded numbers, as DesignExplorer publishes them; peaks per area and volume stay in W/m2 and W/m3.
            Assert.Equal(15_227.84, Row(legacy, "out:ConsumptionHTG[kWh]"));
            Assert.Equal(2_523.75, Row(legacy, "out:ConsumptionCLG[kWh]"));
            Assert.Equal(38.73, Row(legacy, "out:PeakHTGLoad[kW]"));
            Assert.Equal(Core.Query.Round(DemoPeakHeatingW / FloorArea, 0.01), Row(legacy, "out:PeakHTGLoad[W/m2]"));
            Assert.Equal(Core.Query.Round(DemoPeakHeatingW / Volume, 0.01), Row(legacy, "out:PeakHTGLoad[W/m3]"));
            Assert.Equal(Core.Query.Round(DemoHeatingWh / 1000 / FloorArea, 0.01), Row(legacy, "out:ConsumptionHTG[kWh/m2]"));
            Assert.Equal(Core.Query.Round(DemoHeatingWh / 1000 / Volume, 0.01), Row(legacy, "out:ConsumptionHTG[kWh/m3]"));
        }

        [Fact]
        public void DesignExplorer_LegacyAndMarkedTasResults_MatchThePreCorrectionArithmetic_AcrossASweep()
        {
            Random random = new Random(20261012);
            for (int i = 0; i < 5000; i++)
            {
                double heatingWh = random.NextDouble() * System.Math.Pow(10, random.Next(0, 10));
                double coolingWh = random.NextDouble() * System.Math.Pow(10, random.Next(0, 10));
                double peakHeatingW = random.NextDouble() * System.Math.Pow(10, random.Next(0, 7));
                double peakCoolingW = random.NextDouble() * System.Math.Pow(10, random.Next(0, 7));
                double floorArea = 1 + random.NextDouble() * System.Math.Pow(10, random.Next(0, 5));
                double volume = floorArea * (2 + random.NextDouble() * 4);

                Dictionary<string, object> expected = PreCorrectionRows(heatingWh, coolingWh, peakHeatingW, peakCoolingW, floorArea, volume);
                List<Tuple<string, object>> legacy = DesignExplorerRows(Result(AnalyticalModelSimulationResult.TasSource, heatingWh, coolingWh, peakHeatingW, peakCoolingW, floorArea, volume));
                List<Tuple<string, object>> marked = DesignExplorerRows(Marked(AnalyticalModelSimulationResult.TasSource, heatingWh / 1000, coolingWh / 1000, peakHeatingW / 1000, peakCoolingW / 1000, floorArea, volume));

                foreach (Tuple<string, object> row in legacy.Concat(marked))
                {
                    Assert.True(Equals(expected[row.Item1], row.Item2), string.Format("case {0}, {1}: expected {2}, got {3}", i, row.Item1, expected[row.Item1], row.Item2));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DesignExplorer_OpenStudioResult_IsReadAsKilo_NotDividedAgain(bool marked)
        {
            AnalyticalModelSimulationResult openStudio = Result(AnalyticalModelSimulationResult.OpenStudioSource, DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000);
            if (marked)
            {
                openStudio.SetResultUnitsVersion();
            }

            List<Tuple<string, object>> rows = DesignExplorerRows(openStudio);
            Dictionary<string, object> expected = PreCorrectionRows(DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW);
            foreach (Tuple<string, object> row in rows)
            {
                Assert.Equal(expected[row.Item1], row.Item2);
            }
        }

        [Theory]
        [InlineData("Some.Other.Producer", null)]
        [InlineData("SAM.Analytical.Tas", 2)]
        public void DesignExplorer_UnknownUnits_LeaveTheUnitCellsEmpty_AndKeepTheColumns(string source, object marker)
        {
            AnalyticalModelSimulationResult result = Result(source, DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW);
            if (marker != null)
            {
                SetRawMarker(result, marker);
            }

            List<Tuple<string, object>> rows = DesignExplorerRows(result);

            Assert.Equal(PreCorrectionRows(1, 1, 1, 1).Keys, rows.Select(x => x.Item1));
            foreach (Tuple<string, object> row in rows)
            {
                if (row.Item1.EndsWith("Hr", StringComparison.Ordinal))
                {
                    Assert.NotEqual(string.Empty, row.Item2);
                }
                else
                {
                    Assert.Equal(string.Empty, row.Item2);
                }
            }
        }

        [Fact]
        public void DesignExplorerCsv_ALegacyAndAMarkedModel_WriteTheSameLine()
        {
            AnalyticalModel legacy = Model(Result(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh, PartOCoolingWh, DemoPeakHeatingW, PeakCoolingW));
            AnalyticalModel marked = Model(Marked(AnalyticalModelSimulationResult.TasSource, DemoHeatingWh / 1000, PartOCoolingWh / 1000, DemoPeakHeatingW / 1000, PeakCoolingW / 1000));

            List<string> csv = Analytical.Convert.ToDesignExplorer(new[] { legacy, marked });

            Assert.Equal(3, csv.Count);
            Assert.Equal(csv[1], csv[2]);
            Assert.Contains("15227.84", csv[1]);
        }

        // ---- helpers --------------------------------------------------------------------------------------------

        private static AnalyticalModelSimulationResult Result(string source, double consumptionHeating, double consumptionCooling, double peakHeatingLoad, double peakCoolingLoad, double floorArea = FloorArea, double volume = Volume)
        {
            AnalyticalModelSimulationResult result = new AnalyticalModelSimulationResult("Model", source, "model.tsd");
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionHeating, consumptionHeating);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingLoad, peakHeatingLoad);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakHeatingHour, 205);
            result.SetValue(AnalyticalModelSimulationResultParameter.ConsumptionCooling, consumptionCooling);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingLoad, peakCoolingLoad);
            result.SetValue(AnalyticalModelSimulationResultParameter.PeakCoolingHour, 4602);
            result.SetValue(AnalyticalModelSimulationResultParameter.FloorArea, floorArea);
            result.SetValue(AnalyticalModelSimulationResultParameter.Volume, volume);
            return result;
        }

        private static AnalyticalModelSimulationResult Marked(string source, double consumptionHeating, double consumptionCooling, double peakHeatingLoad, double peakCoolingLoad, double floorArea = FloorArea, double volume = Volume)
        {
            AnalyticalModelSimulationResult result = Result(source, consumptionHeating, consumptionCooling, peakHeatingLoad, peakCoolingLoad, floorArea, volume);
            Assert.True(result.SetResultUnitsVersion());
            return result;
        }

        // Writes a marker of any type, as a hand-edited or foreign file could hold it: under the parameter's stored name,
        // bypassing the Integer conversion SetValue would apply.
        private static void SetRawMarker(AnalyticalModelSimulationResult result, object marker)
        {
            string name = Core.Attributes.ParameterProperties.Get(AnalyticalModelSimulationResultParameter.ResultUnitsVersion).Name;
            System.Reflection.Assembly assembly = typeof(AnalyticalModelSimulationResultParameter).Assembly;
            switch (marker)
            {
                case int @int:
                    Assert.True(Core.Modify.SetValue(result, assembly, name, @int));
                    break;
                case double @double:
                    Assert.True(Core.Modify.SetValue(result, assembly, name, @double));
                    break;
                case string @string:
                    Assert.True(Core.Modify.SetValue(result, assembly, name, @string));
                    break;
                case bool @bool:
                    Assert.True(Core.Modify.SetValue(result, assembly, name, @bool));
                    break;
                default:
                    throw new ArgumentException("Unsupported marker type", nameof(marker));
            }

            Assert.True(result.HasValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion));
        }

        private static AnalyticalModel Model(AnalyticalModelSimulationResult result)
        {
            AnalyticalModel analyticalModel = new AnalyticalModel(Guid.NewGuid(), "Model");
            Assert.True(analyticalModel.AddResult(result));
            return analyticalModel;
        }

        private static List<Tuple<string, object>> DesignExplorerRows(AnalyticalModelSimulationResult result)
        {
            return Analytical.Convert.ToDesignExplorer(Model(result));
        }

        private static object Row(List<Tuple<string, object>> rows, string name)
        {
            return rows.Single(x => x.Item1 == name).Item2;
        }

        // The DesignExplorer rows exactly as File.cs computed them before the units correction, from SAM_Tas's Wh and W.
        private static Dictionary<string, object> PreCorrectionRows(double heatingWh, double coolingWh, double peakHeatingW, double peakCoolingW, double floorArea = FloorArea, double volume = Volume)
        {
            return new Dictionary<string, object>
            {
                ["out:ConsumptionHTG[kWh]"] = Core.Query.Round(heatingWh / 1000, 0.01),
                ["out:ConsumptionHTG[kWh/m2]"] = Core.Query.Round(heatingWh / floorArea / 1000, 0.01),
                ["out:ConsumptionHTG[kWh/m3]"] = Core.Query.Round(heatingWh / volume / 1000, 0.01),
                ["out:PeakHTGLoad[kW]"] = Core.Query.Round(peakHeatingW / 1000, 0.01),
                ["out:PeakHTGLoad[W/m2]"] = Core.Query.Round(peakHeatingW / floorArea, 0.01),
                ["out:PeakHTGLoad[W/m3]"] = Core.Query.Round(peakHeatingW / volume, 0.01),
                ["out:PeakHTGHr"] = 205.0,
                ["out:ConsumptionCLG[kWh]"] = Core.Query.Round(coolingWh / 1000, 0.01),
                ["out:ConsumptionCLG[kWh/m2]"] = Core.Query.Round(coolingWh / floorArea / 1000, 0.01),
                ["out:ConsumptionCLG[kWh/m3]"] = Core.Query.Round(coolingWh / volume / 1000, 0.01),
                ["out:PeakCLGLoad[kW]"] = Core.Query.Round(peakCoolingW / 1000, 0.01),
                ["out:PeakCLGLoad[W/m2]"] = Core.Query.Round(peakCoolingW / floorArea, 0.01),
                ["out:PeakCLGLoad[W/m3]"] = Core.Query.Round(peakCoolingW / volume, 0.01),
                ["out:PeakCLGHr"] = 4602.0,
            };
        }
    }
}
