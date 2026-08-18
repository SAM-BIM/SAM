// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical
{
    /// <summary>
    /// One TM59 criterion applied to one space, as a report row: what was counted, what it was allowed to be,
    /// and what that means.
    /// <para>
    /// <b>A view, never a second assessment.</b> Every value here is read off a <c>TMResult</c> the
    /// assessment already produced. In particular <see cref="ComplianceStatus"/> and
    /// <see cref="RiskStatus"/> come from the result's own verdict - <c>Pass</c>, or
    /// <c>TM59NaturalVentilationBedroomResult.Criterion2</c> - and are <b>not</b> derived from
    /// <see cref="Margin"/>. The two would disagree: the comfort-range, &gt;26 °C and &gt;28 °C criteria are
    /// strict (a zero margin is a failure) while the bedroom night-time criterion is inclusive (a zero margin
    /// passes), and re-deriving the verdict from the arithmetic would quietly overrule the calculation for
    /// every space sitting exactly on its limit.
    /// </para>
    /// </summary>
    public class TM59AssessmentReportCheck
    {
        internal TM59AssessmentReportCheck(string spaceName, string use, string check, int? actual, int? limit, TM59ComplianceStatus tM59ComplianceStatus, TM59RiskStatus tM59RiskStatus = TM59RiskStatus.Undefined)
        {
            SpaceName = spaceName;
            Use = use;
            Check = check;
            Actual = actual;
            Limit = limit;
            ComplianceStatus = tM59ComplianceStatus;
            RiskStatus = tM59RiskStatus;
        }

        /// <summary>The simulated space the result was produced for.</summary>
        public string SpaceName { get; }

        /// <summary>
        /// The TM59 space applications the assessment read for this space - <c>Sleeping</c>, <c>Living</c>,
        /// <c>Cooking</c>, or none. Null where the result carries no applications at all.
        /// </summary>
        public string Use { get; }

        /// <summary>Which criterion this row states.</summary>
        public string Check { get; }

        /// <summary>
        /// What the assessment counted. Null where the criterion does not apply, or where the result could
        /// produce no count because its hourly series could not be read.
        /// </summary>
        public int? Actual { get; }

        /// <summary>The number of hours the criterion allows, as the assessment itself derived it.</summary>
        public int? Limit { get; }

        /// <summary>
        /// <see cref="Limit"/> - <see cref="Actual"/>: positive is allowance remaining, negative is the
        /// threshold exceeded. Null where either side is unknown.
        /// </summary>
        public int? Margin => Actual.HasValue && Limit.HasValue ? Limit.Value - Actual.Value : (int?)null;

        /// <summary>
        /// The occupied-space verdict. <c>NotApplicable</c> where the criterion is not required of this
        /// space, and on every corridor row - a corridor is reported through <see cref="RiskStatus"/>.
        /// </summary>
        public TM59ComplianceStatus ComplianceStatus { get; }

        /// <summary>The corridor verdict, and <c>Undefined</c> on every occupied-space row.</summary>
        public TM59RiskStatus RiskStatus { get; }
    }
}
