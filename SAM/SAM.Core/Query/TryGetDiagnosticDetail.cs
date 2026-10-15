// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    public static partial class Query
    {
        /// <summary>
        /// Converts an integer, as typed into a Grasshopper <c>diagnosticDetail_</c> input, to a <see cref="DiagnosticDetail"/>:
        /// 0 Summary, 1 Standard, 2 Detailed.
        /// </summary>
        /// <param name="value">Integer to convert.</param>
        /// <param name="diagnosticDetail">The level; <see cref="DiagnosticDetail.Summary"/> (the default) when <paramref name="value"/> is not a defined level.</param>
        /// <returns>True when <paramref name="value"/> is a defined level.</returns>
        public static bool TryGetDiagnosticDetail(int value, out DiagnosticDetail diagnosticDetail)
        {
            if (System.Enum.IsDefined(typeof(DiagnosticDetail), value))
            {
                diagnosticDetail = (DiagnosticDetail)value;
                return true;
            }

            diagnosticDetail = DiagnosticDetail.Summary;
            return false;
        }
    }
}
