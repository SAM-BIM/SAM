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
    //Without apertureConstructions every aperture takes its construction from ActiveSetting's default aperture
    //construction library, which PartFTransferAirDoorTests temporarily replaces with an empty one; sharing its
    //collection keeps the two classes from running at the same time.
    [Collection("SAM.Analytical.ActiveSetting default aperture construction library")]
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
            Assert.Contains(diagnostics, x => x.Contains("from 2 solids"));
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
        public void AperturesBySolids_RowOfTouchingFramedWindows_StaysOneAperturePerWindow()
        {
            // Arrange: three frame-and-glass windows in a row, each frame sharing a jamb face with the next (as the
            // Softhouse ribbons of doors and windows), no group keys.
            List<ApertureSolid> solids = new List<ApertureSolid>();
            foreach ((double x0, double x1, string name) in new[] { (1.0, 3.0, "A"), (3.0, 5.0, "B"), (5.0, 7.0, "C") })
            {
                solids.Add(Solid(WindowGlass(x0, x1), ApertureType.Window, name + " glass"));
                solids.Add(Solid(WindowFrame(x0, x1), ApertureType.Window, name + " frame"));
            }

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out List<int> excluded);

            // Assert
            Assert.Equal(3, apertures.Count);
            Assert.All(apertures, x => Assert.Equal(2.4, x.GetArea(), 6));
            Assert.Equal(new[] { "A glass; A frame", "B glass; B frame", "C glass; C frame" }, apertures.ConvertAll(x => x.GetValue<string>(ApertureSourceParameter.SourceName)));
            Assert.Empty(excluded);
        }

        [Fact]
        public void AperturesBySolids_SashInsideFrame_IsOneWindowWithItsGlass()
        {
            // Arrange: an outer frame, a sash ring inside it and the glass inside the sash.
            Shell frame = WindowFrame();
            Shell sash = Prism(XZ, Rectangle(1.06, 0.96, 2.94, 2.04), new List<IList<(double U, double V)>> { Rectangle(1.1, 1.0, 2.9, 2.0) }, 0.1, 0.15);
            Shell glass = Box(1.1, 0.12, 1.0, 2.9, 0.13, 2.0);
            List<ApertureSolid> solids = new List<ApertureSolid> { Solid(glass, ApertureType.Window, "glass"), Solid(sash, ApertureType.Window, "sash"), Solid(frame, ApertureType.Window, "frame") };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out _);

            // Assert
            Aperture aperture = Assert.Single(apertures);
            Assert.Equal(2.4, aperture.GetArea(), 6);
            Assert.Equal("glass; sash; frame", aperture.GetValue<string>(ApertureSourceParameter.SourceName));
        }

        // A rigid motion: rotation about Z by angle (degrees), then translation.
        private static Func<double, double, double, Point3D> Rigid(Func<double, double, double, Point3D> map, double angle, double dx = 3.7, double dy = -1.2, double dz = 0.5)
        {
            double radians = angle * System.Math.PI / 180, cos = System.Math.Cos(radians), sin = System.Math.Sin(radians);
            return (u, v, w) =>
            {
                Point3D p = map(u, v, w);
                return new Point3D(p.X * cos - p.Y * sin + dx, p.X * sin + p.Y * cos + dy, p.Z + dz);
            };
        }

        // The inverse of Rigid with the same arguments.
        private static Point3D Unmoved(Point3D point3D, double angle, double dx = 3.7, double dy = -1.2, double dz = 0.5)
        {
            double radians = angle * System.Math.PI / 180, cos = System.Math.Cos(radians), sin = System.Math.Sin(radians);
            double x = point3D.X - dx, y = point3D.Y - dy;
            return new Point3D(x * cos + y * sin, -x * sin + y * cos, point3D.Z - dz);
        }

        private static Aperture BySource(List<Aperture> apertures, string sourceName)
        {
            return Assert.Single(apertures, x => x.GetValue<string>(ApertureSourceParameter.SourceName) == sourceName);
        }

        private static List<string> Members(Aperture aperture)
        {
            return aperture.GetValue<string>(ApertureSourceParameter.SourceName).Split(new[] { "; " }, StringSplitOptions.None).OrderBy(x => x).ToList();
        }

        private static IEnumerable<List<T>> Permutations<T>(List<T> items)
        {
            if (items.Count <= 1)
            {
                yield return new List<T>(items);
                yield break;
            }

            for (int i = 0; i < items.Count; i++)
            {
                List<T> rest = new List<T>(items);
                rest.RemoveAt(i);
                foreach (List<T> permutation in Permutations(rest))
                {
                    permutation.Insert(0, items[i]);
                    yield return permutation;
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_SeparateParallelPanes_StaySeparateUnderRigidMotion(double angle)
        {
            // Arrange: two parallel 0.1 m panes 0.9 m apart in depth (far beyond maxGap 0.01 m); the small one lies
            // within the large one's world bounding box once both are rotated.
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Prism(Rigid(XY, angle), Rectangle(-2, -0.05, 2, 0.05), null, 0, 3), ApertureType.Window, "large"),
                Solid(Prism(Rigid(XY, angle), Rectangle(-0.4, 0.95, 0.4, 1.05), null, 1, 2), ApertureType.Window, "small"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out List<int> excluded);

            // Assert: each pane is its own aperture, on its own mid-plane, at its own depth.
            Assert.Equal(2, apertures.Count);
            Assert.Empty(excluded);
            Aperture large = BySource(apertures, "large"), small = BySource(apertures, "small");
            Assert.Equal(12.0, large.GetArea(), 6);
            Assert.Equal(0.8, small.GetArea(), 6);
            Assert.Equal(0.1, large.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.Equal(0.1, small.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.All(((ISegmentable3D)large.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.0, Unmoved(x, angle).Y, 6));
            Assert.All(((ISegmentable3D)small.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(1.0, Unmoved(x, angle).Y, 6));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_SeparateParallelFramedWindows_StaySeparateUnderRigidMotion(double angle)
        {
            // Arrange: a 4 x 3 m framed window and, 0.9 m behind it, a 0.8 x 1 m framed window inside its world box when
            // rotated. Frames 0.07 m deep (y 0.09..0.16 and 1.09..1.16), glass 0.01 m in each.
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            Shell Frame(double u0, double v0, double u1, double v1, double w0) => Prism(xz, Rectangle(u0, v0, u1, v1), new List<IList<(double U, double V)>> { Rectangle(u0 + 0.06, v0 + 0.06, u1 - 0.06, v1 - 0.06) }, w0, w0 + 0.07);
            Shell Glass(double u0, double v0, double u1, double v1, double w0) => Prism(xz, Rectangle(u0 + 0.06, v0 + 0.06, u1 - 0.06, v1 - 0.06), null, w0 + 0.03, w0 + 0.04);
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Frame(-2, 0, 2, 3, 0.09), ApertureType.Window, "large frame"), Solid(Glass(-2, 0, 2, 3, 0.09), ApertureType.Window, "large glass"),
                Solid(Frame(-0.4, 1, 0.4, 2, 1.09), ApertureType.Window, "small frame"), Solid(Glass(-0.4, 1, 0.4, 2, 1.09), ApertureType.Window, "small glass"),
            };

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out List<int> excluded);

            // Assert
            Assert.Equal(2, apertures.Count);
            Assert.Empty(excluded);
            Aperture large = BySource(apertures, "large frame; large glass"), small = BySource(apertures, "small frame; small glass");
            Assert.Equal(12.0, large.GetArea(), 6);
            Assert.Equal(0.8, small.GetArea(), 6);
            Assert.Equal(0.07, large.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.Equal(0.07, small.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.All(((ISegmentable3D)large.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.125, Unmoved(x, angle).Y, 6));
            Assert.All(((ISegmentable3D)small.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(1.125, Unmoved(x, angle).Y, 6));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_RowOfTouchingFramedWindows_SameUnitsUnderRigidMotion(double angle)
        {
            // Arrange: the row of three touching framed windows, rotated and moved.
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            List<ApertureSolid> solids = new List<ApertureSolid>();
            foreach ((double x0, double x1, string name) in new[] { (1.0, 3.0, "A"), (3.0, 5.0, "B"), (5.0, 7.0, "C") })
            {
                solids.Add(Solid(Prism(xz, Rectangle(x0 + 0.06, 0.96, x1 - 0.06, 2.04), null, 0.12, 0.13), ApertureType.Window, name + " glass"));
                solids.Add(Solid(Prism(xz, Rectangle(x0, 0.9, x1, 2.1), new List<IList<(double U, double V)>> { Rectangle(x0 + 0.06, 0.96, x1 - 0.06, 2.04) }, 0.09, 0.16), ApertureType.Window, name + " frame"));
            }

            // Act
            List<Aperture> apertures = solids.AperturesBySolids(out _, out List<int> excluded);

            // Assert
            Assert.Equal(new[] { "A glass; A frame", "B glass; B frame", "C glass; C frame" }, apertures.ConvertAll(x => x.GetValue<string>(ApertureSourceParameter.SourceName)));
            Assert.All(apertures, x => Assert.Equal(2.4, x.GetArea(), 6));
            Assert.All(apertures, x => Assert.Equal(0.07, x.GetValue<double>(ApertureSourceParameter.SourceThickness), 6));
            Assert.Empty(excluded);
        }

        [Fact]
        public void AperturesBySolids_ToleranceChainOfNestedMembers_SameApertureForEveryInputOrder()
        {
            // Arrange: A holds B within maxGap and B holds C within maxGap, but A does not hold C (2.018 > 2.0 + 0.01):
            // containment within a tolerance is not transitive.
            Dictionary<string, Shell> shells = new Dictionary<string, Shell>
            {
                { "A", Prism(XZ, Rectangle(0, 0, 2, 2), null, 0, 0.1) },
                { "B", Prism(XZ, Rectangle(0.02, 0.02, 2.009, 1.98), null, 0, 0.1) },
                { "C", Prism(XZ, Rectangle(0.04, 0.04, 2.018, 1.96), null, 0, 0.1) },
            };

            foreach (List<string> order in Permutations(shells.Keys.ToList()))
            {
                // Act
                List<Aperture> apertures = order.ConvertAll(x => Solid(shells[x], ApertureType.Window, x)).AperturesBySolids(out _, out List<int> excluded);

                // Assert: one aperture of all three, as without units (4 + 0.009 x 1.96 + 0.009 x 1.92 m2).
                Aperture aperture = Assert.Single(apertures);
                Assert.Equal(new[] { "A", "B", "C" }, Members(aperture));
                Assert.Equal(4.03492, aperture.GetArea(), 6);
                Assert.Equal(0.1, aperture.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
                Assert.Empty(excluded);
            }
        }

        [Fact]
        public void AperturesBySolids_NestedSashAndStickFrames_SameAperturesForEveryInputOrder()
        {
            // Arrange: a frame-sash-glass window and, touching it, a stick-frame window (four bars and glass).
            Dictionary<string, Shell> shells = new Dictionary<string, Shell>
            {
                { "frame", WindowFrame() },
                { "sash", Prism(XZ, Rectangle(1.06, 0.96, 2.94, 2.04), new List<IList<(double U, double V)>> { Rectangle(1.1, 1.0, 2.9, 2.0) }, 0.1, 0.15) },
                { "glass", Box(1.1, 0.12, 1.0, 2.9, 0.13, 2.0) },
                { "left", Box(3, 0.09, 0.9, 3.06, 0.16, 2.1) },
                { "right", Box(4.94, 0.09, 0.9, 5, 0.16, 2.1) },
                { "head", Box(3.06, 0.09, 2.04, 4.94, 0.16, 2.1) },
                { "sill", Box(3.06, 0.09, 0.9, 4.94, 0.16, 0.96) },
                { "stick glass", WindowGlass(3, 5) },
            };

            List<string> names = shells.Keys.ToList();
            List<List<string>> expected = null;
            foreach (List<string> order in new[] { names, Enumerable.Reverse(names).ToList(), new List<string> { "sill", "glass", "left", "frame", "stick glass", "head", "sash", "right" } })
            {
                // Act
                List<Aperture> apertures = order.ConvertAll(x => Solid(shells[x], ApertureType.Window, x)).AperturesBySolids(out _, out _);

                // Assert: the stick bars are neither around nor inside another member, so they join by proximity - the
                // touching framed unit too, as before units existed; the result does not depend on the input order.
                List<List<string>> members = apertures.ConvertAll(Members).OrderBy(x => x.First()).ToList();
                expected = expected ?? members;
                Assert.Equal(expected, members);
                Assert.Equal(names.OrderBy(x => x), Assert.Single(members));
                Assert.Equal(4.8, Assert.Single(apertures).GetArea(), 6);
            }
        }

        [Fact]
        public void AperturesBySolids_ExplicitGroupKeyFrame_KeepsItsGlassAndStaysApartFromAFramedUnit()
        {
            // Arrange: window A's frame is in a Rhino group (its glass is not); window B is an ungrouped frame and glass
            // touching A.
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(WindowGlass(1, 3), ApertureType.Window, "A glass"), Solid(WindowFrame(1, 3), ApertureType.Window, "A frame", "group A"),
                Solid(WindowGlass(3, 5), ApertureType.Window, "B glass"), Solid(WindowFrame(3, 5), ApertureType.Window, "B frame"),
            };

            foreach (List<ApertureSolid> order in Permutations(solids))
            {
                // Act
                List<Aperture> apertures = order.AperturesBySolids(out _, out _);

                // Assert: the group keeps A's glass (by proximity); B is a unit of its own, never merged into the group.
                Assert.Equal(2, apertures.Count);
                Assert.Equal(new[] { "A frame", "A glass" }, Members(apertures.Single(x => Members(x).Contains("A frame"))));
                Assert.Equal(new[] { "B frame", "B glass" }, Members(apertures.Single(x => Members(x).Contains("B frame"))));
                Assert.All(apertures, x => Assert.Equal(2.4, x.GetArea(), 6));
            }
        }

        // Review R4: two touching slim framed windows (2 x 1 m, frames 0.024 m deep, panes inset 0.012 m at the jambs and
        // 0.005 m at head and sill, 0.001 m in depth). At 45 degrees the frame's 12 mm reach beyond the pane is below
        // maxGap in both world axes, so a world bounding box test sees the two as equal.
        private static List<ApertureSolid> SlimFramedRow(double angle)
        {
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            List<ApertureSolid> result = new List<ApertureSolid>();
            foreach ((double start, string name) in new[] { (0.0, "A"), (2.0, "B") })
            {
                IList<(double U, double V)> pane = Rectangle(start + 0.012, 0.005, start + 1.988, 0.995);
                result.Add(Solid(Prism(xz, Rectangle(start, 0, start + 2, 1), new List<IList<(double U, double V)>> { pane }, 0, 0.024), ApertureType.Window, name + " frame"));
                result.Add(Solid(Prism(xz, pane, null, 0.001, 0.023), ApertureType.Window, name + " glass"));
            }

            return result;
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(45)]
        [InlineData(90)]
        [InlineData(137)]
        public void AperturesBySolids_SlimFramedRow_StaysTwoWindowsForEveryRotationAndOrder(double angle)
        {
            foreach (List<ApertureSolid> order in Permutations(SlimFramedRow(angle)))
            {
                // Act
                List<Aperture> apertures = order.AperturesBySolids(out _, out List<int> excluded);

                // Assert
                Assert.Equal(2, apertures.Count);
                Assert.Empty(excluded);
                Assert.Equal(new[] { "A frame", "A glass" }, Members(apertures.Single(x => Members(x).Contains("A frame"))));
                Assert.Equal(new[] { "B frame", "B glass" }, Members(apertures.Single(x => Members(x).Contains("B frame"))));
                Assert.All(apertures, x => Assert.Equal(2.0, x.GetArea(), 6));
                Assert.All(apertures, x => Assert.Equal(0.024, x.GetValue<double>(ApertureSourceParameter.SourceThickness), 6));
                Assert.All(apertures.SelectMany(x => ((ISegmentable3D)x.GetFace3D().GetExternalEdge3D()).GetPoints()), x => Assert.Equal(0.012, Unmoved(x, angle).Y, 6));
            }
        }

        // Review R5: two overlapping outer members, each holding a unique inner member, both holding a shared one.
        private static List<ApertureSolid> SharedInnerOfTwoContainers(double angle)
        {
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            return new List<ApertureSolid>
            {
                Solid(Prism(xz, Rectangle(0, 0, 2, 2), null, 0, 0.1), ApertureType.Window, "left outer"),
                Solid(Prism(xz, Rectangle(1, 0, 3, 2), null, 0, 0.1), ApertureType.Window, "right outer"),
                Solid(Prism(xz, Rectangle(0.2, 0.5, 0.8, 1.5), null, 0.02, 0.08), ApertureType.Window, "left inner"),
                Solid(Prism(xz, Rectangle(2.2, 0.5, 2.8, 1.5), null, 0.02, 0.08), ApertureType.Window, "right inner"),
                Solid(Prism(xz, Rectangle(1.2, 0.5, 1.8, 1.5), null, 0.02, 0.08), ApertureType.Window, "shared inner"),
            };
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_SharedInnerOfTwoContainers_FallsBackToOneApertureForEveryOrder(double angle)
        {
            foreach (List<ApertureSolid> order in Permutations(SharedInnerOfTwoContainers(angle)))
            {
                // Act
                List<Aperture> apertures = order.AperturesBySolids(out _, out List<int> excluded);

                // Assert: no unit can be justified, so the five join by proximity as without units: one 6 m2 aperture,
                // never two overlapping ones.
                Aperture aperture = Assert.Single(apertures);
                Assert.Equal(new[] { "left inner", "left outer", "right inner", "right outer", "shared inner" }, Members(aperture));
                Assert.Equal(6.0, aperture.GetArea(), 6);
                Assert.Equal(0.1, aperture.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
                Assert.All(((ISegmentable3D)aperture.GetFace3D().GetExternalEdge3D()).GetPoints(), x => Assert.Equal(0.05, Unmoved(x, angle).Y, 6));
                Assert.Empty(excluded);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_MullionTouchingTwoFramedWindows_FallsBackToOneApertureForEveryOrder(double angle)
        {
            // Arrange: framed windows at x 1..3 and 3.5..5.5 and an ungrouped mullion bar between them touching both
            // frames; the bar belongs to neither unit.
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            List<ApertureSolid> solids = new List<ApertureSolid>();
            foreach ((double x0, double x1, string name) in new[] { (1.0, 3.0, "A"), (3.5, 5.5, "B") })
            {
                solids.Add(Solid(Prism(xz, Rectangle(x0, 0.9, x1, 2.1), new List<IList<(double U, double V)>> { Rectangle(x0 + 0.06, 0.96, x1 - 0.06, 2.04) }, 0.09, 0.16), ApertureType.Window, name + " frame"));
                solids.Add(Solid(Prism(xz, Rectangle(x0 + 0.06, 0.96, x1 - 0.06, 2.04), null, 0.12, 0.13), ApertureType.Window, name + " glass"));
            }

            solids.Add(Solid(Prism(xz, Rectangle(3, 0.9, 3.5, 2.1), null, 0.09, 0.16), ApertureType.Window, "mullion"));

            foreach (List<ApertureSolid> order in Permutations(solids))
            {
                // Act
                List<Aperture> apertures = order.AperturesBySolids(out _, out _);

                // Assert: as without units, the five join by proximity (2 x 2.4 + 0.5 x 1.2 m2).
                Aperture aperture = Assert.Single(apertures);
                Assert.Equal(new[] { "A frame", "A glass", "B frame", "B glass", "mullion" }, Members(aperture));
                Assert.Equal(5.4, aperture.GetArea(), 6);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(45)]
        public void AperturesBySolids_NearlyEqualMembers_AreNoUnitAndJoinByProximity(double angle)
        {
            // Arrange: a frame and a panel covering its outline to within 5 mm (less than maxGap) on every side and in
            // depth: each lies within the other, so neither encloses the other.
            Func<double, double, double, Point3D> xz = Rigid(XZ, angle);
            List<ApertureSolid> solids = new List<ApertureSolid>
            {
                Solid(Prism(xz, Rectangle(1, 0.9, 3, 2.1), new List<IList<(double U, double V)>> { Rectangle(1.06, 0.96, 2.94, 2.04) }, 0.09, 0.16), ApertureType.Window, "frame"),
                Solid(Prism(xz, Rectangle(1.005, 0.905, 2.995, 2.095), null, 0.095, 0.155), ApertureType.Window, "cover"),
            };

            foreach (List<ApertureSolid> order in Permutations(solids))
            {
                // Act
                List<Aperture> apertures = order.AperturesBySolids(out _, out _);

                // Assert
                Aperture aperture = Assert.Single(apertures);
                Assert.Equal(new[] { "cover", "frame" }, Members(aperture));
                Assert.Equal(2.4, aperture.GetArea(), 6);
                Assert.Equal(0.07, aperture.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            }
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
            Assert.Contains(diagnostics, x => x.Contains("Not planar"));
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
            Assert.Contains(diagnostics, x => x.Contains("'nothing': Skipped"));
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

        [Fact]
        public void AddAperturesByApertures_SourceInstanceParameters_SurviveHosting()
        {
            // Arrange: hosting rebuilds the aperture from its construction and geometry; its instance parameters must not be lost.
            Panel south = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0, 0.125, 0), new Point3D(6, 0.125, 0), new Point3D(6, 0.125, 3), new Point3D(0, 0.125, 3) })));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(south);

            Aperture aperture = new List<ApertureSolid> { Solid(WindowFrame(), ApertureType.Window, "frame"), Solid(WindowGlass(), ApertureType.Window, "glass") }.AperturesBySolids(out _, out _).Single();
            aperture.SetValue(ApertureParameter.Color, System.Drawing.Color.Red);
            ApertureConstruction apertureConstruction = aperture.ApertureConstruction;

            // Act
            adjacencyCluster.AddAperturesByApertures(new[] { aperture }, true, Tolerance.MacroDistance, 0.1);

            // Assert
            Aperture hosted = Assert.Single(adjacencyCluster.GetObject<Panel>(south.Guid).Apertures);
            Assert.Equal("frame; glass", hosted.GetValue<string>(ApertureSourceParameter.SourceName));
            Assert.Equal("frame-id; glass-id", hosted.GetValue<string>(ApertureSourceParameter.SourceId));
            Assert.Equal("Windows", hosted.GetValue<string>(ApertureSourceParameter.SourceLayer));
            Assert.Equal(0.07, hosted.GetValue<double>(ApertureSourceParameter.SourceThickness), 6);
            Assert.True(hosted.TryGetValue(ApertureParameter.Color, out System.Drawing.Color color) && color.ToArgb() == System.Drawing.Color.Red.ToArgb(), "any other instance parameter survives too");
            Assert.Equal(apertureConstruction.Guid, hosted.ApertureConstruction.Guid);
            Assert.Equal(2.4, hosted.GetArea(), 6);
        }

        [Fact]
        public void AddAperturesByApertures_ApertureSplitAcrossTwoPanels_EveryPieceKeepsTheSourceParameters()
        {
            // Arrange: two coplanar wall panels meeting at x = 2; the 2 m window (x 1..3) straddles them.
            Panel left = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(0, 0.125, 0), new Point3D(2, 0.125, 0), new Point3D(2, 0.125, 3), new Point3D(0, 0.125, 3) })));
            Panel right = Analytical.Create.Panel(Analytical.Query.DefaultConstruction(PanelType.WallExternal), PanelType.WallExternal, new Face3D(new Polygon3D(new List<Point3D> { new Point3D(2, 0.125, 0), new Point3D(6, 0.125, 0), new Point3D(6, 0.125, 3), new Point3D(2, 0.125, 3) })));
            AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
            adjacencyCluster.AddObject(left);
            adjacencyCluster.AddObject(right);
            List<Aperture> apertures = new List<ApertureSolid> { Solid(WindowFrame(), ApertureType.Window, "frame"), Solid(WindowGlass(), ApertureType.Window, "glass") }.AperturesBySolids(out _, out _);

            // Act
            List<Aperture> hosted = adjacencyCluster.AddAperturesByApertures(apertures, true, Tolerance.MacroDistance, 0.1);

            // Assert
            Assert.Equal(2, hosted.Count);
            Assert.All(new[] { left, right }.SelectMany(x => adjacencyCluster.GetObject<Panel>(x.Guid).Apertures), x => Assert.Equal("frame; glass", x.GetValue<string>(ApertureSourceParameter.SourceName)));
        }
    }
}
