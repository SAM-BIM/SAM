// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.AperturesBySolids"/> on synthetic window and door solids (metres): frame and glass
    /// assemblies, door frames with leaves, stick frames, groups, non-planar assemblies and hosting with
    /// <see cref="Analytical.Modify.AddAperturesByApertures"/>.
    /// </summary>
    public class AperturesBySolidsTests
    {
        // A 2 x 1.2 m window in a wall at y 0..0.25: 0.06 m frame ring (y 0.09..0.16) around 0.01 m glass (y 0.12..0.13).
        private static Shell WindowFrame(double x0 = 1, double x1 = 3)
        {
            return Prism(XZ, Rectangle(x0, 0.9, x1, 2.1), new List<IList<(double U, double V)>> { Rectangle(x0 + 0.06, 0.96, x1 - 0.06, 2.04) }, 0.09, 0.16);
        }

        private static Shell WindowGlass(double x0 = 1, double x1 = 3, double gap = 0)
        {
            return Box(x0 + 0.06 + gap, 0.12, 0.96 + gap, x1 - 0.06 - gap, 0.13, 2.04 - gap);
        }

        private static ApertureSolid Solid(ISAMGeometry3D geometry, ApertureType apertureType, string name, string groupKey = null)
        {
            return new ApertureSolid(geometry, apertureType) { SourceName = name, SourceId = name + "-id", SourceLayer = apertureType == ApertureType.Door ? "Doors" : "Windows", GroupKey = groupKey };
        }

        private static (double Width, double Height) Size(Aperture aperture)
        {
            Face3D face3D = aperture.GetFace3D();
            Plane plane = face3D.GetPlane();
            Geometry.Planar.BoundingBox2D boundingBox2D = new Geometry.Planar.BoundingBox2D(((Geometry.Planar.ISegmentable2D)plane.Convert(face3D).ExternalEdge2D).GetPoints());
            double a = boundingBox2D.Width, b = boundingBox2D.Height;
            return (System.Math.Max(a, b), System.Math.Min(a, b));
        }

        [Fact]
        public void AperturesBySolids_FrameRingAndGlass_ReturnsOneWindowWithFullFrameOutline()
        {
            // Arrange
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(WindowFrame(), ApertureType.Window, "frame"), Solid(WindowGlass(), ApertureType.Window, "glass") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out List<string> diagnostics, out List<int> excluded);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(ApertureType.Window, aperture.ApertureType);
            Assert.Equal(2.4, aperture.GetArea(), 6);
            Assert.Equal(1.0, System.Math.Abs(aperture.Plane.Normal.Y), 6);
            Assert.Equal(0.125, aperture.GetFace3D().GetBoundingBox().Min.Y, 6);
            Assert.Equal(0.125, aperture.GetFace3D().GetBoundingBox().Max.Y, 6);
            Assert.Empty(excluded);
            Assert.Equal("frame; glass", aperture.GetValue<string>(ApertureSourceParameter.SourceName));
            Assert.Equal("frame-id; glass-id", aperture.GetValue<string>(ApertureSourceParameter.SourceId));
            Assert.Equal("Windows", aperture.GetValue<string>(ApertureSourceParameter.SourceLayer));
            Assert.Equal(0.07, aperture.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.Contains(diagnostics, x => x.Contains("from 2 solid(s)"));
        }

        [Fact]
        public void AperturesBySolids_FrameWithoutGlass_ReturnsFullOutlineNotTheRing()
        {
            // Arrange
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(WindowFrame(), ApertureType.Window, "frame") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Assert.Equal(2.4, Assert.Single(apertures).GetArea(), 6);
        }

        [Fact]
        public void AperturesBySolids_GlassWithGapInsideFrame_JoinsFrameWithinMaxGap()
        {
            // Arrange: 5 mm clearance between glass and frame.
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(WindowGlass(gap: 0.005), ApertureType.Window, "glass"), Solid(WindowFrame(), ApertureType.Window, "frame") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _, maxGap: 0.01);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(2.4, aperture.GetArea(), 6);
            Assert.Equal("glass; frame", aperture.GetValue<string>(ApertureSourceParameter.SourceName));
        }

        [Fact]
        public void AperturesBySolids_DoorFrameAsWindowWithDoorLeaf_ReturnsOneDoor()
        {
            // Arrange: a U-shaped door frame given as window geometry (as on a "Window Frames" layer) and its leaf as a door.
            List<(double U, double V)> uFrame = new List<(double U, double V)> { (4.1, 0), (4.165, 0), (4.165, 2.035), (4.935, 2.035), (4.935, 0), (5, 0), (5, 2.1), (4.1, 2.1) };
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Prism(XZ, uFrame, null, 0.09, 0.16), ApertureType.Window, "front door frame"),
                Solid(Box(4.165, 0.095, 0, 4.935, 0.155, 2.035), ApertureType.Door, "front door"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out List<string> diagnostics, out _);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(ApertureType.Door, aperture.ApertureType);
            Assert.Equal(0.9 * 2.1, aperture.GetArea(), 6);
            Assert.Contains(diagnostics, x => x.Contains("window members joined a door"));
        }

        [Fact]
        public void AperturesBySolids_DoorLeafAlone_ReturnsDoorOnLeafMidPlane()
        {
            // Arrange: internal door leaf centred in a partition at x = 4.
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(Box(3.98, 1.005, 0, 4.02, 1.895, 2.095), ApertureType.Door, "door") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(ApertureType.Door, aperture.ApertureType);
            Assert.Equal(0.89 * 2.095, aperture.GetArea(), 6);
            Assert.Equal(4.0, aperture.GetFace3D().GetBoundingBox().Min.X, 6);
            Assert.Equal(4.0, aperture.GetFace3D().GetBoundingBox().Max.X, 6);
        }

        [Fact]
        public void AperturesBySolids_StickFrameAndGlass_ReturnsOneWindowInTheWallPlane()
        {
            // Arrange: four separate frame members, each thinner in the wall plane (0.06 m) than deep (0.07 m).
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Box(1, 0.09, 0.9, 1.06, 0.16, 2.1), ApertureType.Window, "left"),
                Solid(Box(2.94, 0.09, 0.9, 3, 0.16, 2.1), ApertureType.Window, "right"),
                Solid(Box(1.06, 0.09, 2.04, 2.94, 0.16, 2.1), ApertureType.Window, "head"),
                Solid(Box(1.06, 0.09, 0.9, 2.94, 0.16, 0.96), ApertureType.Window, "sill"),
                Solid(WindowGlass(), ApertureType.Window, "glass"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out List<int> excluded);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(2.4, aperture.GetArea(), 6);
            Assert.Equal(1.0, System.Math.Abs(aperture.Plane.Normal.Y), 6);
            Assert.Empty(excluded);
        }

        [Fact]
        public void AperturesBySolids_TwoWindowsApart_ReturnsTwoApertures()
        {
            // Arrange
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(WindowFrame(1, 3), ApertureType.Window, "A frame"), Solid(WindowGlass(1, 3), ApertureType.Window, "A glass"),
                Solid(WindowFrame(3.5, 5.5), ApertureType.Window, "B frame"), Solid(WindowGlass(3.5, 5.5), ApertureType.Window, "B glass"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Assert.Equal(2, apertures.Count);
            Assert.All(apertures, x => Assert.Equal(2.4, x.GetArea(), 6));
            Assert.Equal(new[] { "A frame; A glass", "B frame; B glass" }, apertures.ConvertAll(x => x.GetValue<string>(ApertureSourceParameter.SourceName)));
        }

        [Fact]
        public void AperturesBySolids_TouchingWindowsWithoutGroupKeys_MergeIntoOneAssembly()
        {
            // Arrange: two frames sharing a jamb face.
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(WindowFrame(1, 3), ApertureType.Window, "A"), Solid(WindowFrame(3, 5), ApertureType.Window, "B") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Assert.Equal(4.8, Assert.Single(apertures).GetArea(), 6);
        }

        [Fact]
        public void AperturesBySolids_TouchingWindowsWithDifferentGroupKeys_StaySeparate()
        {
            // Arrange
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(WindowFrame(1, 3), ApertureType.Window, "A frame", "group A"), Solid(WindowGlass(1, 3), ApertureType.Window, "A glass", "group A"),
                Solid(WindowFrame(3, 5), ApertureType.Window, "B frame", "group B"), Solid(WindowGlass(3, 5), ApertureType.Window, "B glass"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Assert.Equal(2, apertures.Count);
            Assert.All(apertures, x => Assert.Equal(2.4, x.GetArea(), 6));
            Assert.Equal(new[] { "A frame; A glass", "B frame; B glass" }, apertures.ConvertAll(x => x.GetValue<string>(ApertureSourceParameter.SourceName)));
        }

        [Fact]
        public void AperturesBySolids_CornerWindowAcrossTwoWalls_SkippedAsNotPlanar()
        {
            // Arrange: two frames on perpendicular walls overlapping at the corner.
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Box(0.09, 0.09, 0.9, 1.2, 0.16, 2.1), ApertureType.Window, "south"),
                Solid(Box(0.09, 0.09, 0.9, 0.16, 1.2, 2.1), ApertureType.Window, "west"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out List<string> diagnostics, out List<int> excluded);

            // Assert
            Assert.Empty(apertures);
            Assert.Equal(new[] { 0, 1 }, excluded);
            Assert.Contains(diagnostics, x => x.Contains("not planar"));
        }

        [Fact]
        public void AperturesBySolids_RotatedWall_FollowsTheWallPlane()
        {
            // Arrange: the frame-and-glass window rotated 30 degrees about Z.
            double angle = System.Math.PI / 6, cos = System.Math.Cos(angle), sin = System.Math.Sin(angle);
            Point3D Rotated(double u, double v, double w) => new Point3D(u * cos - w * sin, u * sin + w * cos, v);
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Prism(Rotated, Rectangle(1, 0.9, 3, 2.1), new List<IList<(double U, double V)>> { Rectangle(1.06, 0.96, 2.94, 2.04) }, 0.09, 0.16), ApertureType.Window, "frame"),
                Solid(Prism(Rotated, Rectangle(1.06, 0.96, 2.94, 2.04), null, 0.12, 0.13), ApertureType.Window, "glass"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(2.4, aperture.GetArea(), 6);
            Assert.Equal(1.0, System.Math.Abs(aperture.Plane.Normal.DotProduct(new Vector3D(-sin, cos, 0))), 6);
            (double width, double height) = Size(aperture);
            Assert.Equal(2.0, width, 6);
            Assert.Equal(1.2, height, 6);
        }

        [Fact]
        public void AperturesBySolids_ApertureConstructions_AssignedByType()
        {
            // Arrange
            ApertureConstruction windowConstruction = new ApertureConstruction(Guid.NewGuid(), "Test Window", ApertureType.Window);
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(WindowFrame(), ApertureType.Window, "frame"),
                Solid(Box(3.98, 1.005, 0, 4.02, 1.895, 2.095), ApertureType.Door, "door"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _, new[] { windowConstruction });

            // Assert
            Assert.Equal(2, apertures.Count);
            Assert.Equal(windowConstruction.Guid, apertures.Single(x => x.ApertureType == ApertureType.Window).ApertureConstruction.Guid);
            ApertureConstruction doorConstruction = apertures.Single(x => x.ApertureType == ApertureType.Door).ApertureConstruction;
            Assert.NotNull(doorConstruction);
            Assert.Equal(ApertureType.Door, doorConstruction.ApertureType);
        }

        [Fact]
        public void AperturesBySolids_InvalidMember_ExcludedWithReason()
        {
            // Arrange
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(null, ApertureType.Window, "nothing"), Solid(WindowFrame(), ApertureType.Window, "frame") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out List<string> diagnostics, out List<int> excluded);

            // Assert
            Assert.Single(apertures);
            Assert.Equal(new[] { 0 }, excluded);
            Assert.Contains(diagnostics, x => x.Contains("'nothing': skipped"));
        }

        [Fact]
        public void AperturesBySolids_AddAperturesByApertures_HostsOnTheWallOnlyAndLeavesAirPanels()
        {
            // Arrange: south wall mid-surface at y = 0.125, west wall at x = 0.125 and an Air panel in a floor.
            Panel south = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0, 0.125, 0), new Point3D(6, 0.125, 0), new Point3D(6, 0.125, 3), new Point3D(0, 0.125, 3) })));
            Panel west = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0.125, 0, 0), new Point3D(0.125, 6, 0), new Point3D(0.125, 6, 3), new Point3D(0.125, 0, 3) })));
            Panel air = Analytical.Create.Panel(null, PanelType.Air, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(1, 1, 3), new Point3D(2, 1, 3), new Point3D(2, 2, 3), new Point3D(1, 2, 3) })));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(south);
            adjacencyCluster.AddObject(west);
            adjacencyCluster.AddObject(air);

            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(WindowFrame(), ApertureType.Window, "frame"), Solid(WindowGlass(), ApertureType.Window, "glass") };
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Act
            List<Aperture> hosted = adjacencyCluster.AddAperturesByApertures(apertures, true, Tolerance.MacroDistance, 0.1);

            // Assert
            Assert.Equal(2.4, Assert.Single(hosted).GetArea(), 6);
            Assert.Single(adjacencyCluster.GetObject<Panel>(south.Guid).Apertures);
            Assert.True(adjacencyCluster.GetObject<Panel>(west.Guid).Apertures == null || adjacencyCluster.GetObject<Panel>(west.Guid).Apertures.Count == 0);
            Panel air_After = adjacencyCluster.GetObject<Panel>(air.Guid);
            Assert.Equal(PanelType.Air, air_After.PanelType);
            Assert.True(air_After.Apertures == null || air_After.Apertures.Count == 0);
            Assert.Equal(1.0, air_After.GetArea(), 6);
        }
    }
}
