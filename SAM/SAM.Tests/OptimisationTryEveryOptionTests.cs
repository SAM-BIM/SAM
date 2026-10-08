// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The SAM.Math <see cref="TryEveryOption"/> method: every option in order, the whole table, the lowest option
    /// (ties to the lower number, NaN never wins), and the shared evaluation rules (failure and retry, cancellation,
    /// cache and numbering, progress, validation), with a stub evaluator standing in for a simulation.
    /// </summary>
    public class OptimisationTryEveryOptionTests
    {
        private sealed class TableEvaluator : IObjectiveEvaluator
        {
            private readonly Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate;

            /// <summary>Option k returns objectives[k - first] and, as a recorded output, 10·k.</summary>
            public TableEvaluator(double first, params double[] objectives)
                : this((r, c) => ObjectiveEvaluation.Success(objectives[(int)(r.Coordinates[0] - first)], 10 * r.Coordinates[0]))
            {
            }

            public TableEvaluator(Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate)
            {
                this.evaluate = evaluate;
            }

            public List<ObjectiveEvaluationRequest> Requests { get; } = new List<ObjectiveEvaluationRequest>();

            public ObjectiveEvaluation Evaluate(ObjectiveEvaluationRequest request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return evaluate(request, cancellationToken);
            }
        }

        private sealed class ListProgress : IProgress<OptimisationProgress>
        {
            public List<OptimisationProgress> Items { get; } = new List<OptimisationProgress>();

            public void Report(OptimisationProgress value) => Items.Add(value);
        }

        /// <summary>A choice numbered <paramref name="first"/> to <paramref name="last"/>, with two outputs (objective and one recorded).</summary>
        private static OptimisationProblem Choice(double first = 1, double last = 5, double initial = 1, double step = 1)
        {
            return new OptimisationProblem(new[] { new OptimisationParameter("Glazing", initial, first, last, step) }, 2);
        }

        private static ObjectiveEvaluation Objective(ObjectiveEvaluationRequest request)
        {
            double option = request.Coordinates[0];
            return ObjectiveEvaluation.Success(System.Math.Abs(option - 4) + 1, 10 * option);
        }

        // ------------------------------------------------------------------ order and the table

        [Fact]
        public void EveryOption_IsEvaluatedOnce_InOrder_AndTheResultIsTheWholeTable()
        {
            TableEvaluator evaluator = new TableEvaluator(1, 2951.65, 1846.73, 2203.1, 1990.4, 3120);

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator);

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(new double[] { 1, 2, 3, 4, 5 }, evaluator.Requests.Select(r => r.Coordinates.Single()));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, evaluator.Requests.Select(r => r.Simulation));
            Assert.All(evaluator.Requests, r => Assert.Equal(1, r.Attempt));
            Assert.Equal(5, result.Simulations);
            Assert.Equal(0, result.Retries);

            List<OptimisationTraceEntry> options = result.Entries.Take(5).ToList();
            Assert.All(options, e => Assert.Equal(OptimisationEvent.OptionEvaluated, e.Event));
            Assert.Equal(new double[] { 1, 2, 3, 4, 5 }, options.Select(e => e.Coordinates.Single()));
            Assert.Equal(new[] { 2951.65, 1846.73, 2203.1, 1990.4, 3120 }, options.Select(e => e.Objective));
            Assert.Equal(new double[] { 10, 20, 30, 40, 50 }, options.Select(e => e.Outputs[1]));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, options.Select(e => e.Simulation));
            Assert.All(options, e => Assert.Equal(1, e.MainIteration));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, options.Select(e => e.SubIteration));

            // The lowest option closes both listings; it is the only main iteration.
            Assert.Equal(6, result.Entries.Count);
            OptimisationTraceEntry minimum = Assert.Single(result.MainIterations);
            Assert.Same(result.Minimum, minimum);
            Assert.Same(minimum, result.Entries[5]);
            Assert.Equal(OptimisationEvent.MinimumPoint, minimum.Event);
            Assert.Equal(2, minimum.Coordinates.Single());
            Assert.Equal(1846.73, minimum.Objective);
            Assert.Equal(2, minimum.Simulation);
            Assert.Null(result.Interval);
            Assert.Equal(0, result.FailedSimulation);
            Assert.Null(result.FailureMessage);
        }

        [Fact]
        public void Options_RunFromMinimumToMaximum_IgnoringInitialAndStep()
        {
            TableEvaluator evaluator = new TableEvaluator((r, c) => Objective(r));

            OptimisationResult result = new TryEveryOption().Run(Choice(3, 6, 5, 0.25), evaluator);

            Assert.Equal(new double[] { 3, 4, 5, 6 }, evaluator.Requests.Select(r => r.Coordinates.Single()));
            Assert.Equal(4, result.Minimum!.Coordinates.Single());
        }

        [Fact]
        public void SingleOption_IsEvaluated_AndIsTheMinimum()
        {
            OptimisationResult result = new TryEveryOption().Run(Choice(1, 1), new TableEvaluator(1, 7.5));

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(1, result.Simulations);
            Assert.Equal(1, result.Minimum!.Coordinates.Single());
        }

        // ------------------------------------------------------------------ the best option

        [Theory]
        [InlineData(new[] { 5.0, 2.0, 2.0, 3.0 }, 2)]
        [InlineData(new[] { 1.0, 1.0, 1.0, 1.0 }, 1)]
        [InlineData(new[] { 4.0, 3.0, 2.0, 2.0 }, 3)]
        public void Tie_GoesToTheLowerOptionNumber(double[] objectives, double best)
        {
            OptimisationResult result = new TryEveryOption().Run(Choice(1, 4), new TableEvaluator(1, objectives));

            Assert.Equal(best, result.Minimum!.Coordinates.Single());
        }

        [Fact]
        public void NaN_NeverWins()
        {
            OptimisationResult result = new TryEveryOption().Run(Choice(1, 4), new TableEvaluator(1, double.NaN, 4, double.NaN, 4));

            Assert.Equal(2, result.Minimum!.Coordinates.Single());
            Assert.Equal(4, result.Entries.Count(e => e.Event == OptimisationEvent.OptionEvaluated));
        }

        [Fact]
        public void NoObjectiveIsANumber_TheTableIsKept_AndNoMinimumIsReported()
        {
            OptimisationResult result = new TryEveryOption().Run(Choice(1, 3), new TableEvaluator(1, double.NaN, double.NaN, double.NaN));

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Null(result.Minimum);
            Assert.Empty(result.MainIterations);
            Assert.Equal(3, result.Entries.Count);
        }

        // ------------------------------------------------------------------ failures (shared rule: retry once, then stop)

        [Fact]
        public void FirstOptionFailure_StopsWithoutRetry()
        {
            TableEvaluator evaluator = new TableEvaluator((r, c) => ObjectiveEvaluation.Failure("no licence"));

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Single(evaluator.Requests);
            Assert.Equal(0, result.Retries);
            Assert.Equal(1, result.FailedSimulation);
            Assert.Equal("no licence", result.FailureMessage);
            Assert.Empty(result.Entries);
            Assert.Null(result.Minimum);
        }

        [Fact]
        public void LaterFailure_IsRetriedOnceWithTheSameNumber_AndEveryOptionIsStillInTheTable()
        {
            TableEvaluator evaluator = new TableEvaluator((r, c) => r.Simulation == 3 && r.Attempt == 1 ? ObjectiveEvaluation.Failure("transient") : Objective(r));

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator);

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(1, result.Retries);
            Assert.Equal(5, result.Simulations);
            Assert.Equal(new[] { (3, 1), (3, 2) }, evaluator.Requests.Where(r => r.Simulation == 3).Select(r => (r.Simulation, r.Attempt)));
            Assert.Equal(new double[] { 1, 2, 3, 4, 5 }, result.Entries.Where(e => e.Event == OptimisationEvent.OptionEvaluated).Select(e => e.Coordinates.Single()));
            Assert.Equal(4, result.Minimum!.Coordinates.Single());
            Assert.Equal(0, result.FailedSimulation);
        }

        [Fact]
        public void LaterFailure_TwiceInARow_StopsTheRun_KeepsTheOptionsSoFar_AndReportsNoBest()
        {
            TableEvaluator evaluator = new TableEvaluator((r, c) => r.Coordinates[0] == 3 ? ObjectiveEvaluation.Failure("simulation failed") : Objective(r));

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Equal(1, result.Retries);
            Assert.Equal(new double[] { 1, 2, 3, 3 }, evaluator.Requests.Select(r => r.Coordinates.Single()));
            Assert.Equal(3, result.FailedSimulation);
            Assert.Equal("simulation failed", result.FailureMessage);
            Assert.Equal(new double[] { 1, 2 }, result.Entries.Select(e => e.Coordinates.Single()));
            Assert.Null(result.Minimum);
            Assert.Empty(result.MainIterations);
        }

        [Fact]
        public void EvaluatorException_WrongOutputCount_AndNull_CountAsFailures()
        {
            OptimisationResult thrown = new TryEveryOption().Run(Choice(), new DelegateObjectiveEvaluator((r, c) => throw new InvalidOperationException("crash")));
            OptimisationResult wrongCount = new TryEveryOption().Run(Choice(), new DelegateObjectiveEvaluator((r, c) => ObjectiveEvaluation.Success(1.0)));
            OptimisationResult none = new TryEveryOption().Run(Choice(), new DelegateObjectiveEvaluator((r, c) => null!));

            Assert.Equal(OptimisationOutcome.EvaluationFailed, thrown.Outcome);
            Assert.Contains("crash", thrown.FailureMessage);
            Assert.Equal(OptimisationOutcome.EvaluationFailed, wrongCount.Outcome);
            Assert.Equal("Expected 2 output(s), got 1.", wrongCount.FailureMessage);
            Assert.Equal(OptimisationOutcome.EvaluationFailed, none.Outcome);
        }

        // ------------------------------------------------------------------ cancellation

        [Fact]
        public void Cancellation_BeforeRun_EvaluatesNothing()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();
            TableEvaluator evaluator = new TableEvaluator((r, c) => Objective(r));

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Empty(evaluator.Requests);
            Assert.Empty(result.Entries);
            Assert.Null(result.Minimum);
        }

        [Fact]
        public void Cancellation_DuringRun_StopsBeforeTheNextOption_AndReportsNoBest()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            TableEvaluator evaluator = new TableEvaluator((r, c) =>
            {
                if (r.Simulation == 2)
                {
                    source.Cancel();
                }

                return Objective(r);
            });

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Equal(2, evaluator.Requests.Count);
            Assert.Equal(new double[] { 1, 2 }, result.Entries.Select(e => e.Coordinates.Single()));
            Assert.Null(result.Minimum);
            Assert.Empty(result.MainIterations);
        }

        [Fact]
        public void Cancellation_ThrownByTheEvaluator_IsNotAFailureAndIsNotRetried()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            TableEvaluator evaluator = new TableEvaluator((r, c) =>
            {
                if (r.Simulation == 3)
                {
                    source.Cancel();
                    c.ThrowIfCancellationRequested();
                }

                return Objective(r);
            });

            OptimisationResult result = new TryEveryOption().Run(Choice(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Equal(3, evaluator.Requests.Count);
            Assert.Equal(0, result.Retries);
            Assert.Equal(0, result.FailedSimulation);
            Assert.Equal(2, result.Entries.Count);
        }

        // ------------------------------------------------------------------ cache, numbering, progress, reuse

        /// <summary>
        /// The options are distinct points, so the evaluation cache never answers one: every option is a counted
        /// simulation, numbered by its position, and the evaluator sees each option exactly once.
        /// </summary>
        [Fact]
        public void EveryOption_IsACountedSimulation_NeverACacheHit()
        {
            TableEvaluator evaluator = new TableEvaluator((r, c) => ObjectiveEvaluation.Success(1, 1));

            OptimisationResult result = new TryEveryOption().Run(Choice(-2, 5), evaluator);

            Assert.Equal(8, result.Simulations);
            Assert.Equal(8, evaluator.Requests.Count);
            Assert.Equal(Enumerable.Range(1, 8), evaluator.Requests.Select(r => r.Simulation));
            Assert.Equal(8, evaluator.Requests.Select(r => r.Coordinates.Single()).Distinct().Count());
            Assert.Equal(Enumerable.Range(1, 8), result.Entries.Where(e => e.Event == OptimisationEvent.OptionEvaluated).Select(e => e.Simulation));
            Assert.Equal(-2, result.Minimum!.Coordinates.Single());
        }

        [Fact]
        public void Progress_ReportsEveryTraceEntryInOrder()
        {
            ListProgress progress = new ListProgress();

            OptimisationResult result = new TryEveryOption { MaximumSimulations = 9 }.Run(Choice(), new TableEvaluator((r, c) => Objective(r)), progress);

            Assert.Equal(result.Entries.Count + result.MainIterations.Count, progress.Items.Count);
            Assert.Equal(result.Entries, progress.Items.Where(p => !p.MainIteration).Select(p => p.Entry));
            Assert.Equal(result.MainIterations, progress.Items.Where(p => p.MainIteration).Select(p => p.Entry));
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 5, 5 }, progress.Items.Select(p => p.Simulations));
            Assert.All(progress.Items, p => Assert.Equal(9, p.MaximumSimulations));
        }

        [Fact]
        public void Optimiser_IsReusable_AndHoldsNoRunState()
        {
            TryEveryOption optimiser = new TryEveryOption();
            TableEvaluator evaluator = new TableEvaluator((r, c) => Objective(r));

            OptimisationResult first = optimiser.Run(Choice(), evaluator);
            OptimisationResult second = optimiser.Run(Choice(), evaluator);

            Assert.Equal(10, evaluator.Requests.Count);
            Assert.Equal(first.Entries.Select(e => e.Objective), second.Entries.Select(e => e.Objective));
            Assert.Equal(first.Minimum!.Coordinates, second.Minimum!.Coordinates);
        }

        // ------------------------------------------------------------------ validation

        [Fact]
        public void InvalidInput_ThrowsBeforeEvaluating()
        {
            DelegateObjectiveEvaluator evaluator = new DelegateObjectiveEvaluator((r, c) => throw new Xunit.Sdk.XunitException("must not evaluate"));
            OptimisationProblem two = new OptimisationProblem(new[] { new OptimisationParameter("a", 1, 1, 3, 1), new OptimisationParameter("b", 1, 1, 3, 1) });

            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(two, evaluator));
            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(Choice(1, 3.5), evaluator));
            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(Choice(0.5, 3), evaluator));
            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(new OptimisationProblem(new[] { new OptimisationParameter("x", 1, 1) }), evaluator));
            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(Choice(1, 1e17), evaluator));
            Assert.Throws<ArgumentException>(() => new TryEveryOption().Run(Choice(3, 1, 2), evaluator));
            Assert.Throws<InvalidOperationException>(() => new TryEveryOption { MaximumSimulations = 0 }.Run(Choice(), evaluator));

            InvalidOperationException tooMany = Assert.Throws<InvalidOperationException>(() => new TryEveryOption { MaximumSimulations = 4 }.Run(Choice(), evaluator));
            Assert.Equal("Try every option needs one simulation per option: 5 options, but MaximumSimulations is 4.", tooMany.Message);
        }

        [Fact]
        public void MaximumSimulations_EqualToTheOptionCount_RunsEveryOption()
        {
            OptimisationResult result = new TryEveryOption { MaximumSimulations = 5 }.Run(Choice(), new TableEvaluator((r, c) => Objective(r)));

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(5, result.Simulations);
        }
    }
}
