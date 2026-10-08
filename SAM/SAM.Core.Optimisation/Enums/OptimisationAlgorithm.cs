// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>The search method. JSON: "golden-section", "hooke-jeeves".</summary>
    public enum OptimisationAlgorithm
    {
        /// <summary>A line search on exactly one design variable between its bounds.</summary>
        GoldenSection,

        /// <summary>The Hooke-Jeeves generalised pattern search on one or more design variables.</summary>
        HookeJeeves,
    }
}
