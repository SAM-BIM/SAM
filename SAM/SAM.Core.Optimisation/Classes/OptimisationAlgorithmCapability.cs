// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>A search method an engine runs, and how many design variables it accepts.</summary>
    public sealed class OptimisationAlgorithmCapability
    {
        public OptimisationAlgorithmCapability(OptimisationAlgorithm algorithm, int minimumVariables, int? maximumVariables)
        {
            Algorithm = algorithm;
            MinimumVariables = minimumVariables;
            MaximumVariables = maximumVariables;
        }

        public OptimisationAlgorithm Algorithm { get; }

        /// <summary>The fewest design variables, at least 1.</summary>
        public int MinimumVariables { get; }

        /// <summary>The most design variables; null for no limit.</summary>
        public int? MaximumVariables { get; }
    }
}
