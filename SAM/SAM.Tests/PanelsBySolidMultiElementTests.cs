// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolid"/> on solids that hold several planar elements (a ring of walls, an
    /// L-shaped wall, a roof with two pitches), on the parts of one element that have another thickness (a window
    /// niche), and on the panel type of horizontal and sloped elements.
    /// </summary>
    public class PanelsBySolidMultiElementTests
    {
        private static double PhysicalThickness(Panel panel)
        {
            Assert.True(panel.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness));
            return thickness;
        }

        private static Plane Plane(Panel panel) => panel.GetFace3D().GetPlane();

        [Fact]
        public void PanelsBySolid_RingOfWalls_GivesOneWallPanelPerWall()
        {
            // Arrange: one solid holding all four external walls of an 8 x 6 m plan, 0.3 m thick, 1.2 m high. The low
            // height makes the top and bottom faces (1.2 m apart over the whole ring) panel-like by proportion alone:
            // only the ownership rule (that material is the walls') keeps them from becoming a horizontal panel.
            Shell ring = Prism(XY, Rectangle(0, 0, 8, 6), new List<IList<(double U, double V)>> { Rectangle(0.3, 0.3, 7.7, 5.7) }, 0, 1.2);

            // Act
            List<Panel> panels = ring.PanelsBySolid(out List<string> diagnostics);

            // Assert
            Assert.Equal(4, panels.Count);
            Assert.All(panels, x => Assert.Equal(PanelType.Wall, x.PanelType));
            Assert.All(panels, x => Assert.Equal(0.3, PhysicalThickness(x), 6));

            List<double> xs = panels.Where(x => System.Math.Abs(Plane(x).Normal.X) > 0.99).Select(x => System.Math.Round(Plane(x).Origin.X, 6)).OrderBy(x => x).ToList();
            List<double> ys = panels.Where(x => System.Math.Abs(Plane(x).Normal.Y) > 0.99).Select(x => System.Math.Round(Plane(x).Origin.Y, 6)).OrderBy(x => x).ToList();
            Assert.Equal(new List<double> { 0.15, 7.85 }, xs);
            Assert.Equal(new List<double> { 0.15, 5.85 }, ys);

            // Each wall runs the full length of its side of the ring (to the outer faces at the corners).
            Assert.All(panels.Where(x => System.Math.Abs(Plane(x).Normal.X) > 0.99), x => Assert.Equal(6 * 1.2, x.GetArea(), 3));
            Assert.All(panels.Where(x => System.Math.Abs(Plane(x).Normal.Y) > 0.99), x => Assert.Equal(8 * 1.2, x.GetArea(), 3));
        }

        [Fact]
        public void PanelsBySolid_LShapedWall_GivesBothLegs()
        {
            // Arrange: a 0.2 m thick L in plan (4 m along X, 3 m along Y), 2.7 m high.
            List<(double U, double V)> outline = new List<(double U, double V)> { (0, 0), (4, 0), (4, 0.2), (0.2, 0.2), (0.2, 3), (0, 3) };
            Shell wall = Prism(XY, outline, null, 0, 2.7);

            // Act
            List<Panel> panels = wall.PanelsBySolid(out _);

            // Assert
            Assert.Equal(2, panels.Count);
            Assert.All(panels, x => Assert.Equal(0.2, PhysicalThickness(x), 6));
            Assert.Single(panels, x => System.Math.Abs(Plane(x).Normal.Y) > 0.99 && System.Math.Abs(x.GetArea() - 4 * 2.7) < 1e-3);
            Assert.Single(panels, x => System.Math.Abs(Plane(x).Normal.X) > 0.99 && System.Math.Abs(x.GetArea() - 3 * 2.7) < 1e-3);
        }

        [Fact]
        public void PanelsBySolid_TwoPitchRoof_GivesOneRoofPanelPerPitch()
        {
            // Arrange: a 30 degree two-pitch roof, 8 m span, 6 m long (along X), 0.25 m thick normal to the slope,
            // modelled as one solid (profile in YZ, extruded along X).
            double slope = System.Math.PI / 6, thickness = 0.25;
            double h = 4 * System.Math.Tan(slope), d = thickness / System.Math.Cos(slope);
            List<(double U, double V)> profile = new List<(double U, double V)> { (0, 0), (4, h), (8, 0), (8, -d), (4, h - d), (0, -d) };
            Shell roof = Prism((u, v, w) => new Point3D(w, u, v), profile, null, 0, 6);

            // Act
            List<Panel> panels = roof.PanelsBySolid(out _);

            // Assert
            Assert.Equal(2, panels.Count);
            Assert.All(panels, x => Assert.Equal(PanelType.Roof, x.PanelType));
            Assert.All(panels, x => Assert.True(Plane(x).Normal.Z > 0));
            Assert.All(panels, x => Assert.Equal(thickness, PhysicalThickness(x), 6));
            Assert.Single(panels, x => Plane(x).Normal.Y > 0.4);
            Assert.Single(panels, x => Plane(x).Normal.Y < -0.4);
        }

        [Fact]
        public void PanelsBySolid_LowTriangularGable_IsPanelLike()
        {
            // Arrange: a 6 m wide, 1.4 m high, 0.3 m thick gable (as UF_Gable_Cross_South of the contemporary house):
            // thickness / height = 0.214, which the former default ratio of 0.2 rejected (the default is now 0.25).
            Shell gable = Prism((u, v, w) => new Point3D(u, w, v), new List<(double U, double V)> { (0, 0), (6, 0), (3, 1.4) }, null, 0, 0.3);

            // Act
            List<Panel> panels = gable.PanelsBySolid(out _);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(PanelType.Wall, panel.PanelType);
            Assert.Equal(0.3, PhysicalThickness(panel), 6);
            Assert.Equal(4.2, panel.GetArea(), 3);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(2.8)]
        public void PanelsBySolid_StairTreadBlock_IsNotPanelLikeByDefault(double height)
        {
            // Arrange: a 0.95 m wide, 0.25 m deep tread modelled as a block from the floor (as the treads of the accepted
            // 2026-10-14 house, Untitled2_meters.3dm): 0.25 / 0.95 = 0.263, just above the default ratio of 0.25.
            Shell tread = Box(0, 0, 0, 0.95, 0.25, height);

            // Act
            List<Panel> panels = tread.PanelsBySolid(out List<string> diagnostics);

            // Assert
            Assert.Empty(panels);
            Assert.Contains(diagnostics, x => x.Contains("not panel-like") && x.Contains("0.263"));
        }

        [Fact]
        public void PanelsBySolid_StairFlightBlock_IsStillNotPanelLike()
        {
            // Arrange: a 1.125 m wide, 2.8 m long, 3 m high block (the proportions of the house's stair solid).
            Shell stair = Box(0, 0, 0, 1.125, 2.8, 3);

            // Act
            List<Panel> panels = stair.PanelsBySolid(out List<string> diagnostics);

            // Assert
            Assert.Empty(panels);
            Assert.Contains(diagnostics, x => x.Contains("not panel-like"));
        }

        [Fact]
        public void PanelsBySolid_WallWithWindowNiche_IsOneElementOfVariableThickness()
        {
            // Arrange: a 0.3 m wall (y 0..0.3) with a 1 x 1 m window niche cut 0.2 m deep from the outside (y 0.1..0.3),
            // leaving 0.1 m of wall behind the window, and the window occupying the niche.
            Shell wall = WallWithNiche();
            List<ISAMGeometry3D> window = new List<ISAMGeometry3D> { Box(1, 0.1, 1, 2, 0.3, 2) };

            // Act
            List<Panel> panels = wall.PanelsBySolid(out List<string> diagnostics, window);

            // Assert: no second panel 0.1 m thick on its own mid-plane; the niche is filled for the window.
            Panel panel = Assert.Single(panels);
            Assert.Equal(0.3, PhysicalThickness(panel), 6);
            Assert.Equal(0.15, Plane(panel).Origin.Y, 6);
            Assert.Equal(12.0, panel.GetArea(), 3);
            Assert.Contains(diagnostics, x => x.Contains("variable thickness") && x.Contains("0.1"));
        }

        [Fact]
        public void PanelsBySolid_HorizontalSlab_IsFloorFacingDownWhateverTheFaceOrder()
        {
            // Arrange: the same slab with its faces generated bottom-first and top-first.
            Shell slab = Prism(XY, Rectangle(0, 0, 6, 4), null, 2.8, 3.0);
            Shell slab_Reversed = Prism(XY, Rectangle(0, 0, 6, 4), null, 3.0, 2.8);

            // Act
            Panel panel = Assert.Single(slab.PanelsBySolid(out _));
            Panel panel_Reversed = Assert.Single(slab_Reversed.PanelsBySolid(out _));

            // Assert
            Assert.Equal(PanelType.Floor, panel.PanelType);
            Assert.Equal(PanelType.Floor, panel_Reversed.PanelType);
            Assert.True(Plane(panel).Normal.Z < 0);
            Assert.True(Plane(panel_Reversed).Normal.Z < 0);
        }

        [Fact]
        public void PanelsBySolid_PitchedRoof_IsRoofFacingUpWhateverTheFaceOrder()
        {
            // Arrange: a 25 degree roof slab, 0.25 m thick, generated with its two faces in both orders.
            double sin = System.Math.Sin(25 * System.Math.PI / 180), cos = System.Math.Cos(25 * System.Math.PI / 180);
            Point3D Map(double u, double v, double w) => new Point3D(u, v * cos - w * sin, 3 + v * sin + w * cos);
            Shell roof = Prism(Map, Rectangle(0, 0, 8, 5), null, 0, 0.25);
            Shell roof_Reversed = Prism(Map, Rectangle(0, 0, 8, 5), null, 0.25, 0);

            // Act
            Panel panel = Assert.Single(roof.PanelsBySolid(out _));
            Panel panel_Reversed = Assert.Single(roof_Reversed.PanelsBySolid(out _));

            // Assert
            Assert.Equal(PanelType.Roof, panel.PanelType);
            Assert.Equal(PanelType.Roof, panel_Reversed.PanelType);
            Assert.True(Plane(panel).Normal.Z > 0);
            Assert.True(Plane(panel_Reversed).Normal.Z > 0);
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(0.2)]
        [InlineData(0.3)]
        [InlineData(0.6)]
        [InlineData(1.0)]
        public void PanelsBySolid_WallOfAnyThickness_PhysicalThicknessIsTheOpposingFaceDistance(double thickness)
        {
            // Arrange: a 10 m long, 3 m high wall; the ratio is opened up so only the geometry decides.
            Shell wall = Box(0, 0, 0, 10, thickness, 3);

            // Act
            List<Panel> panels = wall.PanelsBySolid(out _, maxThicknessRatio: 1.0);

            // Assert
            Panel panel = Assert.Single(panels);
            Assert.Equal(PanelType.Wall, panel.PanelType);
            Assert.Equal(thickness, PhysicalThickness(panel), 6);
            Assert.Equal(thickness / 2, Plane(panel).Origin.Y, 6);
        }

        [Theory]
        [InlineData(0.3, 1.0)]
        [InlineData(0.6, 2.4)]
        [InlineData(1.0, 3.0)]
        public void PanelsBySolid_ThickOrLowWallRing_GivesFourWallsWhenTheRatioAllowsIt(double thickness, double height)
        {
            // Arrange: thick or low rings of walls. With the ratio opened up, the decomposition alone must still find the
            // four walls and no horizontal plane through them (the top and bottom faces span the whole ring but its
            // material is a band only `thickness` wide).
            Shell ring = Prism(XY, Rectangle(0, 0, 8, 6), new List<IList<(double U, double V)>> { Rectangle(thickness, thickness, 8 - thickness, 6 - thickness) }, 0, height);

            // Act
            List<Panel> panels = ring.PanelsBySolid(out _, maxThicknessRatio: 1.0);

            // Assert
            Assert.Equal(4, panels.Count);
            Assert.All(panels, x => Assert.Equal(PanelType.Wall, x.PanelType));
            Assert.All(panels, x => Assert.Equal(thickness, PhysicalThickness(x), 6));
        }

        [Fact]
        public void PanelsBySolid_ThickWallFailingTheDefaultRatio_IsSkippedWithTheRatioNeeded()
        {
            // Arrange: a 1.0 m thick, 3 m high wall: thickness / height = 0.333, above the default ratio.
            Shell wall = Box(0, 0, 0, 10, 1.0, 3);

            // Act
            List<Panel> panels = wall.PanelsBySolid(out List<string> diagnostics);

            // Assert: the proportion test, not the thickness, rejects it, and the log says what would keep it.
            Assert.Empty(panels);
            Assert.Contains(diagnostics, x => x.Contains("not panel-like") && x.Contains("0.333"));
        }

        [Fact]
        public void SnapToJunctions_SlabVoidReachingTheFacade_StillJoinsTheWallToTheSlab()
        {
            // Arrange: an 8 m wall (top z = 2.75) under a slab (z 2.75..3.0) whose 4 m stairwell void reaches the
            // facade over the middle of the wall (x 2..6, y 0..3): the slab meets the wall only at both ends, not at
            // the middle of the wall's top edge (the contemporary house's west wall).
            Shell wall = Box(0, 0, 0, 8, 0.3, 2.75);
            List<(double U, double V)> outline = new List<(double U, double V)> { (0, 0), (2, 0), (2, 3), (6, 3), (6, 0), (8, 0), (8, 6), (0, 6) };
            Shell slab = Prism(XY, outline, null, 2.75, 3.0);
            List<Panel> panels = wall.PanelsBySolid(out _, sourceName: "Wall").Concat(slab.PanelsBySolid(out _, sourceName: "Slab")).ToList();

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out _, 0.3);

            // Assert: the wall's top edge reaches the slab's mid-plane along its whole length.
            Panel wall_Snapped = snapped.Single(x => x.TryGetValue(PanelSourceParameter.SourceName, out string name) && name == "Wall");
            Assert.Equal(2.875, wall_Snapped.GetFace3D().GetBoundingBox().Max.Z, 6);
        }

        /// <summary>
        /// 4 m long, 3 m high wall, y 0..0.3, with a niche x 1..2, z 1..2 cut from the outer face (y = 0.3) to y = 0.1.
        /// </summary>
        private static Shell WallWithNiche()
        {
            Face3D Rect(params Point3D[] point3Ds) => new Face3D(new Polygon3D(point3Ds.ToList()));

            List<Face3D> face3Ds = new List<Face3D>
            {
                Rect(new Point3D(0, 0, 0), new Point3D(4, 0, 0), new Point3D(4, 0, 3), new Point3D(0, 0, 3)),
                Face3D.Create(new List<IClosedPlanar3D>
                {
                    new Polygon3D(new List<Point3D> { new Point3D(0, 0.3, 0), new Point3D(4, 0.3, 0), new Point3D(4, 0.3, 3), new Point3D(0, 0.3, 3) }),
                    new Polygon3D(new List<Point3D> { new Point3D(1, 0.3, 1), new Point3D(2, 0.3, 1), new Point3D(2, 0.3, 2), new Point3D(1, 0.3, 2) }),
                }),
                Rect(new Point3D(1, 0.1, 1), new Point3D(2, 0.1, 1), new Point3D(2, 0.1, 2), new Point3D(1, 0.1, 2)),
                Rect(new Point3D(1, 0.1, 1), new Point3D(1, 0.3, 1), new Point3D(1, 0.3, 2), new Point3D(1, 0.1, 2)),
                Rect(new Point3D(2, 0.1, 1), new Point3D(2, 0.3, 1), new Point3D(2, 0.3, 2), new Point3D(2, 0.1, 2)),
                Rect(new Point3D(1, 0.1, 1), new Point3D(2, 0.1, 1), new Point3D(2, 0.3, 1), new Point3D(1, 0.3, 1)),
                Rect(new Point3D(1, 0.1, 2), new Point3D(2, 0.1, 2), new Point3D(2, 0.3, 2), new Point3D(1, 0.3, 2)),
                Rect(new Point3D(0, 0, 0), new Point3D(0, 0.3, 0), new Point3D(0, 0.3, 3), new Point3D(0, 0, 3)),
                Rect(new Point3D(4, 0, 0), new Point3D(4, 0.3, 0), new Point3D(4, 0.3, 3), new Point3D(4, 0, 3)),
                Rect(new Point3D(0, 0, 0), new Point3D(4, 0, 0), new Point3D(4, 0.3, 0), new Point3D(0, 0.3, 0)),
                Rect(new Point3D(0, 0, 3), new Point3D(4, 0, 3), new Point3D(4, 0.3, 3), new Point3D(0, 0.3, 3)),
            };

            return new Shell(face3Ds);
        }
    }
}
