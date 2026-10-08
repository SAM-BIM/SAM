// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>Whether the objective output is made as small or as large as possible. JSON: "minimise", "maximise".</summary>
    public enum ObjectiveSense
    {
        Minimise,
        Maximise,
    }
}
