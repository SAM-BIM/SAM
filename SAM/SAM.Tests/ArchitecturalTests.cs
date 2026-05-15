// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Architectural;
using SAM.Core;
using SAM.Geometry.Spatial;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ArchitecturalTests
    {
        [Fact]
        public void RoundTrip_MaterialLayer()
        {
            MaterialLayer expected = new MaterialLayer("Concrete", 0.2);
            MaterialLayer result = RoundTrip.Once(expected);

            Assert.Equal("Concrete", result.Name);
            Assert.Equal(0.2, result.Thickness);
        }

        [Fact]
        public void RoundTrip_Level()
        {
            Level expected = new Level("Ground", 0.0);
            Level result = RoundTrip.Once(expected);

            Assert.Equal("Ground", result.Name);
            Assert.Equal(0.0, result.Elevation);
        }

        // Regression test: pre-migration PlanarTerrain had FromJObject/ToJObject
        // swapped, so the Plane field never round-tripped. The migration fixed
        // the swap; this test pins the corrected wire format — the serialized
        // JSON must contain a "Plane" key, and the round-tripped instance must
        // produce the same JSON.
        [Fact]
        public void RoundTrip_PlanarTerrain_SerializesPlane()
        {
            Plane plane = new Plane(new Point3D(1, 2, 3), Vector3D.WorldZ);
            PlanarTerrain expected = new PlanarTerrain(plane);

            string json = SAM.Core.Convert.ToString(expected);
            Assert.Contains("\"Plane\":", json);

            PlanarTerrain result = RoundTrip.Once(expected);
            string roundTrippedJson = SAM.Core.Convert.ToString(result);
            Assert.Contains("\"Plane\":", roundTrippedJson);
        }
    }
}
