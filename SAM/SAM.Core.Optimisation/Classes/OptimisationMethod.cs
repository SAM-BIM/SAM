// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The search method and its settings. A setting left null uses the engine's default for that method.
    /// </summary>
    public abstract class OptimisationMethod
    {
        public abstract OptimisationAlgorithm Algorithm { get; }

        /// <summary>A copy of this method and its settings.</summary>
        public abstract OptimisationMethod Clone();
    }
}
