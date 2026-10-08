// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Math
{
    /// <summary>What a trace entry records. Each value corresponds to one GenOpt listing comment (spec §3, §4, §5).</summary>
    public enum OptimisationEvent
    {
        Undefined,

        /// <summary>The initial point ("Initial point.").</summary>
        InitialPoint,

        /// <summary>Hooke-Jeeves pattern point ("Exploration base, Delta = Δ."). <see cref="OptimisationTraceEntry.Delta"/> holds Δ.</summary>
        ExplorationBase,

        /// <summary>A coordinate trial reduced the cost ("Cost reduced     at xi+dxi.").</summary>
        CostReduced,

        /// <summary>A coordinate trial did not reduce the cost ("Cost not reduced at xi+dxi.").</summary>
        CostNotReduced,

        /// <summary>The iterate was updated from the local search ("Local search reduced cost.").</summary>
        LocalSearchReducedCost,

        /// <summary>The iterate was updated from the global (pattern) search ("Global search reduced cost.").</summary>
        GlobalSearchReducedCost,

        /// <summary>No reduction; the mesh is refined ("…Reduce step size to 'Δ'."). <see cref="OptimisationTraceEntry.Delta"/> holds the new Δ.</summary>
        StepSizeReduced,

        /// <summary>No reduction at the last mesh level ("…Maximum number of step reductions reached.").</summary>
        MaximumStepReductionsReached,

        /// <summary>The reported minimum after a successful pattern search ("Minimum point.").</summary>
        MinimumPoint,

        /// <summary>The reported minimum after the simulation limit ("Current lowest point.").</summary>
        CurrentLowestPoint,

        /// <summary>A golden-section evaluation ("Linesearch.").</summary>
        LineSearch,

        /// <summary>
        /// <see cref="TryEveryOption"/>: the evaluation of one option. <see cref="OptimisationTraceEntry.Coordinates"/>
        /// holds the option number. A SAM addition with no GenOpt listing comment.
        /// </summary>
        OptionEvaluated,
    }
}
