// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// F-S11-2: <see cref="SpaceSimulationResultParameter.UnmetHours"/> holds fractional time. EnergyPlus reports
    /// unmet time in hours with fractions (OpenStudio S11, e.g. 14.5 h); the former integer metadata rounded it on
    /// SetValue. Tas writes whole-hour counts, which must keep their numerical meaning, and results saved before the
    /// change (integer JSON, e.g. "Value": 9) must still read back.
    /// </summary>
    public class SpaceSimulationResultUnmetHoursTests
    {
        //One space result as SAM_Tas saved it before this change: S10 acc2 s08-fix-box fixture.json (cooling, trimmed
        //to the counters). The counters are integer JSON numbers.
        private const string LegacyTasResultJson = @"{
  ""_type"": ""SAM.Analytical.SpaceSimulationResult,SAM.Analytical"",
  ""ParameterSets"": [
    {
      ""_type"": ""SAM.Core.ParameterSet"",
      ""Name"": ""SAM.Analytical"",
      ""Guid"": ""76cd41af-8835-46fc-9225-a7fea365eae1"",
      ""Parameters"": [
        { ""Name"": ""Load Type"", ""Value"": ""Cooling"" },
        { ""Name"": ""Load Index"", ""Value"": 3878 },
        { ""Name"": ""Unmet Hour First Index"", ""Value"": 3879 },
        { ""Name"": ""Unmet Hours"", ""Value"": 9 },
        { ""Name"": ""Occupied Unmet Hours"", ""Value"": 9 }
      ]
    }
  ],
  ""Name"": ""FixBox"",
  ""Guid"": ""b3a4c82e-3249-4e92-a58f-c255430cbdb2"",
  ""Source"": ""SAM.Analytical.Tas"",
  ""Reference"": ""{25309C7B-8B43-47F8-AE63-3E235CFE2A7B}""
}";

        private static SpaceSimulationResult Result()
        {
            return new SpaceSimulationResult("Zone 1", "SAM.Analytical.OpenStudio", "zone-reference");
        }

        private static SpaceSimulationResult SaveAndReopen(SpaceSimulationResult spaceSimulationResult)
        {
            Space space = new Space("Zone 1", new Point3D(0, 0, 0));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(spaceSimulationResult);
            adjacencyCluster.AddRelation(space, spaceSimulationResult);

            AnalyticalModel analyticalModel = new AnalyticalModel("Model", null, null, null, adjacencyCluster);
            string json = analyticalModel.ToJsonObject().ToJsonString();
            AnalyticalModel reopened = new AnalyticalModel(JsonNode.Parse(json) as JsonObject);

            return reopened.AdjacencyCluster.GetObjects<SpaceSimulationResult>().Single();
        }

        [Theory]
        [InlineData(0.25)]
        [InlineData(14.5)]
        [InlineData(14.75)]
        public void FractionalUnmetHours_SurviveSetValueAndGetValue(double unmetHours)
        {
            SpaceSimulationResult spaceSimulationResult = Result();

            Assert.True(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, unmetHours));
            Assert.True(spaceSimulationResult.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double value));
            Assert.Equal(unmetHours, value);
            Assert.Equal(unmetHours, spaceSimulationResult.GetValue<double>(SpaceSimulationResultParameter.UnmetHours));
        }

        [Theory]
        [InlineData(0.25)]
        [InlineData(14.5)]
        [InlineData(14.75)]
        public void FractionalUnmetHours_SurviveAWholeModelSaveAndReopen(double unmetHours)
        {
            SpaceSimulationResult spaceSimulationResult = Result();
            spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, unmetHours);

            SpaceSimulationResult reopened = SaveAndReopen(spaceSimulationResult);

            Assert.True(reopened.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double value));
            Assert.Equal(unmetHours, value);
        }

        [Fact]
        public void WholeHourCounts_KeepTheirValue_AsSetByTas()
        {
            //SAM_Tas Query.UnmetHours passes an int count.
            SpaceSimulationResult spaceSimulationResult = Result();
            Assert.True(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, 292));

            SpaceSimulationResult reopened = SaveAndReopen(spaceSimulationResult);

            Assert.True(reopened.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double value));
            Assert.Equal(292.0, value);
            Assert.True(reopened.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out int count));
            Assert.Equal(292, count);
        }

        [Fact]
        public void LegacyIntegerJson_ReadsBackUnchanged()
        {
            SpaceSimulationResult spaceSimulationResult = new SpaceSimulationResult(JsonNode.Parse(LegacyTasResultJson) as JsonObject);

            Assert.True(spaceSimulationResult.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double unmetHours));
            Assert.Equal(9.0, unmetHours);
            Assert.Equal(9, spaceSimulationResult.GetValue<int>(SpaceSimulationResultParameter.UnmetHours));

            //The other counters are untouched by this change.
            Assert.Equal(9, spaceSimulationResult.GetValue<int>(SpaceSimulationResultParameter.OccupiedUnmetHours));
            Assert.Equal(3879, spaceSimulationResult.GetValue<int>(SpaceSimulationResultParameter.UnmetHourFirstIndex));
            Assert.Equal(3878, spaceSimulationResult.GetValue<int>(SpaceSimulationResultParameter.LoadIndex));
        }

        [Fact]
        public void LegacyIntegerJson_ResavedAndReopened_KeepsTheValue()
        {
            SpaceSimulationResult legacy = new SpaceSimulationResult(JsonNode.Parse(LegacyTasResultJson) as JsonObject);

            SpaceSimulationResult reopened = SaveAndReopen(legacy);

            Assert.True(reopened.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double unmetHours));
            Assert.Equal(9.0, unmetHours);
            Assert.Equal(9, reopened.GetValue<int>(SpaceSimulationResultParameter.OccupiedUnmetHours));
        }

        [Fact]
        public void Range_KeepsZeroAndBothBoundaries_AndRejectsValuesOutside()
        {
            SpaceSimulationResult spaceSimulationResult = Result();

            Assert.True(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, 0.0));
            Assert.Equal(0.0, spaceSimulationResult.GetValue<double>(SpaceSimulationResultParameter.UnmetHours));

            Assert.True(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, 8760.0));
            Assert.Equal(8760.0, spaceSimulationResult.GetValue<double>(SpaceSimulationResultParameter.UnmetHours));

            //A rejected value leaves the stored one in place.
            Assert.False(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, -0.25));
            Assert.False(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, 8760.25));
            Assert.False(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, -1));
            Assert.False(spaceSimulationResult.SetValue(SpaceSimulationResultParameter.UnmetHours, 8761));
            Assert.Equal(8760.0, spaceSimulationResult.GetValue<double>(SpaceSimulationResultParameter.UnmetHours));
        }

        [Fact]
        public void Missing_IsUnavailable_NotZero()
        {
            SpaceSimulationResult missing = SaveAndReopen(Result());
            Assert.False(missing.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double _));
            Assert.False(missing.HasValue(SpaceSimulationResultParameter.UnmetHours));

            SpaceSimulationResult zero = Result();
            zero.SetValue(SpaceSimulationResultParameter.UnmetHours, 0.0);
            zero = SaveAndReopen(zero);
            Assert.True(zero.TryGetValue(SpaceSimulationResultParameter.UnmetHours, out double value));
            Assert.Equal(0.0, value);
        }

        [Fact]
        public void Metadata_KeepsMemberOrdinalAndKey_OnlyTheValueTypeIsDouble()
        {
            Assert.Equal(34, (int)SpaceSimulationResultParameter.UnmetHours);
            Assert.Equal("Unmet Hours", Core.Attributes.ParameterProperties.Get(SpaceSimulationResultParameter.UnmetHours).Name);
            Assert.Equal(ParameterType.Double, Core.Attributes.ParameterValue.GetParameterType(SpaceSimulationResultParameter.UnmetHours));

            //The other counters stay integers.
            Assert.Equal(ParameterType.Integer, Core.Attributes.ParameterValue.GetParameterType(SpaceSimulationResultParameter.UnmetHourFirstIndex));
            Assert.Equal(ParameterType.Integer, Core.Attributes.ParameterValue.GetParameterType(SpaceSimulationResultParameter.OccupiedUnmetHours));
            Assert.Equal(ParameterType.Integer, Core.Attributes.ParameterValue.GetParameterType(SpaceSimulationResultParameter.OccupiedHours));
            Assert.Equal(ParameterType.Integer, Core.Attributes.ParameterValue.GetParameterType(AdjacencyClusterSimulationResultParameter.UnmetHours));
        }
    }
}
