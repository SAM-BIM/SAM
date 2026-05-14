// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Core.Json;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class NCMDataTests
    {
        // NCMData's enum fields default to non-Undefined values, but ToJObject
        // skips emission when the parsed value is Undefined. Without an explicit
        // reset in FromJObject, a source that omits (or carries an unknown
        // value for) an enum key would re-emit the constructor default on the
        // next round, breaking round-trip stability. Internal Condition library
        // fixtures contain entries like "LightingOccupancyControls": "None"
        // that aren't valid enum names, so we reproduce that shape here.
        [Fact]
        public void RoundTrip_UnknownEnumValue_StaysAbsent()
        {
            string json = @"{
                ""_type"": ""SAM.Analytical.NCMData,SAM.Analytical"",
                ""SystemType"": ""NaturalVentilation"",
                ""LightingOccupancyControls"": ""None"",
                ""LightingPhotoelectricControls"": ""None"",
                ""Country"": ""England"",
                ""LightingPhotoelectricBackSpaceSensor"": false,
                ""LightingPhotoelectricControlsTimeSwitch"": false,
                ""LightingDaylightFactorMethod"": false,
                ""IsMainsGasAvailable"": false,
                ""LightingPhotoelectricParasiticPower"": 0.1,
                ""AirPermeability"": 0.0
            }";

            NCMData ncmData = new NCMData(JObject.Parse(json));

            string first = SAM.Core.Convert.ToString(ncmData);
            NCMData reloaded = new NCMData(JObject.Parse(first));
            string second = SAM.Core.Convert.ToString(reloaded);

            RoundTrip.AssertEquivalent(first, second);
        }

        [Fact]
        public void RoundTrip_MissingEnumKey_StaysAbsent()
        {
            // No LightingOccupancyControls key at all — the constructor default
            // must not leak into the serialized output.
            string json = @"{
                ""_type"": ""SAM.Analytical.NCMData,SAM.Analytical"",
                ""SystemType"": ""NaturalVentilation"",
                ""Country"": ""England""
            }";

            NCMData ncmData = new NCMData(JObject.Parse(json));

            string first = SAM.Core.Convert.ToString(ncmData);
            NCMData reloaded = new NCMData(JObject.Parse(first));
            string second = SAM.Core.Convert.ToString(reloaded);

            RoundTrip.AssertEquivalent(first, second);
            // Use the JSON key shape ("Key":) to avoid matching the prefix
            // inside sibling keys like "LightingPhotoelectricControlsTimeSwitch".
            Assert.DoesNotContain("\"LightingOccupancyControls\":", first);
            Assert.DoesNotContain("\"LightingPhotoelectricControls\":", first);
        }
    }
}
