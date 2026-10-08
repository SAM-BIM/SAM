// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;

namespace SAM.Math
{
    /// <summary>
    /// Exhaustive search over a choice: evaluates every whole number from Minimum to Maximum of exactly one parameter
    /// (the options, numbered in order), one at a time, in ascending order. Initial and Step are ignored. There is no
    /// coordinate rounding. A SAM addition with no GenOpt parity claim; it shares the evaluation layer (cache,
    /// numbering, retry-once rule, cancellation, trace and progress) with the other methods.
    /// <list type="bullet">
    /// <item>Every option is one <see cref="OptimisationEvent.OptionEvaluated"/> entry in
    /// <see cref="OptimisationResult.Entries"/>, in option order, so the result is the whole table.</item>
    /// <item>After the last option the lowest objective is reported as <see cref="OptimisationEvent.MinimumPoint"/>
    /// (<see cref="OptimisationResult.Minimum"/>, also the only main iteration). A tie goes to the lower option number;
    /// NaN never wins; when no objective is a number no minimum is reported.</item>
    /// <item>A failed evaluation follows the shared rule: the first option is not retried, a later one is retried once,
    /// and a second failure stops the run (<see cref="OptimisationOutcome.EvaluationFailed"/>) with the options evaluated
    /// so far and no minimum. An option is never skipped: a best option is only reported once every option has a
    /// result.</item>
    /// <item>The run needs one simulation per option: more options than <see cref="Optimiser.MaximumSimulations"/> is
    /// invalid input, so the run never stops part-way at the simulation limit.</item>
    /// </list>
    /// </summary>
    public sealed class TryEveryOption : Optimiser
    {
        /// <summary>The largest magnitude of an option number: every whole number up to it is exact in a double.</summary>
        private const double largestOption = 9007199254740992; // 2^53

        internal override void Validate(OptimisationProblem problem)
        {
            if (problem.Parameters.Count != 1)
            {
                throw new ArgumentException("Try every option needs exactly one parameter.", nameof(problem));
            }

            OptimisationParameter parameter = problem.Parameters[0];
            if (!Whole(parameter.Minimum) || !Whole(parameter.Maximum))
            {
                throw new ArgumentException("Try every option needs whole-number Minimum and Maximum (the first and last option).", nameof(problem));
            }

            if (parameter.Minimum > parameter.Maximum)
            {
                throw new ArgumentException("Try every option needs Minimum <= Maximum.", nameof(problem));
            }

            double count = (parameter.Maximum - parameter.Minimum) + 1;
            if (count > MaximumSimulations)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Try every option needs one simulation per option: {0} options, but MaximumSimulations is {1}.", count, MaximumSimulations));
            }
        }

        internal override OptimisationOutcome Execute(EvaluationContext context)
        {
            double first = context.Lower[0];
            int count = (int)((context.Upper[0] - first) + 1);
            for (int i = 0; i < count; i++)
            {
                EvaluationPoint point = context.Evaluate(context.CreatePoint(new[] { first + i }, 0))[0];
                point.SetEvent(OptimisationEvent.OptionEvaluated);
                context.Report(point, false, false);
            }

            context.ReportLowest(OptimisationEvent.MinimumPoint);
            return OptimisationOutcome.Success;
        }

        private static bool Whole(double value)
        {
            return !double.IsNaN(value) && System.Math.Abs(value) <= largestOption && value == System.Math.Floor(value);
        }
    }
}
