// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using System;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// TM59 must never calculate, infer, or write SpaceParameter.Area, and must never silently
    /// substitute zero for a missing gain. These tests lock that contract for per-area equipment
    /// gains (Communal Corridor / Riser Communal pipework, both 15 W/m²) and confirm the absolute
    /// HIU gain (78 W) is unaffected by Area either way.
    /// </summary>
    public class TM59AreaValidationTests
    {
        private static Space NewSpace(string name, double? area = null)
        {
            Space space = new Space(Guid.NewGuid(), name, null);
            if (area.HasValue)
                space.SetValue(SpaceParameter.Area, area.Value);

            return space;
        }

        [Fact]
        public void Communal_Corridor_With_Valid_Area_Has_Correct_Gain_And_No_Diagnostic()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space corridor = NewSpace("Corridor1", area: 20.0);

            TM59InternalConditionResult result = resolver.Resolve(corridor, null);

            Assert.True(result.InternalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out double gainPerArea));
            Assert.Equal(15.0, gainPerArea);
            Assert.Equal(300.0, gainPerArea * 20.0);
            Assert.Null(result.Diagnostic);
        }

        [Fact]
        public void Communal_Corridor_Without_Area_Is_Still_Returned_For_Manual_Selection_With_A_Diagnostic()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space corridor = NewSpace("Corridor1"); // no Area set at all

            TM59InternalConditionResult result = resolver.Resolve(corridor, null);

            // Still resolved/visible - a missing Area does not hide the condition, only flags it.
            Assert.NotNull(result.InternalCondition);
            Assert.NotNull(result.Diagnostic);
            Assert.Contains("SAMAnalytical.Check", result.Diagnostic);
            Assert.Contains("SAMAnalytical.CalculateFloorArea", result.Diagnostic);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void Communal_Corridor_With_NaN_Or_Infinite_Area_Is_Treated_As_Missing_Not_Zero(double area)
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space corridor = NewSpace("Corridor1", area: area);

            TM59InternalConditionResult result = resolver.Resolve(corridor, null);

            Assert.NotNull(result.InternalCondition);
            Assert.NotNull(result.Diagnostic);
            Assert.Contains("SAMAnalytical.Check", result.Diagnostic);
            Assert.Contains("SAMAnalytical.CalculateFloorArea", result.Diagnostic);
            Assert.DoesNotContain("NaN", result.Diagnostic);
        }

        [Fact]
        public void Riser_Communal_Pipework_Also_Uses_The_Same_Fifteen_Watt_Per_Area_Gain()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space riser = NewSpace("Communal Riser", area: 5.0);

            TM59InternalConditionResult result = resolver.Resolve(riser, null);

            Assert.Equal("TM59_Riser Communal pipework", result.InternalCondition?.Name);
            Assert.True(result.InternalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGainPerArea, out double gainPerArea));
            Assert.Equal(15.0, gainPerArea);
            Assert.Null(result.Diagnostic);
        }

        [Fact]
        public void HIU_Condition_Absolute_Gain_Never_Flags_Missing_Area_With_Or_Without_It()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space hiuNoArea = NewSpace("HIU Cupboard");
            Space hiuWithArea = NewSpace("HIU Cupboard", area: 4.0);

            TM59InternalConditionResult noAreaResult = resolver.Resolve(hiuNoArea, null);
            TM59InternalConditionResult withAreaResult = resolver.Resolve(hiuWithArea, null);

            // The validation only fires for a non-zero EquipmentSensibleGainPerArea; HIU's is 0/absent
            // (its gain is the absolute EquipmentSensibleGain instead), so no Area is ever required.
            Assert.Null(noAreaResult.Diagnostic);
            Assert.Null(withAreaResult.Diagnostic);

            Assert.True(noAreaResult.InternalCondition.TryGetValue(InternalConditionParameter.EquipmentSensibleGain, out double gain));
            Assert.Equal(78.0, gain);
        }

        [Fact]
        public void TM59_Mapping_Never_Creates_Or_Modifies_SpaceParameter_Area()
        {
            TM59InternalConditionResolver resolver = TM59TestData.NewResolver();
            Space corridor = NewSpace("Corridor1"); // no Area
            Assert.False(corridor.TryGetValue(SpaceParameter.Area, out _));

            resolver.Resolve(corridor, null);

            // Still absent - resolving (even the diagnostic path above) never writes Area as a side effect.
            Assert.False(corridor.TryGetValue(SpaceParameter.Area, out _));
        }
    }
}
