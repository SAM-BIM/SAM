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
    /// <see cref="Analytical.Create.PanelsBySolids"/>: fragments of an element (a pier, a slab patch, a stub) that fail
    /// maxThicknessRatio on their own are kept when they are pieces of a panel-like element, and nothing else is.
    /// </summary>
    public class PanelsBySolidsTests
    {
        private static List<List<Panel>> Run(IList<Shell> shells, out Core.Diagnostics diagnostics, IEnumerable<ISAMGeometry3D> apertures = null, double tolerance_Continuation = 0.01)
        {
            diagnostics = new Core.Diagnostics(DiagnosticDetail.Detailed);
            return shells.PanelsBySolids(diagnostics, apertures, tolerance_Continuation: tolerance_Continuation);
        }

        private static bool Kept(Core.Diagnostics diagnostics, int index)
        {
            return diagnostics.Any(x => x.SourceIndex == index && x.Code == BySolidsDiagnosticCode.PanelsContinuation);
        }

        [Fact]
        public void PanelsBySolids_PierTouchingLongWall_KeepsPierOnTheWallsMidPlane()
        {
            // Arrange: a 0.45 m wall 6 m long, and a 1.5 m pier of it (0.45 / 1.5 = 0.3, above the default 0.2).
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 6, 0.45, 3), Box(6, 0, 0, 7.5, 0.45, 3) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(shells[1].PanelsBySolid(new Core.Diagnostics()));
            Panel pier = Assert.Single(panels[1]);
            Assert.Equal(PanelType.Wall, pier.PanelType);
            Assert.Equal(4.5, pier.GetArea(), 3);
            Assert.All(((ISegmentable3D)pier.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.225, x.Y, 6));
            Assert.True(Kept(diagnostics, 1));
            Assert.DoesNotContain(diagnostics, x => x.SourceIndex == 1 && x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike);
        }

        [Fact]
        public void PanelsBySolids_WallAroundLargeOpeningReportedTooThin_IsKeptAsPieceOfTheWall()
        {
            // Arrange: a 0.45 m wall, and next to it a 1.844 m piece of it that is mostly a 1.818 x 2.87 m opening, leaving
            // 13 mm jambs. Its largest face pair is a jamb, so on its own it is reported below minThickness, while its own
            // 0.45 m plane is only not panel-like (0.45 / 1.844 = 0.244).
            Shell piece = Prism(XZ, Rectangle(6, 0, 7.844, 3.35), new List<IList<(double U, double V)>> { Rectangle(6.013, 0.001, 7.831, 2.871) }, 0, 0.45);
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 6, 0.45, 3.35), piece };
            Core.Diagnostics single = new Core.Diagnostics();
            Assert.Empty(piece.PanelsBySolid(single, minThickness: 0.04));
            Assert.Contains(single, x => x.Code == BySolidsDiagnosticCode.PanelsBelowMinThickness);

            // Act
            Core.Diagnostics diagnostics = new Core.Diagnostics(DiagnosticDetail.Detailed);
            List<List<Panel>> panels = shells.PanelsBySolids(diagnostics, minThickness: 0.04);

            // Assert
            Assert.Contains(panels[1], x => x.PanelType == PanelType.Wall);
            Assert.True(Kept(diagnostics, 1));
            Assert.DoesNotContain(diagnostics, x => x.SourceIndex == 1 && x.Code == BySolidsDiagnosticCode.PanelsBelowMinThickness);
        }

        [Fact]
        public void PanelsBySolids_KeptPiece_RecordsSummaryInfoWithRatioAndLimit()
        {
            // Arrange
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 6, 0.45, 3), Box(6, 0, 0, 7.5, 0.45, 3) };

            // Act
            Run(shells, out Core.Diagnostics diagnostics);

            // Assert: Info, kept at Summary (it keeps input the threshold alone would skip), measured ratio and limit.
            Diagnostic diagnostic = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.PanelsContinuation);
            Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
            Assert.Equal(DiagnosticDetail.Summary, diagnostic.Detail);
            Assert.Equal(1, diagnostic.SourceIndex);
            Assert.Equal(0.3, diagnostic.Value.Value, 3);
            Assert.Equal(0.2, diagnostic.Limit.Value, 6);
            Assert.Contains("Panels of #0", diagnostic.Message);
        }

        [Fact]
        public void PanelsBySolids_PiersAcrossWindow_KeepsBothAsOneElement()
        {
            // Arrange: two 1.5 m piers of a 0.45 m wall with a 1.2 m window between them; neither passes on its own.
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 1.5, 0.45, 3), Box(2.7, 0, 0, 4.2, 0.45, 3) };
            List<ISAMGeometry3D> window = new List<ISAMGeometry3D> { Box(1.5, 0.15, 0.9, 2.7, 0.3, 2.1) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, window);

            // Assert
            Assert.Single(panels[0]);
            Assert.Single(panels[1]);
            Assert.True(Kept(diagnostics, 0) && Kept(diagnostics, 1));
        }

        [Fact]
        public void PanelsBySolids_PiersWithoutWindowBetween_StaySkipped()
        {
            // Arrange: the same piers 1.2 m apart with nothing joining them.
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 1.5, 0.45, 3), Box(2.7, 0, 0, 4.2, 0.45, 3) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[0]);
            Assert.Empty(panels[1]);
            Assert.Equal(2, diagnostics.Count(x => x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike));
        }

        [Fact]
        public void PanelsBySolids_SlabPatchAcrossWallJunction_KeepsPatch()
        {
            // Arrange: a 0.15 m slab, a 0.1 m wall under its edge, and a 0.66 x 0.93 m patch of slab beyond the wall
            // (0.15 / 0.66 = 0.227).
            List<Shell> shells = new List<Shell> { Box(0, 0, 2.85, 4, 4, 3.0), Box(4, 0, 0, 4.1, 4, 2.85), Box(4.1, 0, 2.85, 4.76, 0.93, 3.0) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Panel patch = Assert.Single(panels[2]);
            Assert.Equal(PanelType.Floor, Analytical.Query.PanelType(patch.Normal));
            Assert.True(Kept(diagnostics, 2));
        }

        [Fact]
        public void PanelsBySolids_StubFramedByWallsAndSlabs_KeepsStub()
        {
            // Arrange: a 0.4 m stub 0.87 m long (0.46) between a 0.4 m wall and a 0.1 m partition, on a slab, under a slab.
            List<Shell> shells = new List<Shell>
            {
                Box(-3, 0, 0, 3, 0.4, 3), Box(-3, 1.27, 0, 3, 1.37, 3), Box(-3, -1, -0.2, 3, 2, 0), Box(-3, -1, 3, 3, 2, 3.2),
                Box(0, 0.4, 0, 0.4, 1.27, 3),
            };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Panel stub = Assert.Single(panels[4]);
            Assert.Equal(PanelType.Wall, stub.PanelType);
            Assert.All(((ISegmentable3D)stub.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.2, x.X, 6));
            Assert.Contains(diagnostics, x => x.SourceIndex == 4 && x.Code == BySolidsDiagnosticCode.PanelsContinuation && x.Message.Contains("closes a gap"));
        }

        [Fact]
        public void PanelsBySolids_StubWithOneFreeEdge_StaysSkipped()
        {
            // Arrange: the same stub without the slab above it.
            List<Shell> shells = new List<Shell> { Box(-3, 0, 0, 3, 0.4, 3), Box(-3, 1.27, 0, 3, 1.37, 3), Box(-3, -1, -0.2, 3, 2, 0), Box(0, 0.4, 0, 0.4, 1.27, 3) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[3]);
            Assert.Contains(diagnostics, x => x.SourceIndex == 3 && x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike);
        }

        [Fact]
        public void PanelsBySolids_StairAgainstWallOnSlab_StaysSkipped()
        {
            // Arrange: a four-step flight, 1 m wide, on a slab and against a wall.
            List<(double U, double V)> profile = new List<(double U, double V)> { (0.4, 0), (1.4, 0), (1.4, 0.68), (1.15, 0.68), (1.15, 0.51), (0.9, 0.51), (0.9, 0.34), (0.65, 0.34), (0.65, 0.17), (0.4, 0.17) };
            Shell stair = Prism((u, v, w) => new Point3D(w, u, v), profile, null, 0, 1);
            List<Shell> shells = new List<Shell> { Box(-3, 0, 0, 3, 0.4, 3), Box(-3, -1, -0.2, 3, 2, 0), stair };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[2]);
            Assert.False(Kept(diagnostics, 2));
        }

        [Fact]
        public void PanelsBySolids_StripWithinSlabThicknessOverWall_IsNotKept()
        {
            // Arrange: a 0.15 m slab, a 0.135 m wall under its edge, and a 0.135 x 0.15 m strip on the wall at slab level.
            List<Shell> shells = new List<Shell> { Box(0, 0, 3.4, 4, 4, 3.55), Box(0, 4, 0, 4, 4.135, 3.4), Box(0, 4, 3.4, 4, 4.135, 3.55) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert: the strip is the slab's edge, not a wall; keeping it would enclose a sliver of space.
            Assert.Empty(panels[2]);
            Assert.False(Kept(diagnostics, 2));
        }

        [Fact]
        public void PanelsBySolids_SliverTouchingWall_IsNotKept()
        {
            // Arrange: a 5 mm sliver at the end of a wall (as left by models rebuilt from other tools): not panel-like, and
            // narrower than minThickness (0.08 m) in its plane.
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 6, 0.45, 3), Box(6, 0, 0, 6.005, 0.45, 3) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[1]);
            Assert.False(Kept(diagnostics, 1));
        }

        [Fact]
        public void PanelsBySolids_ToleranceZero_GivesPanelsBySolidPerSolid()
        {
            // Arrange
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 6, 0.45, 3), Box(6, 0, 0, 7.5, 0.45, 3), SlabFixture() };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics, tolerance_Continuation: 0);

            // Assert: the pass is off, so each solid gives exactly what PanelsBySolid gives it, with the same records.
            for (int i = 0; i < shells.Count; i++)
            {
                Core.Diagnostics expected = new Core.Diagnostics(DiagnosticDetail.Detailed);
                List<Panel> single = shells[i].PanelsBySolid(expected, sourceIndex: i);
                Assert.Equal(single.Count, panels[i].Count);
                Assert.Equal(single.Select(x => System.Math.Round(x.GetArea(), 6)), panels[i].Select(x => System.Math.Round(x.GetArea(), 6)));
                Assert.Equal(expected.Select(x => x.ToString()), diagnostics.Where(x => x.SourceIndex == i).Select(x => x.ToString()));
            }
        }

        [Fact]
        public void PanelsBySolids_NullShell_ReportsNotClosed()
        {
            // Arrange
            List<Shell> shells = new List<Shell> { null, Box(0, 0, 0, 6, 0.45, 3) };

            // Act
            List<List<Panel>> panels = Run(shells, out Core.Diagnostics diagnostics);

            // Assert
            Assert.Empty(panels[0]);
            Assert.Single(panels[1]);
            Assert.Contains(diagnostics, x => x.SourceIndex == 0 && x.Code == BySolidsDiagnosticCode.PanelsNotClosed && x.Severity == DiagnosticSeverity.Warning);
        }

        [Fact]
        public void SnapToJunctions_PanelWithinCrossingWall_IsDroppedWithWarning()
        {
            // Arrange: a 0.1 m long wall panel lying across a crossing wall's mid-plane, nothing else near its ends:
            // both ends trim back onto the crossing wall and nothing is left.
            Face3D pier = new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0.05, 0, 0), new Point3D(0.15, 0, 0), new Point3D(0.15, 0, 3), new Point3D(0.05, 0, 3) }));
            Face3D crossing = new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0.1, -1, 0), new Point3D(0.1, 1, 0), new Point3D(0.1, 1, 3), new Point3D(0.1, -1, 3) }));
            List<Panel> panels = new List<Panel> { Analytical.Create.Panel(null, PanelType.Wall, pier), Analytical.Create.Panel(null, PanelType.Wall, crossing) };
            Core.Diagnostics diagnostics = new Core.Diagnostics();

            // Act
            List<Panel> result = panels.SnapToJunctions(diagnostics, 0.3);

            // Assert: no zero-area panel comes back; the drop is a Warning.
            Panel kept = Assert.Single(result);
            Assert.Equal(panels[1].Guid, kept.Guid);
            Diagnostic warning = Assert.Single(diagnostics, x => x.Code == BySolidsDiagnosticCode.SnapCollapsed);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal(0.3, warning.Value.Value, 6);
        }

        private static Shell SlabFixture()
        {
            return Box(0, 0, 2.8, 6, 4, 3.0);
        }
    }
}
