// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Math
{
    /// <summary>The structured result and full trace of one optimisation run.</summary>
    public sealed class OptimisationResult
    {
        internal OptimisationResult(
            OptimisationOutcome outcome,
            int simulations,
            int retries,
            IReadOnlyList<OptimisationTraceEntry> entries,
            IReadOnlyList<OptimisationTraceEntry> mainIterations,
            OptimisationTraceEntry minimum,
            GoldenSectionInterval interval,
            int failedSimulation,
            string failureMessage)
        {
            Outcome = outcome;
            Simulations = simulations;
            Retries = retries;
            Entries = entries;
            MainIterations = mainIterations;
            Minimum = minimum;
            Interval = interval;
            FailedSimulation = failedSimulation;
            FailureMessage = failureMessage;
        }

        public OptimisationOutcome Outcome { get; }

        /// <summary>
        /// Simulations counted, which is the number of distinct evaluations. Cache hits, out-of-bounds points and
        /// retries are not counted. A number already assigned to an evaluation that was cancelled is included.
        /// </summary>
        public int Simulations { get; }

        /// <summary>Number of retries performed after a failed evaluation.</summary>
        public int Retries { get; }

        /// <summary>Every reported entry, in order (GenOpt OutputListingAll), including the final minimum entry.</summary>
        public IReadOnlyList<OptimisationTraceEntry> Entries { get; }

        /// <summary>Main-iteration entries, in order (GenOpt OutputListingMain), including the final minimum entry.</summary>
        public IReadOnlyList<OptimisationTraceEntry> MainIterations { get; }

        /// <summary>
        /// The reported minimum ("Minimum point." / "Current lowest point."), or null. It is null for golden
        /// section, after a failure or cancellation, and when no main iteration was reported.
        /// </summary>
        public OptimisationTraceEntry Minimum { get; }

        /// <summary>Golden section only: the final uncertainty interval (also after a nullspace stop); otherwise null.</summary>
        public GoldenSectionInterval Interval { get; }

        /// <summary>The simulation whose failure stopped the run, or 0.</summary>
        public int FailedSimulation { get; }

        /// <summary>The evaluator's message for that failure, or null.</summary>
        public string FailureMessage { get; }
    }
}
