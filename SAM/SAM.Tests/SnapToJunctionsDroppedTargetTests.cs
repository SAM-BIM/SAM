// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Query.SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/>: a panel that
    /// snapping drops (it lies within the thickness of the elements it meets) is not a junction for any other panel.
    /// <para>
    /// The corner scene (metres, snap 0.3): a 0.4 m facade F (mid-plane y 0) meets a 0.4 m return wall C (mid-plane x 0).
    /// A 0.1 m slab S1 runs under the facade and steps at y 0.35 to a 0.15 m slab S2 with the same soffit. A 0.15 m block
    /// R stands in the corner, flush with the facade's inner face (mid-plane y 0.125) and within the return wall's
    /// thickness in x, so snapping drops it.
    /// </para>
    /// </summary>
    public class SnapToJunctionsDroppedTargetTests
    {
        private const double Snap = 0.3;

        private static Panel Make(string sourceName, PanelType panelType, params Point3D[] point3Ds)
        {
            Panel panel = Analytical.Create.Panel(null, panelType, new Face3D(new Polygon3D(point3Ds.ToList())));
            panel.SetValue(PanelSourceParameter.SourceName, sourceName);
            return panel;
        }

        private static Panel WallY(string sourceName, double y, double x0, double x1, double z0, double z1)
        {
            return Make(sourceName, PanelType.Wall, new Point3D(x0, y, z0), new Point3D(x1, y, z0), new Point3D(x1, y, z1), new Point3D(x0, y, z1));
        }

        private static Panel WallX(string sourceName, double x, double y0, double y1, double z0, double z1)
        {
            return Make(sourceName, PanelType.Wall, new Point3D(x, y0, z0), new Point3D(x, y1, z0), new Point3D(x, y1, z1), new Point3D(x, y0, z1));
        }

        private static Panel Floor(string sourceName, double z, double x0, double x1, double y0, double y1)
        {
            return Make(sourceName, PanelType.Floor, new Point3D(x0, y0, z), new Point3D(x1, y0, z), new Point3D(x1, y1, z), new Point3D(x0, y1, z));
        }

        private static List<Panel> Corner(double slabWest = -0.2)
        {
            return new List<Panel>
            {
                WallY("F", 0, 0.2, 6, 0.15, 3.0),
                WallX("C", 0, 0.2, 4, 0.15, 3.0),
                Floor("S1", 0.05, slabWest, 6, -2, 0.35),
                Floor("S2", 0.075, 0.2, 6, 0.35, 4),
            };
        }

        private static Panel Block()
        {
            return WallY("R", 0.125, -0.2, 0.25, 0.15, 2.8);
        }

        /// <summary>The corner with a 0.1 m slab strip X beside the return wall's face, its south edge on the facade mid-plane.</summary>
        private static List<Panel> CornerWithStrip()
        {
            List<Panel> panels = Corner(0);
            panels.Add(Floor("X", 0.05, -0.5, -0.2, 0, 0.35));
            return panels;
        }

        private static string Name(Panel panel)
        {
            return panel.TryGetValue(PanelSourceParameter.SourceName, out string sourceName) ? sourceName : null;
        }

        private static Dictionary<string, Panel> BySource(IEnumerable<Panel> panels)
        {
            return panels.ToDictionary(Name);
        }

        private static List<Point3D> Outline(Panel panel)
        {
            return ((ISegmentable3D)panel.GetFace3D().GetExternalEdge3D()).GetPoints();
        }

        /// <summary>Same Guid, area and outline vertices (in any order) within 1e-9 m.</summary>
        private static bool Same(Panel a, Panel b)
        {
            List<Point3D> points_A = Outline(a), points_B = Outline(b);
            return a.Guid == b.Guid && System.Math.Abs(a.GetArea() - b.GetArea()) < 1e-9 && points_A.Count == points_B.Count
                && points_A.All(x => points_B.Any(y => x.Distance(y) < 1e-9));
        }

        private static void AssertSame(IEnumerable<Panel> expected, IEnumerable<Panel> actual)
        {
            Dictionary<string, Panel> expected_BySource = BySource(expected), actual_BySource = BySource(actual);
            Assert.Equal(expected_BySource.Keys.OrderBy(x => x), actual_BySource.Keys.OrderBy(x => x));
            foreach (KeyValuePair<string, Panel> pair in expected_BySource)
            {
                Assert.True(Same(pair.Value, actual_BySource[pair.Key]), $"panel {pair.Key} differs");
            }
        }

        [Fact]
        public void SnapToJunctions_BlockWithinCornerWallsThickness_DoesNotMoveSlabStep()
        {
            // Arrange: the same four panels with and without the block (copies keep their Guids).
            List<Panel> without = Corner();
            List<Panel> with = without.ConvertAll(x => Analytical.Create.Panel(x));
            with.Add(Block());

            // Act
            List<Panel> result_With = with.SnapToJunctions(new Core.Diagnostics(), Snap);
            List<Panel> result_Without = without.SnapToJunctions(new Core.Diagnostics(), Snap);

            // Assert: dropping the block moves nobody; the step stays at y 0.35 (not pulled onto the block's 0.125).
            AssertSame(result_Without, result_With);
            Dictionary<string, Panel> bySource = BySource(result_With);
            Assert.Equal(0.35, bySource["S1"].GetFace3D().GetBoundingBox().Max.Y, 9);
            Assert.Equal(0.35, bySource["S2"].GetFace3D().GetBoundingBox().Min.Y, 9);
        }

        [Fact]
        public void SnapToJunctions_StripCollapsingOnlyOnDroppedBlock_IsKept()
        {
            // Arrange: both y-edges of strip X (x -0.5..-0.2, y 0..0.35) would trim onto the block; without it, X is a
            // real slab piece that extends to the return wall's mid-plane.
            List<Panel> without = CornerWithStrip();
            List<Panel> with = without.ConvertAll(x => Analytical.Create.Panel(x));
            with.Add(Block());
            Core.Diagnostics diagnostics = new Core.Diagnostics();

            // Act
            List<Panel> result_With = with.SnapToJunctions(diagnostics, Snap);
            List<Panel> result_Without = without.SnapToJunctions(new Core.Diagnostics(), Snap);

            // Assert: X is kept (revived once the block is no longer a junction) and equals the result without the block.
            AssertSame(result_Without, result_With);
            Assert.Equal(0.175, BySource(result_With)["X"].GetArea(), 9);
            Diagnostic warning = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapCollapsed);
            Assert.Equal("Panel 'R'", warning.Subject);
        }

        [Fact]
        public void SnapToJunctions_BlockWithinCornerWallsThickness_IsDroppedWithWarning()
        {
            // Arrange
            List<Panel> panels = Corner();
            Panel block = Block();
            panels.Add(block);
            Core.Diagnostics diagnostics = new Core.Diagnostics();

            // Act
            List<Panel> result = panels.SnapToJunctions(diagnostics, Snap);

            // Assert: the block is still dropped, with one Warning whose text is unchanged; the summary counts every input panel.
            Assert.DoesNotContain(result, x => x.Guid == block.Guid);
            Assert.Equal(4, result.Count);
            Diagnostic warning = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapCollapsed);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal("Panel 'R'", warning.Subject);
            Assert.StartsWith("Dropped. Snapping its edges to the junctions closed it up (no wider than the distance tolerance, or turned inside out): it lies within the thickness of the elements it meets.", warning.Message);
            Assert.Equal(block.GetArea(), warning.Value.Value, 9);
            Diagnostic summary = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapChecked);
            Assert.StartsWith("Checked 5 panels for junctions:", summary.Message);
            Assert.DoesNotContain(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapNotSettled);
        }

        [Fact]
        public void SnapToJunctions_ThinPartitionOnSlabStep_RemainsTargetOfBothSlabEdges()
        {
            // Arrange: a genuine 0.15 m partition G (long, not within another element's thickness) at y 0.5.
            List<Panel> panels = Corner();
            panels.Add(WallY("G", 0.5, 0.2, 6, 0.15, 2.8));

            // Act
            Dictionary<string, Panel> result = BySource(panels.SnapToJunctions(new Core.Diagnostics(), Snap));

            // Assert: G is kept and both slab edges still move onto it.
            Assert.True(result.ContainsKey("G"));
            Assert.Equal(0.5, result["S1"].GetFace3D().GetBoundingBox().Max.Y, 9);
            Assert.Equal(0.5, result["S2"].GetFace3D().GetBoundingBox().Min.Y, 9);
        }

        [Fact]
        public void SnapToJunctions_JunctionBlockBetweenCollinearWalls_IsKeptAndCrossWallReachesMidPlane()
        {
            // Arrange: 0.4 m wall pieces A and B on one mid-plane (y 10) with a 0.3 m junction block J between them,
            // shorter than the wall thickness, and a 0.2 m cross wall K ending at the walls' face.
            List<Panel> panels = new List<Panel>
            {
                WallY("A", 10, -3, 0, 0, 3),
                WallY("J", 10, 0, 0.3, 0, 3),
                WallY("B", 10, 0.3, 3, 0, 3),
                WallX("K", 1.5, 10.2, 13, 0, 3),
            };

            // Act
            Dictionary<string, Panel> result = BySource(panels.SnapToJunctions(new Core.Diagnostics(), Snap));

            // Assert: J is kept unchanged and K reaches the shared mid-plane.
            Assert.True(result.ContainsKey("J"));
            Assert.Equal(0.9, result["J"].GetArea(), 9);
            Assert.Equal(10, result["K"].GetFace3D().GetBoundingBox().Min.Y, 9);
        }

        [Fact]
        public void SnapToJunctions_ReorderedInput_GivesSameResult()
        {
            // Arrange: the strip scene needs a pass that drops X and a pass that revives it.
            List<Panel> panels = CornerWithStrip();
            panels.Add(Block());
            List<Panel> reversed = Enumerable.Reverse(panels).ToList();
            List<Panel> byGuid = panels.OrderBy(x => x.Guid).ToList();

            // Act
            List<Panel> result = panels.SnapToJunctions(new Core.Diagnostics(), Snap);

            // Assert
            AssertSame(result, reversed.SnapToJunctions(new Core.Diagnostics(), Snap));
            AssertSame(result, byGuid.SnapToJunctions(new Core.Diagnostics(), Snap));
        }

        [Fact]
        public void SnapToJunctions_DetailLevel_DoesNotChangePanels()
        {
            // Arrange
            List<Panel> panels = CornerWithStrip();
            panels.Add(Block());

            // Act
            Core.Diagnostics diagnostics_Detailed = new Core.Diagnostics(DiagnosticDetail.Detailed);
            List<Panel> result = panels.SnapToJunctions(diagnostics_Detailed, Snap);
            int warnings = diagnostics_Detailed.Count(x => x.Severity == DiagnosticSeverity.Warning);

            // Assert: identical panels at every level and with no collection; the Warnings survive Summary.
            foreach (DiagnosticDetail detail in new[] { DiagnosticDetail.Summary, DiagnosticDetail.Standard })
            {
                Core.Diagnostics diagnostics = new Core.Diagnostics(detail);
                AssertSame(result, panels.SnapToJunctions(diagnostics, Snap));
                Assert.Equal(warnings, diagnostics.Count(x => x.Severity == DiagnosticSeverity.Warning));
            }

            Assert.True(warnings > 0);
            AssertSame(result, panels.SnapToJunctions((Core.Diagnostics)null, Snap));
            AssertSame(result, panels.SnapToJunctions(out List<string> _, Snap));
        }

        [Fact]
        public void SnapToJunctions_DroppedSetNotSettledWithinPassLimit_ReportsWarning()
        {
            // Arrange: the corner needs two passes (the block drops in the first and the dropped set is confirmed in the
            // second); a limit of one pass cannot confirm it.
            List<Panel> panels = Corner();
            panels.Add(Block());
            Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Summary);

            // Act
            List<Panel> result = Analytical.Query.SnapToJunctions(panels, diagnostics, Snap, Tolerance.Distance, 1);

            // Assert: the last pass is returned (the block is dropped, the step still pulled onto it) and the run says so.
            Assert.Equal(4, result.Count);
            Diagnostic warning = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapNotSettled);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal(1, warning.Value.Value);
            Assert.Equal(1, warning.Limit.Value);
            Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapCollapsed);
        }
    }
}
