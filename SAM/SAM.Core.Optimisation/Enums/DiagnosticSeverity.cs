// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>How serious an <see cref="OptimisationDiagnostic"/> is.</summary>
    public enum DiagnosticSeverity
    {
        /// <summary>For information; never stops anything.</summary>
        Info,

        /// <summary>Worth checking; never stops anything.</summary>
        Warning,

        /// <summary>The definition cannot be read, or cannot be run as it stands.</summary>
        Error,
    }
}
