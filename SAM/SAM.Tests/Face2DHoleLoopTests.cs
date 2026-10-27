// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Geometries;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Conversion of NTS rings to SAM loops (<see cref="Geometry.Planar.Convert.ToSAM(LinearRing, double)"/>,
    /// <see cref="Geometry.Planar.Convert.ToSAM_Polygon2Ds(Polygon, double)"/>) and the multiple-hole re-union in
    /// <see cref="Geometry.Planar.Create.Face2D(IClosed2D, IEnumerable{IClosed2D}, Geometry.EdgeOrientationMethod)"/>: a ring
    /// the conversion collapses to fewer than 3 distinct points is no loop, so it must not reach a face as a 2-point loop
    /// (which makes the whole face invalid). Every ring with 3 or more distinct points is kept, however thin.
    /// </summary>
    public class Face2DHoleLoopTests
    {
        private static readonly GeometryFactory Factory = new GeometryFactory();

        private static LinearRing Ring(params double[] xy)
        {
            List<Coordinate> coordinates = new List<Coordinate>();
            for (int i = 0; i < xy.Length; i += 2)
            {
                coordinates.Add(new Coordinate(xy[i], xy[i + 1]));
            }

            coordinates.Add(coordinates[0].Copy());
            return Factory.CreateLinearRing(coordinates.ToArray());
        }

        private static Polygon2D Loop(params double[] xy)
        {
            List<Point2D> point2Ds = new List<Point2D>();
            for (int i = 0; i < xy.Length; i += 2)
            {
                point2Ds.Add(new Point2D(xy[i], xy[i + 1]));
            }

            return new Polygon2D(point2Ds);
        }

        // ── LinearRing.ToSAM ─────────────────────────────────────────────

        public static IEnumerable<object[]> RingsCollapsingToTwoPoints()
        {
            // The ring SAM's own hole re-union produced in the seed-400 slab (bit-exact): (-1.625, 1.75) is within the
            // distance tolerance of (-1.625, 1.749999), so the conversion keeps A, B, A.
            yield return new object[] { new[] { 0.875, 1.75, -1.625, 1.749999, -1.625, 1.75 } };

            // A sliver whose third point rounds onto the second (2.5000004 -> 2.5, 4e-7 -> 0).
            yield return new object[] { new[] { 0.0, 0.0, 2.5, 0.0, 2.5000004, 4e-7 } };
        }

        [Theory]
        [MemberData(nameof(RingsCollapsingToTwoPoints))]
        public void ToSAM_RingCollapsingToTwoPoints_ReturnsNull(double[] xy)
        {
            // Arrange
            LinearRing linearRing = Ring(xy);

            // Act
            Polygon2D polygon2D = linearRing.ToSAM(Core.Tolerance.Distance);

            // Assert
            Assert.Null(polygon2D);
        }

        [Fact]
        public void ToSAM_ZeroToleranceRingWithTwoDistinctPoints_ReturnsNull()
        {
            // Arrange: A, B, A (closed). With tolerance 0 nothing is rounded or merged; the closing point alone is dropped.
            LinearRing linearRing = Factory.CreateLinearRing(new[] { new Coordinate(0, 0), new Coordinate(2.5, 0), new Coordinate(0, 0) });

            // Act
            Polygon2D polygon2D = linearRing.ToSAM(0);

            // Assert
            Assert.Null(polygon2D);
        }

        public static IEnumerable<object[]> ThinTrianglesWithDistinctPoints()
        {
            // Two grid steps wide (2e-6 m): three distinct points, area 2.5e-6 m2.
            yield return new object[] { new[] { 0.0, 0.0, 2.5, 0.0, 2.5, 2e-6 }, 0.0 };

            // One step apart in y but far apart in x: no two consecutive points within the tolerance.
            yield return new object[] { new[] { 0.875, 1.75, -1.625, 1.749998, 0.0, 1.75 }, 0.0 };

            // The same thin triangle with tolerance 0 (no rounding, no merging).
            yield return new object[] { new[] { 0.0, 0.0, 2.5, 0.0, 2.5, 3e-7 }, -1.0 };
        }

        [Theory]
        [MemberData(nameof(ThinTrianglesWithDistinctPoints))]
        public void ToSAM_ThinTriangleWithDistinctPoints_KeepsThreePoints(double[] xy, double toleranceFlag)
        {
            // Arrange
            double tolerance = toleranceFlag < 0 ? 0 : Core.Tolerance.Distance;
            LinearRing linearRing = Ring(xy);

            // Act
            Polygon2D polygon2D = linearRing.ToSAM(tolerance);

            // Assert: the same three points, in ring order, starting where the ring starts.
            Assert.NotNull(polygon2D);
            List<Point2D> point2Ds = polygon2D.GetPoints();
            Assert.Equal(3, point2Ds.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(Core.Query.Round(xy[2 * i], tolerance), point2Ds[i].X);
                Assert.Equal(Core.Query.Round(xy[2 * i + 1], tolerance), point2Ds[i].Y);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ToSAM_SquareFromAnyStartOrDirection_KeepsFourPointsInRingOrder(int start)
        {
            // Arrange: a closed square listed from each start vertex, both directions.
            double[] xy = { 0, 0, 1, 0, 1, 1, 0, 1 };
            foreach (bool reverse in new[] { false, true })
            {
                List<double[]> points = Enumerable.Range(0, 4).Select(i => new[] { xy[2 * i], xy[2 * i + 1] }).ToList();
                if (reverse)
                {
                    points.Reverse();
                }

                points = points.Skip(start).Concat(points.Take(start)).ToList();
                LinearRing linearRing = Ring(points.SelectMany(x => x).ToArray());

                // Act
                Polygon2D polygon2D = linearRing.ToSAM(Core.Tolerance.Distance);

                // Assert
                Assert.Equal(points.Select(x => new Point2D(x[0], x[1])), polygon2D.GetPoints());
            }
        }

        // ── ToSAM_Polygon2Ds ─────────────────────────────────────────────

        [Fact]
        public void ToSAM_Polygon2Ds_DegenerateInteriorRing_OmitsIt()
        {
            // Arrange: a 10 x 10 m shell, a genuine 1 x 1 mm hole and a hole ring that converts to two points.
            LinearRing shell = Ring(-5, -5, 5, -5, 5, 5, -5, 5);
            LinearRing genuine = Ring(0, 0, 0, 0.001, 0.001, 0.001, 0.001, 0);
            LinearRing collapsing = Ring(0.875, 1.75, -1.625, 1.749999, -1.625, 1.75);
            Polygon polygon = Factory.CreatePolygon(shell, new[] { genuine, collapsing });

            // Act
            List<Polygon2D> polygon2Ds = polygon.ToSAM_Polygon2Ds(Core.Tolerance.Distance);

            // Assert: the shell and the genuine hole, every one a loop of at least 3 points.
            Assert.Equal(2, polygon2Ds.Count);
            Assert.All(polygon2Ds, x => Assert.True(x != null && x.Count >= 3));
            Assert.Equal(100.0, polygon2Ds[0].GetArea(), 9);
            Assert.Equal(1e-6, polygon2Ds[1].GetArea(), 12);
        }

        [Fact]
        public void ToSAM_Polygon2Ds_DegenerateShellOnly_ReturnsEmpty()
        {
            // Arrange: a polygon whose only ring converts to two points (the single-ring branch).
            Polygon polygon = Factory.CreatePolygon(Ring(0.875, 1.75, -1.625, 1.749999, -1.625, 1.75));

            // Act
            List<Polygon2D> polygon2Ds = polygon.ToSAM_Polygon2Ds(Core.Tolerance.Distance);

            // Assert
            Assert.NotNull(polygon2Ds);
            Assert.Empty(polygon2Ds);
        }

        // ── Face2D.Create multiple-hole re-union ─────────────────────────

        [Fact]
        public void Face2D_TwoHolesOneCollapsing_IsValidAndKeepsGenuineHole()
        {
            // Arrange: the two hole loops that entered Face2D.Create for the seed-400 slab (bit-exact). The second is a
            // 2.5 m x 1 um sliver with a zero-width spike; re-unioned with the first it converts to (0.875, 1.75),
            // (-1.625, 1.749999) and back, two points.
            Polygon2D external = Loop(-10, -10, 10, -10, 10, 10, -10, 10);
            Polygon2D genuine = Loop(-1.625001, 0.75, -1.625001, -2.25, -1.625, -0.25);
            Polygon2D sliver = Loop(-1.625, 2.25, -1.625, 1.749999, 0.875, 1.75, -1.625, 1.75);

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, new List<IClosed2D> { genuine, sliver });

            // Assert: valid, the external edge unchanged and exactly one hole - the genuine one, whole boundary.
            Assert.NotNull(face2D);
            Assert.True(face2D.IsValid());
            Assert.Equal(external.GetPoints().ToHashSet(), ((Polygon2D)face2D.ExternalEdge2D).GetPoints().ToHashSet());
            IClosed2D hole = Assert.Single(face2D.InternalEdge2Ds);
            Assert.Equal(genuine.GetPoints().ToHashSet(), ((Polygon2D)hole).GetPoints().ToHashSet());
        }

        [Fact]
        public void Face2D_TwoGenuineSmallHoles_KeepsBoth()
        {
            // Arrange: two 1 x 1 mm openings and a thin valid triangle (2.5 m x 2 um), all re-unioned by Face2D.Create.
            Polygon2D external = Loop(-10, -10, 10, -10, 10, 10, -10, 10);
            Polygon2D first = Loop(0, 0, 0.001, 0, 0.001, 0.001, 0, 0.001);
            Polygon2D second = Loop(2, 2, 2.001, 2, 2.001, 2.001, 2, 2.001);
            Polygon2D thin = Loop(-5, -5, -2.5, -5, -2.5, -4.999998);

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, new List<IClosed2D> { first, second, thin });

            // Assert
            Assert.True(face2D.IsValid());
            Assert.Equal(3, face2D.InternalEdge2Ds.Count);
            foreach (Polygon2D expected in new[] { first, second, thin })
            {
                Assert.Contains(face2D.InternalEdge2Ds, x => ((Polygon2D)x).GetPoints().ToHashSet().SetEquals(expected.GetPoints()));
            }

            Assert.Equal(400.0 - 2e-6 - 2.5e-6, face2D.GetArea(), 9);
        }
    }
}
