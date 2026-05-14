// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Json;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using Xunit;

namespace SAM.Tests
{
    public class GeometryPrimitiveTests
    {
        // The SAMGeometry base now bridges JObject -> JsonObject and exposes
        // a protected FromJsonObject/ToJsonObject hook. These tests pin the
        // round-trip wire shape for the four primitives migrated so far
        // (Point2D, Vector2D, Point3D, Vector3D); other geometry types will
        // continue to use the old public-override path until migrated.

        [Fact]
        public void RoundTrip_Point2D()
        {
            Point2D expected = new Point2D(1.5, -2.25);
            JObject jObject = expected.ToJObject();
            Point2D result = new Point2D(jObject);

            Assert.Equal(1.5, result.X);
            Assert.Equal(-2.25, result.Y);
        }

        [Fact]
        public void RoundTrip_Vector2D()
        {
            Vector2D expected = new Vector2D(3.0, 4.0);
            JObject jObject = expected.ToJObject();
            Vector2D result = new Vector2D(jObject);

            Assert.Equal(3.0, result.X);
            Assert.Equal(4.0, result.Y);
        }

        [Fact]
        public void RoundTrip_Point3D()
        {
            Point3D expected = new Point3D(1.0, 2.0, 3.0);
            JObject jObject = expected.ToJObject();
            Point3D result = new Point3D(jObject);

            Assert.Equal(1.0, result.X);
            Assert.Equal(2.0, result.Y);
            Assert.Equal(3.0, result.Z);
        }

        [Fact]
        public void RoundTrip_Vector3D()
        {
            Vector3D expected = new Vector3D(0.5, 0.5, -1.0);
            JObject jObject = expected.ToJObject();
            Vector3D result = new Vector3D(jObject);

            Assert.Equal(0.5, result.X);
            Assert.Equal(0.5, result.Y);
            Assert.Equal(-1.0, result.Z);
        }
    }
}
