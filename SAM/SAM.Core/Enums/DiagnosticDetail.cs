// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Core
{
    /// <summary>
    /// Verbosity of Info diagnostics. A higher level includes every lower level.
    /// Warnings and Errors are independent of the level and are never suppressed.
    /// </summary>
    [Description("Diagnostic Detail")]
    public enum DiagnosticDetail
    {
        /// <summary>Default. Totals and one-line outcomes only.</summary>
        [Description("Summary")] Summary = 0,
        /// <summary>Summary plus per-source outcomes.</summary>
        [Description("Standard")] Standard = 1,
        /// <summary>Standard plus engineering values and intermediate decisions.</summary>
        [Description("Detailed")] Detailed = 2
    }
}
