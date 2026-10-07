// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Threading;

namespace SAM.Math
{
    /// <summary>
    /// Evaluates the objective (and any recorded outputs) at one point. The kernel calls it sequentially, never
    /// for a cached or out-of-bounds point.
    /// <para>
    /// Return <see cref="ObjectiveEvaluation.Failure"/> for a failed simulation. Throwing an exception also counts
    /// as a failure, except an <see cref="System.OperationCanceledException"/> while
    /// the run's cancellation token is cancelled, which cancels the run.
    /// </para>
    /// </summary>
    public interface IObjectiveEvaluator
    {
        ObjectiveEvaluation Evaluate(ObjectiveEvaluationRequest request, CancellationToken cancellationToken);
    }
}
