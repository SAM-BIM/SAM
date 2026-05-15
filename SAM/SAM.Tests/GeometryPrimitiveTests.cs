// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using Xunit;

namespace SAM.Tests
{
    public class GeometryPrimitiveTests
    {
        // The SAMGeometry base now exposes only the BCL JsonObject API
        // (ToJsonObject/FromJsonObject). These tests pin the round-trip wire
        // shape for the four primitives migrated (Point2D, Vector2D, Point3D,
        // Vector3D) without going through the legacy JObject shim.

        [Fact]
        public void RoundTrip_Point2D()
        {
            Point2D expected = new Point2D(1.5, -2.25);
            JsonObject jsonObject = expected.ToJsonObject();
            Point2D result = new Point2D(jsonObject);

            Assert.Equal(1.5, result.X);
            Assert.Equal(-2.25, result.Y);
        }

        [Fact]
        public void RoundTrip_Vector2D()
        {
            Vector2D expected = new Vector2D(3.0, 4.0);
            JsonObject jsonObject = expected.ToJsonObject();
            Vector2D result = new Vector2D(jsonObject);

            Assert.Equal(3.0, result.X);
            Assert.Equal(4.0, result.Y);
        }

        [Fact]
        public void RoundTrip_Point3D()
        {
            Point3D expected = new Point3D(1.0, 2.0, 3.0);
            JsonObject jsonObject = expected.ToJsonObject();
            Point3D result = new Point3D(jsonObject);

            Assert.Equal(1.0, result.X);
            Assert.Equal(2.0, result.Y);
            Assert.Equal(3.0, result.Z);
        }

        [Fact]
        public void RoundTrip_Vector3D()
        {
            Vector3D expected = new Vector3D(0.5, 0.5, -1.0);
            JsonObject jsonObject = expected.ToJsonObject();
            Vector3D result = new Vector3D(jsonObject);

            Assert.Equal(0.5, result.X);
            Assert.Equal(0.5, result.Y);
            Assert.Equal(-1.0, result.Z);
        }
    }
}
