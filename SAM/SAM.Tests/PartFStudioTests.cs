// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Geometry.Spatial;
using SAM.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Tests for the SAM design convention covering studios and open plan living kitchens under
    /// Approved Document F, Volume 1: Dwellings (2021 edition, for use in England).
    /// </summary>
    /// <remarks>
    /// Both rooms are habitable under Appendix A, because neither is <i>solely</i> a kitchen, and both
    /// contain the cooking function that paragraph 1.17a requires extract from. SAM assigns one terminal
    /// role per space, so by deliberate design convention both receive the supply role only and their
    /// kitchen extract is reported rather than assigned. These tests lock that convention.
    /// </remarks>
    public class PartFStudioTests
    {
        private const string dataFileName = "SAM_PartFSpaceRulesUKDwellingsMVHR.json";

        private const double tolerance = 1e-6;

        // ------------------------------------------------------------------
        // Intended supply / extract arrangement
        // ------------------------------------------------------------------

        /// <summary>
        /// The intended arrangement: studio, living kitchen, bedroom and living room take mechanical
        /// supply; bathroom, ensuite, utility room and sanitary accommodation take mechanical extract.
        /// </summary>
        [Theory]
        [InlineData("Studio", PartFVentilationType.supply)]
        [InlineData("Living Kitchen", PartFVentilationType.supply)]
        [InlineData("Bedroom", PartFVentilationType.supply)]
        [InlineData("Living Room", PartFVentilationType.supply)]
        [InlineData("Bathroom", PartFVentilationType.extract)]
        [InlineData("Ensuite", PartFVentilationType.extract)]
        [InlineData("Utility Room", PartFVentilationType.extract)]
        [InlineData("WC", PartFVentilationType.extract)]
        public void IntendedArrangement_AssignsTheExpectedTerminalRole(string name, PartFVentilationType expected)
        {
            PartFCategory partFCategory = DataFile().GetPartFCategory(name);

            Assert.NotNull(partFCategory);
            Assert.Equal(expected, partFCategory.PartFVentilationType);
        }

        /// <summary>
        /// A studio and a living kitchen are habitable rooms, not wet rooms, so they carry no Table 1.2
        /// minimum extract rate of their own.
        /// </summary>
        [Theory]
        [InlineData("Studio")]
        [InlineData("Living Kitchen")]
        public void StudioAndLivingKitchen_AreHabitableWithNoExtractMinimum(string name)
        {
            PartFCategory partFCategory = DataFile().GetPartFCategory(name);

            Assert.NotNull(partFCategory);
            Assert.Equal(PartFType.Habitable, partFCategory.PartFType);
            Assert.True(partFCategory.IsCookingSpace);
            Assert.True(partFCategory.MinFlowRate_Lps is null || partFCategory.MinFlowRate_Lps.Value == 0);
        }

        /// <summary>
        /// A living kitchen must receive supply and no extract. It previously received extract only,
        /// which left the dwelling's habitable rooms with no supply provision at all.
        /// </summary>
        [Fact]
        public void LivingKitchen_ReceivesSupplyAndNoExtract()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Living Kitchen", 30, 75),
                ("Bedroom 1", 14, 35),
                ("Bathroom", 6, 15));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.True(Rate(partFCalculator, "Living Kitchen") > 0);

            //Total supply is carried by the living kitchen and the bedroom; the bathroom carries all the
            //extract. Supply and extract each equal the design system rate.
            Assert.Equal(dwellingResult.ContinuousDesignSystemRate_Lps, dwellingResult.TotalSupply_Lps, tolerance);
            Assert.Equal(dwellingResult.ContinuousDesignSystemRate_Lps, dwellingResult.TotalExtract_Lps, tolerance);
            Assert.Equal(dwellingResult.TotalExtract_Lps, Rate(partFCalculator, "Bathroom"), tolerance);
        }

        /// <summary>A studio receives supply and no extract, with the bathroom providing the extract.</summary>
        [Fact]
        public void Studio_ReceivesSupplyAndTheBathroomProvidesTheExtract()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 40, 100),
                ("Bathroom", 6, 15));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(dwellingResult.ContinuousDesignSystemRate_Lps, Rate(partFCalculator, "Studio"), tolerance);
            Assert.Equal(dwellingResult.ContinuousDesignSystemRate_Lps, Rate(partFCalculator, "Bathroom"), tolerance);
        }

        // ------------------------------------------------------------------
        // Habitable-room count, bedroom count and the whole dwelling rate
        // ------------------------------------------------------------------

        /// <summary>A studio counts as one bedroom.</summary>
        [Fact]
        public void Studio_CountsAsOneBedroom()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 20, 50),
                ("Bathroom", 4, 10));

            Assert.Equal(1, Assert.Single(partFCalculator.DwellingResults).BedroomCount);
        }

        /// <summary>
        /// A studio counts as one bedroom equivalent AND is one habitable room, so a studio flat sizes
        /// from Table 1.3 note 1 rather than the one bedroom figure of Table 1.3.
        /// </summary>
        [Fact]
        public void Studio_IsOneHabitableRoomAndOneBedroomEquivalent()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 20, 50),
                ("Bathroom", 4, 10));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(1, dwellingResult.HabitableRoomCount);
            Assert.Equal(1, dwellingResult.BedroomCount);
            Assert.True(dwellingResult.OneHabitableRoomRuleApplied);
        }

        /// <summary>
        /// The previous SAM convention of a fixed 19 l/s studio minimum is removed. A studio flat whose
        /// floor area and wet room minimums are both small now sizes at the Table 1.3 note 1 rate of
        /// 13 l/s, not 19 l/s.
        /// </summary>
        [Fact]
        public void Studio_NoLongerUsesTheFixedNineteenLitrePerSecondMinimum()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 20, 50),
                ("Bathroom", 4, 10));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(13, dwellingResult.BedroomOrHabitableRate_Lps, tolerance);
            Assert.Equal(13, dwellingResult.ContinuousDesignSystemRate_Lps, tolerance);
            Assert.NotEqual(19, dwellingResult.ContinuousDesignSystemRate_Lps);
        }

        /// <summary>
        /// Adding a second habitable room takes the dwelling out of Table 1.3 note 1 and back onto the
        /// bedroom table, where a studio counts as one bedroom: 19 l/s.
        /// </summary>
        [Theory]
        [InlineData("Living Room")]
        [InlineData("Study")]
        public void StudioPlusASecondHabitableRoom_ReturnsToTheBedroomTable(string name_Second)
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 20, 50),
                (name_Second, 10, 25),
                ("Bathroom", 4, 10));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(2, dwellingResult.HabitableRoomCount);
            Assert.False(dwellingResult.OneHabitableRoomRuleApplied);
            Assert.Equal(19, dwellingResult.BedroomOrHabitableRate_Lps, tolerance);
            Assert.Equal(19, dwellingResult.ContinuousDesignSystemRate_Lps, tolerance);
        }

        /// <summary>
        /// The floor area rate of paragraph 1.24a governs where it exceeds the note 1 rate:
        /// 0.3 x 100 m2 = 30 l/s beats 13 l/s.
        /// </summary>
        [Fact]
        public void Studio_FloorAreaRateGovernsAboveTheNote1Rate()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 96, 240),
                ("Bathroom", 4, 10));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(100, dwellingResult.InternalFloorArea_M2, tolerance);
            Assert.Equal(30, dwellingResult.AreaBasedRate_Lps, tolerance);
            Assert.Equal(13, dwellingResult.BedroomOrHabitableRate_Lps, tolerance);
            Assert.Equal(30, dwellingResult.ContinuousDesignSystemRate_Lps, tolerance);
        }

        /// <summary>
        /// The total of the wet room minimums governs where it exceeds both the note 1 rate and the floor
        /// area rate: bathroom 8 + WC 6 + utility 8 = 22 l/s beats 13 l/s.
        /// </summary>
        [Fact]
        public void Studio_WetRoomMinimumsGovernAboveTheNote1Rate()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 20, 50),
                ("Bathroom", 4, 10),
                ("WC", 2, 5),
                ("Utility Room", 4, 10));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.Equal(22, dwellingResult.WetRoomMinimumTotal_Lps, tolerance);
            Assert.Equal(13, dwellingResult.BedroomOrHabitableRate_Lps, tolerance);
            Assert.True(dwellingResult.AreaBasedRate_Lps < 22);
            Assert.Equal(22, dwellingResult.ContinuousDesignSystemRate_Lps, tolerance);
        }

        /// <summary>
        /// The governing continuous design rate is the greatest of every applicable minimum, whichever
        /// that happens to be.
        /// </summary>
        [Fact]
        public void ContinuousDesignRate_IsTheGreatestApplicableMinimum()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 96, 240),
                ("Bathroom", 4, 10),
                ("WC", 2, 5));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            double expected = System.Math.Max(
                System.Math.Max(dwellingResult.BedroomOrHabitableRate_Lps, dwellingResult.AreaBasedRate_Lps),
                dwellingResult.WetRoomMinimumTotal_Lps);

            Assert.Equal(expected, dwellingResult.ContinuousDesignSystemRate_Lps, tolerance);
        }

        // ------------------------------------------------------------------
        // The LOCAL kitchen extract limitation
        // ------------------------------------------------------------------
        //
        // The limitation is the absence of an explicitly modelled LOCAL kitchen or cooker extract, NOT
        // the absence of general dwelling extract. A cooking space counts as having explicit local
        // kitchen extract only where that space itself takes an extract terminal. Wet-room extract may
        // balance the dwelling airflow but is not evidence of local kitchen extract, so it must not
        // suppress the warning.

        /// <summary>
        /// A studio with a separate bathroom is the normal design arrangement: supply and extract balance,
        /// yet the local kitchen extract warning MUST remain, because no local kitchen or cooker extract
        /// is represented for the studio itself.
        /// </summary>
        [Fact]
        public void StudioPlusBathroom_BalancesButStillRaisesTheLocalKitchenExtractWarning()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 40, 100),
                ("Bathroom", 6, 15));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            //Balanced at both conditions - the bathroom extract does provide the dwelling's general extract.
            Assert.True(dwellingResult.TotalExtract_Lps > 0);
            Assert.Equal(dwellingResult.TotalSupply_Lps, dwellingResult.TotalExtract_Lps, tolerance);
            Assert.Equal(dwellingResult.TotalSetbackSupply_Lps, dwellingResult.TotalSetbackExtract_Lps, tolerance);

            //...but that is not local kitchen extract, so the warning stands.
            Assert.Contains(partFCalculator.Warnings, x =>
                x.Contains("ENGINEERING CHECK REQUIRED") &&
                x.Contains("no explicit local kitchen or cooker extract is represented") &&
                x.Contains("Studio"));
        }

        /// <summary>The same applies to an open plan living kitchen with an ensuite.</summary>
        [Fact]
        public void LivingKitchenPlusEnsuite_BalancesButStillRaisesTheLocalKitchenExtractWarning()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Living Kitchen", 30, 75),
                ("Bedroom 1", 14, 35),
                ("Ensuite", 5, 12.5));

            PartFDwellingResult dwellingResult = Assert.Single(partFCalculator.DwellingResults);

            Assert.True(dwellingResult.TotalExtract_Lps > 0);
            Assert.Equal(dwellingResult.TotalSupply_Lps, dwellingResult.TotalExtract_Lps, tolerance);
            Assert.Equal(dwellingResult.TotalSetbackSupply_Lps, dwellingResult.TotalSetbackExtract_Lps, tolerance);

            Assert.Contains(partFCalculator.Warnings, x =>
                x.Contains("ENGINEERING CHECK REQUIRED") &&
                x.Contains("no explicit local kitchen or cooker extract is represented") &&
                x.Contains("Living Kitchen"));
        }

        /// <summary>
        /// The warning must NOT claim the dwelling has no extract terminal when wet room extract terminals
        /// exist. That was the defect in the previous wording.
        /// </summary>
        [Fact]
        public void LocalKitchenExtractWarning_DoesNotClaimThereIsNoExtractTerminal()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Studio", 40, 100),
                ("Bathroom", 6, 15));

            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("no space that takes an extract terminal"));
            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("NO extract ventilation at all"));
            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("has no wet room that takes an extract terminal"));
        }

        /// <summary>
        /// A room classified as solely a kitchen DOES take an extract terminal, so it represents the
        /// configured local kitchen extract and raises no missing-local-kitchen-extract warning.
        /// </summary>
        [Fact]
        public void SolelyKitchenExtractTerminal_RaisesNoLocalKitchenExtractWarning()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Living Room", 20, 50),
                ("Bedroom 1", 14, 35),
                ("Kitchen", 12, 30),
                ("Bathroom", 6, 15));

            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("ENGINEERING CHECK REQUIRED"));
            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("local kitchen or cooker extract"));

            //And it holds at least its Table 1.2 kitchen minimum.
            Assert.True(Rate(partFCalculator, "Kitchen") >= 13 - tolerance);
        }

        /// <summary>
        /// A dwelling with a cooking space and NO extract terminal at all gets BOTH warnings: the local
        /// kitchen extract warning, and the separate paragraph 1.17 no-extract-terminal warning. The two
        /// are independent conditions and are reported independently.
        /// </summary>
        [Fact]
        public void CookingSpaceAndNoExtractTerminal_RaisesBothWarnings()
        {
            PartFCalculator partFCalculator = Calculate(("Studio", 40, 100));

            Assert.Contains(partFCalculator.Warnings, x =>
                x.Contains("ENGINEERING CHECK REQUIRED") &&
                x.Contains("no explicit local kitchen or cooker extract is represented"));

            Assert.Contains(partFCalculator.Warnings, x => x.Contains("no wet room that takes an extract terminal"));
        }

        /// <summary>
        /// A dwelling with no cooking space at all raises no kitchen extract warning of either kind - it
        /// raises the separate "no cooking space" warning instead.
        /// </summary>
        [Fact]
        public void DwellingWithoutACookingSpace_RaisesNoKitchenExtractWarning()
        {
            PartFCalculator partFCalculator = Calculate(
                ("Bedroom 1", 14, 35),
                ("Living Room", 20, 50),
                ("Bathroom", 6, 15));

            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("ENGINEERING CHECK REQUIRED"));
            Assert.DoesNotContain(partFCalculator.Warnings, x => x.Contains("local kitchen or cooker extract"));

            Assert.Contains(partFCalculator.Warnings, x => x.Contains("contains no kitchen, open plan living kitchen or studio"));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static PartFData DataFile()
        {
            return Analytical.Create.PartFData(Fixtures.GetPath(dataFileName));
        }

        private static double Rate(PartFCalculator partFCalculator, string name)
        {
            return partFCalculator.AdjacencyCluster.GetSpaces().Find(x => x.Name == name)
                ?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.ContinuousDesignFlowRate_Lps ?? 0;
        }

        private static PartFCalculator Calculate(params (string Name, double Area_M2, double Volume_M3)[] spaces)
        {
            AdjacencyCluster adjacencyCluster = new();

            for (int i = 0; i < spaces.Length; i++)
            {
                Space space = new(spaces[i].Name, new Point3D(i * 10, 0, 1.5));
                space.SetValue(SpaceParameter.Area, spaces[i].Area_M2);
                space.SetValue(SpaceParameter.Volume, spaces[i].Volume_M3);
                adjacencyCluster.AddObject(space);
            }

            PartFCalculator result = new(DataFile()) { AdjacencyCluster = adjacencyCluster };

            Assert.True(result.Calculate());

            return result;
        }
    }
}
