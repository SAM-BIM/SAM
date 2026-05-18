// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ModelTests
    {
        private static readonly Guid Fixed = Guid.Parse("99999999-aaaa-bbbb-cccc-dddddddddddd");

        [Fact]
        public void RoundTrip_Address()
        {
            Address expected = new Address(Fixed, "HQ", "1 Main St", "London", "SW1A 1AA", CountryCode.GB);

            Address result = RoundTrip.Once(expected);

            Assert.Equal("HQ", result.Name);
            Assert.Equal(Fixed, result.Guid);
            Assert.Equal("1 Main St", result.Street);
            Assert.Equal("London", result.City);
            Assert.Equal("SW1A 1AA", result.PostalCode);
            Assert.Equal(CountryCode.GB, result.CountryCode);
        }

        [Fact]
        public void RoundTrip_Address_OmitsUnsetFields()
        {
            // Null strings and Undefined country must not show up on the wire.
            Address address = new Address(Fixed, "Bare", null, null, null, CountryCode.Undefined);

            string json = SAM.Core.Convert.ToString(address);

            Assert.DoesNotContain("\"Street\":", json);
            Assert.DoesNotContain("\"City\":", json);
            Assert.DoesNotContain("\"PostalCode\":", json);
            Assert.DoesNotContain("\"CountryCode\":", json);
        }

        [Fact]
        public void RoundTrip_Location()
        {
            Location expected = new Location(Fixed, "London", longitude: -0.1276, latitude: 51.5074, elevation: 35.0);

            Location result = RoundTrip.Once(expected);

            Assert.Equal("London", result.Name);
            Assert.Equal(Fixed, result.Guid);
            Assert.Equal(-0.1276, result.Longitude);
            Assert.Equal(51.5074, result.Latitude);
            Assert.Equal(35.0, result.Elevation);
        }

        [Fact]
        public void RoundTrip_SAMModel_PreservesIdentityAndParameterSets()
        {
            // SAMModel has no fields of its own — only base SAMObject identity
            // plus parameter sets. The migration removed the vestigial JObject
            // override; this test asserts the inherited bridge still works.
            ParameterSet parameterSet = new ParameterSet(Fixed, "ModelMeta");
            parameterSet.Add("Description", "test fixture");
            parameterSet.Add("Revision", 3);

            SAMModel expected = new SAMModel(Fixed, "Site");
            expected.Add(parameterSet);

            SAMModel result = RoundTrip.Once(expected);

            Assert.Equal("Site", result.Name);
            Assert.Equal(Fixed, result.Guid);
            ParameterSet roundTripSet = result.GetParameterSet("ModelMeta");
            Assert.NotNull(roundTripSet);
            Assert.Equal("test fixture", roundTripSet.ToString("Description"));
            Assert.Equal(3, roundTripSet.ToInt("Revision"));
        }
    }
}
