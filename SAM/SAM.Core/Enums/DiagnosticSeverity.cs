// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Core
{
    /// <summary>
    /// Structured severity of a <see cref="Diagnostic"/>. Never inferred from formatted text.
    /// </summary>
    [Description("Diagnostic Severity")]
    public enum DiagnosticSeverity
    {
        /// <summary>What was done.</summary>
        [Description("Info")] Info = 0,
        /// <summary>Input that was skipped or changed.</summary>
        [Description("Warning")] Warning = 1,
        /// <summary>A run that could not complete.</summary>
        [Description("Error")] Error = 2
    }
}
