// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using Xunit;

namespace SAM.Tests.Helpers
{
    // The helpers deliberately use the parameterless Convert.ToString overload
    // so they compile against both the Newtonsoft baseline (where Formatting
    // lives in Newtonsoft.Json) and the shim branch (where it lives in
    // SAM.Core.Json). Whitespace differences don't matter because
    // JsonEquivalence is whitespace-insensitive.
    public static class RoundTrip
    {
        public static T Once<T>(T input) where T : IJSAMObject
        {
            Assert.NotNull(input);

            string? first = Convert.ToString(input);
            Assert.False(string.IsNullOrWhiteSpace(first), "Initial serialization produced empty JSON.");

            T reconstructed = Create.IJSAMObject<T>(first!);
            Assert.NotNull(reconstructed);

            string? second = Convert.ToString(reconstructed);
            Assert.False(string.IsNullOrWhiteSpace(second), "Round-trip serialization produced empty JSON.");

            AssertEquivalent(first!, second!);
            return reconstructed;
        }

        public static T FromJson<T>(string json) where T : IJSAMObject
        {
            T first = Create.IJSAMObject<T>(json);
            Assert.NotNull(first);

            string? jsonFirst = Convert.ToString(first);
            Assert.False(string.IsNullOrWhiteSpace(jsonFirst));

            T second = Create.IJSAMObject<T>(jsonFirst!);
            Assert.NotNull(second);

            string? jsonSecond = Convert.ToString(second);
            Assert.False(string.IsNullOrWhiteSpace(jsonSecond));

            AssertEquivalent(jsonFirst!, jsonSecond!);
            return second;
        }

        public static void AssertEquivalent(string leftJson, string rightJson)
        {
            if (!JsonEquivalence.AreEquivalent(leftJson, rightJson, out string? difference))
            {
                Assert.Fail($"JSON round-trip mismatch: {difference}\n--- LEFT ---\n{leftJson}\n--- RIGHT ---\n{rightJson}");
            }
        }
    }
}
