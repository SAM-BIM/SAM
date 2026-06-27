// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using SAM.Analytical;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ProfileTests
    {
        // Profile's ToJsonObject deliberately routes the Values array through
        // an explicit "1.0"-style numeric writer (see the comment block in
        // Profile.cs:1330) so whole-number double entries round-trip as Float
        // rather than getting silently classified as Integer. This was the
        // single most fragile piece of wire-format compatibility in the
        // migration. The fixtures already exercise it indirectly; this test
        // pins it explicitly so future refactors can't regress the shape.

        [Fact]
        public void RoundTrip_Profile_WholeNumberValuesStayFloat()
        {
            // Values contain both fractional and whole-number doubles. After
            // round-trip the in-memory representation must still be double
            // for every entry (no Integer/Double type drift), and reading the
            // same value back yields the original numeric value.
            List<double> values = new List<double> { 1.0, 0.5, 2.0, 3.25, 5.0 };
            Profile profile = new Profile("WireFormatGuard", "Occupancy", values);

            Profile result = RoundTrip.Once(profile);

            Assert.Equal("WireFormatGuard", result.Name);
            Assert.Equal("Occupancy", result.Category);

            List<double> roundTripped = new List<double>();
            for (int i = 0; i < values.Count; i++)
            {
                roundTripped.Add(result[i]);
            }

            Assert.Equal(values, roundTripped);
        }

        [Fact]
        public void RoundTrip_Profile_WireJsonRetainsDecimalSuffix()
        {
            // Render a Profile with whole-number values to JSON, parse the
            // text, and assert the Values entries are emitted with their
            // decimal point intact. STJ's default JsonValue<double> writer
            // strips trailing zeros (would emit "1" not "1.0"), so the
            // migration kept a custom number formatter. Guard that here.
            List<double> values = new List<double> { 1.0, 2.0, 3.0 };
            Profile profile = new Profile("DecimalSuffix", "Occupancy", values);

            string json = SAM.Core.Convert.ToString(profile);

            Assert.NotNull(json);
            // The Values block should contain entries like "1.0" rather than
            // bare "1". The fixture-style emission writes each entry as a
            // [startIndex, endIndex, value] array, where the third element is
            // the double. We just check the substring "1.0" appears somewhere
            // in the values block.
            Assert.Contains("1.0", json);
        }
    }
}
