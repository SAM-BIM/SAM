// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    /// <summary>
    /// A <see cref="TM59AssessmentResult"/> arranged for a person to check: one row per criterion per space,
    /// carrying what was counted, what the criterion allowed, the margin between them, and the verdict.
    /// <para>
    /// <b>A view over the assessment, not a second assessment.</b> Nothing here counts an hour, derives a
    /// threshold or decides a pass. Every number is read off the <c>TMResult</c> objects the calculation
    /// produced, and every verdict is that result's own - which is why building a report cannot change what
    /// the assessment said.
    /// </para>
    /// <para>
    /// <b>Compliance and risk are kept apart on purpose.</b> The occupied spaces are judged Pass/Fail against
    /// the criterion that applies to them; <see cref="TM59AssessmentResult.CorridorResults"/> is reported as
    /// <see cref="TM59RiskStatus"/> instead, and exceeding its threshold never turns
    /// <see cref="OccupiedSpaceComplianceStatus"/> into a failure. That threshold is a reporting reference for
    /// design review, and stating it as a regulatory failure would claim an outcome TM59 does not reach.
    /// </para>
    /// <para>
    /// <b>"Corridor" is this assessment's bucket name, not a proven physical identification.</b>
    /// <c>TMOverheatingCalculator.Calculate_TM59</c> routes a space here when it carries no TM59 space
    /// application at all, or when its ventilation strategy is the closed vocabulary's <c>"UV"</c> - which the
    /// shipped capability index describes as "Unconditioned", not "communal corridor". A bathroom, hall,
    /// ensuite or plant room reaches this bucket the same way a real corridor does, and nothing on the
    /// resulting <c>TM59CorridorResult</c> records which reason applied - so this report cannot, and does not,
    /// claim any row here is a confirmed communal corridor. See <see cref="TM59AssessmentReportFormatter.Note_CorridorBucket"/>.
    /// </para>
    /// <para>
    /// <b>What this report does not say.</b> It is an assessment of simulated temperatures. It cannot show
    /// that the Approved Document O modelling assumptions behind the simulation were applied, so it never
    /// states Part O compliance - only the TM59 outcome.
    /// </para>
    /// <para>
    /// Structured rather than pre-rendered so a CSV, JSON or PDF view can be added beside
    /// <see cref="TM59AssessmentReportFormatter"/> without any of them restating the assessment.
    /// </para>
    /// </summary>
    public class TM59AssessmentReport
    {
        /// <summary>The naturally ventilated day criterion - TM52 adaptive comfort, as TM59 applies it.</summary>
        public const string Check_Criterion1 = "Criterion 1";

        /// <summary>The bedroom night-time criterion.</summary>
        public const string Check_Criterion2 = "Criterion 2";

        /// <summary>The mechanically ventilated fixed-temperature criterion.</summary>
        public const string Check_HoursExceeding26 = ">26 C hours";

        /// <summary>The communal-corridor reference threshold.</summary>
        public const string Check_HoursExceeding28 = ">28 C hours";

        private const string reason_NoResult = "produced no TM59 result - either no criterion was settled for it, or its hourly series could not be read";

        /// <param name="tM59AssessmentResult">The assessment to report on. Its numbers and verdicts are read, never recomputed.</param>
        /// <param name="source">Where the results came from, for the report header - typically the TSD path.</param>
        public TM59AssessmentReport(TM59AssessmentResult tM59AssessmentResult, string source = null)
            : this(tM59AssessmentResult?.Spaces, tM59AssessmentResult?.MechanicalVentilationResults, tM59AssessmentResult?.NaturalVentilationResults, tM59AssessmentResult?.CorridorResults, tM59AssessmentResult?.VentilationStrategyRefusals, source)
        {
        }

        /// <summary>
        /// The same report from the four lists directly, which is the shape a caller holding Grasshopper
        /// outputs already has - and the shape that can be exercised without running a simulation.
        /// </summary>
        public TM59AssessmentReport(
            IEnumerable<Space> spaces,
            IEnumerable<TMResult> tMResults_MechanicalVentilation,
            IEnumerable<TMResult> tMResults_NaturalVentilation,
            IEnumerable<TMResult> tMResults_Corridor,
            IEnumerable<string> refusals = null,
            string source = null)
        {
            Source = source;

            List<TMResult> tMResults_Mechanical = Clean(tMResults_MechanicalVentilation);
            List<TMResult> tMResults_Natural = Clean(tMResults_NaturalVentilation);
            List<TMResult> tMResults_Corridors = Clean(tMResults_Corridor);

            TM52BuildingCategory = tMResults_Natural.Concat(tMResults_Mechanical).Concat(tMResults_Corridors).FirstOrDefault()?.TM52BuildingCategory ?? TM52BuildingCategory.Undefined;

            NaturalVentilationChecks = [.. tMResults_Natural.SelectMany(NaturalVentilationChecksFor)];
            MechanicalVentilationChecks = [.. tMResults_Mechanical.Select(MechanicalVentilationCheckFor)];
            CorridorChecks = [.. tMResults_Corridors.Select(CorridorCheckFor)];

            NaturalVentilationComplianceStatus = Combine(NaturalVentilationChecks);
            MechanicalVentilationComplianceStatus = Combine(MechanicalVentilationChecks);
            OccupiedSpaceComplianceStatus = Combine([.. NaturalVentilationChecks, .. MechanicalVentilationChecks]);

            //Any corridor over its threshold makes the whole building's corridor risk significant - and that is
            //as far as it travels. It is deliberately not folded into OccupiedSpaceComplianceStatus above.
            CorridorRiskStatus = CorridorChecks.Count == 0
                ? TM59RiskStatus.Undefined
                : CorridorChecks.Exists(x => x.RiskStatus == TM59RiskStatus.SignificantRisk) ? TM59RiskStatus.SignificantRisk : TM59RiskStatus.Acceptable;

            //A space the assessment covered but produced no result for is named, never quietly absent. The
            //refusals the assessment itself recorded say why for the spaces a scenario left unsettled; a space
            //whose hourly series were unreadable is silent at source, so it gets the combined sentence.
            HashSet<string> references = [.. tMResults_Natural.Concat(tMResults_Mechanical).Concat(tMResults_Corridors).Select(x => x.Reference).Where(x => !string.IsNullOrWhiteSpace(x))];

            UnassessedSpaces =
            [
                .. (spaces ?? []).Where(x => x != null && !references.Contains(x.Guid.ToString())).Select(x => string.Format("Space '{0}' {1}.", x.Name, reason_NoResult)),
                .. (refusals ?? []).Where(x => !string.IsNullOrWhiteSpace(x)),
            ];
        }

        /// <summary>Where the results came from, for the header. Null where the caller stated none.</summary>
        public string Source { get; }

        /// <summary>The category the comfort limits were derived for, as the results themselves report it.</summary>
        public TM52BuildingCategory TM52BuildingCategory { get; }

        /// <summary>
        /// Criterion 1 for every naturally ventilated space, and Criterion 2 alongside it - satisfied,
        /// exceeded, or not applicable where the space is not a bedroom.
        /// </summary>
        public List<TM59AssessmentReportCheck> NaturalVentilationChecks { get; }

        /// <summary>The fixed-temperature criterion for every mechanically ventilated space.</summary>
        public List<TM59AssessmentReportCheck> MechanicalVentilationChecks { get; }

        /// <summary>
        /// The full-year &gt;28 °C threshold, for every space the assessment put in the corridor-style bucket -
        /// which is not the same as every communal corridor, and not only communal corridors. Every row carries
        /// <c>ComplianceStatus.NotApplicable</c> and a real <c>RiskStatus</c>.
        /// </summary>
        public List<TM59AssessmentReportCheck> CorridorChecks { get; }

        /// <summary>
        /// Spaces the assessment covered without producing a result, one sentence each, followed by the
        /// assessment's own refusals. Empty where every space produced a result.
        /// </summary>
        public List<string> UnassessedSpaces { get; }

        /// <summary>Every applicable natural-ventilation criterion, combined.</summary>
        public TM59ComplianceStatus NaturalVentilationComplianceStatus { get; }

        /// <summary>Every applicable mechanical-ventilation criterion, combined.</summary>
        public TM59ComplianceStatus MechanicalVentilationComplianceStatus { get; }

        /// <summary>
        /// The TM59 outcome for the occupied spaces: every applicable natural- and mechanical-ventilation
        /// criterion combined. <b>The corridor takes no part in this</b> - see
        /// <see cref="CorridorRiskStatus"/>.
        /// </summary>
        public TM59ComplianceStatus OccupiedSpaceComplianceStatus { get; }

        /// <summary>
        /// <c>SignificantRisk</c> where any corridor exceeds its threshold, and <c>Undefined</c> where no
        /// corridor was assessed.
        /// </summary>
        public TM59RiskStatus CorridorRiskStatus { get; }

        /// <summary>The multiline text report, for a Grasshopper panel.</summary>
        public override string ToString()
        {
            return TM59AssessmentReportFormatter.Text(this);
        }

        private static List<TMResult> Clean(IEnumerable<TMResult> tMResults)
        {
            return [.. (tMResults ?? []).Where(x => x != null)];
        }

        /// <summary>
        /// Criterion 1, and Criterion 2 beside it on every row - a bedroom's verdict where it is one, and
        /// <c>NotApplicable</c> where it is not. A non-bedroom is stated rather than left off the report, so
        /// a reader can see the criterion was considered and not required.
        /// </summary>
        private static IEnumerable<TM59AssessmentReportCheck> NaturalVentilationChecksFor(TMResult tMResult)
        {
            string use = Use(tMResult);

            //The extended and plain results are sibling branches, not parent and child, so both are read
            //explicitly - and the bedroom variant is tested before its non-bedroom parent on each branch,
            //since a bedroom result also matches the parent type.
            //
            //Limit is deliberately the SUMMER basis (MaxExceedableSummerHours / GetSummerMaxExceedableHours),
            //not the base type's annual MaxExceedableHours - TAS's own "Max. Exceedable Hours" column is
            //paired with "Occupied Summer Hours", not the whole-year count, and the two differ by roughly the
            //ratio of summer to annual occupied hours (found comparing this report's Studio 1_0 row against
            //the real Flat1 BasePassive TAS report: annual gives 262, TAS's actual figure is 110).
            int? actual_Criterion1 = tMResult is TM59NaturalVentilationExtendedResult extended
                ? Count(extended.GetOccupiedHoursExceedingComfortRange())
                : Count((tMResult as TM59NaturalVentilationResult)?.HoursExceedingComfortRange);

            int? limit_Criterion1 = tMResult is TM59NaturalVentilationExtendedResult extended_ForLimit
                ? Count(extended_ForLimit.GetSummerMaxExceedableHours())
                : Count((tMResult as TM59NaturalVentilationResult)?.MaxExceedableSummerHours);

            yield return new TM59AssessmentReportCheck(tMResult.Name, use, Check_Criterion1, actual_Criterion1, limit_Criterion1, tMResult.Pass ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail);

            if (tMResult is TM59NaturalVentilationBedroomExtendedResult bedroom_Extended)
            {
                yield return new TM59AssessmentReportCheck(tMResult.Name, use, Check_Criterion2, Count(bedroom_Extended.GetNightHoursNumberExceeding26()), Count(bedroom_Extended.GetAnnualMaxExceedableNightHours()), bedroom_Extended.Criterion2 ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail);
            }
            else if (tMResult is TM59NaturalVentilationBedroomResult bedroom)
            {
                yield return new TM59AssessmentReportCheck(tMResult.Name, use, Check_Criterion2, Count(bedroom.NightHoursNumberExceeding26), Count(bedroom.MaxExceedableNightHours), bedroom.Criterion2 ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail);
            }
            else
            {
                yield return new TM59AssessmentReportCheck(tMResult.Name, use, Check_Criterion2, null, null, TM59ComplianceStatus.NotApplicable);
            }
        }

        private static TM59AssessmentReportCheck MechanicalVentilationCheckFor(TMResult tMResult)
        {
            int? actual = tMResult is TM59MechanicalVentilationExtendedResult extended
                ? Count(extended.GetHoursNumberExceeding26())
                : Count((tMResult as TM59MechanicalVentilationResult)?.HoursExceeding26);

            return new TM59AssessmentReportCheck(tMResult.Name, Use(tMResult), Check_HoursExceeding26, actual, Count(tMResult.MaxExceedableHours), tMResult.Pass ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.Fail);
        }

        /// <summary>
        /// The full-year &gt;28 °C row: <c>NotApplicable</c> as a compliance status - both because the threshold
        /// is not a mandatory occupied-space test, and because the space is not positively identified as a
        /// communal corridor - with the real answer in the risk status beside it.
        /// </summary>
        private static TM59AssessmentReportCheck CorridorCheckFor(TMResult tMResult)
        {
            int? actual = tMResult is TM59CorridorExtendedResult extended
                ? Count(extended.GetHoursNumberExceeding28())
                : Count((tMResult as TM59CorridorResult)?.HoursExceeding28);

            return new TM59AssessmentReportCheck(tMResult.Name, Use(tMResult), Check_HoursExceeding28, actual, Count(tMResult.MaxExceedableHours), TM59ComplianceStatus.NotApplicable, tMResult.Pass ? TM59RiskStatus.Acceptable : TM59RiskStatus.SignificantRisk);
        }

        /// <summary>
        /// A fixed order rather than the set's own, so two runs of the same model read identically.
        /// </summary>
        private static string Use(TMResult tMResult)
        {
            HashSet<TM59SpaceApplication> tM59SpaceApplications = (tMResult as TM59Result)?.TM59SpaceApplications ?? (tMResult as TM59ExtendedResult)?.TM59SpaceApplications;
            if (tM59SpaceApplications == null)
            {
                return null;
            }

            List<string> result = [];
            foreach (TM59SpaceApplication tM59SpaceApplication in new[] { TM59SpaceApplication.Sleeping, TM59SpaceApplication.Living, TM59SpaceApplication.Cooking })
            {
                if (tM59SpaceApplications.Contains(tM59SpaceApplication))
                {
                    result.Add(tM59SpaceApplication.ToString());
                }
            }

            return result.Count == 0 ? null : string.Join(", ", result);
        }

        /// <summary>
        /// -1 is the extended results' sentinel for "no readable series to count from" (see
        /// <c>TM59MechanicalVentilationExtendedResult.GetHoursNumberExceeding26</c>), and <c>int.MinValue</c>
        /// is the plain results' unset marker. Both are reported as absent rather than as a count, so a
        /// fabricated margin cannot be computed from them.
        /// </summary>
        private static int? Count(int? value)
        {
            return value.HasValue && value.Value >= 0 && value.Value != int.MinValue ? value : null;
        }

        /// <summary>
        /// Any failure fails the group; a group with nothing applicable to it is <c>NotApplicable</c> rather
        /// than a vacuous pass.
        /// </summary>
        private static TM59ComplianceStatus Combine(List<TM59AssessmentReportCheck> tM59AssessmentReportChecks)
        {
            if (tM59AssessmentReportChecks.Exists(x => x.ComplianceStatus == TM59ComplianceStatus.Fail))
            {
                return TM59ComplianceStatus.Fail;
            }

            return tM59AssessmentReportChecks.Exists(x => x.ComplianceStatus == TM59ComplianceStatus.Pass) ? TM59ComplianceStatus.Pass : TM59ComplianceStatus.NotApplicable;
        }
    }
}
