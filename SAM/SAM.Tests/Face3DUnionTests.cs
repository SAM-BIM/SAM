// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

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
    }
}
