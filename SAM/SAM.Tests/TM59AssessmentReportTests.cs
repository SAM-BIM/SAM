// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <c>TM59AssessmentReport</c> - the verification view over a TM59 assessment.
    /// <para>
    /// <b>What these tests are guarding.</b> The report exists so an engineer can check a TM59 run without
    /// re-deriving it, which only works if the report cannot disagree with the assessment. So the assertions
    /// are mostly about what the report is <i>not</i> allowed to do: invent a verdict from arithmetic, turn a
    /// corridor's overheating risk into an occupied-space compliance failure, or quietly upgrade a criterion
    /// that was never required of a space into a pass.
    /// </para>
    /// <para>
    /// The results are built directly rather than simulated. Every value below is a fixed number chosen to
    /// make one distinction visible, which is what lets a margin or a status be asserted exactly.
    /// </para>
    /// </summary>
    public class TM59AssessmentReportTests
    {
        private const string source = "SAM.Tests";

        // ---------------------------------------------------------------------------------------------
        // Natural ventilation - Criterion 1 and Criterion 2 stay separate
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void NaturalVentilation_Passing_ReportsCriterion1AsPassWithTheRemainingAllowance()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(naturalVentilation: [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.NaturalVentilationChecks, "Living 1_0", TM59AssessmentReport.Check_Criterion1);

            Assert.Equal(TM59ComplianceStatus.Pass, check.ComplianceStatus);
            Assert.Equal(37, check.Actual);
            Assert.Equal(110, check.Limit);
            Assert.Equal(73, check.Margin);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.NaturalVentilationComplianceStatus);
        }

        [Fact]
        public void NaturalVentilation_Failing_ReportsCriterion1AsFailWithANegativeMargin()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(naturalVentilation: [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 150, maxExceedableSummerHours: 110, pass: false)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.NaturalVentilationChecks, "Living 1_0", TM59AssessmentReport.Check_Criterion1);

            Assert.Equal(TM59ComplianceStatus.Fail, check.ComplianceStatus);
            Assert.Equal(-40, check.Margin);
            Assert.Equal(TM59ComplianceStatus.Fail, tM59AssessmentReport.NaturalVentilationComplianceStatus);
            Assert.Equal(TM59ComplianceStatus.Fail, tM59AssessmentReport.OccupiedSpaceComplianceStatus);
        }

        /// <summary>
        /// Criterion 2 is a separate row with its own numbers, and it is the bedroom night-time figures that
        /// appear on it - not Criterion 1's, which is the confusion a single merged Pass/Fail would create.
        /// </summary>
        [Theory]
        [InlineData(11, 32, true)]
        [InlineData(45, 32, false)]
        public void Bedroom_ReportsCriterion2SeparatelyFromCriterion1(int nightHoursExceeding26, int maxExceedableNightHours, bool expected_Pass)
        {
            TM59AssessmentReport tM59AssessmentReport = Report(naturalVentilation: [Bedroom("Bedroom 2_3", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, nightHoursNumberExceeding26: nightHoursExceeding26, maxExceedableNightHours: maxExceedableNightHours, pass: true)]);

            TM59AssessmentReportCheck check_Criterion1 = Check(tM59AssessmentReport.NaturalVentilationChecks, "Bedroom 2_3", TM59AssessmentReport.Check_Criterion1);
            TM59AssessmentReportCheck check_Criterion2 = Check(tM59AssessmentReport.NaturalVentilationChecks, "Bedroom 2_3", TM59AssessmentReport.Check_Criterion2);

            Assert.Equal(37, check_Criterion1.Actual);
            Assert.Equal(110, check_Criterion1.Limit);

            Assert.Equal(nightHoursExceeding26, check_Criterion2.Actual);
            Assert.Equal(maxExceedableNightHours, check_Criterion2.Limit);
            Assert.Equal(maxExceedableNightHours - nightHoursExceeding26, check_Criterion2.Margin);
            Assert.Equal(expected_Pass ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail, check_Criterion2.ComplianceStatus);

            //And a failed Criterion 2 fails the assessment even though Criterion 1 passed - Pass on a TM59
            //result is Criterion 1 alone, so a report reading only Pass would have called this a clean pass.
            Assert.Equal(expected_Pass ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail, tM59AssessmentReport.OccupiedSpaceComplianceStatus);
        }

        /// <summary>
        /// Criterion 2 is not required of a room that is not a bedroom. It is stated as N/A rather than left
        /// off the report or folded into a pass, so a reader can see it was considered.
        /// </summary>
        [Fact]
        public void NonBedroom_ReportsCriterion2AsNotApplicableRatherThanPass()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(naturalVentilation: [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.NaturalVentilationChecks, "Living 1_0", TM59AssessmentReport.Check_Criterion2);

            Assert.Equal(TM59ComplianceStatus.NotApplicable, check.ComplianceStatus);
            Assert.Null(check.Actual);
            Assert.Null(check.Limit);
            Assert.Null(check.Margin);

            //A group of nothing but non-applicable criteria is not a pass either.
            Assert.Equal(TM59ComplianceStatus.NotApplicable, Report(naturalVentilation: []).NaturalVentilationComplianceStatus);

            Assert.Contains("N/A", tM59AssessmentReport.ToString());
        }

        /// <summary>
        /// <b>Real numbers, from the Iteration 1 BasePassive validation pass.</b> Flat1's <c>Studio 1_0</c>
        /// against TAS's own "Domestic Overheating (CIBSE TM59)" report for the same run: Occupied Summer
        /// Hours 3672, Max. Exceedable Hours 110, Criterion 1 exceedance 37, Pass. The annual figures this
        /// fixture also carries (8760 occupied, 999 as a deliberately wrong-looking annual limit) exist only
        /// to prove the row does NOT read them - it did, once: the annual <c>MaxExceedableHours</c> (262 for
        /// this space) was shown as the Limit until this test caught it against the real TAS figures.
        /// </summary>
        [Fact]
        public void NaturalVentilation_Criterion1Limit_IsTheSummerBasisTasActuallyReports()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(naturalVentilation: [NaturalVentilation("Studio 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.NaturalVentilationChecks, "Studio 1_0", TM59AssessmentReport.Check_Criterion1);

            Assert.Equal(37, check.Actual);
            Assert.Equal(110, check.Limit);
            Assert.Equal(73, check.Margin);
            Assert.NotEqual(262, check.Limit);
        }

        // ---------------------------------------------------------------------------------------------
        // Mechanical ventilation
        // ---------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(135, 142, true, TM59ComplianceStatus.Pass, 7)]
        [InlineData(200, 142, false, TM59ComplianceStatus.Fail, -58)]
        public void MechanicalVentilation_ReportsTheFixedTemperatureCheck(int hoursExceeding26, int maxExceedableHours, bool pass, TM59ComplianceStatus expected, int expected_Margin)
        {
            TM59AssessmentReport tM59AssessmentReport = Report(mechanicalVentilation: [Mechanical("Kitchen_4", hoursExceeding26, maxExceedableHours, pass)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.MechanicalVentilationChecks, "Kitchen_4", TM59AssessmentReport.Check_HoursExceeding26);

            Assert.Equal(hoursExceeding26, check.Actual);
            Assert.Equal(maxExceedableHours, check.Limit);
            Assert.Equal(expected_Margin, check.Margin);
            Assert.Equal(expected, check.ComplianceStatus);
            Assert.Equal(expected, tM59AssessmentReport.MechanicalVentilationComplianceStatus);
        }

        // ---------------------------------------------------------------------------------------------
        // Corridors - risk, and only risk
        // ---------------------------------------------------------------------------------------------

        [Theory]
        [InlineData(120, 262, true, TM59RiskStatus.Acceptable)]
        [InlineData(337, 262, false, TM59RiskStatus.SignificantRisk)]
        public void Corridor_IsReportedAsRiskNeverAsCompliance(int hoursExceeding28, int maxExceedableHours, bool pass, TM59RiskStatus expected)
        {
            TM59AssessmentReport tM59AssessmentReport = Report(corridor: [Corridor("Corridor_1", hoursExceeding28, maxExceedableHours, pass)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.CorridorChecks, "Corridor_1", TM59AssessmentReport.Check_HoursExceeding28);

            Assert.Equal(expected, check.RiskStatus);
            Assert.Equal(expected, tM59AssessmentReport.CorridorRiskStatus);

            //The >28 C threshold is not a mandatory occupied-space test, so the row carries no Pass/Fail.
            Assert.Equal(TM59ComplianceStatus.NotApplicable, check.ComplianceStatus);
            Assert.DoesNotContain("Corridor = FAIL", tM59AssessmentReport.ToString());
            Assert.DoesNotContain("Corridor = PASS", tM59AssessmentReport.ToString());
        }

        // ---------------------------------------------------------------------------------------------
        // The corridor bucket is a bucket, not an identification
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>An ancillary room is never presented as a communal corridor.</b> A bathroom with no TM59 space
        /// application reaches <c>CorridorResults</c> by exactly the same route a corridor does - see
        /// <c>TMOverheatingCalculator.Calculate_TM59</c>, which buckets on "no space application OR strategy
        /// 'UV'" - and nothing on the result records which of the two applied. So the report reports the
        /// number and refuses to assert the identification.
        /// </summary>
        [Fact]
        public void AnAncillaryRoom_IsNotPresentedAsACommunalCorridor()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(
                naturalVentilation: [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true)],
                corridor: [Corridor("Bathroom_2", hoursExceeding28: 2, maxExceedableHours: 262, pass: true)]);

            TM59AssessmentReportCheck check = Check(tM59AssessmentReport.CorridorChecks, "Bathroom_2", TM59AssessmentReport.Check_HoursExceeding28);

            //Not a compliance verdict, and not counted towards the occupied-space assessment either.
            Assert.Equal(TM59ComplianceStatus.NotApplicable, check.ComplianceStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.OccupiedSpaceComplianceStatus);

            string text = tM59AssessmentReport.ToString();

            //The section it appears under does not call it a communal corridor, and the caveat is beside the
            //table rather than buried in the legend.
            Assert.Contains(TM59AssessmentReportFormatter.Heading_Corridors, text);
            Assert.DoesNotContain("COMMUNAL CORRIDORS", text);
            Assert.Contains(TM59AssessmentReportFormatter.Note_CorridorBucket, text);
            Assert.Contains("does not by itself prove that the space is a TM59 communal corridor", text);
        }

        /// <summary>
        /// The same refusal to identify holds when the room really is over the threshold: the risk is stated
        /// plainly, and it still does not become a corridor claim or an occupied-space failure.
        /// </summary>
        [Fact]
        public void AnAncillaryRoomOverTheThreshold_StatesRiskWithoutClaimingItIsACorridor()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(
                naturalVentilation: [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true)],
                corridor: [Corridor("Ensuite_5", hoursExceeding28: 400, maxExceedableHours: 262, pass: false)]);

            Assert.Equal(TM59RiskStatus.SignificantRisk, Check(tM59AssessmentReport.CorridorChecks, "Ensuite_5", TM59AssessmentReport.Check_HoursExceeding28).RiskStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.OccupiedSpaceComplianceStatus);

            string text = tM59AssessmentReport.ToString();

            Assert.DoesNotContain("COMMUNAL CORRIDORS", text);
            Assert.Contains("not a proven communal corridor", text);
        }

        /// <summary>
        /// <b>The regulatory point of the whole separation.</b> A corridor over its threshold is highly
        /// visible in the summary and changes nothing about whether the dwellings passed.
        /// </summary>
        [Fact]
        public void ASignificantRiskCorridor_DoesNotFailTheOccupiedSpaceAssessment()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(
                naturalVentilation: [Bedroom("Bedroom 2_3", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, nightHoursNumberExceeding26: 11, maxExceedableNightHours: 32, pass: true)],
                mechanicalVentilation: [Mechanical("Kitchen_4", hoursExceeding26: 135, maxExceedableHours: 142, pass: true)],
                corridor: [Corridor("Corridor_1", hoursExceeding28: 337, maxExceedableHours: 262, pass: false)]);

            Assert.Equal(TM59RiskStatus.SignificantRisk, tM59AssessmentReport.CorridorRiskStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.OccupiedSpaceComplianceStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.NaturalVentilationComplianceStatus);
            Assert.Equal(TM59ComplianceStatus.Pass, tM59AssessmentReport.MechanicalVentilationComplianceStatus);

            string text = tM59AssessmentReport.ToString();

            //Visible at the top and in the summary, and the two verdicts are stated as different things.
            Assert.Contains("OCCUPIED SPACE ASSESSMENT: PASS", text);
            Assert.Contains("TM59 occupied-space assessment: PASS", text);
            Assert.Contains("SIGNIFICANT RISK", text);

            //And the risk line names the bucket rather than asserting the space is a communal corridor.
            Assert.Contains("Full-year >28 C risk (corridor-style bucket, not a proven communal corridor): SIGNIFICANT RISK", text);
        }

        // ---------------------------------------------------------------------------------------------
        // Margins
        // ---------------------------------------------------------------------------------------------

        /// <summary>Margin is Limit - Actual, and it is signed in the text so the two directions are distinct.</summary>
        [Fact]
        public void Margin_IsLimitMinusActualAndIsRenderedSigned()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(
                mechanicalVentilation: [Mechanical("Kitchen_4", hoursExceeding26: 135, maxExceedableHours: 142, pass: true)],
                corridor: [Corridor("Corridor_1", hoursExceeding28: 337, maxExceedableHours: 262, pass: false)]);

            Assert.Equal(7, Check(tM59AssessmentReport.MechanicalVentilationChecks, "Kitchen_4", TM59AssessmentReport.Check_HoursExceeding26).Margin);
            Assert.Equal(-75, Check(tM59AssessmentReport.CorridorChecks, "Corridor_1", TM59AssessmentReport.Check_HoursExceeding28).Margin);

            string text = tM59AssessmentReport.ToString();

            Assert.Contains("+7", text);
            Assert.Contains("-75", text);
        }

        // ---------------------------------------------------------------------------------------------
        // Spaces that produced nothing
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// A space the assessment covered but produced no result for is named, with the assessment's own
        /// refusals beside it. Silently dropping it would let a short count read as a clean run.
        /// </summary>
        [Fact]
        public void ASpaceWithNoResult_IsNamedRatherThanOmitted()
        {
            Space space_Assessed = new("Living 1_0");
            Space space_Unassessed = new("Bathroom_2");

            TM59AssessmentReport tM59AssessmentReport = new(
                [space_Assessed, space_Unassessed],
                null,
                [NaturalVentilation("Living 1_0", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, pass: true, reference: space_Assessed.Guid.ToString())],
                null,
                ["No scenario covers space 'Hall_6'."]);

            Assert.Equal(2, tM59AssessmentReport.UnassessedSpaces.Count);
            Assert.Contains(tM59AssessmentReport.UnassessedSpaces, x => x.Contains("Bathroom_2"));
            Assert.Contains(tM59AssessmentReport.UnassessedSpaces, x => x.Contains("Hall_6"));

            //And the assessed one is not listed as missing.
            Assert.DoesNotContain(tM59AssessmentReport.UnassessedSpaces, x => x.Contains("Living 1_0"));

            string text = tM59AssessmentReport.ToString();
            Assert.Contains("Bathroom_2", text);
            Assert.Contains("Hall_6", text);
        }

        // ---------------------------------------------------------------------------------------------
        // The text
        // ---------------------------------------------------------------------------------------------

        [Fact]
        public void TheText_CarriesEverySectionAndNamesItsSource()
        {
            TM59AssessmentReport tM59AssessmentReport = Report(
                naturalVentilation: [Bedroom("Bedroom 2_3", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, nightHoursNumberExceeding26: 11, maxExceedableNightHours: 32, pass: true)],
                mechanicalVentilation: [Mechanical("Kitchen_4", hoursExceeding26: 135, maxExceedableHours: 142, pass: true)],
                corridor: [Corridor("Corridor_1", hoursExceeding28: 337, maxExceedableHours: 262, pass: false)]);

            string text = tM59AssessmentReport.ToString();

            foreach (string heading in new[]
            {
                TM59AssessmentReportFormatter.Heading,
                TM59AssessmentReportFormatter.Heading_NaturalVentilation,
                TM59AssessmentReportFormatter.Heading_MechanicalVentilation,
                TM59AssessmentReportFormatter.Heading_Corridors,
                TM59AssessmentReportFormatter.Heading_Unassessed,
                TM59AssessmentReportFormatter.Heading_Summary,
                TM59AssessmentReportFormatter.Heading_Legend,
            })
            {
                Assert.Contains(heading, text);
            }

            Assert.Contains(source, text);
            Assert.Contains(TM52BuildingCategory.CategoryII.Description(), text);

            //Every criterion appears under its own name rather than as one merged verdict.
            Assert.Contains(TM59AssessmentReport.Check_Criterion1, text);
            Assert.Contains(TM59AssessmentReport.Check_Criterion2, text);
            Assert.Contains(TM59AssessmentReport.Check_HoursExceeding26, text);
            Assert.Contains(TM59AssessmentReport.Check_HoursExceeding28, text);
        }

        /// <summary>
        /// The legend has to explain what a reader is looking at, and it has to include the caveat: passing
        /// temperatures are not a statement of Approved Document O compliance.
        /// </summary>
        [Fact]
        public void TheLegend_ExplainsEveryTermTheTableUses()
        {
            string text = Report(corridor: [Corridor("Corridor_1", hoursExceeding28: 337, maxExceedableHours: 262, pass: false)]).ToString();

            string legend = text[text.IndexOf(TM59AssessmentReportFormatter.Heading_Legend)..];

            foreach (string term in new[] { "Actual", "Limit", "Margin", "Criterion 1", "Criterion 2", ">26 C hours", ">28 C hours", "ComplianceStatus", "RiskStatus", "N/A" })
            {
                Assert.Contains(term, legend);
            }

            //Margin's direction, and the two criteria's differing strictness, are both stated - a reader who
            //re-derives a verdict from a zero margin gets Criterion 2 wrong otherwise.
            Assert.Contains("Limit - Actual", legend);
            Assert.Contains("inclusive", legend);

            //Risk is explicitly not a compliance failure, and explicitly not a corridor identification.
            Assert.Contains("NOT an", legend);
            Assert.Contains("NOT, by itself, proof that the", legend);

            //And the report never claims Part O compliance.
            Assert.Contains(TM59AssessmentReportFormatter.Caveat, text);
            Assert.DoesNotContain("Part O compliant", text);
        }

        // ---------------------------------------------------------------------------------------------
        // The report changes nothing
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Building a report is a read. The results it was built from carry the same numbers and the same
        /// verdicts afterwards, and building it twice produces the same text.
        /// </summary>
        [Fact]
        public void BuildingAReport_LeavesTheAssessmentUntouched()
        {
            List<TMResult> tMResults_Natural = [Bedroom("Bedroom 2_3", hoursExceedingComfortRange: 37, maxExceedableSummerHours: 110, nightHoursNumberExceeding26: 11, maxExceedableNightHours: 32, pass: true)];
            List<TMResult> tMResults_Mechanical = [Mechanical("Kitchen_4", hoursExceeding26: 135, maxExceedableHours: 142, pass: true)];
            List<TMResult> tMResults_Corridor = [Corridor("Corridor_1", hoursExceeding28: 337, maxExceedableHours: 262, pass: false)];

            List<string> before = Transcript(tMResults_Natural, tMResults_Mechanical, tMResults_Corridor);

            string text = new TM59AssessmentReport(null, tMResults_Mechanical, tMResults_Natural, tMResults_Corridor, null, source).ToString();

            Assert.Equal(before, Transcript(tMResults_Natural, tMResults_Mechanical, tMResults_Corridor));
            Assert.Equal(text, new TM59AssessmentReport(null, tMResults_Mechanical, tMResults_Natural, tMResults_Corridor, null, source).ToString());
        }

        [Fact]
        public void AnAbsentAssessment_ProducesAReportRatherThanThrowing()
        {
            TM59AssessmentReport tM59AssessmentReport = new((TM59AssessmentResult)null);

            Assert.Equal(TM59ComplianceStatus.NotApplicable, tM59AssessmentReport.OccupiedSpaceComplianceStatus);
            Assert.Equal(TM59RiskStatus.Undefined, tM59AssessmentReport.CorridorRiskStatus);
            Assert.NotNull(tM59AssessmentReport.ToString());

            Assert.Null(TM59AssessmentReportFormatter.Text(null));
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture
        // ---------------------------------------------------------------------------------------------

        private static List<string> Transcript(params List<TMResult>[] tMResults)
        {
            return [.. tMResults.SelectMany(x => x).Select(x => string.Format("{0}|{1}|{2}|{3}|{4}", x.Name, x.OccupiedHours, x.MaxExceedableHours, x.Pass, x.ToJsonObject()?.ToJsonString()))];
        }

        private static TM59AssessmentReportCheck Check(List<TM59AssessmentReportCheck> tM59AssessmentReportChecks, string spaceName, string check)
        {
            return tM59AssessmentReportChecks.Single(x => x.SpaceName == spaceName && x.Check == check);
        }

        private static TM59AssessmentReport Report(List<TMResult> naturalVentilation = null, List<TMResult> mechanicalVentilation = null, List<TMResult> corridor = null)
        {
            return new TM59AssessmentReport(null, mechanicalVentilation, naturalVentilation, corridor, null, source);
        }

        /// <summary>
        /// <c>maxExceedableSummerHours</c> is deliberately the only limit a caller controls: the report reads
        /// Criterion 1's limit off the summer basis, never the annual <c>maxExceedableHours</c> field this
        /// fixture fixes at an unrelated 999 - so a regression back to reading the annual field would show up
        /// here as a wrong Limit, not pass by coincidence the way an equal-looking pair of test values would.
        /// </summary>
        private static TMResult NaturalVentilation(string name, int hoursExceedingComfortRange, int maxExceedableSummerHours, bool pass, string reference = null)
        {
            return new TM59NaturalVentilationResult(name, source, reference, TM52BuildingCategory.CategoryII, 8760, 999, 3672, maxExceedableSummerHours, hoursExceedingComfortRange, pass, TM59SpaceApplication.Living);
        }

        private static TMResult Bedroom(string name, int hoursExceedingComfortRange, int maxExceedableSummerHours, int nightHoursNumberExceeding26, int maxExceedableNightHours, bool pass)
        {
            return new TM59NaturalVentilationBedroomResult(name, source, null, TM52BuildingCategory.CategoryII, 8760, 999, hoursExceedingComfortRange, 3285, 3672, maxExceedableSummerHours, maxExceedableNightHours, nightHoursNumberExceeding26, pass);
        }

        private static TMResult Mechanical(string name, int hoursExceeding26, int maxExceedableHours, bool pass)
        {
            return new TM59MechanicalVentilationResult(name, source, null, TM52BuildingCategory.CategoryII, 4740, maxExceedableHours, hoursExceeding26, pass, TM59SpaceApplication.Cooking);
        }

        private static TMResult Corridor(string name, int hoursExceeding28, int maxExceedableHours, bool pass)
        {
            return new TM59CorridorResult(name, source, null, TM52BuildingCategory.CategoryII, 8760, maxExceedableHours, hoursExceeding28, pass);
        }
    }
}
