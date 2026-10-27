// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Geometries;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Geometry.Spatial.Query.Union(IEnumerable{Face3D}, double)"/>: coplanar faces merge plane by plane, and a
    /// face without an internal point (a sliver) does not fail the union of all the others.
    /// </summary>
    public class Face3DUnionTests
    {
        private static Face3D Rectangle(double x0, double y0, double x1, double y1, double z)
        {
            return new Face3D(new Polygon3D(new List<Point3D> { new Point3D(x0, y0, z), new Point3D(x1, y0, z), new Point3D(x1, y1, z), new Point3D(x0, y1, z) }));
        }

        [Fact]
        public void Union_TwoAdjacentCoplanarFaces_ReturnsOneFace()
        {
            // Arrange
            List<Face3D> face3Ds = new List<Face3D> { Rectangle(0, 0, 1, 1, 3), Rectangle(1, 0, 2, 1, 3) };

            // Act
            List<Face3D> result = face3Ds.Union();

            // Assert
            Face3D face3D = Assert.Single(result);
            Assert.Equal(2.0, face3D.GetArea(), 6);
        }

        [Fact]
        public void Union_SliverWithoutInternalPointFirst_StillMergesTheOtherFaces()
        {
            // Arrange: a wall face collapsed to 7e-15 m wide (valid, but without an internal point), exactly as junction
            // snapping left one in a real model, first in the list; then two adjacent coplanar faces.
            Plane plane = new Plane(new Point3D(16.51, -2.749457597732544, 8), new Vector3D(0, -1, 0));
            Face3D sliver = new Face3D(new Polygon3D(plane, new List<Point3D> { new Point3D(16.510114669799805, -2.749457597732544, 6.475000143051148), new Point3D(16.510114669799805, -2.749457597732544, 9.449999809265135), new Point3D(16.510114669799798, -2.749457597732544, 9.449999809265135), new Point3D(16.510114669799798, -2.749457597732544, 6.475000143051148) }.ConvertAll(x => plane.Convert(x))));
            Assert.True(sliver.IsValid());
            Assert.Null(sliver.InternalPoint3D(Core.Tolerance.Distance));
            List<Face3D> face3Ds = new List<Face3D> { sliver, Rectangle(0, 0, 1, 1, 3), Rectangle(1, 0, 2, 1, 3) };

            // Act
            List<Face3D> result = face3Ds.Union();

            // Assert: the sliver is returned as it is and the two faces are merged.
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => System.Math.Abs(x.GetArea() - 2.0) < 1e-6);
            Assert.Contains(result, x => ReferenceEquals(x, sliver));
        }

        // ── Seed-400 slab: a converted degenerate hole must not lose the merged face ──

        /// <summary>
        /// The seed-400 synthetic slab at z = 3 m (xy per piece, bit-exact, copied from SAM_OCCT SnapStageTests): a 14 x 9 m
        /// slab of six abutting pieces with +-0.5 um seams - a dominant notched piece (s0, clockwise), its notch filler, two
        /// west pieces, an east piece with a slanted seam, a second east piece - and a detached 2 x 2 m piece. Listed as is,
        /// the union of the slab has two sliver holes and the hole re-union converts one to a 2-point loop.
        /// </summary>
        private static readonly double[][] Seed400Pieces =
        {
            new[] { 9.000000435333426, 9.00000030032906, 8.999999618007468, -3.7129389873769014E-07, 4.999999615938999, -3.5770081119324004E-08, 5.000000274143515, 1.9999996847723955, 7.500000196368946, 2.0000001688898568, 7.5000004494617185, 3.9999998632188802, 5.000000113082614, 3.9999999348384625, 5.000000470897542, 9.000000338725508 },
            new[] { 4.999999505998211, 2.000000495077903, 7.500000005335688, 1.9999996965447544, 7.500000465123198, 4.000000482730804, 5.000000093906317, 4.000000270976929 },
            new[] { 4.0222737363038163E-07, 2.994883283591321E-07, 5.000000340004174, 3.080416144385367E-08, 4.999999532013835, 5.999999724946233, 3.582212286801223E-07, 6.000000316056031 },
            new[] { -3.5324155778226896E-07, 5.999999516793181, 4.999999814146901, 5.999999598170239, 4.99999987011676, 9.000000480531687, 3.439054268028485E-07, 8.999999578686603 },
            new[] { 9.000000083507445, 6.000000345507554, 8.950000477637182, 6.000000039051776, 5.90000030148899, 6.059999550740095, 5.899999925694103, 9.000000223566726, 8.999999724308735, 8.999999589063167 },
            new[] { 9.00000038181994, 4.989084958495482E-07, 13.999999822841723, 4.787328578004235E-07, 13.999999786778648, 9.000000213638142, 9.000000090866077, 9.000000092241951 },
            new[] { 20.0, 0.0, 22.0, 0.0, 22.0, 2.0, 20.0, 2.0 },
        };

        private const double Seed400Z = 3;

        /// <summary>The seed-400 pieces as world points, in list order.</summary>
        private static List<List<Point3D>> Seed400Points()
        {
            return Seed400Pieces.Select(xy => Enumerable.Range(0, xy.Length / 2).Select(j => new Point3D(xy[2 * j], xy[2 * j + 1], Seed400Z)).ToList()).ToList();
        }

        /// <summary>A rigid motion: rotation by <paramref name="degrees"/> about <paramref name="axis"/> through
        /// <paramref name="centre"/>, then a translation; <see cref="Inverse"/> maps back.</summary>
        private class Rigid
        {
            private readonly double[,] r = new double[3, 3];
            private readonly double[] c;
            private readonly double[] t;

            public Rigid(double[] axis, double degrees, double[] centre, double[] translation)
            {
                double n = System.Math.Sqrt(axis.Sum(x => x * x));
                double ux = axis[0] / n, uy = axis[1] / n, uz = axis[2] / n;
                double a = degrees * System.Math.PI / 180, cos = System.Math.Cos(a), sin = System.Math.Sin(a), k = 1 - cos;
                r[0, 0] = cos + ux * ux * k; r[0, 1] = ux * uy * k - uz * sin; r[0, 2] = ux * uz * k + uy * sin;
                r[1, 0] = uy * ux * k + uz * sin; r[1, 1] = cos + uy * uy * k; r[1, 2] = uy * uz * k - ux * sin;
                r[2, 0] = uz * ux * k - uy * sin; r[2, 1] = uz * uy * k + ux * sin; r[2, 2] = cos + uz * uz * k;
                c = centre;
                t = translation;
            }

            public Point3D Apply(Point3D point3D)
            {
                double[] p = { point3D.X - c[0], point3D.Y - c[1], point3D.Z - c[2] };
                return new Point3D(
                    r[0, 0] * p[0] + r[0, 1] * p[1] + r[0, 2] * p[2] + c[0] + t[0],
                    r[1, 0] * p[0] + r[1, 1] * p[1] + r[1, 2] * p[2] + c[1] + t[1],
                    r[2, 0] * p[0] + r[2, 1] * p[1] + r[2, 2] * p[2] + c[2] + t[2]);
            }

            public Point3D Inverse(Point3D point3D)
            {
                double[] p = { point3D.X - c[0] - t[0], point3D.Y - c[1] - t[1], point3D.Z - c[2] - t[2] };
                return new Point3D(
                    r[0, 0] * p[0] + r[1, 0] * p[1] + r[2, 0] * p[2] + c[0],
                    r[0, 1] * p[0] + r[1, 1] * p[1] + r[2, 1] * p[2] + c[1],
                    r[0, 2] * p[0] + r[1, 2] * p[1] + r[2, 2] * p[2] + c[2]);
            }
        }

        /// <summary>
        /// One presentation of the seed-400 slab: the world pieces (what the union must cover, after any perturbation), the
        /// faces handed to the union (re-ordered, re-started, reversed or moved) and the inverse motion back to the world.
        /// </summary>
        private static (List<List<Point3D>> World, List<Face3D> Faces, Rigid Motion) Seed400Variant(string variant)
        {
            List<List<Point3D>> world = Seed400Points();
            System.Random random = new System.Random(variant.Aggregate(17, (h, ch) => unchecked(h * 31 + ch)) & 0xFFFF);
            switch (variant)
            {
                case "jitter-1":
                case "jitter-2":
                case "jitter-3":
                    // Sub-tolerance vertex noise (|d| <= 1e-9 m) in the plane of the slab.
                    world = world.ConvertAll(x => x.ConvertAll(p => new Point3D(p.X + (random.NextDouble() * 2 - 1) * 1e-9, p.Y + (random.NextDouble() * 2 - 1) * 1e-9, p.Z)));
                    break;
                case "znoise":
                    // ULP-level z noise on the dominant piece: geometrically identical, its normal not bit-exactly -Z, which
                    // moves the union frame (the Softhouse trigger).
                    world[0] = world[0].ConvertAll(p => new Point3D(p.X, p.Y, p.Z + (random.NextDouble() * 2 - 1) * 2e-14));
                    break;
            }

            List<List<Point3D>> presented = world.ConvertAll(x => x.ToList());
            Rigid motion = null;
            switch (variant)
            {
                case "s0-reversed":
                    presented[0].Reverse();
                    break;
                case "all-reversed":
                    presented.ForEach(x => x.Reverse());
                    break;
                case "start-1":
                case "start-2":
                case "start-3":
                    int shift = int.Parse(variant.Substring(6));
                    presented = presented.ConvertAll(x => x.Skip(shift).Concat(x.Take(shift)).ToList());
                    break;
                case "s0-last":
                    presented.Add(presented[0]);
                    presented.RemoveAt(0);
                    break;
                case "list-reversed":
                    presented.Reverse();
                    break;
                case "rotz-16.13":
                    motion = new Rigid(new double[] { 0, 0, 1 }, 16.13, new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 });
                    break;
                case "rotz-30":
                    motion = new Rigid(new double[] { 0, 0, 1 }, 30, new double[] { 7, 4.5, 3 }, new double[] { 0, 0, 0 });
                    break;
                case "translated":
                    motion = new Rigid(new double[] { 0, 0, 1 }, 0, new double[] { 0, 0, 0 }, new double[] { 1000.25, -500.5, 10 });
                    break;
                case "wall":
                    motion = new Rigid(new double[] { 1, 0, 0 }, 90, new double[] { 0, 0, 3 }, new double[] { 0, 0, 0 });
                    break;
                case "tilted":
                    motion = new Rigid(new double[] { 1, 1, 0 }, 30, new double[] { 7, 4.5, 3 }, new double[] { 0, 0, 0 });
                    break;
            }

            if (motion != null)
            {
                presented = presented.ConvertAll(x => x.ConvertAll(motion.Apply));
            }

            return (world, presented.ConvertAll(x => new Face3D(new Polygon3D(x))), motion);
        }

        private static readonly GeometryFactory Factory = new GeometryFactory();

        private static LinearRing XYRing(IEnumerable<Point3D> point3Ds)
        {
            List<Coordinate> coordinates = point3Ds.Select(x => new Coordinate(x.X, x.Y)).ToList();
            coordinates.Add(coordinates[0].Copy());
            return Factory.CreateLinearRing(coordinates.ToArray());
        }

        private static List<Point3D> Points(IClosedPlanar3D closedPlanar3D)
        {
            return ((ISegmentable3D)closedPlanar3D).GetPoints();
        }

        /// <summary>
        /// The union kept every piece, whole: <paramref name="expectedFaces"/> valid faces with loops of at least 3 points;
        /// mapped back to the world they lie on the slab plane; their region equals the region of the input pieces except
        /// within a band of 2 um about the pieces' own edges (the sub-micrometre seams the union closes or leaves as slits);
        /// and every loop they return - outer and inner - lies in that seam band, so no boundary is invented and no piece,
        /// opening or part of a piece is lost or filled away from the seams. The slab totals 126 m2 and the detached piece 4 m2.
        /// </summary>
        private static void AssertCoversPiecesWhole(List<Face3D> result, List<List<Point3D>> world, Rigid motion, int expectedFaces)
        {
            Assert.NotNull(result);
            Assert.True(result.Count == expectedFaces, $"Expected {expectedFaces} face(s), got {result.Count}: {string.Join(", ", result.Select(x => x.GetArea().ToString("R")))} m2.");

            NetTopologySuite.Geometries.Geometry seams = Factory.BuildGeometry(world.Select(XYRing)).Buffer(2e-6);
            List<Polygon> polygons = new List<Polygon>();
            foreach (Face3D face3D in result)
            {
                Assert.True(face3D.IsValid());
                List<List<Point3D>> loops = new List<IClosedPlanar3D> { face3D.GetExternalEdge3D() }
                    .Concat(face3D.GetInternalEdge3Ds() ?? new List<IClosedPlanar3D>())
                    .Select(x => Points(x).ConvertAll(p => motion == null ? p : motion.Inverse(p)))
                    .ToList();
                Assert.All(loops, x => Assert.True(x.Count >= 3, "A loop of fewer than 3 points was returned."));
                Assert.All(loops.SelectMany(x => x), p => Assert.Equal(Seed400Z, p.Z, 6));
                Assert.All(loops, x => Assert.True(seams.Covers(XYRing(x)), "A loop away from the pieces' seams was returned."));
                polygons.Add(Factory.CreatePolygon(XYRing(loops[0]), loops.Skip(1).Select(XYRing).ToArray()));
            }

            NetTopologySuite.Geometries.Geometry expected = Factory.BuildGeometry(world.Select(x => Factory.CreatePolygon(XYRing(x)))).Union();
            NetTopologySuite.Geometries.Geometry actual = Factory.BuildGeometry(polygons).Union();
            double outsideSeams = expected.SymmetricDifference(actual).Difference(seams).Area;
            Assert.True(outsideSeams < 1e-9, $"Covered region differs by {outsideSeams} m2 away from the pieces' seams.");

            List<double> areas = result.Select(x => x.GetArea()).OrderBy(x => x).ToList();
            Assert.Equal(4.0, areas[0], 5); // vertex rounding in a rotated frame moves an 8 m perimeter by < 1 um
            Assert.Equal(126.0, areas.Skip(1).Sum(), 4);
        }

        [Fact]
        public void Union_Seed400DominantPieceFirst_KeepsMergedSlab()
        {
            // Arrange: as listed (s0 first, clockwise) - the merged slab's hole re-union yields a 2-point loop.
            (List<List<Point3D>> world, List<Face3D> faces, Rigid motion) = Seed400Variant("listed");

            // Act
            List<Face3D> result = Geometry.Spatial.Query.Union(faces, Core.Tolerance.Distance);

            // Assert: one slab face and the detached piece.
            AssertCoversPiecesWhole(result, world, motion, 2);
        }

        [Theory]
        [InlineData("s0-reversed", 2)]
        [InlineData("all-reversed", 2)]
        [InlineData("start-1", 2)]
        [InlineData("start-2", 2)]
        [InlineData("start-3", 2)]
        [InlineData("s0-last", 2)]
        [InlineData("list-reversed", 2)]
        [InlineData("rotz-16.13", 2)]
        [InlineData("rotz-30", 2)]
        [InlineData("translated", 2)]
        [InlineData("wall", 2)]
        [InlineData("jitter-1", 2)]
        [InlineData("jitter-2", 2)]
        [InlineData("jitter-3", 2)]
        // In these two frames the union leaves the x = 5 seam open, so the slab comes back as its 45 m2 and 81 m2 halves
        // (the same before this correction): a merge left incomplete, not a loss - every piece is still covered.
        [InlineData("tilted", 3)]
        [InlineData("znoise", 3)]
        public void Union_Seed400Represented_KeepsMergedSlabWhole(string variant, int expectedFaces)
        {
            // Arrange: the same slab re-presented (order, loop start, direction), rigidly moved or perturbed below tolerance.
            (List<List<Point3D>> world, List<Face3D> faces, Rigid motion) = Seed400Variant(variant);

            // Act
            List<Face3D> result = Geometry.Spatial.Query.Union(faces, Core.Tolerance.Distance);

            // Assert
            AssertCoversPiecesWhole(result, world, motion, expectedFaces);
        }

        // ── Genuine small openings survive the union and its hole re-union ──

        private static Geometry.Planar.Polygon2D Rectangle2D(double x0, double y0, double x1, double y1)
        {
            return new Geometry.Planar.Polygon2D(new List<Geometry.Planar.Point2D>
            {
                new Geometry.Planar.Point2D(x0, y0), new Geometry.Planar.Point2D(x1, y0), new Geometry.Planar.Point2D(x1, y1), new Geometry.Planar.Point2D(x0, y1),
            });
        }

        private static Face3D HorizontalFace(double z, Geometry.Planar.Polygon2D outline, params Geometry.Planar.Polygon2D[] openings)
        {
            Plane plane = new Plane(new Point3D(0, 0, z), Vector3D.WorldZ);
            Geometry.Planar.Face2D face2D = openings.Length == 0
                ? new Geometry.Planar.Face2D(outline)
                : Geometry.Planar.Create.Face2D(outline, openings.Cast<Geometry.Planar.IClosed2D>().ToList());
            return new Face3D(plane, face2D);
        }

        public static IEnumerable<object[]> GenuineSmallOpenings()
        {
            // A face carrying a valid 1 x 1 mm opening beside an abutting face.
            yield return new object[]
            {
                new List<Face3D> { HorizontalFace(3, Rectangle2D(-1, -1, 1, 1), Rectangle2D(0, 0, 0.001, 0.001)), HorizontalFace(3, Rectangle2D(1, -1, 3, 1)) },
                new[] { Rectangle2D(0, 0, 0.001, 0.001) },
                8.0 - 1e-6,
            };

            // Four pieces framing a 1 x 1 mm opening that only the union forms.
            yield return new object[]
            {
                new List<Face3D>
                {
                    HorizontalFace(3, Rectangle2D(-1, -1, 1, 0)), HorizontalFace(3, Rectangle2D(-1, 0.001, 1, 1)),
                    HorizontalFace(3, Rectangle2D(-1, 0, 0, 0.001)), HorizontalFace(3, Rectangle2D(0.001, 0, 1, 0.001)),
                },
                new[] { Rectangle2D(0, 0, 0.001, 0.001) },
                4.0 - 1e-6,
            };

            // Two 1 x 1 mm openings in one merged face, so the union's hole re-union runs on genuine small holes.
            yield return new object[]
            {
                new List<Face3D> { HorizontalFace(3, Rectangle2D(-1, -1, 1, 1), Rectangle2D(0, 0, 0.001, 0.001), Rectangle2D(0.5, 0.5, 0.501, 0.501)), HorizontalFace(3, Rectangle2D(1, -1, 3, 1)) },
                new[] { Rectangle2D(0, 0, 0.001, 0.001), Rectangle2D(0.5, 0.5, 0.501, 0.501) },
                8.0 - 2e-6,
            };

            // A thin but valid triangular opening (2.5 m x 2 um) beside a 1 x 1 mm opening.
            yield return new object[]
            {
                new List<Face3D>
                {
                    HorizontalFace(3, Rectangle2D(-3, -1, 1, 1), Rectangle2D(0, 0, 0.001, 0.001),
                        new Geometry.Planar.Polygon2D(new List<Geometry.Planar.Point2D> { new Geometry.Planar.Point2D(-2.75, -0.5), new Geometry.Planar.Point2D(-0.25, -0.5), new Geometry.Planar.Point2D(-0.25, -0.499998) })),
                    HorizontalFace(3, Rectangle2D(1, -1, 3, 1)),
                },
                new[] { Rectangle2D(0, 0, 0.001, 0.001), new Geometry.Planar.Polygon2D(new List<Geometry.Planar.Point2D> { new Geometry.Planar.Point2D(-2.75, -0.5), new Geometry.Planar.Point2D(-0.25, -0.5), new Geometry.Planar.Point2D(-0.25, -0.499998) }) },
                12.0 - 1e-6 - 2.5e-6,
            };
        }

        [Theory]
        [MemberData(nameof(GenuineSmallOpenings))]
        public void Union_GenuineSmallOpenings_KeepsEveryOpeningWhole(List<Face3D> faces, Geometry.Planar.Polygon2D[] openings, double expectedArea)
        {
            // Act
            List<Face3D> result = Geometry.Spatial.Query.Union(faces, Core.Tolerance.Distance);

            // Assert: one valid face; each opening kept as one loop with exactly its own vertices.
            Face3D merged = Assert.Single(result);
            Assert.True(merged.IsValid());
            List<IClosedPlanar3D> holes = merged.GetInternalEdge3Ds();
            Assert.Equal(openings.Length, holes.Count);
            foreach (Geometry.Planar.Polygon2D opening in openings)
            {
                Assert.Contains(holes, x =>
                {
                    List<Point3D> points = Points(x);
                    return points.Count == opening.Count && opening.GetPoints().All(p => points.Any(q => System.Math.Abs(q.X - p.X) < 1e-9 && System.Math.Abs(q.Y - p.Y) < 1e-9 && System.Math.Abs(q.Z - 3) < 1e-9));
                });
            }

            Assert.Equal(expectedArea, merged.GetArea(), 9);
        }
    }
}
