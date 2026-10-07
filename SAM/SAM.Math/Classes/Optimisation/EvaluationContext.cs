// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SAM.Math
{
    /// <summary>
    /// The evaluation layer shared by all algorithms (documentation/GenOpt-3.1.1-Behaviour.md §2 and §5). It owns
    /// the per-run state:
    /// <list type="bullet">
    /// <item>coordinate rounding and bounds for pattern search;</item>
    /// <item>the approximate result cache;</item>
    /// <item>simulation numbering and the retry-once rule;</item>
    /// <item>cancellation;</item>
    /// <item>the trace with its main/sub counters, and progress.</item>
    /// </list>
    /// One instance serves exactly one run.
    /// </summary>
    internal sealed class EvaluationContext
    {
        private readonly IObjectiveEvaluator evaluator;
        private readonly IProgress<OptimisationProgress> progress;
        private readonly CancellationToken cancellationToken;
        private readonly ApproximatePointCache<double[]> cache = new ApproximatePointCache<double[]>();
        private readonly List<OptimisationTraceEntry> entries = new List<OptimisationTraceEntry>();
        private readonly List<OptimisationTraceEntry> mainIterations = new List<OptimisationTraceEntry>();
        private readonly List<OptimisationTraceEntry> mainPoints = new List<OptimisationTraceEntry>();
        private readonly List<OptimisationTraceEntry> subPoints = new List<OptimisationTraceEntry>();
        private int mainIteration = 1;
        private int subIteration = 1;
        private bool firstBatch = true;
        private OptimisationTraceEntry minimum;
        private GoldenSectionInterval interval;
        private int failedSimulation;
        private string failureMessage;

        public EvaluationContext(OptimisationProblem problem, IObjectiveEvaluator evaluator, int maximumSimulations, IProgress<OptimisationProgress> progress, CancellationToken cancellationToken)
        {
            this.evaluator = evaluator;
            this.progress = progress;
            this.cancellationToken = cancellationToken;
            Problem = problem;
            Dimension = problem.Parameters.Count;
            OutputCount = problem.OutputCount;
            MaximumSimulations = maximumSimulations;
            Lower = problem.Parameters.Select(x => x.Minimum).ToArray();
            Upper = problem.Parameters.Select(x => x.Maximum).ToArray();
            Initial = problem.Parameters.Select(x => x.Initial).ToArray();
            Step = problem.Parameters.Select(x => x.Step).ToArray();
        }

        public OptimisationProblem Problem { get; }

        public int Dimension { get; }

        public int OutputCount { get; }

        public int MaximumSimulations { get; }

        public double[] Lower { get; }

        public double[] Upper { get; }

        public double[] Initial { get; }

        public double[] Step { get; }

        public int Simulations { get; private set; }

        public int Retries { get; private set; }

        public bool MaximumSimulationsReached => Simulations >= MaximumSimulations;

        public EvaluationPoint CreatePoint(double[] x, double fill)
        {
            return new EvaluationPoint((double[])x.Clone(), Enumerable.Repeat(fill, OutputCount).ToArray());
        }

        /// <summary>
        /// Pattern-search evaluation: round every coordinate to float text, then apply the bounds rule. An
        /// out-of-bounds point gets every output = <see cref="double.MaxValue"/>, with no simulation, no cache entry
        /// and no count. Otherwise the shared evaluation runs.
        /// </summary>
        public EvaluationPoint EvaluateRounded(EvaluationPoint point)
        {
            EvaluationPoint rounded = point.Clone();
            for (int i = 0; i < Dimension; i++)
            {
                rounded.X[i] = Java8FloatText.Round(rounded.X[i]);
            }

            for (int i = 0; i < Dimension; i++)
            {
                if (rounded.X[i] > Upper[i] || rounded.X[i] < Lower[i])
                {
                    rounded.F = Enumerable.Repeat(double.MaxValue, OutputCount).ToArray();
                    return rounded;
                }
            }

            return Evaluate(rounded)[0];
        }

        /// <summary>
        /// Evaluates one batch: cache lookup, de-duplication within the batch, numbering, simulation with the retry
        /// rule, and caching. Returns copies.
        /// </summary>
        public EvaluationPoint[] Evaluate(params EvaluationPoint[] points)
        {
            bool[] evaluate = new bool[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                double[] known;
                evaluate[p] = !cache.TryGetValue(points[p].X, out known);
                if (!evaluate[p])
                {
                    points[p].F = (double[])known.Clone();
                }
            }

            int[] equalTo = new int[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                equalTo[p] = -1;
                for (int q = 0; q < p; q++)
                {
                    if (ApproximatePointCache<double[]>.Compare(points[p].X, points[q].X) == 0)
                    {
                        equalTo[p] = q;
                        break;
                    }
                }

                if (equalTo[p] > -1)
                {
                    evaluate[p] = false;
                }
            }

            // Every point first takes the running count; then the points to simulate are numbered in array order
            // before any simulation runs.
            for (int p = 0; p < points.Length; p++)
            {
                if (evaluate[p])
                {
                    Simulations++;
                }

                points[p].Simulation = Simulations;
            }

            int number = Simulations - evaluate.Count(x => x) + 1;
            bool first = firstBatch;
            bool failed = false;
            for (int p = 0; p < points.Length; p++)
            {
                if (!evaluate[p])
                {
                    continue;
                }

                points[p].Simulation = number++;
                string message;
                if (!Simulate(points[p], !first, out message) && !failed)
                {
                    failed = true;
                    failedSimulation = points[p].Simulation;
                    failureMessage = message;
                }
            }

            firstBatch = false;
            if (failed)
            {
                throw new OptimisationTermination(OptimisationOutcome.EvaluationFailed);
            }

            for (int p = 0; p < points.Length; p++)
            {
                if (equalTo[p] > -1)
                {
                    points[p].F = (double[])points[equalTo[p]].F.Clone();
                    points[p].Simulation = points[equalTo[p]].Simulation;
                }
            }

            return points.Select(x => x.Clone()).ToArray();
        }

        /// <summary>
        /// Records a point in the trace. A sub report advances the sub counter. A main report advances the main
        /// counter and resets the sub counter. Pattern search never reports a point whose outputs are all
        /// <see cref="double.MaxValue"/>.
        /// </summary>
        public void Report(EvaluationPoint point, bool main, bool suppressMaxValue)
        {
            if (suppressMaxValue && point.F.All(x => x == double.MaxValue))
            {
                return;
            }

            OptimisationTraceEntry entry = new OptimisationTraceEntry(point, mainIteration, subIteration);
            if (main)
            {
                mainPoints.Add(entry);
                mainIterations.Add(entry);
                mainIteration++;
                subIteration = 1;
            }
            else
            {
                subPoints.Add(entry);
                entries.Add(entry);
                subIteration++;
            }

            progress?.Report(new OptimisationProgress(entry, main, Simulations, MaximumSimulations));
        }

        /// <summary>
        /// Reports the minimum (spec §5). Only when at least one main iteration was reported. Start from the last
        /// main point, scan the main points backwards, then the sub points backwards, replacing only on a strictly
        /// smaller objective. The point keeps its stored numbers and is written to both listings.
        /// </summary>
        public void ReportMinimum(OptimisationEvent @event)
        {
            if (mainIteration <= 1)
            {
                return;
            }

            OptimisationTraceEntry lowest = mainPoints[mainPoints.Count - 1];
            double objective = lowest.Objective;
            for (int j = mainPoints.Count - 1; j > -1; j--)
            {
                if (objective > mainPoints[j].Objective)
                {
                    lowest = mainPoints[j];
                    objective = lowest.Objective;
                }
            }

            for (int j = subPoints.Count - 1; j > -1; j--)
            {
                if (objective > subPoints[j].Objective)
                {
                    lowest = subPoints[j];
                    objective = lowest.Objective;
                }
            }

            minimum = new OptimisationTraceEntry(lowest, @event);
            mainIterations.Add(minimum);
            progress?.Report(new OptimisationProgress(minimum, true, Simulations, MaximumSimulations));
            entries.Add(minimum);
            progress?.Report(new OptimisationProgress(minimum, false, Simulations, MaximumSimulations));
        }

        public void SetInterval(GoldenSectionInterval value)
        {
            interval = value;
        }

        public OptimisationResult ToResult(OptimisationOutcome outcome)
        {
            return new OptimisationResult(
                outcome,
                Simulations,
                Retries,
                entries.AsReadOnly(),
                mainIterations.AsReadOnly(),
                minimum,
                interval,
                failedSimulation,
                failureMessage);
        }

        /// <summary>
        /// Retry rule (spec §2.4). The first batch is not retried. Later, one retry with the same simulation number.
        /// Only a successful simulation is cached.
        /// </summary>
        private bool Simulate(EvaluationPoint point, bool allowRetry, out string message)
        {
            if (!TrySimulate(point, 1, out message))
            {
                if (!allowRetry)
                {
                    return false;
                }

                Retries++;
                if (!TrySimulate(point, 2, out message))
                {
                    return false;
                }
            }

            cache.Put(point.X, (double[])point.F.Clone());
            return true;
        }

        private bool TrySimulate(EvaluationPoint point, int attempt, out string message)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ObjectiveEvaluation evaluation;
            try
            {
                evaluation = evaluator.Evaluate(new ObjectiveEvaluationRequest(point.Simulation, attempt, point.X), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message = exception.GetType().Name + ": " + exception.Message;
                return false;
            }

            if (evaluation == null)
            {
                message = "The evaluator returned no result.";
                return false;
            }

            if (!evaluation.Succeeded)
            {
                message = evaluation.Message;
                return false;
            }

            if (evaluation.Outputs.Count != OutputCount)
            {
                message = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Expected {0} output(s), got {1}.", OutputCount, evaluation.Outputs.Count);
                return false;
            }

            point.F = evaluation.Outputs.ToArray();
            message = null;
            return true;
        }
    }
}
