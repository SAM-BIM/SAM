// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Threading;

namespace SAM.Math
{
    /// <summary>
    /// Base of the native optimisation kernel: <see cref="CoordinateSearch"/>, <see cref="HookeJeeves"/>,
    /// <see cref="GoldenSection"/> and <see cref="TryEveryOption"/>.
    /// <para>
    /// Behaviour-compatible with GenOpt 3.1.1. Implementation written independently from the behavioural
    /// specification; no GenOpt source code copied. Specification: documentation/GenOpt-3.1.1-Behaviour.md; licence
    /// and provenance details: THIRD_PARTY.md. <see cref="TryEveryOption"/> is a SAM addition with no GenOpt parity
    /// claim; it shares the same evaluation layer.
    /// </para>
    /// <para>
    /// The kernel implements that specification, not the textbook algorithms. For example, a pattern-search
    /// improvement found on the last permitted simulation is not recorded, and the result cache uses approximate
    /// keys. Evaluations run one at a time. Settings are read when <see cref="Run"/> starts. An optimiser instance
    /// holds no run state and can be reused.
    /// </para>
    /// </summary>
    public abstract class Optimiser
    {
        internal Optimiser()
        {
        }

        /// <summary>
        /// GenOpt "MaxIte": the upper limit on simulations (cache hits and out-of-bounds points are not counted).
        /// The default is 2000, the value Tas Generic Optimisation writes.
        /// </summary>
        public int MaximumSimulations { get; set; } = 2000;

        /// <summary>
        /// Runs the optimisation. Evaluation failures, the simulation limit, cancellation and the algorithms' own
        /// stops are all reported through <see cref="OptimisationResult.Outcome"/>, not thrown. Invalid input throws
        /// before the first evaluation.
        /// </summary>
        public OptimisationResult Run(OptimisationProblem problem, IObjectiveEvaluator evaluator, IProgress<OptimisationProgress> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (problem == null)
            {
                throw new ArgumentNullException(nameof(problem));
            }

            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            if (MaximumSimulations < 1)
            {
                throw new InvalidOperationException("MaximumSimulations must be at least 1.");
            }

            Validate(problem);

            EvaluationContext context = new EvaluationContext(problem, evaluator, MaximumSimulations, progress, cancellationToken);
            OptimisationOutcome outcome;
            try
            {
                outcome = Execute(context);
            }
            catch (OptimisationTermination termination)
            {
                outcome = termination.Outcome;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                outcome = OptimisationOutcome.Cancelled;
            }

            return context.ToResult(outcome);
        }

        /// <summary>Throws when the settings or the problem do not suit the algorithm.</summary>
        internal abstract void Validate(OptimisationProblem problem);

        internal abstract OptimisationOutcome Execute(EvaluationContext context);
    }
}
