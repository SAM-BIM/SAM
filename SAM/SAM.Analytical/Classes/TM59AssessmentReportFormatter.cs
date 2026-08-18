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
        public const string Heading = "CIBSE TM59 / PART O OVERHEATING VERIFICATION";

        public const string Heading_NaturalVentilation = "NATURAL VENTILATION";
        public const string Heading_MechanicalVentilation = "MECHANICAL VENTILATION";

        /// <summary>
        /// <b>Deliberately not "COMMUNAL CORRIDORS".</b> <c>TMOverheatingCalculator.Calculate_TM59</c> puts a
        /// space here when it has no TM59 space application at all, OR when its stated ventilation strategy is
        /// <c>"UV"</c> - which the shipped <c>SAM_Systems</c> capability index itself describes as
        /// "Unconditioned. Provides nothing", not "communal corridor" - so a bathroom, hall, ensuite or plant
        /// room lands here exactly the way a real corridor does, and nothing on the resulting
        /// <c>TM59CorridorResult</c> records which reason applied. Calling this section "communal corridors"
        /// would assert an identification the domain does not make.
        /// </summary>
        public const string Heading_Corridors = "FULL-YEAR >28 C / CORRIDOR-STYLE RESULTS";

        /// <summary>The disclaimer <see cref="Heading_Corridors"/>'s ambiguity requires, shown beside the table itself and not only in the legend.</summary>
        public const string Note_CorridorBucket =
            "These results use the existing SAM full-year >28 C assessment bucket. A result in this section\r\n" +
            "does not by itself prove that the space is a TM59 communal corridor - the same bucket also holds\r\n" +
            "any space with no assessed occupied-space use. Confirm corridor applicability from the model or\r\n" +
            "the assessment scope before treating a row here as a corridor.";

        public const string Heading_Unassessed = "SPACES NOT ASSESSED";
        public const string Heading_Summary = "SUMMARY";
        public const string Heading_Legend = "LEGEND";

        /// <summary>
        /// The one sentence the whole separation exists to allow. Passing temperatures do not establish that
        /// the Approved Document O modelling assumptions behind the simulation were applied, so no Part O
        /// compliance is claimed anywhere in this report.
        /// </summary>
        public const string Caveat =
            "This is a CIBSE TM59 assessment of simulated temperatures. It is not a statement of Approved\r\n" +
            "Document O compliance: these results alone cannot show that every Part O modelling assumption\r\n" +
            "was applied to the simulation they came from.";

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
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(string.Format("OCCUPIED SPACE ASSESSMENT: {0}", Display(tM59AssessmentReport.OccupiedSpaceComplianceStatus)));
            stringBuilder.AppendLine(string.Format("FULL-YEAR >28 C RISK (CORRIDOR-STYLE BUCKET): {0}", Display(tM59AssessmentReport.CorridorRiskStatus)));

            AppendComplianceSection(stringBuilder, Heading_NaturalVentilation, tM59AssessmentReport.NaturalVentilationChecks);
            AppendComplianceSection(stringBuilder, Heading_MechanicalVentilation, tM59AssessmentReport.MechanicalVentilationChecks);
            AppendRiskSection(stringBuilder, Heading_Corridors, tM59AssessmentReport.CorridorChecks);

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
            stringBuilder.AppendLine(string.Format("Natural ventilation:            {0}", Display(tM59AssessmentReport.NaturalVentilationComplianceStatus)));
            stringBuilder.AppendLine(string.Format("Mechanical ventilation:         {0}", Display(tM59AssessmentReport.MechanicalVentilationComplianceStatus)));
            stringBuilder.AppendLine(string.Format("TM59 occupied-space assessment: {0}", Display(tM59AssessmentReport.OccupiedSpaceComplianceStatus)));
            stringBuilder.AppendLine(string.Format("Full-year >28 C risk (corridor-style bucket, not a proven communal corridor): {0}", Display(tM59AssessmentReport.CorridorRiskStatus)));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(Caveat);

            AppendHeading(stringBuilder, Heading_Legend);
            stringBuilder.Append(Legend());

            return stringBuilder.ToString();
        }

        private static void AppendComplianceSection(StringBuilder stringBuilder, string heading, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, heading);
            AppendTable(stringBuilder, tM59AssessmentReportChecks, "Status", x => Display(x.ComplianceStatus));
        }

        private static void AppendRiskSection(StringBuilder stringBuilder, string heading, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            AppendHeading(stringBuilder, heading);
            stringBuilder.AppendLine(Note_CorridorBucket);
            stringBuilder.AppendLine();
            AppendTable(stringBuilder, tM59AssessmentReportChecks, "Risk", x => Display(x.RiskStatus));
        }

        private static void AppendHeading(StringBuilder stringBuilder, string heading)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(heading);
            stringBuilder.AppendLine(new string('-', heading.Length));
        }

        private static void AppendTable(StringBuilder stringBuilder, List<TM59AssessmentReportCheck> tM59AssessmentReportChecks, string heading_Verdict, System.Func<TM59AssessmentReportCheck, string> verdict)
        {
            if (tM59AssessmentReportChecks.Count == 0)
            {
                stringBuilder.AppendLine("No space was assessed against this criterion.");
                return;
            }

            List<string[]> rows =
            [
                ["Space", "Use", "Check", "Actual", "Limit", "Margin", heading_Verdict],
                .. tM59AssessmentReportChecks.Select(x => new string[] { x.SpaceName ?? "-", x.Use ?? "-", x.Check, Number(x.Actual), Number(x.Limit), Margin(x.Margin), verdict(x) }),
            ];

            //Right-aligned from the Actual column on, so the three counts and the margin line up under each
            //other and a negative margin is visible at a glance down the column.
            bool[] rightAligned = [false, false, false, true, true, true, false];

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
        /// Short enough to stay useful in a panel. It explains the engineering meaning of each column and
        /// each criterion as <b>this</b> implementation applies them - including the two places where the
        /// implementation is easy to misread - and does not reproduce the publication.
        /// </summary>
        private static string Legend()
        {
            return
                "Actual  Hours the assessment counted for that criterion.\r\n" +
                "Limit   Hours the criterion allows, as the assessment itself derived them.\r\n" +
                "Margin  Limit - Actual. Positive = allowance remaining. Negative = threshold exceeded.\r\n" +
                "        Status is taken from the assessment's own verdict, never re-derived from Margin:\r\n" +
                "        Criterion 1, >26 C and >28 C are strict, so a zero margin is a failure, while\r\n" +
                "        Criterion 2 is inclusive, so a zero margin passes.\r\n" +
                "\r\n" +
                "Criterion 1     Adaptive thermal comfort. Occupied hours (1 May - 30 Sep) whose operative\r\n" +
                "                temperature exceeds the TM52 comfort range for the stated building category,\r\n" +
                "                against 3% of the space's summer occupied hours - the same \"Occupied Summer\r\n" +
                "                Hours\" / \"Max. Exceedable Hours\" basis TAS's own TM59 report states this\r\n" +
                "                criterion on. Applies to naturally ventilated spaces.\r\n" +
                "Criterion 2     Bedroom night-time overheating. Night occupied hours (22:00-07:00) above 26 C,\r\n" +
                "                against 1% of the annual night occupied hours. Bedrooms only; every other\r\n" +
                "                space shows N/A.\r\n" +
                ">26 C hours     Fixed-temperature check for mechanically ventilated and restricted-opening\r\n" +
                "                spaces: occupied hours above 26 C against 3% of the occupied hours.\r\n" +
                ">28 C hours     Full-year overheating-risk indicator: hours above 28 C against 3% of the hours in\r\n" +
                "                the series. Counted over the whole year regardless of occupancy, which is why\r\n" +
                "                it can report hours for a room TAS's own canned report leaves out. Reported for\r\n" +
                "                every space with no assessed occupied-space use - a communal corridor and an\r\n" +
                "                unassessed bathroom, hall or ensuite reach it the same way, and this assessment\r\n" +
                "                has nothing that tells the two apart.\r\n" +
                "\r\n" +
                "ComplianceStatus\r\n" +
                "    PASS  the applicable criterion was satisfied.\r\n" +
                "    FAIL  the applicable criterion was exceeded.\r\n" +
                "    N/A   the criterion is not required of that space, or the space could not be positively\r\n" +
                "          identified as a communal corridor - every row in the >28 C section is N/A for that\r\n" +
                "          second reason. Never read as a pass.\r\n" +
                "RiskStatus\r\n" +
                "    ACCEPTABLE       the >28 C reference threshold was not exceeded.\r\n" +
                "    SIGNIFICANT RISK the threshold was exceeded. Highlight for design review. This is NOT an\r\n" +
                "                     occupied-space compliance failure and never changes the TM59\r\n" +
                "                     occupied-space assessment above - and it is NOT, by itself, proof that the\r\n" +
                "                     space is a communal corridor.\r\n";
        }
    }
}
