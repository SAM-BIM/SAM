// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using SAM.Geometry;
using SAM.Geometry.Planar;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Triangle2D.Reverse() used to call the LINQ Enumerable.Reverse on its Point2D[] and discard the result, so it
    /// never changed anything. Face.Reverse() reaches it through IReversible when an edge is a Triangle2D.
    /// </summary>
    public class Triangle2DReverseTests
    {
        private const double Tol = 1e-9;

        private static readonly Point2D A = new Point2D(0, 0);
        private static readonly Point2D B = new Point2D(4, 0);
        private static readonly Point2D C = new Point2D(0, 3);

        [Fact]
        public void Reverse_ReversesStoredVertexOrder()
        {
            Triangle2D triangle2D = new Triangle2D(A, B, C);

            triangle2D.Reverse();

            List<Point2D> point2Ds = triangle2D.GetPoints();
            AssertSame(C, point2Ds[0]);
            AssertSame(B, point2Ds[1]);
            AssertSame(A, point2Ds[2]);
        }

        [Fact]
        public void Reverse_FlipsOrientation()
        {
            Triangle2D triangle2D = new Triangle2D(A, B, C);
            Assert.Equal(Orientation.CounterClockwise, triangle2D.GetOrientation());

            triangle2D.Reverse();

            Assert.Equal(Orientation.Clockwise, triangle2D.GetOrientation());
        }

        [Fact]
        public void Reverse_ThroughIReversible_ReversesStoredVertexOrder()
        {
            Triangle2D triangle2D = new Triangle2D(A, B, C);
            IReversible reversible = triangle2D;

            reversible.Reverse();

            List<Point2D> point2Ds = triangle2D.GetPoints();
            AssertSame(C, point2Ds[0]);
            AssertSame(B, point2Ds[1]);
            AssertSame(A, point2Ds[2]);
        }

        [Fact]
        public void Reverse_ThroughFace2D_ReversesTriangleExternalEdge()
        {
            Face2D face2D = new Triangle2D(A, B, C);

            face2D.Reverse();

            Triangle2D externalEdge2D = Assert.IsType<Triangle2D>(face2D.ExternalEdge2D);
            List<Point2D> point2Ds = externalEdge2D.GetPoints();
            AssertSame(C, point2Ds[0]);
            AssertSame(B, point2Ds[1]);
            AssertSame(A, point2Ds[2]);
            Assert.Equal(Orientation.Clockwise, externalEdge2D.GetOrientation());
        }

        [Fact]
        public void Reverse_Twice_RestoresOriginalOrder()
        {
            Triangle2D triangle2D = new Triangle2D(A, B, C);

            triangle2D.Reverse();
            triangle2D.Reverse();

            List<Point2D> point2Ds = triangle2D.GetPoints();
            AssertSame(A, point2Ds[0]);
            AssertSame(B, point2Ds[1]);
            AssertSame(C, point2Ds[2]);
        }

        [Fact]
        public void Reverse_KeepsAreaAndPerimeter()
        {
            Triangle2D triangle2D = new Triangle2D(A, B, C);
            double area = triangle2D.GetArea();
            double perimeter = triangle2D.GetPerimeter();

            triangle2D.Reverse();

            Assert.Equal(area, triangle2D.GetArea(), 9);
            Assert.Equal(perimeter, triangle2D.GetPerimeter(), 9);
            Assert.Equal(6.0, triangle2D.GetArea(), 9);
            Assert.Equal(12.0, triangle2D.GetPerimeter(), 9);
        }

        private static void AssertSame(Point2D expected, Point2D actual)
        {
            Assert.NotNull(actual);
            Assert.True(expected.Distance(actual) < Tol, $"expected {expected.X},{expected.Y} but was {actual.X},{actual.Y}");
        }
    }
}
