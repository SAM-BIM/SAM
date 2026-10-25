// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolids"/> does not depend on the order of the solids: which solids are kept,
    /// which plane gives their Panels and the pieces named in their PBS103 record are the same in every input order, with
    /// several disconnected elements, solids with several candidate planes, and pieces recovered only on a later pass.
    /// </summary>
    public class PanelsBySolidsOrderTests
    {
        /// <summary>What one solid gave in one run: its Panel normals, and the names of the solids its PBS103 record cites.</summary>
        private sealed class Outcome
        {
            public List<Vector3D> Normals { get; } = new List<Vector3D>();

            public bool Kept { get; set; }

            public string Message { get; set; }
        }

        private static IEnumerable<List<int>> Permutations(int count)
        {
            if (count == 0)
            {
                yield return new List<int>();
                yield break;
            }

            foreach (List<int> permutation in Permutations(count - 1))
            {
                for (int i = 0; i <= permutation.Count; i++)
                {
                    List<int> result = new List<int>(permutation);
                    result.Insert(i, count - 1);
                    yield return result;
                }
            }
        }

        /// <summary>The cyclic rotations of the input and of its reverse: 2n orders for n solids.</summary>
        private static IEnumerable<List<int>> Rotations(int count)
        {
            List<int> forward = Enumerable.Range(0, count).ToList();
            foreach (List<int> order in new[] { forward, Enumerable.Reverse(forward).ToList() })
            {
                for (int i = 0; i < count; i++)
                {
                    yield return order.Skip(i).Concat(order.Take(i)).ToList();
                }
            }
        }

        /// <summary>
        /// Runs the named solids in <paramref name="order"/> and returns the outcome per name; the solids are named in the
        /// diagnostics by name, so the cited pieces read the same whatever their positions.
        /// </summary>
        private static Dictionary<string, Outcome> Run(List<(string Name, Shell Shell)> solids, List<int> order)
        {
            List<(string Name, Shell Shell)> ordered = order.ConvertAll(i => solids[i]);
            Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Detailed);
            List<List<Panel>> panels = ordered.ConvertAll(x => x.Shell).PanelsBySolids(diagnostics, sourceNames: ordered.ConvertAll(x => x.Name));

            Dictionary<string, Outcome> result = new Dictionary<string, Outcome>();
            for (int i = 0; i < ordered.Count; i++)
            {
                Diagnostic continuation = diagnostics.FirstOrDefault(x => x.SourceIndex == i && x.Code == BySolidsDiagnosticCode.PanelsContinuation);
                Outcome outcome = new Outcome { Kept = continuation != null, Message = continuation?.Message };
                outcome.Normals.AddRange(panels[i].ConvertAll(x => x.Normal));
                result[ordered[i].Name] = outcome;
            }

            return result;
        }

        private static string Order(List<(string Name, Shell Shell)> solids, List<int> order) => string.Join(", ", order.Select(i => solids[i].Name));

        [Fact]
        public void PanelsBySolids_WallJoinedAndIsolatedPierInAnyOrder_KeepsOnlyTheWallAndTheJoinedPier()
        {
            // Arrange: the review case. A 0.45 m wall, a 1.5 m pier touching its end, and the same pier 12.5 m away.
            List<(string Name, Shell Shell)> solids = new List<(string Name, Shell Shell)>
            {
                ("wall", Box(0, 0, 0, 6, 0.45, 3)), ("joined", Box(6, 0, 0, 7.5, 0.45, 3)), ("isolated", Box(20, 0, 0, 21.5, 0.45, 3)),
            };

            foreach (List<int> order in Permutations(solids.Count))
            {
                // Act
                Dictionary<string, Outcome> outcomes = Run(solids, order);

                // Assert
                string context = Order(solids, order);
                Assert.True(outcomes["wall"].Normals.Count == 1 && !outcomes["wall"].Kept, context);
                Assert.True(outcomes["joined"].Normals.Count == 1 && outcomes["joined"].Kept, context);
                Assert.True(outcomes["isolated"].Normals.Count == 0 && !outcomes["isolated"].Kept, context);
                Assert.True(outcomes["joined"].Message.EndsWith("Other pieces: Panels of #" + order.IndexOf(0) + " 'wall'."), context + ": " + outcomes["joined"].Message);
            }
        }

        [Fact]
        public void PanelsBySolids_TwoDisconnectedElementsAndIsolatedPierInAnyOrder_KeepsEachPierWithItsOwnWall()
        {
            // Arrange: two walls along x with a pier each, 10 m apart in y, and a pier that touches neither.
            List<(string Name, Shell Shell)> solids = new List<(string Name, Shell Shell)>
            {
                ("wallA", Box(0, 0, 0, 6, 0.45, 3)), ("pierA", Box(6, 0, 0, 7.5, 0.45, 3)),
                ("wallB", Box(0, 10, 0, 6, 10.45, 3)), ("pierB", Box(-1.5, 10, 0, 0, 10.45, 3)),
                ("isolated", Box(20, 5, 0, 21.5, 5.45, 3)),
            };

            foreach (List<int> order in Permutations(solids.Count))
            {
                // Act
                Dictionary<string, Outcome> outcomes = Run(solids, order);

                // Assert
                string context = Order(solids, order);
                Assert.True(outcomes["pierA"].Kept && outcomes["pierA"].Normals.Count == 1, context);
                Assert.True(outcomes["pierB"].Kept && outcomes["pierB"].Normals.Count == 1, context);
                Assert.True(!outcomes["isolated"].Kept && outcomes["isolated"].Normals.Count == 0, context);
                Assert.True(outcomes["pierA"].Message.EndsWith("Other pieces: Panels of #" + order.IndexOf(0) + " 'wallA'."), context + ": " + outcomes["pierA"].Message);
                Assert.True(outcomes["pierB"].Message.EndsWith("Other pieces: Panels of #" + order.IndexOf(2) + " 'wallB'."), context + ": " + outcomes["pierB"].Message);
            }
        }

        [Fact]
        public void PanelsBySolids_ColumnsWithTwoCandidatePlanesInAnyOrder_KeepTheirWallsPlaneEveryTime()
        {
            // Arrange: a 0.6 x 0.6 m column has two candidate planes (0.6 / 0.6 = 1 each way). Column A ends a 0.6 m wall
            // along x, column B ends a 0.6 m wall along y (walls 3.5 m high: 0.6 / 3.5 = 0.17, panel-like). Each column's
            // plane across its wall lies within the wall's thickness; only the plane along the wall continues it. A third
            // column continues nothing.
            List<(string Name, Shell Shell)> solids = new List<(string Name, Shell Shell)>
            {
                ("wallX", Box(-6, 0, 0, 0, 0.6, 3.5)), ("columnA", Box(0, 0, 0, 0.6, 0.6, 3.5)),
                ("wallY", Box(10, 0, 0, 10.6, 6, 3.5)), ("columnB", Box(10, -0.6, 0, 10.6, 0, 3.5)),
                ("isolated", Box(20, 20, 0, 20.6, 20.6, 3.5)),
            };

            foreach (List<int> order in Permutations(solids.Count))
            {
                // Act
                Dictionary<string, Outcome> outcomes = Run(solids, order);

                // Assert: each column gives one Panel on its own wall's plane, cites only that wall; the isolated one none.
                string context = Order(solids, order);
                Vector3D normalA = Assert.Single(outcomes["columnA"].Normals);
                Vector3D normalB = Assert.Single(outcomes["columnB"].Normals);
                Assert.True(System.Math.Abs(normalA.Unit.Y) > 0.999, context);
                Assert.True(System.Math.Abs(normalB.Unit.X) > 0.999, context);
                Assert.True(outcomes["columnA"].Message.EndsWith("Other pieces: Panels of #" + order.IndexOf(0) + " 'wallX'."), context + ": " + outcomes["columnA"].Message);
                Assert.True(outcomes["columnB"].Message.EndsWith("Other pieces: Panels of #" + order.IndexOf(2) + " 'wallY'."), context + ": " + outcomes["columnB"].Message);
                Assert.True(!outcomes["isolated"].Kept && outcomes["isolated"].Normals.Count == 0, context);
            }
        }

        [Fact]
        public void PanelsBySolids_StubFramedOnlyByARecoveredPier_IsKeptOnALaterPassInAnyOrder()
        {
            // Arrange: a pier recovered from a 0.45 m wall; a 0.4 m stub between that pier and a 0.1 m wall, on a slab and
            // under a slab. One edge of the stub meets only the pier, so it closes a gap only once the pier is a Panel.
            List<(string Name, Shell Shell)> solids = new List<(string Name, Shell Shell)>
            {
                ("wall", Box(0, 0, 0, 6, 0.45, 3)), ("pier", Box(6, 0, 0, 7.5, 0.45, 3)), ("stub", Box(7.1, 0.45, 0, 7.5, 1.32, 3)),
                ("partition", Box(6, 1.32, 0, 9, 1.42, 3)), ("floor", Box(5, -1, -0.2, 9, 2, 0)), ("ceiling", Box(5, -1, 3, 9, 2, 3.2)),
            };

            foreach (List<int> order in Rotations(solids.Count))
            {
                // Act
                Dictionary<string, Outcome> outcomes = Run(solids, order);

                // Assert
                string context = Order(solids, order);
                Assert.True(outcomes["pier"].Kept && outcomes["pier"].Normals.Count == 1, context);
                Assert.True(outcomes["stub"].Kept && outcomes["stub"].Normals.Count == 1, context);
                Assert.Contains("closes a gap", outcomes["stub"].Message);
            }
        }
    }
}
