// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel.Types;
using NUnit.Framework;
using Rhino.Geometry;
using SAM.Geometry.Grasshopper;

namespace SAM.Core.Grasshopper.Tests
{
    /// <summary>
    /// Rhino Text Dot resolution for analytical spaces. A Text Dot (directly, or through the Rhino 8
    /// "Query Model Objects > Model Object" workflow) is a Space seed: its text is the name and its
    /// position the location point.
    /// </summary>
    [TestFixture]
    public class TextDotSpaceTests
    {
        [Test]
        public void TryGetTextDot_FromRhinoTextDot()
        {
            TextDot textDot = new TextDot("Living Room", new Point3d(5, 3, 1));

            Assert.True(SAM.Geometry.Grasshopper.Query.TryGetTextDot(textDot, out TextDot resolved));
            Assert.Same(textDot, resolved);
        }

        [Test]
        public void TryGetTextDot_FromGrasshopperGoo()
        {
            // GH_TextDot is not a Rhino TextDot: it exercises the IGH_Goo.CastTo path that the Rhino 8
            // Model Object uses as well.
            TextDot textDot = new TextDot("Living Room", new Point3d(5, 3, 1));
            GH_TextDot gH_TextDot = new GH_TextDot(textDot);

            Assert.True(SAM.Geometry.Grasshopper.Query.TryGetTextDot(gH_TextDot, out TextDot resolved));
            Assert.NotNull(resolved);
            Assert.Equal("Living Room", resolved.Text);
            NUnit.Framework.Assert.That(resolved.Point.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
            NUnit.Framework.Assert.That(resolved.Point.Y, NUnit.Framework.Is.EqualTo(3).Within(1e-9));
            NUnit.Framework.Assert.That(resolved.Point.Z, NUnit.Framework.Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void TryGetTextDot_NullAndUnsupported()
        {
            Assert.False(SAM.Geometry.Grasshopper.Query.TryGetTextDot(null, out TextDot resolvedNull));
            Assert.Null(resolvedNull);

            Assert.False(SAM.Geometry.Grasshopper.Query.TryGetTextDot("not a text dot", out TextDot resolvedString));
            Assert.Null(resolvedString);
        }

        [Test]
        public void GooSAMGeometry_CastFromTextDot_ProducesPoint()
        {
            TextDot textDot = new TextDot("Living Room", new Point3d(5, 3, 1));

            GooSAMGeometry goo = new GooSAMGeometry();
            Assert.True(goo.CastFrom(textDot));

            NUnit.Framework.Assert.That(goo.Value, NUnit.Framework.Is.InstanceOf<SAM.Geometry.Spatial.Point3D>());

            SAM.Geometry.Spatial.Point3D point3D = (SAM.Geometry.Spatial.Point3D)goo.Value;
            NUnit.Framework.Assert.That(point3D.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
            NUnit.Framework.Assert.That(point3D.Y, NUnit.Framework.Is.EqualTo(3).Within(1e-9));
            NUnit.Framework.Assert.That(point3D.Z, NUnit.Framework.Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void GooSAMGeometry_CastFromGrasshopperGoo_ProducesPoint()
        {
            GH_TextDot gH_TextDot = new GH_TextDot(new TextDot("Living Room", new Point3d(5, 3, 1)));

            GooSAMGeometry goo = new GooSAMGeometry();
            Assert.True(goo.CastFrom(gH_TextDot));

            NUnit.Framework.Assert.That(goo.Value, NUnit.Framework.Is.InstanceOf<SAM.Geometry.Spatial.Point3D>());

            SAM.Geometry.Spatial.Point3D point3D = (SAM.Geometry.Spatial.Point3D)goo.Value;
            NUnit.Framework.Assert.That(point3D.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
        }
    }
}
