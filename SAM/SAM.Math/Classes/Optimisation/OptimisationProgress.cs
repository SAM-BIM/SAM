// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>Progress notification, sent once for every trace entry as it is recorded.</summary>
    public sealed class OptimisationProgress
    {
        internal OptimisationProgress(OptimisationTraceEntry entry, bool mainIteration, int simulations, int maximumSimulations)
        {
            Entry = entry;
            MainIteration = mainIteration;
            Simulations = simulations;
            MaximumSimulations = maximumSimulations;
        }

        public OptimisationTraceEntry Entry { get; }

        /// <summary>True when <see cref="Entry"/> was added to <see cref="OptimisationResult.MainIterations"/>, false for <see cref="OptimisationResult.Entries"/>.</summary>
        public bool MainIteration { get; }

        /// <summary>Simulations counted so far.</summary>
        public int Simulations { get; }

        public int MaximumSimulations { get; }
    }
}
