// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel.Types;
using NUnit.Framework;
using Rhino.Geometry;
using SAM.Analytical.Grasshopper;

namespace SAM.Core.Grasshopper.Tests
{
    /// <summary>
    /// A Rhino Text Dot supplies an analytical Space directly: the Text Dot text is the Space name and its
    /// position is the location point. Covers the direct Text Dot and the Rhino 8 "Query Model Objects >
    /// Model Object" workflow.
    /// </summary>
    [TestFixture]
    public class SpaceTextDotTests
    {
        [Test]
        public void GooSpace_CastFromTextDot_UsesTextAsNameAndPointAsLocation()
        {
            TextDot textDot = new TextDot("Living Room", new Point3d(5, 3, 1));

            GooSpace goo = new GooSpace();
            Assert.True(goo.CastFrom(textDot));

            Assert.NotNull(goo.Value);
            Assert.Equal("Living Room", goo.Value.Name);
            NUnit.Framework.Assert.That(goo.Value.Location.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
            NUnit.Framework.Assert.That(goo.Value.Location.Y, NUnit.Framework.Is.EqualTo(3).Within(1e-9));
            NUnit.Framework.Assert.That(goo.Value.Location.Z, NUnit.Framework.Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void GooSpace_CastFromGrasshopperGoo_UsesTextAsNameAndPointAsLocation()
        {
            GH_TextDot gH_TextDot = new GH_TextDot(new TextDot("Living Room", new Point3d(5, 3, 1)));

            GooSpace goo = new GooSpace();
            Assert.True(goo.CastFrom(gH_TextDot));

            Assert.NotNull(goo.Value);
            Assert.Equal("Living Room", goo.Value.Name);
            NUnit.Framework.Assert.That(goo.Value.Location.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
        }

        [Test]
        public void GooSpace_CastFromModelObject_UsesTextAsNameAndPointAsLocation()
        {
            global::Rhino.RhinoDoc doc = global::Rhino.RhinoDoc.CreateHeadless(null);
            Assert.NotNull(doc);

            try
            {
                System.Guid id = doc.Objects.AddTextDot(new TextDot("Living Room", new Point3d(5, 3, 1)));
                global::Rhino.DocObjects.RhinoObject rhinoObject = doc.Objects.FindId(id);
                Assert.NotNull(rhinoObject);

                global::Grasshopper.Rhinoceros.Model.ModelObject modelObject = new global::Grasshopper.Rhinoceros.Model.ModelObject(rhinoObject);

                GooSpace goo = new GooSpace();
                Assert.True(goo.CastFrom(modelObject));

                Assert.NotNull(goo.Value);
                Assert.Equal("Living Room", goo.Value.Name);
                NUnit.Framework.Assert.That(goo.Value.Location.X, NUnit.Framework.Is.EqualTo(5).Within(1e-9));
                NUnit.Framework.Assert.That(goo.Value.Location.Y, NUnit.Framework.Is.EqualTo(3).Within(1e-9));
                NUnit.Framework.Assert.That(goo.Value.Location.Z, NUnit.Framework.Is.EqualTo(1).Within(1e-9));

                // The original failure was "Data conversion failed from Model Text Dot to ISAMGeometry".
                SAM.Geometry.Grasshopper.GooSAMGeometry gooSAMGeometry = new SAM.Geometry.Grasshopper.GooSAMGeometry();
                Assert.True(gooSAMGeometry.CastFrom(modelObject));
                NUnit.Framework.Assert.That(gooSAMGeometry.Value, NUnit.Framework.Is.InstanceOf<SAM.Geometry.Spatial.Point3D>());
            }
            finally
            {
                doc.Dispose();
            }
        }

        [Test]
        public void GooSpace_CastFromEmptyTextDot_UsesFallbackName()
        {
            GooSpace goo = new GooSpace();
            Assert.True(goo.CastFrom(new TextDot(string.Empty, new Point3d(5, 3, 1))));

            Assert.NotNull(goo.Value);
            Assert.Equal("Cell", goo.Value.Name);
            NUnit.Framework.Assert.That(goo.Value.Location.Y, NUnit.Framework.Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void GooSpace_CastFromNullAndUnsupported_DoesNotThrow()
        {
            GooSpace goo = new GooSpace();
            Assert.False(goo.CastFrom(null));
            Assert.False(goo.CastFrom("not a text dot"));
        }

        [Test]
        public void GooSpace_CastFromExistingSamSpace_IsUnchanged()
        {
            SAM.Analytical.Space space = new SAM.Analytical.Space("Space_Default", new SAM.Geometry.Spatial.Point3D(0, 0, 0.75));

            GooSpace goo = new GooSpace();
            Assert.True(goo.CastFrom(space));
            Assert.Same(space, goo.Value);
        }
    }
}
