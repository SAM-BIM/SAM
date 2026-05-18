// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Drawing;
using SAM.Core;
using SAM.Tests.Helpers;
using Xunit;

namespace SAM.Tests
{
    public class ParameterSetTests
    {
        // Query.Guid(JObject) synthesizes a fresh Guid when the stored Guid is
        // empty, so every test must build the ParameterSet with an explicit
        // non-empty Guid to keep round-trip JSON deterministic.
        private static readonly Guid Fixed = Guid.Parse("11111111-2222-3333-4444-555555555555");

        [Fact]
        public void RoundTrip_Empty()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Empty");
            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.Equal("Empty", result.Name);
            Assert.Equal(Fixed, result.Guid);
        }

        [Fact]
        public void RoundTrip_String()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Strings");
            parameterSet.Add("Plain", "hello");
            parameterSet.Add("WithUnicode", "façade — naïve");
            parameterSet.Add("Empty", string.Empty);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.Equal("hello", result.ToString("Plain"));
            Assert.Equal("façade — naïve", result.ToString("WithUnicode"));
            Assert.Equal(string.Empty, result.ToString("Empty"));
        }

        [Fact]
        public void RoundTrip_Integer()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Integers");
            parameterSet.Add("Zero", 0);
            parameterSet.Add("Positive", 42);
            parameterSet.Add("Negative", -17);
            parameterSet.Add("MaxValue", int.MaxValue);
            parameterSet.Add("MinValue", int.MinValue);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.Equal(0, result.ToInt("Zero"));
            Assert.Equal(42, result.ToInt("Positive"));
            Assert.Equal(-17, result.ToInt("Negative"));
            Assert.Equal(int.MaxValue, result.ToInt("MaxValue"));
            Assert.Equal(int.MinValue, result.ToInt("MinValue"));
        }

        [Fact]
        public void RoundTrip_DoubleFractional()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Doubles");
            parameterSet.Add("Half", 0.5);
            parameterSet.Add("Pi", 3.14159265358979);
            parameterSet.Add("NegativeFraction", -0.0001);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.Equal(0.5, result.ToDouble("Half"));
            Assert.Equal(3.14159265358979, result.ToDouble("Pi"));
            Assert.Equal(-0.0001, result.ToDouble("NegativeFraction"));
        }

        // Guards against the shim's JTokenType heuristic that may classify
        // whole-numbered doubles as Integer (System.Text.Json TryGetValue<int>
        // is permissive on Number nodes), which would corrupt the Type switch
        // in ParameterSet.FromJObject and round-trip a double as int.
        [Fact]
        public void RoundTrip_DoubleWholeNumber_RemainsDouble()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "DoublesWhole");
            parameterSet.Add("Five", 5.0);
            parameterSet.Add("Hundred", 100.0);
            parameterSet.Add("Zero", 0.0);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.IsType<double>(result.ToObject("Five"));
            Assert.IsType<double>(result.ToObject("Hundred"));
            Assert.IsType<double>(result.ToObject("Zero"));

            Assert.Equal(5.0, result.ToDouble("Five"));
            Assert.Equal(100.0, result.ToDouble("Hundred"));
            Assert.Equal(0.0, result.ToDouble("Zero"));
        }

        [Fact]
        public void RoundTrip_Boolean()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Bools");
            parameterSet.Add("Yes", true);
            parameterSet.Add("No", false);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.True(result.ToBool("Yes"));
            Assert.False(result.ToBool("No"));
        }

        [Fact]
        public void RoundTrip_DateTime()
        {
            DateTime expected = new DateTime(2025, 3, 17, 10, 30, 45, DateTimeKind.Utc);

            ParameterSet parameterSet = new ParameterSet(Fixed, "Dates");
            parameterSet.Add("When", expected);

            ParameterSet result = RoundTrip.Once(parameterSet);

            DateTime actual = result.ToDateTime("When");
            Assert.Equal(expected.ToUniversalTime(), actual.ToUniversalTime());
        }

        [Fact]
        public void RoundTrip_Guid()
        {
            Guid expected = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

            ParameterSet parameterSet = new ParameterSet(Fixed, "Guids");
            parameterSet.Add("Id", expected);

            ParameterSet result = RoundTrip.Once(parameterSet);

            object? value = result.ToObject("Id");
            Guid actual = value is Guid guid ? guid : Guid.Parse((string)value!);
            Assert.Equal(expected, actual);
        }

        // Names that look like dates must not be reclassified as JTokenType.Date.
        // The shim's Type getter parses every string with DateTime.TryParse — if
        // that bleeds into ParameterSet, a string parameter "2025-01-01" would
        // round-trip as a DateTime.
        [Fact]
        public void RoundTrip_StringThatLooksLikeDate_StaysString()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "DateLike");
            parameterSet.Add("Label", "2025-01-01");
            parameterSet.Add("VersionTag", "2024-12-31T00:00:00");

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.IsType<string>(result.ToObject("Label"));
            Assert.IsType<string>(result.ToObject("VersionTag"));

            Assert.Equal("2025-01-01", result.ToString("Label"));
            Assert.Equal("2024-12-31T00:00:00", result.ToString("VersionTag"));
        }

        [Fact]
        public void RoundTrip_Color()
        {
            Color expected = Color.FromArgb(255, 128, 64, 32);

            ParameterSet parameterSet = new ParameterSet(Fixed, "Colors");
            parameterSet.Add("Accent", expected);

            ParameterSet result = RoundTrip.Once(parameterSet);

            Color actual = result.ToColor("Accent");
            Assert.Equal(expected.ToArgb(), actual.ToArgb());
        }

        [Fact]
        public void RoundTrip_MultipleHeterogeneousParameters()
        {
            ParameterSet parameterSet = new ParameterSet(Fixed, "Mixed");
            parameterSet.Add("S", "value");
            parameterSet.Add("I", 7);
            parameterSet.Add("D", 1.25);
            parameterSet.Add("DWhole", 9.0);
            parameterSet.Add("B", true);
            parameterSet.Add("G", Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

            ParameterSet result = RoundTrip.Once(parameterSet);

            Assert.Equal("value", result.ToString("S"));
            Assert.Equal(7, result.ToInt("I"));
            Assert.Equal(1.25, result.ToDouble("D"));
            Assert.Equal(9.0, result.ToDouble("DWhole"));
            Assert.True(result.ToBool("B"));
            Assert.Equal("Mixed", result.Name);
        }
    }
}
