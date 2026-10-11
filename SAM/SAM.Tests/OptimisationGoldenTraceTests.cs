// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using SAM.Tests.Helpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// PR2 acceptance gate: the native SAM.Math kernel replays all 31 GenOpt 3.1.1 golden traces bit for bit.
    /// Compared: coordinates, outputs, simulation numbers, main/sub counters, comments, outcome, retries and the
    /// golden-section overview (documentation/GenOpt-3.1.1-Behaviour.md).
    /// </summary>
    public class OptimisationGoldenTraceTests
    {
        public static IEnumerable<object[]> TraceNames() => GenOptGoldenTrace.Names().Select(x => new object[] { x });

        [Fact]
        public void GoldenTraces_AllThirtyOneArePresent()
        {
            Assert.Equal(31, GenOptGoldenTrace.Names().Count());
        }

        [Theory]
        [MemberData(nameof(TraceNames))]
        public void Kernel_ReplaysGoldenTrace_BitForBit(string name)
        {
            GenOptGoldenTrace trace = GenOptGoldenTrace.Load(name);

            OptimisationResult result = trace.Run();

            List<string> differences = trace.Compare(result);
            Assert.True(differences.Count == 0, name + ":\n" + string.Join("\n", differences));
        }

        [Theory]
        [MemberData(nameof(TraceNames))]
        public void Kernel_IsDeterministic(string name)
        {
            GenOptGoldenTrace trace = GenOptGoldenTrace.Load(name);

            OptimisationResult first = trace.Run();
            OptimisationResult second = trace.Run();

            Assert.Equal(first.Outcome, second.Outcome);
            Assert.Equal(first.Simulations, second.Simulations);
            Assert.Equal(Describe(first.Entries), Describe(second.Entries));
            Assert.Equal(Describe(first.MainIterations), Describe(second.MainIterations));
        }

        [Theory]
        [MemberData(nameof(TraceNames))]
        public void Minimum_IsReportedOnlyForPatternSearchSuccessOrLimit(string name)
        {
            GenOptGoldenTrace trace = GenOptGoldenTrace.Load(name);

            OptimisationResult result = trace.Run();

            bool expectMinimum = trace.Algorithm != "GoldenSection"
                && (result.Outcome == OptimisationOutcome.Success || result.Outcome == OptimisationOutcome.MaximumSimulationsReached);
            Assert.Equal(expectMinimum, result.Minimum != null);
            if (expectMinimum)
            {
                Assert.Same(result.Minimum, result.Entries[result.Entries.Count - 1]);
                Assert.Same(result.Minimum, result.MainIterations[result.MainIterations.Count - 1]);
            }
        }

        private static List<string> Describe(IReadOnlyList<OptimisationTraceEntry> entries)
        {
            return entries.Select(e => string.Join(
                "|",
                e.Simulation,
                e.MainIteration,
                e.SubIteration,
                e.Event,
                System.BitConverter.DoubleToInt64Bits(e.Delta),
                e.ParameterIndex,
                e.Direction,
                string.Join(",", e.Outputs.Select(System.BitConverter.DoubleToInt64Bits)),
                string.Join(",", e.Coordinates.Select(System.BitConverter.DoubleToInt64Bits)))).ToList();
        }
    }
}
