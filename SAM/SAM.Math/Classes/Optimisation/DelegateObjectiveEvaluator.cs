// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Threading;

namespace SAM.Math
{
    /// <summary>An <see cref="IObjectiveEvaluator"/> backed by a delegate.</summary>
    public sealed class DelegateObjectiveEvaluator : IObjectiveEvaluator
    {
        private readonly Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate;

        public DelegateObjectiveEvaluator(Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate)
        {
            this.evaluate = evaluate ?? throw new ArgumentNullException(nameof(evaluate));
        }

        public ObjectiveEvaluation Evaluate(ObjectiveEvaluationRequest request, CancellationToken cancellationToken)
        {
            return evaluate(request, cancellationToken);
        }
    }
}
