// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Behaviour of the native SAM.Math optimisation kernel beyond the golden traces: bounds, the retry-once-then-stop
    /// failure rule, simulation counting, cancellation, progress, input validation and dependency hygiene.
    /// </summary>
    public class OptimisationKernelTests
    {
        private sealed class RecordingEvaluator : IObjectiveEvaluator
        {
            private readonly Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate;

            public RecordingEvaluator(Func<ObjectiveEvaluationRequest, CancellationToken, ObjectiveEvaluation> evaluate)
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

        private static ObjectiveEvaluation Quadratic(ObjectiveEvaluationRequest request)
        {
            double f = 0;
            for (int i = 0; i < request.Coordinates.Count; i++)
            {
                double d = request.Coordinates[i] - (0.3 * (i + 1));
                f += d * d;
            }

            return ObjectiveEvaluation.Success(f);
        }

        private static OptimisationProblem Problem2D(double min = -1, double max = 1)
        {
            return new OptimisationProblem(new[]
            {
                new OptimisationParameter("x1", 0.9, min, max, 0.05),
                new OptimisationParameter("x2", 0.9, min, max, 0.05),
            });
        }

        // ------------------------------------------------------------------ counting and numbering

        /// <summary>
        /// For every golden case: each evaluator call is a counted simulation or a retry. Cache hits and
        /// out-of-bounds points never reach the evaluator. Numbers are consecutive, and a retry repeats its number.
        /// </summary>
        [Theory]
        [MemberData(nameof(OptimisationGoldenTraceTests.TraceNames), MemberType = typeof(OptimisationGoldenTraceTests))]
        public void EvaluatorCalls_EqualSimulationsPlusRetries(string name)
        {
            GenOptGoldenTrace trace = GenOptGoldenTrace.Load(name);
            IObjectiveEvaluator inner = trace.CreateEvaluator();
            RecordingEvaluator evaluator = new RecordingEvaluator(inner.Evaluate);
            OptimisationProblem problem = trace.CreateProblem();

            OptimisationResult result = trace.CreateOptimiser().Run(problem, evaluator);

            Assert.Equal(result.Simulations + result.Retries, evaluator.Requests.Count);
            Assert.Equal(result.Retries, evaluator.Requests.Count(r => r.Attempt == 2));
            Assert.Equal(Enumerable.Range(1, result.Simulations), evaluator.Requests.Where(r => r.Attempt == 1).Select(r => r.Simulation));
            Assert.True(result.Simulations <= trace.MaxIte || trace.Algorithm == "GoldenSection");

            if (trace.Algorithm != "GoldenSection")
            {
                foreach (ObjectiveEvaluationRequest request in evaluator.Requests)
                {
                    for (int i = 0; i < problem.Parameters.Count; i++)
                    {
                        Assert.InRange(request.Coordinates[i], problem.Parameters[i].Minimum, problem.Parameters[i].Maximum);
                    }
                }
            }
        }

        [Fact]
        public void CacheHit_IsReportedWithTheCurrentCountAndIsNotSimulated()
        {
            GenOptGoldenTrace trace = GenOptGoldenTrace.Load("ec-plateau");

            OptimisationResult result = trace.Run();

            // A repeated point after a failed iteration is a cache hit: it is reported but not counted.
            HashSet<string> seen = new HashSet<string>();
            int repeats = 0;
            foreach (OptimisationTraceEntry entry in result.Entries.Where(e => e.Event == OptimisationEvent.CostNotReduced || e.Event == OptimisationEvent.ExplorationBase))
            {
                if (!seen.Add(string.Join(",", entry.Coordinates)))
                {
                    repeats++;
                }
            }

            Assert.True(repeats > 0);
            Assert.True(result.Simulations < result.Entries.Count);
        }

        // ------------------------------------------------------------------ coordinate rounding

        /// <summary>
        /// The evaluator receives coordinates rounded by the Java 8 model, not by shortest-decimal. 2^-27 and 2^31 are
        /// values where the two differ (spec §2.1). No golden trace happens to visit such a value, so this pins it end
        /// to end.
        /// </summary>
        [Theory]
        [InlineData(7.450580596923828E-9, 7.4505806E-9, 7.450581E-9)]
        [InlineData(2147483648.0, 2147483650.0, 2147483600.0)]
        public void EvaluationLayer_RoundsCoordinatesThroughTheJava8Model(double initial, double java, double shortest)
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => ObjectiveEvaluation.Success(0.0));
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", initial, 0) });

            new CoordinateSearch { NumberOfStepReduction = 1 }.Run(problem, evaluator);

            Assert.NotEqual(java, shortest);
            Assert.Equal(java, evaluator.Requests[0].Coordinates[0]);
        }

        // ------------------------------------------------------------------ bounds

        [Fact]
        public void InitialPointOutOfBounds_StopsBeforeAnyEvaluation()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => Quadratic(r));
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 2, -1, 1, 0.1) });

            OptimisationResult result = new HookeJeeves().Run(problem, evaluator);

            Assert.Equal(OptimisationOutcome.InitialPointInfeasible, result.Outcome);
            Assert.Empty(evaluator.Requests);
            Assert.Empty(result.Entries);
            Assert.Null(result.Minimum);
        }

        [Fact]
        public void OutOfBoundsTrial_IsNeitherSimulatedNorReported()
        {
            // Optimum below the lower bound: the search walks onto the bound and every trial below it is infeasible.
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => ObjectiveEvaluation.Success(r.Coordinates[0]));
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 3, 0, 10, 1) });

            OptimisationResult result = new HookeJeeves().Run(problem, evaluator);

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.All(evaluator.Requests, r => Assert.True(r.Coordinates[0] >= 0));
            Assert.All(result.Entries, e => Assert.True(e.Coordinates[0] >= 0 && e.Objective != double.MaxValue));
            Assert.Equal(0, result.Minimum!.Coordinates[0]);
        }

        // ------------------------------------------------------------------ failures

        [Fact]
        public void FailureInFirstBatch_StopsWithoutRetry()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => ObjectiveEvaluation.Failure("boom"));

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Single(evaluator.Requests);
            Assert.Equal(0, result.Retries);
            Assert.Equal(1, result.FailedSimulation);
            Assert.Equal("boom", result.FailureMessage);
            Assert.Empty(result.Entries);
            Assert.Null(result.Minimum);
        }

        [Fact]
        public void LaterFailure_IsRetriedOnceWithTheSameNumber_AndTheRunContinues()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => r.Simulation == 4 && r.Attempt == 1 ? ObjectiveEvaluation.Failure("transient") : Quadratic(r));
            OptimisationResult reference = new HookeJeeves().Run(Problem2D(), new DelegateObjectiveEvaluator((r, c) => Quadratic(r)));

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator);

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(1, result.Retries);
            Assert.Equal(new[] { 4, 4 }, evaluator.Requests.Where(r => r.Simulation == 4).Select(r => r.Simulation));
            Assert.Equal(new[] { 1, 2 }, evaluator.Requests.Where(r => r.Simulation == 4).Select(r => r.Attempt));
            Assert.Equal(reference.Entries.Count, result.Entries.Count);
            Assert.Equal(reference.Minimum!.Objective, result.Minimum!.Objective);
            Assert.Equal(0, result.FailedSimulation);
        }

        [Fact]
        public void LaterFailure_TwiceInARow_StopsWithNoMinimum()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => r.Simulation == 4 ? ObjectiveEvaluation.Failure("persistent") : Quadratic(r));

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Equal(1, result.Retries);
            Assert.Equal(5, evaluator.Requests.Count);
            Assert.Equal(4, result.FailedSimulation);
            Assert.Equal("persistent", result.FailureMessage);
            Assert.Null(result.Minimum);
            Assert.All(result.Entries, e => Assert.True(e.Simulation < 4));
        }

        [Fact]
        public void EvaluatorException_WrongOutputCount_AndNull_CountAsFailures()
        {
            OptimisationResult thrown = new HookeJeeves().Run(Problem2D(), new DelegateObjectiveEvaluator((r, c) => throw new InvalidOperationException("crash")));
            OptimisationResult wrongCount = new HookeJeeves().Run(Problem2D(), new DelegateObjectiveEvaluator((r, c) => ObjectiveEvaluation.Success(1.0, 2.0)));
            OptimisationResult none = new HookeJeeves().Run(Problem2D(), new DelegateObjectiveEvaluator((r, c) => null!));

            Assert.Equal(OptimisationOutcome.EvaluationFailed, thrown.Outcome);
            Assert.Contains("crash", thrown.FailureMessage);
            Assert.Equal(OptimisationOutcome.EvaluationFailed, wrongCount.Outcome);
            Assert.Equal(OptimisationOutcome.EvaluationFailed, none.Outcome);
        }

        [Fact]
        public void GoldenSection_FirstBatchFailure_StillSimulatesTheBatch_ThenStops()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => r.Simulation == 1 ? ObjectiveEvaluation.Failure("first") : Quadratic(r));
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 0, -5, 35, 1) });

            OptimisationResult result = new GoldenSection().Run(problem, evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Equal(new[] { 1, 2 }, evaluator.Requests.Select(r => r.Simulation));
            Assert.Equal(0, result.Retries);
            Assert.Equal(1, result.FailedSimulation);
            Assert.Null(result.Interval);
        }

        // ------------------------------------------------------------------ cancellation

        [Fact]
        public void Cancellation_BeforeRun_EvaluatesNothing()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            source.Cancel();
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => Quadratic(r));

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Empty(evaluator.Requests);
            Assert.Empty(result.Entries);
        }

        [Fact]
        public void Cancellation_DuringRun_StopsBeforeTheNextSimulation()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) =>
            {
                if (r.Simulation == 5)
                {
                    source.Cancel();
                }

                return Quadratic(r);
            });

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Equal(5, evaluator.Requests.Count);
            Assert.Null(result.Minimum);
            Assert.Contains(result.Entries, e => e.Simulation == 5);
        }

        [Fact]
        public void Cancellation_ThrownByTheEvaluator_IsNotAFailureAndIsNotRetried()
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) =>
            {
                if (r.Simulation == 6)
                {
                    source.Cancel();
                    c.ThrowIfCancellationRequested();
                }

                return Quadratic(r);
            });

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator, null, source.Token);

            Assert.Equal(OptimisationOutcome.Cancelled, result.Outcome);
            Assert.Equal(6, evaluator.Requests.Count);
            Assert.Equal(0, result.Retries);
            Assert.Equal(0, result.FailedSimulation);
        }

        [Fact]
        public void OperationCanceled_WithoutCancellationRequested_IsAFailure()
        {
            RecordingEvaluator evaluator = new RecordingEvaluator((r, c) => r.Simulation == 3 ? throw new OperationCanceledException("timeout") : Quadratic(r));

            OptimisationResult result = new HookeJeeves().Run(Problem2D(), evaluator);

            Assert.Equal(OptimisationOutcome.EvaluationFailed, result.Outcome);
            Assert.Equal(1, result.Retries);
            Assert.Equal(3, result.FailedSimulation);
        }

        // ------------------------------------------------------------------ progress

        [Theory]
        [InlineData("e1-hj-quad-2d")]
        [InlineData("e5-maxite-7")]
        [InlineData("e10-gs-absdiff")]
        public void Progress_ReportsEveryTraceEntryInOrder(string name)
        {
            ListProgress progress = new ListProgress();

            OptimisationResult result = GenOptGoldenTrace.Load(name).Run(progress);

            Assert.Equal(result.Entries.Count + result.MainIterations.Count, progress.Items.Count);
            Assert.Equal(result.Entries, progress.Items.Where(p => !p.MainIteration).Select(p => p.Entry));
            Assert.Equal(result.MainIterations, progress.Items.Where(p => p.MainIteration).Select(p => p.Entry));
            Assert.True(progress.Items.Zip(progress.Items.Skip(1), (a, b) => a.Simulations <= b.Simulations).All(x => x));
            Assert.All(progress.Items, p => Assert.Equal(GenOptGoldenTrace.Load(name).MaxIte, p.MaximumSimulations));
        }

        // ------------------------------------------------------------------ settings and validation

        [Fact]
        public void GoldenSection_MaximumSimulationsOfOne_UsesNineReductions()
        {
            OptimisationProblem problem = new OptimisationProblem(new[] { new OptimisationParameter("x", 0, -5, 35, 1) });

            OptimisationResult result = new GoldenSection { MaximumSimulations = 1 }.Run(problem, new DelegateObjectiveEvaluator((r, c) => Quadratic(r)));

            Assert.Equal(OptimisationOutcome.Success, result.Outcome);
            Assert.Equal(10, result.Entries.Count);
            Assert.All(result.Entries, e => Assert.Equal(OptimisationEvent.LineSearch, e.Event));
            Assert.Empty(result.MainIterations);
            Assert.NotNull(result.Interval);
        }

        [Fact]
        public void Optimiser_IsReusable_AndHoldsNoRunState()
        {
            HookeJeeves optimiser = new HookeJeeves();
            DelegateObjectiveEvaluator evaluator = new DelegateObjectiveEvaluator((r, c) => Quadratic(r));

            OptimisationResult first = optimiser.Run(Problem2D(), evaluator);
            OptimisationResult second = optimiser.Run(Problem2D(), evaluator);

            Assert.Equal(first.Simulations, second.Simulations);
            Assert.Equal(first.Entries.Select(e => e.Objective), second.Entries.Select(e => e.Objective));
        }

        [Fact]
        public void InvalidInput_ThrowsBeforeEvaluating()
        {
            DelegateObjectiveEvaluator evaluator = new DelegateObjectiveEvaluator((r, c) => throw new Xunit.Sdk.XunitException("must not evaluate"));
            OptimisationProblem unbounded = new OptimisationProblem(new[] { new OptimisationParameter("x", 0, 1) });

            Assert.Throws<ArgumentNullException>(() => new HookeJeeves().Run(null!, evaluator));
            Assert.Throws<ArgumentNullException>(() => new HookeJeeves().Run(Problem2D(), null!));
            Assert.Throws<InvalidOperationException>(() => new HookeJeeves { MaximumSimulations = 0 }.Run(Problem2D(), evaluator));
            Assert.Throws<InvalidOperationException>(() => new HookeJeeves { MeshSizeDivider = 1 }.Run(Problem2D(), evaluator));
            Assert.Throws<InvalidOperationException>(() => new CoordinateSearch { InitialMeshSizeExponent = -1 }.Run(Problem2D(), evaluator));
            Assert.Throws<InvalidOperationException>(() => new CoordinateSearch { MeshSizeExponentIncrement = 0 }.Run(Problem2D(), evaluator));
            Assert.Throws<InvalidOperationException>(() => new CoordinateSearch { NumberOfStepReduction = 0 }.Run(Problem2D(), evaluator));
            Assert.Throws<ArgumentException>(() => new GoldenSection().Run(Problem2D(), evaluator));
            Assert.Throws<ArgumentException>(() => new GoldenSection().Run(unbounded, evaluator));
            Assert.Throws<InvalidOperationException>(() => new GoldenSection { StoppingCriterion = GoldenSectionStoppingCriterion.IntervalReduction }.Run(new OptimisationProblem(new[] { new OptimisationParameter("x", 0, 0, 1, 1) }), evaluator));
            Assert.Throws<ArgumentException>(() => new OptimisationProblem(new OptimisationParameter[0]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OptimisationProblem(Problem2D().Parameters, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OptimisationParameter("x", double.NaN, 1));
        }

        // ------------------------------------------------------------------ dependency hygiene

        /// <summary>SAM.Math must stay generic: no Tas, EDSL, GenOpt file-format or Java-process dependency.</summary>
        [Fact]
        public void SamMath_HasNoTasEdslGenOptOrJavaDependency()
        {
            Regex forbidden = new Regex("Tas|EDSL|GenOpt|Java|IKVM", RegexOptions.IgnoreCase);

            IEnumerable<string> references = typeof(Optimiser).Assembly.GetReferencedAssemblies().Select(a => a.Name!);

            Assert.DoesNotContain(references, forbidden.IsMatch);
        }
    }
}
