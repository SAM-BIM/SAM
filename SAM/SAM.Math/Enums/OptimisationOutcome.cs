// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>How an optimisation run ended (documentation/GenOpt-3.1.1-Behaviour.md §6).</summary>
    public enum OptimisationOutcome
    {
        Undefined,

        /// <summary>Pattern search: the step reductions are exhausted. Golden section: the stopping criterion is met.</summary>
        Success,

        /// <summary>
        /// Pattern search: the simulation limit was reached. Golden section in absolute-difference mode: the reduction
        /// count ran out before the criterion was met.
        /// </summary>
        MaximumSimulationsReached,

        /// <summary>An evaluation failed in the first batch (no retry), or failed again on its single retry.</summary>
        EvaluationFailed,

        /// <summary>Golden section (not in absolute-difference mode): two consecutive equal function values.</summary>
        Nullspace,

        /// <summary>Pattern search: the initial point is outside the bounds.</summary>
        InitialPointInfeasible,

        /// <summary>Pattern search: an internal consistency check failed (the run stops with an error, as GenOpt does).</summary>
        AlgorithmError,

        /// <summary>The run was stopped through its cancellation token.</summary>
        Cancelled,
    }
}
