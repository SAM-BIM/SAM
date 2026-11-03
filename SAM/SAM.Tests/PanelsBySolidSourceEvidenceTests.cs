// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;
using AnalyticalCreate = SAM.Analytical.Create;
using AnalyticalQuery = SAM.Analytical.Query;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="SourceEvidence"/>: what <see cref="Create.PanelsBySolid(Shell, out List{string}, IEnumerable{ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>
    /// proves about a solid (closed, valid, layered prismatic only), how the record is saved and reloaded, when it stops being
    /// evidence (<see cref="Query.ValidSourceEvidence(Panel, double)"/>), and how <see cref="Query.SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/>
    /// rebinds it. Metres throughout.
    /// </summary>
    public class PanelsBySolidSourceEvidenceTests
    {
        // Host plane of a wall whose thickness runs along y, centred on y = 0.
        private static readonly Plane HostY = new Plane(new Point3D(0, 0, 0), new Vector3D(0, 1, 0));

        // Profile (u = y, v = z), extruded along x.
        private static Point3D YZ(double u, double v, double w) => new Point3D(w, u, v);

        private static Panel Host(Shell shell, string name = "Wall")
        {
            return Assert.Single(shell.PanelsBySolid(out _, sourceName: name), x => x.PanelType != PanelType.Air);
        }

        private static double Area(IEnumerable<Face2D> face2Ds) => face2Ds.Sum(x => x.GetArea());

        /// <summary>
        /// 3 m long (x), 2.85 m high (z), 0.548 m thick (y -0.274..0.274), optionally with a 0.1 x 0.1 m tunnel along x at depth
        /// y 0.1..0.2 and z 0.3..0.4 (a rectangle, or a diamond whose faces are at 45 degrees).
        /// </summary>
        private static Shell Wall(bool tunnel = false, bool diamond = false)
        {
            List<IList<(double U, double V)>> holes = null;
            if (tunnel)
            {
                IList<(double U, double V)> hole = diamond
                    ? new List<(double U, double V)> { (0.1, 0.35), (0.15, 0.3), (0.2, 0.35), (0.15, 0.4) }
                    : Rectangle(0.1, 0.3, 0.2, 0.4);
                holes = new List<IList<(double U, double V)>> { hole };
            }

            return Prism(YZ, Rectangle(-0.274, 0, 0.274, 2.85), holes, 0, 3);
        }

        /// <summary>
        /// A wall x 0..length, y -0.125..0.125 with the +y face's far end <paramref name="tilt"/> metres off, z 0..3, as an XY
        /// profile extruded in z.
        /// </summary>
        private static Shell TiltedWall(double length, double tilt)
        {
            IList<(double U, double V)> outline = new List<(double U, double V)> { (0, -0.125), (length, -0.125), (length, 0.125 + tilt), (0, 0.125) };
            return Prism(XY, outline, null, 0, 3);
        }

        private static Face3D Rect(Plane plane, double u0, double v0, double u1, double v1)
        {
            return new Face3D(plane, new Polygon2D(new List<Point2D> { new Point2D(u0, v0), new Point2D(u1, v0), new Point2D(u1, v1), new Point2D(u0, v1) }));
        }

        private static Panel Edited(Panel panel, double fromU, double toU)
        {
            // Same plane, same frame, a different footprint: the share [fromU, toU] of the panel's width.
            BoundingBox2D bounds = panel.Plane.Convert(panel.GetFace3D()).GetBoundingBox();
            Face3D face3D = Rect(panel.Plane, bounds.Min.X + fromU * bounds.Width, bounds.Min.Y, bounds.Min.X + toU * bounds.Width, bounds.Max.Y);
            return AnalyticalCreate.Panel(panel.Guid, panel, face3D, null, false);
        }

        // Same plane, same frame: the panel without the part of its face west of world x = <paramref name="x"/> (a vertical panel in an xz plane).
        private static Panel TrimmedWestOf(Panel panel, double x)
        {
            BoundingBox3D bounds = panel.GetFace3D().GetBoundingBox();
            Point3D[] corners = { new Point3D(x, bounds.Min.Y, bounds.Min.Z), new Point3D(bounds.Max.X, bounds.Min.Y, bounds.Min.Z), new Point3D(bounds.Max.X, bounds.Min.Y, bounds.Max.Z), new Point3D(x, bounds.Min.Y, bounds.Max.Z) };
            Face3D face3D = new Face3D(panel.Plane, new Polygon2D(corners.Select(c => panel.Plane.Convert(c)).ToList()));
            Panel result = AnalyticalCreate.Panel(panel.Guid, panel, face3D, null, false);
            Assert.Equal(panel.GetArea() - (x - bounds.Min.X) * (bounds.Max.Z - bounds.Min.Z), result.GetArea(), 6);
            return result;
        }

        // Raw box faces, for solids that are not closed or valid.
        private static List<Face3D> BoxFaces(double x0, double y0, double z0, double x1, double y1, double z1, bool splitPlusY = false, bool omitPlusX = false)
        {
            Point3D P(double x, double y, double z) => new Point3D(x, y, z);
            Face3D Quad(Point3D a, Point3D b, Point3D c, Point3D d) => new Face3D(new Polygon3D(new List<Point3D> { a, b, c, d }));
            double xm = (x0 + x1) / 2;
            List<Face3D> result = new List<Face3D>
            {
                Quad(P(x0, y0, z0), P(x1, y0, z0), P(x1, y0, z1), P(x0, y0, z1)),
                Quad(P(x0, y0, z0), P(x1, y0, z0), P(x1, y1, z0), P(x0, y1, z0)),
                Quad(P(x0, y0, z1), P(x1, y0, z1), P(x1, y1, z1), P(x0, y1, z1)),
                Quad(P(x0, y0, z0), P(x0, y1, z0), P(x0, y1, z1), P(x0, y0, z1)),
            };

            if (splitPlusY)
            {
                result.Add(Quad(P(x0, y1, z0), P(xm, y1, z0), P(xm, y1, z1), P(x0, y1, z1)));
                result.Add(Quad(P(xm, y1, z0), P(x1, y1, z0), P(x1, y1, z1), P(xm, y1, z1)));
            }
            else
            {
                result.Add(Quad(P(x0, y1, z0), P(x1, y1, z0), P(x1, y1, z1), P(x0, y1, z1)));
            }

            if (!omitPlusX)
            {
                result.Add(Quad(P(x1, y0, z0), P(x1, y1, z0), P(x1, y1, z1), P(x1, y0, z1)));
            }

            return result;
        }

        private static bool Certify(Shell shell, double halfThickness, out List<Face2D> support, out string reason, Func<Shell, Plane, List<Face3D>> section = null, Func<Face2D, Face2D, double, List<Face2D>> intersection = null)
        {
            return AnalyticalCreate.TryCertifyMaterial(shell, HostY, halfThickness, Tolerance.Angle, Tolerance.Distance, Tolerance.MacroDistance, out support, out reason, section, intersection);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Certification

        [Fact]
        public void PanelsBySolid_PrismaticWall_StampsRecordBoundToThePanel()
        {
            // Arrange
            Shell wall = Box(0, 0, 0, 4, 0.25, 3);

            // Act
            Panel panel = Host(wall);

            // Assert
            SourceEvidence evidence = panel.SourceEvidence;
            Assert.NotNull(evidence);
            Assert.Equal(0.25, evidence.GuaranteedThickness, 6);
            Assert.Equal(12.0, Area(evidence.Support), 6);
            Assert.Equal(12.0, Area(evidence.Origin), 6);
            Assert.Equal(12.0, Area(evidence.BoundFootprint), 6);
            Assert.True(evidence.Plane.Origin.Distance(panel.Plane.Origin) <= 1e-9);
            Assert.True(1 - evidence.Plane.Normal.Unit.DotProduct(panel.Plane.Normal.Unit) <= 1e-12);
            Assert.True(1 - evidence.Plane.AxisY.Unit.DotProduct(panel.Plane.AxisY.Unit) <= 1e-12);
            Assert.NotNull(panel.ValidSourceEvidence(out string reason));
            Assert.Null(reason);
        }

        [Fact]
        public void PanelsBySolids_PrismaticWall_StampsRecord()
        {
            // Arrange
            List<Shell> shells = new List<Shell> { Box(0, 0, 0, 4, 0.25, 3) };

            // Act
            List<List<Panel>> panels = shells.PanelsBySolids(new Core.Diagnostics());

            // Assert
            Panel panel = Assert.Single(panels[0]);
            Assert.NotNull(panel.ValidSourceEvidence());
        }

        [Fact]
        public void PanelsBySolid_SteppedWall_GuaranteesTheLeastSlab()
        {
            // Arrange: 2 m of 0.548122 m and 1 m of 0.568122 m (the D203 #250 wall), one mid-plane.
            IList<(double U, double V)> outline = new List<(double U, double V)>
            {
                (0, -0.274061), (2, -0.274061), (2, -0.284061), (3, -0.284061), (3, 0.284061), (2, 0.284061), (2, 0.274061), (0, 0.274061),
            };
            Shell wall = Prism(XY, outline, null, 0, 2.85);

            // Act
            Panel panel = Host(wall);

            // Assert
            SourceEvidence evidence = panel.SourceEvidence;
            Assert.NotNull(evidence);
            Assert.Equal(0.548122, evidence.GuaranteedThickness, 6);
            Assert.Equal(8.55, Area(evidence.Support), 5);
        }

        [Fact]
        public void PanelsBySolid_SlabWithStairVoid_RecordsHostOnlyAndKeepsTheHoleOpen()
        {
            // Arrange
            Shell slab = Prism(XY, Rectangle(0, 0, 6, 4), new List<IList<(double U, double V)>> { Rectangle(2, 1, 3, 3) }, 2.8, 3.0);

            // Act
            List<Panel> panels = slab.PanelsBySolid(out _);

            // Assert
            Panel host = Assert.Single(panels, x => x.PanelType != PanelType.Air);
            Panel air = Assert.Single(panels, x => x.PanelType == PanelType.Air);
            Assert.Null(air.SourceEvidence);
            Assert.NotNull(host.SourceEvidence);
            Assert.Equal(0.2, host.SourceEvidence.GuaranteedThickness, 6);
            Assert.Equal(22.0, Area(host.SourceEvidence.Support), 6);
            Assert.Equal(22.0, Area(host.SourceEvidence.Origin), 6);
            Assert.NotNull(host.ValidSourceEvidence());
        }

        [Fact]
        public void PanelsBySolid_RectangularTunnelBeyondTheMidPlane_CertifiesAndExcludesThePocket()
        {
            // Arrange: the section at the tunnel's depth is 8.25 of the wall's 8.55 m2, which sections at the mid-plane never see.
            Shell wall = Wall(tunnel: true);

            // Act
            Panel panel = Host(wall);

            // Assert
            SourceEvidence evidence = panel.SourceEvidence;
            Assert.NotNull(evidence);
            Assert.Equal(0.548, evidence.GuaranteedThickness, 6);
            Assert.Equal(8.25, Area(evidence.Support), 5);
            Assert.Equal(8.55, Area(evidence.Origin), 5);
        }

        [Fact]
        public void PanelsBySolid_DiamondTunnel_GivesNoEvidence()
        {
            // Arrange: faces at 45 degrees are neither parallel nor perpendicular to the wall.
            Shell wall = Wall(tunnel: true, diamond: true);

            // Act
            Panel panel = Host(wall);

            // Assert
            Assert.Null(panel.SourceEvidence);
        }

        [Fact]
        public void PanelsBySolid_ChamferInTheSlab_GivesNoEvidence()
        {
            // Arrange: a 45 degree chamfer at one end of a 0.25 m wall.
            IList<(double U, double V)> outline = new List<(double U, double V)> { (0, -0.125), (4, -0.125), (4, 0.05), (3.925, 0.125), (0, 0.125) };
            Shell wall = Prism(XY, outline, null, 0, 3);

            // Act
            Panel panel = Host(wall);

            // Assert
            Assert.Null(panel.SourceEvidence);
        }

        [Fact]
        public void PanelsBySolid_LongWallWithFarEndTwoMillimetresOff_GivesNoEvidence()
        {
            // Arrange: 30 m wall whose +y face is 2 mm off at the far end (6.7e-5 rad): an angular classifier at 1e-4 rad would accept it.
            Shell wall = TiltedWall(30, 0.002);

            // Act
            Panel panel = Host(wall);

            // Assert
            Assert.Null(panel.SourceEvidence);
        }

        [Fact]
        public void PanelsBySolid_LongWallWithFarEndHalfAMicrometreOff_CertifiesWithinTheBound()
        {
            // Arrange
            Shell wall = TiltedWall(30, 5e-7);

            // Act
            Panel panel = Host(wall);

            // Assert
            Assert.NotNull(panel.SourceEvidence);
        }

        [Fact]
        public void TryCertifyMaterial_PositionalBound_AcceptsHalfBoundRejectsTwiceBound()
        {
            // Arrange
            Shell within = TiltedWall(30, 5e-7);
            Shell beyond = TiltedWall(30, 2e-6);

            // Act
            bool certifiedWithin = Certify(within, 0.125, out List<Face2D> support, out _);
            bool certifiedBeyond = Certify(beyond, 0.125, out List<Face2D> supportBeyond, out string reason);

            // Assert
            Assert.True(certifiedWithin);
            Assert.NotEmpty(support);
            Assert.False(certifiedBeyond);
            Assert.Null(supportBeyond);
            Assert.Contains("neither parallel nor perpendicular", reason);
        }

        [Fact]
        public void TryCertifyMaterial_ClosedPrism_Certifies()
        {
            // Arrange
            Shell wall = Wall();

            // Act
            bool certified = Certify(wall, 0.274, out List<Face2D> support, out string reason);

            // Assert
            Assert.True(certified, reason);
            Assert.Equal(8.55, Area(support), 6);
        }

        [Fact]
        public void TryCertifyMaterial_WallWithAnEndFaceMissing_GivesNoEvidence()
        {
            // Arrange
            Shell open = new Shell(BoxFaces(0, -0.125, 0, 3, 0.125, 2.85, omitPlusX: true));

            // Act
            bool certified = Certify(open, 0.125, out List<Face2D> support, out string reason);

            // Assert
            Assert.False(certified);
            Assert.Null(support);
            Assert.Contains("not a closed, valid solid", reason);
            Assert.Contains("shared by 0 other edge(s)", reason);
        }

        [Fact]
        public void TryCertifyMaterial_FaceSplitByATJunction_GivesNoEvidence()
        {
            // Arrange: the +y face is two faces, so the long edges of its neighbours have no single partner.
            Shell split = new Shell(BoxFaces(0, -0.125, 0, 4, 0.125, 3, splitPlusY: true));
            Shell whole = new Shell(BoxFaces(0, -0.125, 0, 4, 0.125, 3));

            // Act
            bool certifiedSplit = Certify(split, 0.125, out _, out string reason);
            bool certifiedWhole = Certify(whole, 0.125, out List<Face2D> support, out _);

            // Assert
            Assert.False(certifiedSplit);
            Assert.Contains("not a closed, valid solid", reason);
            Assert.True(certifiedWhole);
            Assert.Equal(12.0, Area(support), 6);
        }

        [Fact]
        public void TryCertifyMaterial_LayerThinnerThanTheSnapTolerance_GivesNoEvidence()
        {
            // Arrange: steps of the -y face at y -0.05 and -0.0495 leave a 0.5 mm layer inside the 0.25 m slab.
            IList<(double U, double V)> outline = new List<(double U, double V)>
            {
                (0, -0.125), (1, -0.125), (1, -0.05), (2, -0.05), (2, -0.0495), (3, -0.0495), (3, 0.125), (0, 0.125),
            };
            Shell wall = Prism(XY, outline, null, 0, 3);

            // Act
            bool certified = Certify(wall, 0.125, out List<Face2D> support, out string reason);

            // Assert
            Assert.False(certified);
            Assert.Null(support);
            Assert.Contains("thinner than the snap tolerance", reason);
        }

        [Fact]
        public void TryCertifyMaterial_SectionReturnsNullOrEmpty_GivesNoEvidence()
        {
            // Arrange
            Shell wall = Wall();

            // Act
            bool certifiedNull = Certify(wall, 0.274, out _, out string reasonNull, section: (x, y) => null);
            bool certifiedEmpty = Certify(wall, 0.274, out _, out string reasonEmpty, section: (x, y) => new List<Face3D>());

            // Assert: an empty or missing section is never proof.
            Assert.False(certifiedNull);
            Assert.Contains("failed or is empty", reasonNull);
            Assert.False(certifiedEmpty);
            Assert.Contains("failed or is empty", reasonEmpty);
        }

        [Fact]
        public void TryCertifyMaterial_IntersectionReturnsNull_GivesNoEvidence()
        {
            // Arrange: the tunnel makes three layers, so the support is an intersection of regions.
            Shell wall = Wall(tunnel: true);

            // Act
            bool certified = Certify(wall, 0.274, out List<Face2D> support, out string reason, intersection: (x, y, tolerance) => null);

            // Assert: a null result with overlapping bounds is a failure, not an empty intersection.
            Assert.False(certified);
            Assert.Null(support);
            Assert.Contains("intersection of regions failed", reason);
        }

        [Fact]
        public void DefiniteIntersection_DisjointBounds_IsEmptyAndOverlappingNullIsFailure()
        {
            // Arrange
            Plane plane = new Plane(new Point3D(0, 0, 0), new Vector3D(0, 0, 1));
            Face2D a = plane.Convert(Rect(plane, 0, 0, 1, 1));
            Face2D b = plane.Convert(Rect(plane, 5, 5, 6, 6));
            Face2D c = plane.Convert(Rect(plane, 0.5, 0.5, 1.5, 1.5));

            // Act
            List<Face2D> disjoint = AnalyticalQuery.DefiniteIntersection(new[] { a }, new[] { b }, Tolerance.Distance, (x, y, t) => null);
            List<Face2D> overlapping = AnalyticalQuery.DefiniteIntersection(new[] { a }, new[] { c }, Tolerance.Distance, (x, y, t) => null);
            List<Face2D> real = AnalyticalQuery.DefiniteIntersection(new[] { a }, new[] { c });

            // Assert
            Assert.NotNull(disjoint);
            Assert.Empty(disjoint);
            Assert.Null(overlapping);
            Assert.Equal(0.25, Area(real), 6);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Save and reload

        [Fact]
        public void Panel_WithSourceEvidence_SurvivesSaveAndReload()
        {
            // Arrange
            Panel panel = Host(Wall(tunnel: true));
            SourceEvidence before = panel.SourceEvidence;

            // Act
            Panel back = Core.Create.IJSAMObject<Panel>(Core.Convert.ToString(panel));

            // Assert
            SourceEvidence after = back.SourceEvidence;
            Assert.NotNull(after);
            Assert.True(before.Plane.Origin.Distance(after.Plane.Origin) <= 1e-9);
            Assert.True(1 - before.Plane.Normal.Unit.DotProduct(after.Plane.Normal.Unit) <= 1e-12);
            Assert.True(1 - before.Plane.AxisY.Unit.DotProduct(after.Plane.AxisY.Unit) <= 1e-12);
            Assert.Equal(before.GuaranteedThickness, after.GuaranteedThickness, 12);
            foreach ((List<Face2D> x, List<Face2D> y) in new[] { (before.Support, after.Support), (before.Origin, after.Origin), (before.BoundFootprint, after.BoundFootprint) })
            {
                Assert.Equal(x.Count, y.Count);
                Assert.Equal(Area(x), Area(y), 9);
                Assert.Equal(x.Sum(f => AnalyticalQuery.RegionPerimeter(new[] { f })), y.Sum(f => AnalyticalQuery.RegionPerimeter(new[] { f })), 9);
            }

            Assert.NotNull(back.ValidSourceEvidence(out string reason));
            Assert.Null(reason);
        }

        [Fact]
        public void Panel_WithSourceEvidence_RoundTripsThroughJsonUnchanged()
        {
            // Arrange
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));

            // Act
            Panel back = Helpers.RoundTrip.Once(panel);

            // Assert
            Assert.NotNull(back.SourceEvidence);
        }

        [Fact]
        public void Panel_WithoutSourceEvidenceKey_HasNoEvidence()
        {
            // Arrange: a panel saved before the record existed.
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));
            JsonObject json = panel.ToJsonObject();
            Assert.True(json.Remove("SourceEvidence"));

            // Act
            Panel back = new Panel(json);

            // Assert
            Assert.Null(back.SourceEvidence);
            Assert.Null(back.ValidSourceEvidence(out string reason));
            Assert.Contains("no source evidence", reason);
        }

        [Fact]
        public void Panel_WithIncompleteRecord_IsLoadedWithoutIt()
        {
            // Arrange: an all-or-nothing record with its support removed is no evidence.
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));
            JsonObject json = panel.ToJsonObject();
            ((JsonObject)json["SourceEvidence"]).Remove("Support");

            // Act
            Panel back = new Panel(json);

            // Assert
            Assert.Null(back.SourceEvidence);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Validity: a record is evidence only while it describes the panel

        [Fact]
        public void ValidSourceEvidence_CopyOfPanel_StaysValid()
        {
            // Arrange
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));

            // Act
            Panel copy = AnalyticalCreate.Panel(panel);
            Panel copy_Guid = AnalyticalCreate.Panel(Guid.NewGuid(), panel);

            // Assert
            Assert.NotNull(copy.ValidSourceEvidence());
            Assert.NotNull(copy_Guid.ValidSourceEvidence());
        }

        [Theory]
        [InlineData("trim", 0.0, 0.5)]
        [InlineData("extend", -0.25, 1.25)]
        [InlineData("split", 0.5, 1.0)]
        public void ValidSourceEvidence_SamePlaneEditKeepsTheFrame_ButNotTheEvidence(string edit, double fromU, double toU)
        {
            // Arrange
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));

            // Act
            Panel edited = Edited(panel, fromU, toU);

            // Assert: the frame survives every edit that rebuilds the face on the same plane, so only the footprint tells.
            Assert.True(edited.Plane.Origin.Distance(panel.SourceEvidence.Plane.Origin) <= 1e-9, edit);
            Assert.NotNull(edited.SourceEvidence);
            Assert.Null(edited.ValidSourceEvidence(out string reason));
            Assert.Contains("footprint differs", reason);
        }

        [Fact]
        public void ValidSourceEvidence_MergedPieces_IsNoEvidence()
        {
            // Arrange: two panels on one plane merged into one face.
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));
            Panel other = Host(Box(4, 0, 0, 6, 0.25, 3));
            Plane frame = panel.Plane;
            List<Face2D> union = Geometry.Planar.Query.Union(new List<Face2D> { frame.Convert(panel.GetFace3D()), frame.Convert(other.GetFace3D()) }, Tolerance.Distance);
            Assert.Single(union);

            // Act
            Panel merged = AnalyticalCreate.Panel(panel.Guid, panel, frame.Convert(union[0]), null, false);

            // Assert
            Assert.Equal(18.0, merged.GetArea(), 6);
            Assert.Null(merged.ValidSourceEvidence());
        }

        [Fact]
        public void ValidSourceEvidence_MovedByAMillimetre_IsNoEvidence()
        {
            // Arrange
            Panel panel = AnalyticalCreate.Panel(Host(Box(0, 0, 0, 4, 0.25, 3)));

            // Act
            panel.Move(new Vector3D(0.001, 0, 0));

            // Assert
            Assert.Null(panel.ValidSourceEvidence(out string reason));
            Assert.Contains("frame differs", reason);
        }

        [Fact]
        public void ValidSourceEvidence_Rotated_IsNoEvidence()
        {
            // Arrange
            Panel panel = AnalyticalCreate.Panel(Host(Box(0, 0, 0, 4, 0.25, 3)));

            // Act
            panel.Transform(Transform3D.GetRotationZ(0.1));

            // Assert
            Assert.Null(panel.ValidSourceEvidence(out string reason));
            Assert.Contains("frame differs", reason);
        }

        [Fact]
        public void ValidSourceEvidence_Scaled_IsNoEvidence()
        {
            // Arrange
            Panel panel = AnalyticalCreate.Panel(Host(Box(0, 0, 0, 4, 0.25, 3)));

            // Act
            panel.Transform(Transform3D.GetScale(1.01));

            // Assert: a scaling moves the frame, or at least changes the footprint.
            Assert.Null(panel.ValidSourceEvidence());
        }

        [Fact]
        public void ValidSourceEvidence_Flipped_IsNoEvidence()
        {
            // Arrange
            Panel panel = AnalyticalCreate.Panel(Host(Box(0, 0, 0, 4, 0.25, 3)));

            // Act
            panel.FlipNormal(false);

            // Assert
            Assert.Null(panel.ValidSourceEvidence());
        }

        [Fact]
        public void ValidSourceEvidence_SameFootprintRebuiltOnAnotherFrame_IsNoEvidence()
        {
            // Arrange: the same face in 3D on the same plane, expressed in another frame (origin moved in the plane, axes turned 90 degrees).
            Panel panel = Host(Box(0, 0, 0, 4, 0.25, 3));
            Plane frame = panel.Plane;
            Point3D origin = new Point3D(frame.Origin.X + 0.37 * frame.AxisX.X, frame.Origin.Y + 0.37 * frame.AxisX.Y, frame.Origin.Z + 0.37 * frame.AxisX.Z);
            Plane other = new Plane(origin, frame.AxisY, frame.AxisX.GetNegated());
            Assert.True(1 - other.Normal.Unit.DotProduct(frame.Normal.Unit) <= 1e-12, "the same plane, facing the same way");
            Face3D rebuilt = new Face3D(other, other.Convert(panel.GetFace3D()));
            Panel edited = AnalyticalCreate.Panel(panel.Guid, panel, rebuilt, null, false);
            Assert.Equal(panel.GetArea(), edited.GetArea(), 6);
            Assert.True(edited.Plane.Origin.Distance(panel.SourceEvidence.Plane.Origin) > 1e-6, "the test needs a different frame");

            // Act
            SourceEvidence evidence = edited.ValidSourceEvidence(out string reason);

            // Assert
            Assert.Null(evidence);
            Assert.Contains("frame differs", reason);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // SnapToJunctions rebinding

        // A facade (mid-plane y 0, from x 0.2) and a return wall (mid-plane x 0, from y 0.2), 0.4 m thick: each ends at the other's
        // face, 0.2 m short of its mid-plane.
        private static List<Panel> Corner()
        {
            return new List<Panel>
            {
                Host(Box(0.2, -0.2, 0, 6, 0.2, 3), "F"),
                Host(Box(-0.2, 0.2, 0, 0.2, 4, 3), "C"),
            };
        }

        private static Panel Named(IEnumerable<Panel> panels, string name)
        {
            return panels.Single(x => x.GetValue<string>(PanelSourceParameter.SourceName) == name);
        }

        [Fact]
        public void SnapToJunctions_PanelsWithEvidence_RebindsItToTheSnappedFootprint()
        {
            // Arrange
            List<Panel> panels = Corner();
            Assert.All(panels, x => Assert.NotNull(x.ValidSourceEvidence()));
            Assert.Equal(17.4, Named(panels, "F").GetArea(), 5);

            // Act
            List<Panel> snapped = panels.SnapToJunctions(out _);

            // Assert: the facade now runs to the return wall's mid-plane, and its record is bound to that footprint.
            Panel facade = Named(snapped, "F");
            Assert.Equal(18.0, facade.GetArea(), 5);
            SourceEvidence evidence = facade.ValidSourceEvidence(out string reason);
            Assert.NotNull(evidence);
            Assert.Null(reason);
            Assert.Equal(18.0, Area(evidence.BoundFootprint), 5);
            Assert.Equal(17.4, Area(evidence.Origin), 5);
            Assert.Equal(17.4, Area(evidence.Support), 5);
            Assert.NotNull(Named(snapped, "C").ValidSourceEvidence());
        }

        [Fact]
        public void SnapToJunctions_SnappedPanels_StayValidAfterSaveReloadAndASecondPass()
        {
            // Arrange
            List<Panel> snapped = Corner().SnapToJunctions(out _);

            // Act
            List<Panel> reloaded = snapped.ConvertAll(x => Core.Create.IJSAMObject<Panel>(Core.Convert.ToString(x)));
            List<Panel> again = reloaded.SnapToJunctions(out _);

            // Assert
            Assert.All(reloaded, x => Assert.NotNull(x.ValidSourceEvidence()));
            Assert.All(again, x => Assert.NotNull(x.ValidSourceEvidence()));
            Assert.Equal(18.0, Named(again, "F").GetArea(), 5);
        }

        [Fact]
        public void SnapToJunctions_SamePlaneEditThenSnapBackToTheBoundFootprint_StaleEvidenceStaysDropped()
        {
            // Arrange: snapped panels carry records bound to their snapped footprints.
            List<Panel> snapped = Corner().SnapToJunctions(out _);
            Panel facade = Named(snapped, "F");
            Assert.NotNull(facade.ValidSourceEvidence());

            // A same-plane edit trims 0.12 m off the facade's west end: the frame survives, the record is stale.
            Panel edited = TrimmedWestOf(facade, facade.GetFace3D().GetBoundingBox().Min.X + 0.12);
            Assert.Null(edited.ValidSourceEvidence());
            Assert.NotNull(edited.SourceEvidence);
            List<Panel> editedSet = new List<Panel> { edited, Named(snapped, "C") };

            // Act: snapping extends the trimmed edge back to the return wall's mid-plane, to exactly the bound footprint.
            List<Panel> result = editedSet.SnapToJunctions(out _);

            // Assert
            Panel resnapped = Named(result, "F");
            Assert.Equal(facade.GetArea(), resnapped.GetArea(), 6);
            Assert.Equal(0, AnalyticalQuery.RegionArea(new[] { resnapped.Plane.Convert(resnapped.GetFace3D()) }) - Area(facade.SourceEvidence.BoundFootprint), 6);
            Assert.Null(resnapped.SourceEvidence);
            Assert.Null(resnapped.ValidSourceEvidence());

            // The panel that was not edited keeps valid evidence.
            Assert.NotNull(Named(result, "C").ValidSourceEvidence());
        }

        [Fact]
        public void SnapToJunctions_PanelWithoutEvidence_StaysWithout()
        {
            // Arrange: a wall that is not layered prismatic (a chamfered end) has no record to rebind.
            IList<(double U, double V)> outline = new List<(double U, double V)> { (0.2, -0.2), (6, -0.2), (6, 0.1), (5.9, 0.2), (0.2, 0.2) };
            Panel chamfered = Host(Prism(XY, outline, null, 0, 3), "F");
            Panel returnWall = Host(Box(-0.2, 0.2, 0, 0.2, 4, 3), "C");
            Assert.Null(chamfered.SourceEvidence);

            // Act
            List<Panel> snapped = new List<Panel> { chamfered, returnWall }.SnapToJunctions(out _);

            // Assert
            Assert.Null(Named(snapped, "F").SourceEvidence);
            Assert.NotNull(Named(snapped, "C").ValidSourceEvidence());
        }

        [Fact]
        public void SnapToJunctions_ZeroSnapDistance_ReturnsCopiesThatKeepTheirRecords()
        {
            // Arrange
            List<Panel> panels = Corner();

            // Act
            List<Panel> copies = panels.SnapToJunctions(out _, 0);

            // Assert
            Assert.All(copies, x => Assert.NotNull(x.ValidSourceEvidence()));
        }
    }
}
