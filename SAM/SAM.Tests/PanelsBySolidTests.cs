// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolid"/> on synthetic building elements (metres): walls, slabs with stair
    /// voids, partitions with door notches, gables, and elements that are not panel-like.
    /// </summary>
    public class PanelsBySolidTests
    {
        /// <summary>
        /// A prism: the profile (u, v) with optional holes, extruded from w0 to w1, mapped to 3D by
        /// <paramref name="map"/>(u, v, w).
        /// </summary>
        internal static Shell Prism(Func<double, double, double, Point3D> map, IList<(double U, double V)> outer, IList<IList<(double U, double V)>> holes, double w0, double w1)
        {
            List<IList<(double U, double V)>> loops = new List<IList<(double U, double V)>> { outer };
            if (holes != null)
            {
                loops.AddRange(holes);
            }

            List<Face3D> face3Ds = new List<Face3D>();
            foreach (double w in new[] { w0, w1 })
            {
                List<IClosedPlanar3D> closedPlanar3Ds = loops.ConvertAll(loop => (IClosedPlanar3D)new Polygon3D(loop.Select(p => map(p.U, p.V, w)).ToList()));
                face3Ds.Add(Face3D.Create(closedPlanar3Ds));
            }

            foreach (IList<(double U, double V)> loop in loops)
            {
                for (int i = 0; i < loop.Count; i++)
                {
                    (double U, double V) a = loop[i], b = loop[(i + 1) % loop.Count];
                    face3Ds.Add(new Face3D(new Polygon3D(new List<Point3D> { map(a.U, a.V, w0), map(b.U, b.V, w0), map(b.U, b.V, w1), map(a.U, a.V, w1) })));
                }
            }

            return new Shell(face3Ds);
        }

        // Vertical element in the XZ plane, thick along Y.
        internal static Point3D XZ(double u, double v, double w) => new Point3D(u, w, v);

        // Horizontal element in the XY plane, thick along Z.
        internal static Point3D XY(double u, double v, double w) => new Point3D(u, v, w);

        internal static IList<(double U, double V)> Rectangle(double u0, double v0, double u1, double v1)
        {
            return new List<(double U, double V)> { (u0, v0), (u1, v0), (u1, v1), (u0, v1) };
        }

        internal static Shell Box(double x0, double y0, double z0, double x1, double y1, double z1)
        {
            return Prism(XY, Rectangle(x0, y0, x1, y1), null, z0, z1);
        }

        private static Shell SlabWithStairVoid()
        {
            // 6 x 4 m slab, 0.2 m thick (z 2.8..3.0), with a 1 x 2 m stair void.
            return Prism(XY, Rectangle(0, 0, 6, 4), new List<IList<(double U, double V)>> { Rectangle(2, 1, 3, 3) }, 2.8, 3.0);
        }

        private static Shell PartitionWithDoorNotch()
        {
            // 4 m long, 3 m high, 0.1 m thick partition (y 0..0.1) with a 1 m wide, 2 m high door opening reaching the floor.
            List<(double U, double V)> outline = new List<(double U, double V)> { (0, 0), (1, 0), (1, 2), (2, 2), (2, 0), (4, 0), (4, 3), (0, 3) };
            return Prism(XZ, outline, null, 0, 0.1);
        }

        [Fact]
        public void PanelsBySolid_Wall_ReturnsOneWallPanelOnMidPlane()
        {
            // Arrange
            Shell wall = Box(0, 0, 0, 4, 0.25, 3);

            // Act
            List<Panel> panels = wall.PanelsBySolid(out List<string> diagnostics, sourceName: "GF South wall", sourceId: "abc", sourceLayer: "Ground Floor::Walls");

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(PanelType.Wall, panel.PanelType);
            Assert.Equal(12.0, panel.GetArea(), 3);
            Assert.All(((ISegmentable3D)panel.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.125, x.Y, 6));
            Assert.True(panel.TryGetValue(PanelSourceParameter.SourceName, out string name) && name == "GF South wall");
            Assert.True(panel.TryGetValue(PanelSourceParameter.SourceLayer, out string layer) && layer == "Ground Floor::Walls");
            Assert.True(panel.TryGetValue(PanelSourceParameter.SourceThickness, out double thickness));
            Assert.Equal(0.25, thickness, 6);
        }

        [Fact]
        public void PanelsBySolid_SlabWithUnclaimedStairVoid_KeepsHoleAndAddsAirPanel()
        {
            // Arrange
            Shell slab = SlabWithStairVoid();

            // Act
            List<Panel> panels = slab.PanelsBySolid(out _, sourceName: "UF slab");

            // Assert
            Assert.Equal(2, panels.Count);
            Panel host = Assert.Single(panels, x => x.PanelType != PanelType.Air);
            Panel air = Assert.Single(panels, x => x.PanelType == PanelType.Air);
            Assert.Equal(22.0, host.GetArea(), 3);
            Assert.Equal(2.0, air.GetArea(), 3);
            Assert.All(((ISegmentable3D)air.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(2.9, x.Z, 6));
            Assert.True(air.TryGetValue(PanelSourceParameter.SourceRole, out string role) && role == "Opening");
            Assert.True(air.TryGetValue(PanelSourceParameter.SourceName, out string name) && name == "UF slab");
        }

        [Fact]
        public void PanelsBySolid_SlabVoidClaimedByAperture_FillsHole()
        {
            // Arrange: something (e.g. a roof light) occupies the opening.
            Shell slab = SlabWithStairVoid();
            List<ISAMGeometry3D> apertures = new List<ISAMGeometry3D> { Box(2.1, 1.1, 2.85, 2.9, 2.9, 2.95) };

            // Act
            List<Panel> panels = slab.PanelsBySolid(out _, apertures);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(24.0, panel.GetArea(), 3);
        }

        [Fact]
        public void PanelsBySolid_SlabVoidWithAirDisabled_KeepsHoleWithoutAirPanel()
        {
            // Arrange
            Shell slab = SlabWithStairVoid();

            // Act
            List<Panel> panels = slab.PanelsBySolid(out _, airForUnclaimedOpenings: false);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(22.0, panel.GetArea(), 3);
        }

        [Fact]
        public void PanelsBySolid_DoorNotchClaimedByDoorLeaf_FillsNotch()
        {
            // Arrange
            Shell partition = PartitionWithDoorNotch();
            List<ISAMGeometry3D> door = new List<ISAMGeometry3D> { Box(1.0, 0.03, 0, 2.0, 0.07, 2.0) };

            // Act
            List<Panel> panels = partition.PanelsBySolid(out _, door);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(12.0, panel.GetArea(), 3);
        }

        [Fact]
        public void PanelsBySolid_UnclaimedNotch_KeepsRealOutline()
        {
            // Arrange
            Shell partition = PartitionWithDoorNotch();

            // Act
            List<Panel> panels = partition.PanelsBySolid(out _);

            // Assert: a notch is not a hole - without an aperture it is the element's real shape, never an Air panel.
            Panel panel = Assert.Single(panels);
            Assert.Equal(10.0, panel.GetArea(), 3);
        }

        [Fact]
        public void PanelsBySolid_Gable_ReturnsTriangularPanel()
        {
            // Arrange: 8 m wide, 2.8 m high triangular gable, 0.25 m thick (x 0..0.25), profile in the YZ plane.
            Shell gable = Prism((u, v, w) => new Point3D(w, u, v), new List<(double U, double V)> { (0, 0), (8, 0), (4, 2.8) }, null, 0, 0.25);

            // Act
            List<Panel> panels = gable.PanelsBySolid(out _);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(PanelType.Wall, panel.PanelType);
            Assert.Equal(11.2, panel.GetArea(), 3);
        }

        [Fact]
        public void PanelsBySolid_LongWallWithReturnFaces_PairsTheLargeOppositeFaces()
        {
            // Arrange: the ends (0.25 x 3) are parallel to each other and 4 m apart, but barely overlap - the
            // thickness pair must be the two large faces, not the ends.
            Shell wall = Box(0, 0, 0, 4, 0.25, 3);

            // Act
            wall.PanelsBySolid(out _).Single().TryGetValue(PanelSourceParameter.SourceThickness, out double thickness);

            // Assert
            Assert.Equal(0.25, thickness, 6);
        }

        [Fact]
        public void PanelsBySolid_StairTread_IsSkippedAsNotPanelLike()
        {
            // Arrange: 0.95 x 0.25 x 0.2 tread.
            Shell tread = Box(0, 0, 0, 0.95, 0.25, 0.2);

            // Act
            List<Panel> panels = tread.PanelsBySolid(out List<string> diagnostics);

            // Assert
            Assert.Empty(panels);
            Assert.Contains(diagnostics, x => x.Contains("not panel-like") || x.Contains("no pair"));
        }

        private static BoundingBox3D SnappedBox(List<Panel> panels, string sourceName)
        {
            return panels.Single(x => x.TryGetValue(PanelSourceParameter.SourceName, out string name) && name == sourceName).GetFace3D().GetBoundingBox();
        }

        [Fact]
        public void SnapToJunctions_LCorner_WallsMeetOnSharedMidPlanes()
        {
            // Arrange: wall A runs along X (mid-plane y = 0.125) over the corner; wall B runs along Y
            // (mid-plane x = 0.125) and stops at A's inner face (y = 0.25).
            List<Panel> panels = Box(0, 0, 0, 4, 0.25, 3).PanelsBySolid(out _, sourceName: "A")
                .Concat(Box(0, 0.25, 0, 0.25, 4, 3).PanelsBySolid(out _, sourceName: "B")).ToList();

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out List<string> diagnostics, 0.3);

            // Assert: A is trimmed back to B's mid-plane, B is extended to A's mid-plane.
            Assert.Equal(0.125, SnappedBox(snapped, "A").Min.X, 6);
            Assert.Equal(0.125, SnappedBox(snapped, "B").Min.Y, 6);
            Assert.Equal(panels.Select(x => x.Guid).OrderBy(x => x), snapped.Select(x => x.Guid).OrderBy(x => x));
        }

        [Fact]
        public void SnapToJunctions_ParallelPlaneWithoutLocalJunction_IsIgnored()
        {
            // Arrange: D's mid-plane (x = 0.2) is within snap reach of A's end edge, but D is 5 m away - there is
            // no junction, so A must keep its end.
            List<Panel> panels = Box(0, 0, 0, 4, 0.25, 3).PanelsBySolid(out _, sourceName: "A")
                .Concat(Box(0.075, 5, 0, 0.325, 9, 3).PanelsBySolid(out _, sourceName: "D")).ToList();

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out _, 0.3);

            // Assert
            Assert.Equal(0.0, SnappedBox(snapped, "A").Min.X, 6);
        }

        [Fact]
        public void SnapToJunctions_WallUnderCeilingAndRoof_ExtendsToTheFarthestEnclosingPlane()
        {
            // Arrange: a wall (top z = 3) under a ceiling slab (mid z = 3.075) and a flat roof slab (mid z = 3.2),
            // both bearing on it. The wall carries on past the ceiling T-junction up to the roof, which closes the
            // void between ceiling and roof.
            List<Panel> panels = Box(0, 0, 0, 4, 0.25, 3).PanelsBySolid(out _, sourceName: "Wall")
                .Concat(Box(0, 0.25, 3, 4, 4, 3.15).PanelsBySolid(out _, sourceName: "Ceiling", maxThicknessRatio: 0.5))
                .Concat(Box(0, 0, 3.1, 4, 4, 3.3).PanelsBySolid(out _, sourceName: "Roof", maxThicknessRatio: 0.5)).ToList();

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out _, 0.3);

            // Assert
            Assert.Equal(3.2, SnappedBox(snapped, "Wall").Max.Z, 6);
        }

        [Fact]
        public void SnapToJunctions_ZeroDistance_ReturnsUnchangedCopies()
        {
            // Arrange
            List<Panel> panels = Box(0, 0, 0, 4, 0.25, 3).PanelsBySolid(out _, sourceName: "A");

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out _, 0);

            // Assert
            Assert.Equal(panels.Single().GetArea(), snapped.Single().GetArea(), 9);
        }

        [Fact]
        public void PanelsBySolid_Balustrade_IsSkippedBelowMinThickness()
        {
            // Arrange: 0.05 m thick balustrade.
            Shell balustrade = Box(0, 0, 0, 3.5, 0.05, 1.0);

            // Act
            List<Panel> panels = balustrade.PanelsBySolid(out List<string> diagnostics);

            // Assert
            Assert.Empty(panels);
            Assert.Contains(diagnostics, x => x.Contains("skipped"));
        }
    }
}
