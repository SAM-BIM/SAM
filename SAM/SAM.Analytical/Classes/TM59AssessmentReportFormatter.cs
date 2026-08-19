// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SAM.Analytical
{
    /// <summary>
    /// Renders a <see cref="TM59AssessmentReport"/> as the multiline text an engineer reads in a Grasshopper
    /// panel.
    /// <para>
    /// <b>Presentation only.</b> It reads the report's rows and statuses and lays them out. It counts
    /// nothing, decides nothing and cannot disagree with the assessment - which is what lets a CSV or JSON
    /// view be added beside it later without any of them restating TM59.
    /// </para>
    /// </summary>
    public static class TM59AssessmentReportFormatter
    {
        /// <summary>Section headings, exposed so a caller checking for a section does not restate the string.</summary>
        public const string Heading = "CIBSE TM59:2017 OVERHEATING ASSESSMENT";

        public const string Heading_NaturalVentilation = "NATURAL VENTILATION";
        public const string Heading_AssessmentHours = "ASSESSMENT HOURS";
        public const string Heading_MechanicalVentilation = "MECHANICAL VENTILATION";

        /// <summary>Only a positively identified communal corridor - see <see cref="TM59AssessmentReport.CorridorChecks"/> - is presented here.</summary>
        public const string Heading_CommunalCorridorRisk = "COMMUNAL CORRIDOR RISK";

        /// <summary>
        /// The same &gt;28 °C calculation for every space that reached it WITHOUT a positively identified
        /// communal-corridor InternalCondition - see <see cref="TM59AssessmentReport.SupplementaryChecks"/>.
        /// Real engineering information, never a mandatory communal-corridor criterion.
        /// </summary>
        public const string Heading_Supplementary = "SUPPLEMENTARY >28 C CHECKS";

        public const string Heading_Unassessed = "SPACES NOT ASSESSED";
        public const string Heading_Summary = "SUMMARY";
        public const string Heading_Legend = "LEGEND";

        /// <summary>
        /// Printed near the header, beside the TM52 building category. This component assesses simulated
        /// temperatures against TM59:2017 - it cannot itself prove that every Approved Document O modelling
        /// assumption was applied to the simulation the results came from, so it states plainly, up front,
        /// that it never verifies that.
        /// </summary>
        public const string PartOModellingAssumptionsNotice = "Part O modelling assumptions: NOT VERIFIED BY THIS RESULT REPORT";

        /// <summary>
        /// The one sentence the whole separation exists to allow. Passing temperatures do not establish that
        /// the Approved Document O modelling assumptions behind the simulation were applied, so no Part O
        /// compliance is claimed anywhere in this report.
        /// </summary>
        public const string Caveat =
            "This report assesses simulated temperatures against CIBSE TM59:2017. It does not by itself\r\n" +
            "verify that every Approved Document O modelling assumption was applied to the simulation.";

        public static string Text(TM59AssessmentReport tM59AssessmentReport)
        {
            if (tM59AssessmentReport == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new();

            stringBuilder.AppendLine(Heading);
            stringBuilder.AppendLine(new string('=', Heading.Length));

            if (!string.IsNullOrWhiteSpace(tM59AssessmentReport.Source))
            {
                stringBuilder.AppendLine(string.Format("Source: {0}", tM59AssessmentReport.Source));
            }

            stringBuilder.AppendLine(string.Format("TM52 building category: {0}", tM59AssessmentReport.TM52BuildingCategory.Description()));
            stringBuilder.AppendLine(PartOModellingAssumptionsNotice);
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(string.Format("TM59 OCCUPIED-SPACE ASSESSMENT: {0}", Display(tM59AssessmentReport.OccupiedSpaceComplianceStatus)));
            stringBuilder.AppendLine(string.Format("TM59 COMMUNAL-CORRIDOR RISK: {0}", Display(tM59AssessmentReport.CorridorRiskStatus)));

            AppendNaturalVentilationSection(stringBuilder, tM59AssessmentReport.NaturalVentilationChecks);
            AppendAssessmentHoursSection(stringBuilder, tM59AssessmentReport.NaturalVentilationChecks);
            AppendMechanicalVentilationSection(stringBuilder, tM59AssessmentReport.MechanicalVentilationChecks);
            AppendRiskSection(stringBuilder, Heading_CommunalCorridorRisk, tM59AssessmentReport.CorridorChecks);
            AppendRiskSection(stringBuilder, Heading_Supplementary, tM59AssessmentReport.SupplementaryChecks);

            AppendHeading(stringBuilder, Heading_Unassessed);
            if (tM59AssessmentReport.UnassessedSpaces.Count == 0)
            {
                stringBuilder.AppendLine("Every space the assessment covered produced a result.");
            }
            else
            {
                foreach (string unassessedSpace in tM59AssessmentReport.UnassessedSpaces)
                {
                    stringBuilder.AppendLine(unassessedSpace);
                }
            }

            AppendHeading(stringBuilder, Heading_Summary);
            stringBuilder.AppendLine(string.Format("Natural ventilation:             {0}", Display(tM59AssessmentReport.NaturalVentilationComplianceStatus)));
            stringBuilder.AppendLine(string.Format("Mechanical ventilation:           {0}", Display(tM59AssessmentReport.MechanicalVentilationComplianceStatus)));
            stringBuilder.AppendLine(string.Format("TM59 occupied-space assessment:   {0}", Display(tM59AssessmentReport.OccupiedSpaceComplianceStatus)));
            stringBuilder.AppendLine(string.Format("TM59 communal-corridor risk:      {0}", Display(tM59AssessmentReport.CorridorRiskStatus)));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Caveat);

            AppendHeading(stringBuilder, Heading_Legend);
            stringBuilder.Append(Legend());

            return stringBuilder.ToString();
        }

        /// <summary>
        /// One row per space - Criterion 1 and Criterion 2 side by side, grouped by
        /// <see cref="TM59AssessmentReportCheck.Reference"/> (the simulated space's Guid), never by
        /// <see cref="TM59AssessmentReportCheck.SpaceName"/>. Two dwellings can share a room name; they
        /// cannot share a Guid.
        /// </summary>
        private static void AppendNaturalVentilationSection(StringBuilder stringBuilder, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, Heading_NaturalVentilation);

            if (tM59AssessmentReportChecks.Count == 0)
            {
                stringBuilder.AppendLine("No space was assessed against this criterion.");
                return;
            }

            List<string[]> rows =
            [
                ["Space", "Internal Condition", "TM59 Application", "Criterion 1", "Criterion 2", "Overall"],
            ];

            foreach (IGrouping<string, TM59AssessmentReportCheck> group in GroupBySpace(tM59AssessmentReportChecks))
            {
                TM59AssessmentReportCheck check_Criterion1 = group.FirstOrDefault(x => x.Check == TM59AssessmentReport.Check_Criterion1);
                TM59AssessmentReportCheck check_Criterion2 = group.FirstOrDefault(x => x.Check == TM59AssessmentReport.Check_Criterion2);
                TM59AssessmentReportCheck any = check_Criterion1 ?? check_Criterion2 ?? group.First();

                rows.Add(
                [
                    any.SpaceName ?? "-",
                    any.InternalCondition ?? "-",
                    any.Use ?? "-",
                    CriterionCell(check_Criterion1),
                    CriterionCell(check_Criterion2),
                    Display(CombineForDisplay(check_Criterion1, check_Criterion2)),
                ]);
            }

            AppendTable(stringBuilder, rows, rightAligned: [false, false, false, true, true, false]);
        }

        /// <summary>
        /// The basis hours behind Criterion 1 and Criterion 2, kept out of the main table so it stays
        /// compact - one row per space, grouped exactly as <see cref="AppendNaturalVentilationSection"/>
        /// groups them.
        /// </summary>
        private static void AppendAssessmentHoursSection(StringBuilder stringBuilder, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, Heading_AssessmentHours);

            if (tM59AssessmentReportChecks.Count == 0)
            {
                stringBuilder.AppendLine("No space was assessed against this criterion.");
                return;
            }

            List<string[]> rows =
            [
                ["Space", "Occupied Summer Hours", "Annual Night Occupied Hours"],
            ];

            foreach (IGrouping<string, TM59AssessmentReportCheck> group in GroupBySpace(tM59AssessmentReportChecks))
            {
                TM59AssessmentReportCheck check_Criterion1 = group.FirstOrDefault(x => x.Check == TM59AssessmentReport.Check_Criterion1);
                TM59AssessmentReportCheck check_Criterion2 = group.FirstOrDefault(x => x.Check == TM59AssessmentReport.Check_Criterion2);
                TM59AssessmentReportCheck any = check_Criterion1 ?? check_Criterion2 ?? group.First();

                rows.Add([any.SpaceName ?? "-", Number(check_Criterion1?.BasisHours), Number(check_Criterion2?.BasisHours)]);
            }

            AppendTable(stringBuilder, rows, rightAligned: [false, true, true]);
        }

        private static void AppendMechanicalVentilationSection(StringBuilder stringBuilder, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, Heading_MechanicalVentilation);

            if (tM59AssessmentReportChecks.Count == 0)
            {
                stringBuilder.AppendLine("No space was assessed against this criterion.");
                return;
            }

            List<string[]> rows =
            [
                ["Space", "Internal Condition", "TM59 Application", "Occupied Hours", "Actual", "Limit", "Margin", "Status"],
                .. tM59AssessmentReportChecks.Select(x => new[] { x.SpaceName ?? "-", x.InternalCondition ?? "-", x.Use ?? "-", Number(x.BasisHours), Number(x.Actual), Number(x.Limit), Margin(x.Margin), Display(x.ComplianceStatus) }),
            ];

            AppendTable(stringBuilder, rows, rightAligned: [false, false, false, true, true, true, true, false]);
        }

        private static void AppendRiskSection(StringBuilder stringBuilder, string heading, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, heading);

            if (tM59AssessmentReportChecks.Count == 0)
            {
                stringBuilder.AppendLine("No space was assessed against this criterion.");
                return;
            }

            List<string[]> rows =
            [
                ["Space", "Internal Condition", "Annual Hours", "Actual", "Limit", "Margin", "Risk"],
                .. tM59AssessmentReportChecks.Select(x => new[] { x.SpaceName ?? "-", x.InternalCondition ?? "-", Number(x.BasisHours), Number(x.Actual), Number(x.Limit), Margin(x.Margin), Display(x.RiskStatus) }),
            ];

            AppendTable(stringBuilder, rows, rightAligned: [false, false, true, true, true, true, false]);
        }

        /// <summary>
        /// Groups by <see cref="TM59AssessmentReportCheck.Reference"/> - the stable identity every row from
        /// a real assessment carries - preserving first-seen order so the table reads in the same order the
        /// assessment produced its results.
        /// </summary>
        private static IEnumerable<IGrouping<string, TM59AssessmentReportCheck>> GroupBySpace(List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            return tM59AssessmentReportChecks.GroupBy(x => x.Reference);
        }

        private static string CriterionCell(TM59AssessmentReportCheck check)
        {
            if (check == null || check.ComplianceStatus == TM59ComplianceStatus.NotApplicable)
            {
                return "N/A";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}/{1} ({2}) {3}", Number(check.Actual), Number(check.Limit), Margin(check.Margin), Display(check.ComplianceStatus));
        }

        /// <summary>
        /// The per-space Overall verdict: any failure fails the row; a row with nothing applicable (both
        /// criteria N/A) is <c>NotApplicable</c> rather than a vacuous pass. The same combining rule
        /// <see cref="TM59AssessmentReport"/> already applies across a whole section, applied here to the
        /// one or two checks a single space carries.
        /// </summary>
        private static TM59ComplianceStatus CombineForDisplay(TM59AssessmentReportCheck check_Criterion1, TM59AssessmentReportCheck check_Criterion2)
        {
            TM59ComplianceStatus[] statuses = new[] { check_Criterion1?.ComplianceStatus, check_Criterion2?.ComplianceStatus }
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .ToArray();

            if (statuses.Contains(TM59ComplianceStatus.Fail))
            {
                return TM59ComplianceStatus.Fail;
            }

            return statuses.Contains(TM59ComplianceStatus.Pass) ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.NotApplicable;
        }

        private static void AppendHeading(StringBuilder stringBuilder, string heading)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(heading);
            stringBuilder.AppendLine(new string('-', heading.Length));
        }

        /// <summary>
        /// Lays out any rectangular table of already-rendered cells - the header is <c>rows[0]</c>. Column
        /// widths are computed from the actual content, so every section shares the same automatic layout
        /// regardless of how many columns it has.
        /// </summary>
        private static void AppendTable(StringBuilder stringBuilder, List<string[]> rows, bool[] rightAligned)
        {
            int[] widths = new int[rows[0].Length];
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = rows.Max(x => x[i].Length);
            }

            foreach (string[] row in rows)
            {
                List<string> cells = [];
                for (int i = 0; i < row.Length; i++)
                {
                    cells.Add(rightAligned[i] ? row[i].PadLeft(widths[i]) : row[i].PadRight(widths[i]));
                }

                stringBuilder.AppendLine(string.Join("  ", cells).TrimEnd());
            }
        }

        private static string Number(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "-";
        }

        /// <summary>Signed, so remaining allowance and an exceeded threshold are distinguishable at a glance.</summary>
        private static string Margin(int? value)
        {
            if (!value.HasValue)
            {
                return "-";
            }

            return value.Value > 0 ? string.Format(CultureInfo.InvariantCulture, "+{0}", value.Value) : value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Display(TM59ComplianceStatus tM59ComplianceStatus)
        {
            return tM59ComplianceStatus switch
            {
                TM59ComplianceStatus.Pass => "PASS",
                TM59ComplianceStatus.Fail => "FAIL",
                TM59ComplianceStatus.NotApplicable => "N/A",
                _ => "-",
            };
        }

        private static string Display(TM59RiskStatus tM59RiskStatus)
        {
            return tM59RiskStatus switch
            {
                TM59RiskStatus.Acceptable => "ACCEPTABLE",
                TM59RiskStatus.SignificantRisk => "SIGNIFICANT RISK",
                _ => "-",
            };
        }

        /// <summary>
        /// Short enough to stay useful in a panel. States the engineering meaning of each column and each
        /// criterion; the historical SAM-vs-TAS validation evidence lives in
        /// <c>documentation/PartO-TAS-VALIDATION.md</c>, not here.
        /// </summary>
        private static string Legend()
        {
            return
                "Actual  Hours exceeding the relevant TM59 criterion.\r\n" +
                "Limit   Maximum permitted hours for the criterion.\r\n" +
                "Margin  Limit - Actual. Positive = allowance remaining, negative = threshold exceeded.\r\n" +
                "        Status is taken from the assessment's own verdict, never re-derived from Margin:\r\n" +
                "        Criterion 1, >26 C and >28 C are strict, so a zero margin is a failure, while\r\n" +
                "        Criterion 2 is inclusive, so a zero margin passes.\r\n" +
                "\r\n" +
                "Criterion 1     Adaptive thermal comfort criterion for applicable naturally ventilated\r\n" +
                "                occupied rooms during 1 May - 30 September, against the Occupied Summer\r\n" +
                "                Hours shown in the Assessment Hours table.\r\n" +
                "Criterion 2     Bedroom night-time overheating criterion. Applies to bedrooms only; every\r\n" +
                "                other space shows N/A. Assessed against Annual Night Occupied Hours.\r\n" +
                ">26 C hours     Fixed-temperature criterion for applicable mechanically ventilated occupied\r\n" +
                "                rooms, against Occupied Hours.\r\n" +
                ">28 C hours     Annual communal-corridor overheating-risk criterion, against the annual\r\n" +
                "                series length. A significant-risk result does not change the occupied-space\r\n" +
                "                TM59 compliance result.\r\n" +
                "\r\n" +
                "ComplianceStatus\r\n" +
                "    PASS  the applicable criterion was satisfied.\r\n" +
                "    FAIL  the applicable criterion was exceeded.\r\n" +
                "    N/A   the criterion is not required of that space.\r\n" +
                "RiskStatus\r\n" +
                "    ACCEPTABLE       the >28 C reference threshold was not exceeded.\r\n" +
                "    SIGNIFICANT RISK the threshold was exceeded. Highlight for design review. This is NOT\r\n" +
                "                     an occupied-space compliance failure and never changes the TM59\r\n" +
                "                     occupied-space assessment above.\r\n";
        }
    }
}
