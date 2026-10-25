// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Grasshopper.Tests
{
    /// <summary>
    /// <b><c>SAM.Geometry.Rhino.Query.UnitScale()</c> without an active Rhino document.</b>
    /// <para>
    /// Rhino.Inside, Compute/Hops and tests have no <c>RhinoDoc.ActiveDoc</c>. The converters multiply their tolerances by
    /// the unit scale, so a NaN scale sent every planar Brep face down the mesh path and returned distorted, open shells.
    /// SAM geometry is metres, so the scale is 1 there.
    /// </para>
    /// <para>
    /// Needs the headless Rhino 8 runtime (<see cref="GrasshopperTestSetup"/>); run locally, not in CI.
    /// </para>
    /// </summary>
    [TestFixture]
    public class UnitScaleNoActiveDocTests
    {
        // Rhino types are used in the SetUp and TearDown bodies only: the field is typed object.
        private object activeBefore;

        [SetUp]
        public void ClearTheActiveModel()
        {
            activeBefore = global::Rhino.RhinoDoc.ActiveDoc;
            global::Rhino.RhinoDoc.ActiveDoc = null;
        }

        [TearDown]
        public void RestoreTheActiveModel()
        {
            global::Rhino.RhinoDoc.ActiveDoc = (global::Rhino.RhinoDoc)activeBefore;
        }

        [Test]
        public void UnitScale_NoActiveDoc_ReturnsOne()
        {
            // Act
            double unitScale = Geometry.Rhino.Query.UnitScale();

            // Assert
            Assert.Equal(1.0, unitScale);
        }

        [Test]
        public void ToSAM_PlanarBoxBrepNoActiveDoc_ReturnsClosedSixFaceShellWithExactAreas()
        {
            // Arrange: a 0.35 x 2.084 x 3.35 m wall pier, the size that came back distorted before the fix. Its faces are
            // extrusion (SumSurface) faces, as in models rebuilt from other tools: IsPlanar(NaN) is false for them, while a
            // PlaneSurface is planar whatever the tolerance.
            double x = 0.35, y = 2.084, z = 3.35;
            global::Rhino.Geometry.Brep brep = SumSurfaceBox(x, y, z);

            // Act
            List<ISAMGeometry3D> geometries = Geometry.Rhino.Convert.ToSAM(brep, true);

            // Assert
            Shell shell = Assert.Single(geometries.OfType<Shell>());
            Assert.True(shell.IsClosed());

            List<Face3D> face3Ds = shell.Face3Ds;
            Assert.Equal(6, face3Ds.Count);

            List<double> expected = new List<double> { x * y, x * y, x * z, x * z, y * z, y * z };
            List<double> actual = face3Ds.Select(face3D => face3D.GetArea()).OrderBy(area => area).ToList();
            expected.Sort();
            for (int i = 0; i < expected.Count; i++)
            {
                NUnit.Framework.Assert.That(actual[i], Is.EqualTo(expected[i]).Within(1e-9));
            }
        }

        private static global::Rhino.Geometry.Brep SumSurfaceBox(double x, double y, double z)
        {
            global::Rhino.Geometry.Point3d[] p = new global::Rhino.Geometry.BoundingBox(0, 0, 0.05, x, y, 0.05 + z).GetCorners();
            int[][] quads = { new[] { 0, 1, 2, 3 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 1, 2, 6, 5 }, new[] { 2, 3, 7, 6 }, new[] { 3, 0, 4, 7 } };

            List<global::Rhino.Geometry.Brep> faces = quads
                .Select(q => global::Rhino.Geometry.Brep.CreateFromSurface(global::Rhino.Geometry.SumSurface.Create(
                    new global::Rhino.Geometry.LineCurve(p[q[0]], p[q[1]]), new global::Rhino.Geometry.LineCurve(p[q[0]], p[q[3]]))))
                .ToList();

            return global::Rhino.Geometry.Brep.JoinBreps(faces, 1e-6).Single();
        }
    }
}
