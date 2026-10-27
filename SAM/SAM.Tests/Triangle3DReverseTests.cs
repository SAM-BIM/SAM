// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Triangle3D.Reverse() used to call the LINQ Enumerable.Reverse on its Point3D[] and discard the result, so it
    /// never changed anything. Its two callers (Query.Volume(Shell) and Create.Mesh3D(Shell)) rely on it to correct
    /// triangle winding against the parent face normal.
    /// </summary>
    public class Triangle3DReverseTests
    {
        private const double Tol = 1e-9;

        private static readonly Point3D A = new Point3D(0, 0, 0);
        private static readonly Point3D B = new Point3D(4, 0, 0);
        private static readonly Point3D C = new Point3D(0, 3, 0);

        public static IEnumerable<object[]> Poses()
        {
            foreach (string pose in new string[] { "identity", "translation", "rotationZ", "rotationXYZ", "rotationAndTranslation" })
            {
                yield return new object[] { pose };
            }
        }

        private static Transform3D GetPose(string pose)
        {
            switch (pose)
            {
                case "translation":
                    return Transform3D.GetTranslation(12.5, -7.25, 3.0);
                case "rotationZ":
                    return Transform3D.GetRotationZ(0.7);
                case "rotationXYZ":
                    return Transform3D.GetRotationX(0.9).Multiply(Transform3D.GetRotationY(-1.1)).Multiply(Transform3D.GetRotationZ(0.4));
                case "rotationAndTranslation":
                    return Transform3D.GetRotationY(2.3).Multiply(Transform3D.GetRotationX(0.6)).Multiply(Transform3D.GetTranslation(-3, 9, 21));
                default:
                    return Transform3D.GetIdentity();
            }
        }

        // ---- Triangle3D unit tests -------------------------------------------------------------------------

        [Fact]
        public void Reverse_Triangle_ReversesVertexOrder()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);

            triangle3D.Reverse();

            List<Point3D> point3Ds = triangle3D.GetPoints();
            AssertSame(C, point3Ds[0]);
            AssertSame(B, point3Ds[1]);
            AssertSame(A, point3Ds[2]);
        }

        [Fact]
        public void Reverse_Triangle_IndexerReflectsNewOrder()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);

            triangle3D.Reverse();

            AssertSame(C, triangle3D[0]);
            AssertSame(B, triangle3D[1]);
            AssertSame(A, triangle3D[2]);
        }

        [Fact]
        public void Reverse_Triangle_FlipsNormal()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);
            Vector3D before = triangle3D.GetNormal();

            triangle3D.Reverse();

            Vector3D after = triangle3D.GetNormal();
            Assert.Equal(-before.X, after.X, 9);
            Assert.Equal(-before.Y, after.Y, 9);
            Assert.Equal(-before.Z, after.Z, 9);
            Assert.False(after.SameHalf(before));
        }

        [Theory]
        [MemberData(nameof(Poses))]
        public void Reverse_NonzeroSignedVolume_ChangesSign(string pose)
        {
            Triangle3D triangle3D = (Triangle3D)new Triangle3D(new Point3D(1, 2, 3), new Point3D(5, 2, 3.5), new Point3D(1, 6, 4)).GetTransformed(GetPose(pose));
            double before = triangle3D.SignedVolume();
            Assert.True(System.Math.Abs(before) > 1e-6, pose);

            triangle3D.Reverse();

            Assert.Equal(-before, triangle3D.SignedVolume(), 9);
        }

        [Theory]
        [MemberData(nameof(Poses))]
        public void Reverse_Twice_RestoresOriginalVertexSequence(string pose)
        {
            Triangle3D original = (Triangle3D)new Triangle3D(A, B, C).GetTransformed(GetPose(pose));
            Triangle3D triangle3D = new Triangle3D(original);
            triangle3D.Reverse();
            Assert.False(original == triangle3D, pose);

            triangle3D.Reverse();

            Assert.True(original == triangle3D, pose);
            Assert.True(original.Equals(triangle3D), pose);
        }

        [Theory]
        [MemberData(nameof(Poses))]
        public void Reverse_Triangle_PreservesAreaCentroidAndBounds(string pose)
        {
            Triangle3D triangle3D = (Triangle3D)new Triangle3D(A, B, C).GetTransformed(GetPose(pose));
            double area = triangle3D.GetArea();
            Point3D centroid = triangle3D.GetCentroid();
            BoundingBox3D bounds = triangle3D.GetBoundingBox();
            double length = triangle3D.GetLength();

            triangle3D.Reverse();

            Assert.Equal(area, triangle3D.GetArea(), 9);
            Assert.Equal(length, triangle3D.GetLength(), 9);
            AssertSame(centroid, triangle3D.GetCentroid());
            BoundingBox3D boundsAfter = triangle3D.GetBoundingBox();
            AssertSame(bounds.Min, boundsAfter.Min);
            AssertSame(bounds.Max, boundsAfter.Max);
        }

        [Fact]
        public void Reverse_Triangle_ReversesSegmentsAndKeepsClosedLoop()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);

            triangle3D.Reverse();

            List<Segment3D> segment3Ds = triangle3D.GetSegments();
            Assert.Equal(3, segment3Ds.Count);
            AssertSame(C, segment3Ds[0][0]);
            AssertSame(B, segment3Ds[0][1]);
            AssertSame(B, segment3Ds[1][0]);
            AssertSame(A, segment3Ds[1][1]);
            AssertSame(A, segment3Ds[2][0]);
            AssertSame(C, segment3Ds[2][1]);
        }

        [Fact]
        public void Reverse_Triangle_SurvivesCloneAndJsonRoundTrip()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);
            triangle3D.Reverse();

            Triangle3D clone = (Triangle3D)triangle3D.Clone();
            Triangle3D fromJson = new Triangle3D(triangle3D.ToJsonObject());

            Assert.True(triangle3D == clone);
            Assert.True(triangle3D == fromJson);
            AssertSame(C, fromJson[0]);
            AssertSame(A, fromJson[2]);
        }

        [Fact]
        public void Reverse_Triangle_DoesNotAffectCopiesOrSourcePoints()
        {
            Triangle3D triangle3D = new Triangle3D(A, B, C);
            Triangle3D copy = new Triangle3D(triangle3D);

            triangle3D.Reverse();

            AssertSame(A, copy[0]);
            AssertSame(C, copy[2]);
            Assert.Equal(0, A.X);
            Assert.Equal(4, B.X);
            Assert.Equal(3, C.Y);
        }

        [Fact]
        public void Reverse_Triangle3DObject_ReversesAndKeepsMetadata()
        {
            Triangle3DObject triangle3DObject = new Triangle3DObject(new Triangle3D(A, B, C));
            Vector3D before = triangle3DObject.GetNormal();

            triangle3DObject.Reverse();

            AssertSame(C, triangle3DObject[0]);
            AssertSame(A, triangle3DObject[2]);
            Assert.False(triangle3DObject.GetNormal().SameHalf(before));
        }

        // ---- Caller 1: Query.Volume(Shell) -----------------------------------------------------------------

        public static IEnumerable<object[]> VolumeCases()
        {
            foreach (object[] pose in Poses())
            {
                foreach (int flipMask in new int[] { 0, 0b111111111, 0b000010101, 0b101010010, 0b000000001 })
                {
                    yield return new object[] { pose[0], flipMask };
                }
            }
        }

        [Theory]
        [MemberData(nameof(VolumeCases))]
        public void Volume_ConcaveLPrismWithMixedFaceWinding_EqualsAnalyticVolume(string pose, int flipMask)
        {
            // L footprint 2x2 minus 1x1 corner = 3, height 2 -> 6
            Shell shell = CreateLPrism(2.0, GetPose(pose), flipMask);

            Assert.Equal(6.0, shell.Volume(), 5);
        }

        [Theory]
        [MemberData(nameof(VolumeCases))]
        public void Volume_BoxWithMixedFaceWinding_EqualsAnalyticVolume(string pose, int flipMask)
        {
            Shell shell = CreateBox(2, 3, 4, GetPose(pose), flipMask);

            Assert.Equal(24.0, shell.Volume(), 5);
        }

        [Theory]
        [MemberData(nameof(VolumeCases))]
        public void Volume_TriangularPrismWithMixedFaceWinding_EqualsAnalyticVolume(string pose, int flipMask)
        {
            // right triangle legs 3 and 4 (area 6), height 5 -> 30
            List<Point3D> footprint = new List<Point3D>() { new Point3D(0, 0, 0), new Point3D(3, 0, 0), new Point3D(0, 4, 0) };
            Shell shell = CreatePrism(footprint, 5.0, GetPose(pose), flipMask);

            Assert.Equal(30.0, shell.Volume(), 5);
        }

        // ---- Caller 2: Create.Mesh3D(Shell) ----------------------------------------------------------------

        [Theory]
        [MemberData(nameof(VolumeCases))]
        public void Mesh3D_Shell_EveryTriangleFollowsParentFaceNormal(string pose, int flipMask)
        {
            Shell shell = CreateLPrism(2.0, GetPose(pose), flipMask);
            List<Face3D> face3Ds = shell.Face3Ds;

            Mesh3D mesh3D = Geometry.Spatial.Create.Mesh3D(shell);

            List<Triangle3D> triangle3Ds = mesh3D.GetTriangles();
            Assert.NotEmpty(triangle3Ds);
            foreach (Triangle3D triangle3D in triangle3Ds)
            {
                Point3D centroid = triangle3D.GetCentroid();
                List<Face3D> parents = face3Ds.FindAll(x => x.Inside(centroid, 1e-6));
                Assert.Single(parents);

                Vector3D triangleNormal = triangle3D.GetNormal().GetNormalized();
                Vector3D faceNormal = parents[0].GetPlane().Normal.GetNormalized();
                double dot = (triangleNormal.X * faceNormal.X) + (triangleNormal.Y * faceNormal.Y) + (triangleNormal.Z * faceNormal.Z);
                Assert.True(dot > 0.99, $"{pose} mask {flipMask}: triangle normal disagrees with face normal (dot {dot})");
            }
        }

        [Theory]
        [MemberData(nameof(Poses))]
        public void Mesh3D_OutwardShell_HasPositiveClosedSignedVolume(string pose)
        {
            Shell shell = CreateLPrism(2.0, GetPose(pose), 0);

            Mesh3D mesh3D = Geometry.Spatial.Create.Mesh3D(shell);

            double signedVolume = mesh3D.GetTriangles().Sum(x => x.SignedVolume());
            Assert.Equal(6.0, signedVolume, 5);
        }

        [Theory]
        [MemberData(nameof(Poses))]
        public void Mesh3D_InwardShell_HasNegativeClosedSignedVolume(string pose)
        {
            Shell shell = CreateLPrism(2.0, GetPose(pose), 0b111111111);

            Mesh3D mesh3D = Geometry.Spatial.Create.Mesh3D(shell);

            double signedVolume = mesh3D.GetTriangles().Sum(x => x.SignedVolume());
            Assert.Equal(-6.0, signedVolume, 5);
        }

        // ---- helpers ---------------------------------------------------------------------------------------

        private static void AssertSame(Point3D expected, Point3D actual)
        {
            Assert.NotNull(actual);
            Assert.True(expected.Distance(actual) < Tol, $"expected {expected.X},{expected.Y},{expected.Z} but was {actual.X},{actual.Y},{actual.Z}");
        }

        private static Shell CreateLPrism(double height, Transform3D transform3D, int flipMask)
        {
            List<Point3D> footprint = new List<Point3D>()
            {
                new Point3D(0, 0, 0), new Point3D(2, 0, 0), new Point3D(2, 1, 0),
                new Point3D(1, 1, 0), new Point3D(1, 2, 0), new Point3D(0, 2, 0)
            };

            return CreatePrism(footprint, height, transform3D, flipMask);
        }

        private static Shell CreateBox(double width, double depth, double height, Transform3D transform3D, int flipMask)
        {
            List<Point3D> footprint = new List<Point3D>()
            {
                new Point3D(0, 0, 0), new Point3D(width, 0, 0), new Point3D(width, depth, 0), new Point3D(0, depth, 0)
            };

            return CreatePrism(footprint, height, transform3D, flipMask);
        }

        /// <summary>
        /// Outward-wound prism over a counter-clockwise (seen from +Z) footprint; every face whose bit is set in
        /// <paramref name="flipMask"/> has its vertex order reversed afterwards, giving mixed or inward winding.
        /// </summary>
        private static Shell CreatePrism(List<Point3D> footprint, double height, Transform3D transform3D, int flipMask)
        {
            int n = footprint.Count;
            List<List<Point3D>> loops = new List<List<Point3D>>();

            loops.Add(footprint.AsEnumerable().Reverse().ToList());
            loops.Add(footprint.ConvertAll(x => new Point3D(x.X, x.Y, x.Z + height)));
            for (int i = 0; i < n; i++)
            {
                Point3D p = footprint[i];
                Point3D q = footprint[(i + 1) % n];
                loops.Add(new List<Point3D>()
                {
                    new Point3D(p.X, p.Y, p.Z), new Point3D(q.X, q.Y, q.Z),
                    new Point3D(q.X, q.Y, q.Z + height), new Point3D(p.X, p.Y, p.Z + height)
                });
            }

            List<Face3D> face3Ds = new List<Face3D>();
            for (int i = 0; i < loops.Count; i++)
            {
                List<Point3D> loop = loops[i].ConvertAll(x => (Point3D)x.GetTransformed(transform3D));
                if ((flipMask & (1 << i)) != 0)
                {
                    loop.Reverse();
                }

                face3Ds.Add(new Face3D(new Polygon3D(loop)));
            }

            return new Shell(face3Ds);
        }
    }
}
