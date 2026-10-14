// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The implicit conversions between points and vectors. <c>Vector3D</c> to <c>Point3D</c> used to return a new
    /// <c>Vector3D</c>, which the same operator converted again until the stack overflowed (the test host crashes rather
    /// than reporting a failure if it regresses).
    /// </summary>
    public class VectorPointConversionTests
    {
        private static Point3D ToPoint3D(Point3D point3D) => point3D;

        [Fact]
        public void Vector3D_ImplicitToPoint3D_KeepsCoordinates()
        {
            // Arrange
            Vector3D vector3D = new Vector3D(1.5, -2.25, 3.0);

            // Act: an implicit conversion, as when a vector is passed where a point is expected.
            Point3D point3D = ToPoint3D(vector3D);

            // Assert
            Assert.IsType<Point3D>(point3D);
            Assert.Equal(1.5, point3D.X);
            Assert.Equal(-2.25, point3D.Y);
            Assert.Equal(3.0, point3D.Z);
        }

        [Fact]
        public void Vector3D_ImplicitToPoint3D_NullIsNull()
        {
            // Arrange
            Vector3D vector3D = null;

            // Act
            Point3D point3D = vector3D;

            // Assert
            Assert.Null(point3D);
        }

        [Fact]
        public void Point3D_ImplicitToVector3DAndBack_KeepsCoordinates()
        {
            // Arrange
            Point3D point3D = new Point3D(-4, 5.5, 0.125);

            // Act
            Vector3D vector3D = point3D;
            Point3D result = vector3D;

            // Assert
            Assert.Equal(point3D.X, result.X);
            Assert.Equal(point3D.Y, result.Y);
            Assert.Equal(point3D.Z, result.Z);
        }

        [Fact]
        public void Vector2D_ImplicitToPoint2D_KeepsCoordinates()
        {
            // Arrange
            Vector2D vector2D = new Vector2D(3.0, 4.0);

            // Act
            Point2D point2D = vector2D;

            // Assert
            Assert.IsType<Point2D>(point2D);
            Assert.Equal(3.0, point2D.X);
            Assert.Equal(4.0, point2D.Y);
        }
    }
}
