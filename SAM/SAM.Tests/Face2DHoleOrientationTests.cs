// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Geometry.Planar.Create.Face2D(IClosed2D, IEnumerable{IClosed2D}, EdgeOrientationMethod)"/> re-unions two or more
    /// holes. The union returns its loops in its own winding, so the winding the caller asked for
    /// (<see cref="EdgeOrientationMethod.Opposite"/> or <see cref="EdgeOrientationMethod.Similar"/> relative to the external edge)
    /// must be applied to the unioned loops as it is to a single hole; <see cref="EdgeOrientationMethod.Undefined"/> keeps the union output.
    /// A hole wound like its outer loop is rejected by the native face builder, which then drops the whole face.
    /// </summary>
    public class Face2DHoleOrientationTests
    {
        private static Polygon2D Square(double x, double y, double size, bool counterClockwise)
        {
            List<Point2D> point2Ds = new List<Point2D>
            {
                new Point2D(x, y),
                new Point2D(x + size, y),
                new Point2D(x + size, y + size),
                new Point2D(x, y + size),
            };

            if (!counterClockwise)
            {
                point2Ds.Reverse();
            }

            return new Polygon2D(point2Ds);
        }

        private static Orientation OrientationOf(IClosed2D closed2D) => ((Polygon2D)closed2D).GetOrientation();

        private static Orientation Opposite(Orientation orientation) => orientation == Orientation.Clockwise ? Orientation.CounterClockwise : Orientation.Clockwise;

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Face2D_TwoSeparateHolesOpposite_HolesWindOppositeToOuter(bool outerCounterClockwise)
        {
            // Arrange: 10 x 10 outer and two 2 x 2 holes that do not touch (the union keeps them as two loops).
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            Orientation outer = OrientationOf(external);
            List<IClosed2D> holes = new List<IClosed2D> { Square(2, 2, 2, true), Square(6, 6, 2, false) };

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, holes, EdgeOrientationMethod.Opposite);

            // Assert
            Assert.NotNull(face2D);
            Assert.True(face2D.IsValid());
            Assert.Equal(outerCounterClockwise ? Orientation.CounterClockwise : Orientation.Clockwise, OrientationOf(face2D.ExternalEdge2D));
            Assert.Equal(2, face2D.InternalEdge2Ds.Count);
            Assert.All(face2D.InternalEdge2Ds, x => Assert.Equal(Opposite(outer), OrientationOf(x)));
            Assert.Equal(92.0, face2D.GetArea(), 9);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Face2D_TwoSeparateHolesSimilar_HolesWindLikeOuter(bool outerCounterClockwise)
        {
            // Arrange
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            Orientation outer = OrientationOf(external);
            List<IClosed2D> holes = new List<IClosed2D> { Square(2, 2, 2, true), Square(6, 6, 2, false) };

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, holes, EdgeOrientationMethod.Similar);

            // Assert
            Assert.Equal(2, face2D.InternalEdge2Ds.Count);
            Assert.All(face2D.InternalEdge2Ds, x => Assert.Equal(outer, OrientationOf(x)));
            Assert.Equal(92.0, face2D.GetArea(), 9);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Face2D_TwoSeparateHolesDefault_IsOpposite(bool outerCounterClockwise)
        {
            // Arrange: the overload default (Opposite) is what the panel and face builders use.
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            List<IClosed2D> holes = new List<IClosed2D> { Square(2, 2, 2, outerCounterClockwise), Square(6, 6, 2, outerCounterClockwise) };

            // Act: holes given wound like the outer loop, as a caller that built them from the same plane would.
            Face2D face2D = Geometry.Planar.Create.Face2D(external, holes);

            // Assert
            Assert.Equal(2, face2D.InternalEdge2Ds.Count);
            Assert.All(face2D.InternalEdge2Ds, x => Assert.Equal(Opposite(OrientationOf(external)), OrientationOf(x)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Face2D_TwoSeparateHolesUndefined_KeepsUnionOutput(bool outerCounterClockwise)
        {
            // Arrange
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            Polygon2D first = Square(2, 2, 2, true);
            Polygon2D second = Square(6, 6, 2, true);
            List<Polygon2D> union = new List<Polygon2D> { new Polygon2D(first.GetPoints()), new Polygon2D(second.GetPoints()) }.Union(Core.Tolerance.Distance);

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, new List<IClosed2D> { first, second }, EdgeOrientationMethod.Undefined);

            // Assert: no orientation applied - the holes carry exactly the winding the union gave them.
            Assert.Equal(union.Count, face2D.InternalEdge2Ds.Count);
            Assert.Equal(union.Select(x => x.GetOrientation()).OrderBy(x => x), face2D.InternalEdge2Ds.Select(OrientationOf).OrderBy(x => x));
            Assert.Equal(92.0, face2D.GetArea(), 9);
        }

        [Theory]
        [InlineData(true, EdgeOrientationMethod.Opposite)]
        [InlineData(false, EdgeOrientationMethod.Opposite)]
        [InlineData(true, EdgeOrientationMethod.Similar)]
        [InlineData(false, EdgeOrientationMethod.Similar)]
        public void Face2D_OverlappingHolesUnionIntoOne_HonoursRequestedOrientation(bool outerCounterClockwise, EdgeOrientationMethod method)
        {
            // Arrange: two 2 x 2 holes overlapping by 1 x 1 -> one hole of area 7.
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            Orientation outer = OrientationOf(external);
            List<IClosed2D> holes = new List<IClosed2D> { Square(2, 2, 2, true), Square(3, 3, 2, true) };

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, holes, method);

            // Assert
            IClosed2D hole = Assert.Single(face2D.InternalEdge2Ds);
            Assert.Equal(method == EdgeOrientationMethod.Similar ? outer : Opposite(outer), OrientationOf(hole));
            Assert.Equal(7.0, hole.GetArea(), 9);
            Assert.Equal(93.0, face2D.GetArea(), 9);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PanelReload_TwoHoles_GetFace3DWindsHolesOppositeToOuter(bool outerCounterClockwise)
        {
            // Arrange: a panel (saved to JSON and reloaded) whose face has a 10 x 10 outer loop and two 2 x 2 holes on a -z plane.
            // Panel.GetFace3D() rebuilds the face through Face2D.Create from the stored loops, so a saved panel gets the
            // requested winding on every reload.
            Geometry.Spatial.Plane plane = new Geometry.Spatial.Plane(new Geometry.Spatial.Point3D(0, 0, 3.475), new Geometry.Spatial.Vector3D(0, 0, -1));
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);
            Geometry.Spatial.Face3D face3D = Geometry.Spatial.Face3D.Create(plane, external, new List<IClosed2D> { Square(2, 2, 2, true), Square(6, 6, 2, true) });
            Analytical.Panel panel = Analytical.Create.Panel(new Analytical.Construction("test"), Analytical.PanelType.Floor, face3D);

            // Act
            Analytical.Panel reloaded = new Analytical.Panel(panel.ToJsonObject());
            Geometry.Spatial.Face3D face = reloaded.GetFace3D();

            // Assert
            Assert.NotNull(face);
            Assert.Equal(2, face.GetInternalEdge3Ds().Count);
            Assert.Equal(92.0, face.GetArea(), 9);
            Orientation outer = OrientationOf(face.ExternalEdge2D);
            Assert.All(face.InternalEdge2Ds, x => Assert.Equal(Opposite(outer), OrientationOf(x)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Face2D_SingleHole_StillHonoursOrientation(bool outerCounterClockwise)
        {
            // Arrange: the single-hole path (no union) is unchanged.
            Polygon2D external = Square(0, 0, 10, outerCounterClockwise);

            // Act
            Face2D face2D = Geometry.Planar.Create.Face2D(external, new List<IClosed2D> { Square(2, 2, 2, outerCounterClockwise) });

            // Assert
            IClosed2D hole = Assert.Single(face2D.InternalEdge2Ds);
            Assert.Equal(Opposite(OrientationOf(external)), OrientationOf(hole));
            Assert.Equal(96.0, face2D.GetArea(), 9);
        }
    }
}
