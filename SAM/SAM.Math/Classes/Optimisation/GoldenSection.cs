// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Math
{
    /// <summary>
    /// GenOpt "GoldenSection" line search on exactly one parameter between its finite bounds
    /// (documentation/GenOpt-3.1.1-Behaviour.md §4). Initial and Step are ignored. There is no coordinate rounding
    /// and no feasibility check. Every evaluation is a <see cref="OptimisationEvent.LineSearch"/> entry, and no
    /// main iterations are reported. The result carries the final <see cref="GoldenSectionInterval"/>.
    /// </summary>
    public sealed class GoldenSection : Optimiser
    {
        public GoldenSectionStoppingCriterion StoppingCriterion { get; set; } = GoldenSectionStoppingCriterion.MaximumSimulations;

        /// <summary>GenOpt "AbsDiffFunction" d (at least 0), used with <see cref="GoldenSectionStoppingCriterion.AbsoluteDifference"/>.</summary>
        public double AbsoluteDifference { get; set; }

        /// <summary>GenOpt "IntervalReduction" ρ (positive), used with <see cref="GoldenSectionStoppingCriterion.IntervalReduction"/>.</summary>
        public double IntervalReduction { get; set; }

        internal override void Validate(OptimisationProblem problem)
        {
            if (problem.Parameters.Count != 1)
            {
                throw new ArgumentException("Golden section needs exactly one parameter.", nameof(problem));
            }

            OptimisationParameter parameter = problem.Parameters[0];
            if (double.IsInfinity(parameter.Minimum) || double.IsInfinity(parameter.Maximum))
            {
                throw new ArgumentException("Golden section needs a finite Minimum and Maximum.", nameof(problem));
            }

            if (!(parameter.Minimum < parameter.Maximum))
            {
                throw new ArgumentException("Golden section needs Minimum < Maximum.", nameof(problem));
            }

            switch (StoppingCriterion)
            {
                case GoldenSectionStoppingCriterion.MaximumSimulations:
                    break;
                case GoldenSectionStoppingCriterion.AbsoluteDifference:
                    if (double.IsNaN(AbsoluteDifference) || AbsoluteDifference < 0 || double.IsInfinity(AbsoluteDifference))
                    {
                        throw new InvalidOperationException("AbsoluteDifference must be finite and not negative.");
                    }

                    break;
                case GoldenSectionStoppingCriterion.IntervalReduction:
                    if (double.IsNaN(IntervalReduction) || IntervalReduction <= 0 || double.IsInfinity(IntervalReduction))
                    {
                        throw new InvalidOperationException("IntervalReduction must be finite and positive.");
                    }

                    break;
                default:
                    throw new InvalidOperationException("Unknown stopping criterion.");
            }
        }

        /// <summary>GenOpt IntervalDivider with the golden ratio, followed literally (spec §4).</summary>
        internal override OptimisationOutcome Execute(EvaluationContext context)
        {
            double goldenRatio = (-1 + System.Math.Pow(5, 0.5)) / 2;
            bool absoluteDifferenceMode = StoppingCriterion == GoldenSectionStoppingCriterion.AbsoluteDifference;
            double minimumDifference = absoluteDifferenceMode ? AbsoluteDifference : 0;
            int maxReductions;
            if (StoppingCriterion == GoldenSectionStoppingCriterion.IntervalReduction)
            {
                // Java: Math.round((float)(ln ρ / ln gr + 1.49999)) - 1, where Math.round(float) = (int)floor(x + 0.5f).
                float estimate = (float)((System.Math.Log(IntervalReduction) / System.Math.Log(goldenRatio)) + 1.49999);
                int n = (int)System.Math.Floor((float)(estimate + 0.5f));
                maxReductions = n > 1 ? n - 1 : 9;
            }
            else
            {
                maxReductions = context.MaximumSimulations > 1 ? context.MaximumSimulations - 1 : 9;
            }

            double lower = context.Lower[0];
            double upper = context.Upper[0];

            EvaluationPoint x0 = Make(context, lower);
            EvaluationPoint x3 = Make(context, upper);
            double dx = x3.X[0] - x0.X[0];
            double interval = 1.0;
            int reductionsDone = 0;
            interval *= goldenRatio;
            EvaluationPoint second = Make(context, x0.X[0] + (interval * dx));
            reductionsDone++;
            interval *= goldenRatio;
            EvaluationPoint first = Make(context, x0.X[0] + (interval * dx));

            EvaluationPoint[] batch = context.Evaluate(first, second);
            foreach (EvaluationPoint point in batch)
            {
                point.SetEvent(OptimisationEvent.LineSearch);
                context.Report(point, false, false);
            }

            EvaluationPoint x1 = batch[0].Clone();
            EvaluationPoint x2 = batch[1].Clone();
            double lowBorder = 0;
            bool terminate = false;
            bool equalBefore = false;

            do
            {
                reductionsDone++;
                interval *= goldenRatio;
                if (x2.F[0] < x1.F[0])
                {
                    lowBorder = x1.F[0];
                    x0 = x1.Clone();
                    x1 = x2.Clone();
                    x2 = LineSearch(context, x3.X[0] - (interval * dx));
                }
                else
                {
                    lowBorder = x2.F[0];
                    x3 = x2.Clone();
                    x2 = x1.Clone();
                    x1 = LineSearch(context, x0.X[0] + (interval * dx));
                }

                if (!absoluteDifferenceMode && x1.F[0] == x2.F[0])
                {
                    if (equalBefore)
                    {
                        terminate = true;
                    }

                    equalBefore = true;
                }
                else
                {
                    equalBefore = false;
                }
            }
            while (Iterate(absoluteDifferenceMode, lowBorder, x1, x2, minimumDifference, reductionsDone, maxReductions) && !terminate);

            EvaluationPoint xLow;
            EvaluationPoint xUpp;
            if (x1.F[0] < x2.F[0])
            {
                xLow = x0;
                xUpp = x2;
            }
            else
            {
                xLow = x1;
                xUpp = x3;
            }

            context.SetInterval(new GoldenSectionInterval(xLow.X[0], xUpp.X[0], lower, upper));

            if (terminate)
            {
                return OptimisationOutcome.Nullspace;
            }

            if (absoluteDifferenceMode && !DifferenceReached(lowBorder, x1, x2, minimumDifference))
            {
                return OptimisationOutcome.MaximumSimulationsReached;
            }

            return OptimisationOutcome.Success;
        }

        private static EvaluationPoint Make(EvaluationContext context, double value)
        {
            return context.CreatePoint(new[] { value }, 0);
        }

        private static EvaluationPoint LineSearch(EvaluationContext context, double value)
        {
            EvaluationPoint result = context.Evaluate(Make(context, value))[0];
            result.SetEvent(OptimisationEvent.LineSearch);
            context.Report(result, false, false);
            return result;
        }

        private static bool DifferenceReached(double lowBorder, EvaluationPoint x1, EvaluationPoint x2, double minimumDifference)
        {
            double fLow = x2.F[0] < x1.F[0] ? x2.F[0] : x1.F[0];
            return System.Math.Abs(lowBorder - fLow) < minimumDifference;
        }

        /// <summary>
        /// GenOpt's switch fall-through: in absolute-difference mode, stop once the difference is reached; in every
        /// mode, stop at the maximum number of reductions.
        /// </summary>
        private static bool Iterate(bool absoluteDifferenceMode, double lowBorder, EvaluationPoint x1, EvaluationPoint x2, double minimumDifference, int reductionsDone, int maxReductions)
        {
            if (absoluteDifferenceMode && DifferenceReached(lowBorder, x1, x2, minimumDifference))
            {
                return false;
            }

            return reductionsDone != maxReductions;
        }
    }
}
