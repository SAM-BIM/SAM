// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Tests
{
    /// <summary>
    /// Scaling benchmarks for the planar proximity scans indexed by the planar-proximity
    /// branch. Each benchmark runs at N, 2N and 4N so the growth exponent can be read from
    /// the output. Guards are correctness-only; there are no wall-clock thresholds.
    /// </summary>
    [Trait("Category", "Benchmark")]
    public class PlanarProximityBenchmarks
    {
        private readonly ITestOutputHelper _output;

        public PlanarProximityBenchmarks(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Snap_Segments_Scaling()
        {
            Report("Query.Snap(segments, averaging)", new int[] { 1000, 2000, 4000 }, count =>
            {
                List<Segment2D> segments = ChainedSegments(count);
                Stopwatch stopwatch = Stopwatch.StartNew();
                List<Segment2D> result = Geometry.Planar.Query.Snap(segments, true, 1e-3);
                stopwatch.Stop();
                Assert.NotNull(result);
                return stopwatch.Elapsed.TotalMilliseconds;
            });
        }

        [Fact]
        public void RemoveAlmostSimilar_Generic_Scaling()
        {
            Report("Modify.RemoveAlmostSimilar<T>", new int[] { 500, 1000, 2000 }, count =>
            {
                List<Segment2D> segments = NearDuplicateSegments(count);
                Stopwatch stopwatch = Stopwatch.StartNew();
                Geometry.Planar.Modify.RemoveAlmostSimilar(segments, 1e-3);
                stopwatch.Stop();
                Assert.NotEmpty(segments);
                return stopwatch.Elapsed.TotalMilliseconds;
            });
        }

        [Fact]
        public void Split_Segments_Scaling()
        {
            Report("Query.Split(segments)", new int[] { 300, 600, 1200 }, count =>
            {
                List<Segment2D> segments = CrossingGrid(count);
                Stopwatch stopwatch = Stopwatch.StartNew();
                List<Segment2D> result = Geometry.Planar.Query.Split(segments, Core.Tolerance.Distance);
                stopwatch.Stop();
                Assert.NotNull(result);
                return stopwatch.Elapsed.TotalMilliseconds;
            });
        }

        // --- Fixtures -------------------------------------------------------------------

        /// <summary>Segment chains whose joints are just inside the snap tolerance, on a lattice.</summary>
        private static List<Segment2D> ChainedSegments(int count)
        {
            int columns = (int)System.Math.Ceiling(System.Math.Sqrt(count));
            List<Segment2D> result = new List<Segment2D>();
            for (int i = 0; i < count; i++)
            {
                double x = (i % columns) * 10;
                double y = (i / columns) * 10;
                result.Add(new Segment2D(new Point2D(x, y), new Point2D(x + 4, y)));
                result.Add(new Segment2D(new Point2D(x + 4.0005, y), new Point2D(x + 8, y)));
                i++;
            }

            return result;
        }

        /// <summary>About one in three segments is a near-duplicate of a lattice sibling.</summary>
        private static List<Segment2D> NearDuplicateSegments(int count)
        {
            int columns = (int)System.Math.Ceiling(System.Math.Sqrt(count));
            List<Segment2D> result = new List<Segment2D>();
            for (int i = 0; i < count; i++)
            {
                double x = (i % columns) * 5;
                double y = (i / columns) * 5;
                double offset = (i % 3 == 0) ? 0.0004 : 0;
                result.Add(new Segment2D(new Point2D(x + offset, y + offset), new Point2D(x + 4 + offset, y + offset)));
            }

            return result;
        }

        /// <summary>Half horizontal, half vertical segments on a grid: every crossing splits.</summary>
        private static List<Segment2D> CrossingGrid(int count)
        {
            int half = count / 2;
            List<Segment2D> result = new List<Segment2D>();
            for (int i = 0; i < half; i++)
            {
                result.Add(new Segment2D(new Point2D(0, i * 1.0), new Point2D(half, i * 1.0)));
                result.Add(new Segment2D(new Point2D(i * 1.0, 0), new Point2D(i * 1.0, half)));
            }

            return result;
        }

        private void Report(string name, int[] counts, System.Func<int, double> run)
        {
            // Warm up so JIT and first-touch allocation do not land in the smallest sample.
            run(counts[0]);

            List<double> times = new List<double>();
            foreach (int count in counts)
            {
                times.Add(run(count));
            }

            _output.WriteLine(name);
            for (int i = 0; i < counts.Length; i++)
            {
                string growth = i == 0 ? "-" : (times[i] / times[i - 1]).ToString("0.00") + "x";
                _output.WriteLine(string.Format("  n={0,-6} {1,10:0.0} ms   growth {2}", counts[i], times[i], growth));
            }
        }
    }
}
