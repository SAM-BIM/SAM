// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The pattern-search coordinate rounding model (documentation/GenOpt-3.1.1-Behaviour.md §2.1) against the
    /// Java-observed fixture Golden/GenOpt/float-rounding-java8.tsv, and the exact decimal-to-double conversion.
    /// </summary>
    public class Java8FloatTextTests
    {
        private sealed class FixtureRow
        {
            public float Value;
            public double Java;
            public string Note = string.Empty;
        }

        private static List<FixtureRow> Fixture()
        {
            List<FixtureRow> rows = new List<FixtureRow>();
            foreach (string line in File.ReadLines(Path.Combine(GenOptGoldenTrace.Directory_GenOpt, "float-rounding-java8.tsv")))
            {
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("floatBits", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] cells = line.Split('\t');
                rows.Add(new FixtureRow
                {
                    Value = BitConverter.Int32BitsToSingle(int.Parse(cells[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
                    Java = GenOptGoldenTrace.ParseJavaDouble(cells[1]),
                    Note = cells[2],
                });
            }

            return rows;
        }

        [Fact]
        public void Model_MatchesJava_OnEveryModelledFixtureValue()
        {
            List<FixtureRow> rows = Fixture().Where(r => r.Note != "unmodelled").ToList();
            Assert.True(rows.Count > 3400, "fixture rows: " + rows.Count);

            List<string> mismatches = rows
                .Where(r => !GenOptGoldenTrace.SameBits(Java8FloatText.ParseOfToString(r.Value), r.Java))
                .Select(r => r.Value.ToString("R", CultureInfo.InvariantCulture) + ": java " + r.Java.ToString("R", CultureInfo.InvariantCulture) + ", model " + Java8FloatText.ParseOfToString(r.Value).ToString("R", CultureInfo.InvariantCulture))
                .ToList();
            Assert.True(mismatches.Count == 0, string.Join("\n", mismatches.Take(20)));
        }

        /// <summary>The kernel's rounding entry point is the model applied to (float)x.</summary>
        [Fact]
        public void Round_IsTheModelOfTheFloatCast()
        {
            foreach (FixtureRow row in Fixture())
            {
                double x = row.Value;
                Assert.True(GenOptGoldenTrace.SameBits(Java8FloatText.ParseOfToString(row.Value), Java8FloatText.Round(x)));
                if (row.Note != "unmodelled")
                {
                    Assert.True(GenOptGoldenTrace.SameBits(row.Java, Java8FloatText.Round(x)), row.Value.ToString("R", CultureInfo.InvariantCulture));
                }
            }
        }

        /// <summary>
        /// Decision D1: the 42 probed values in 2^83 ≤ |f| &lt; 2^86 that Java prints differently are outside the parity
        /// domain. This pins them, so a change on either side is noticed.
        /// </summary>
        [Fact]
        public void Model_KnownDifference_IsExactlyTheFortyTwoValuesOfTheD1Band()
        {
            List<FixtureRow> unmodelled = Fixture().Where(r => r.Note == "unmodelled").ToList();

            Assert.Equal(42, unmodelled.Count);
            foreach (FixtureRow row in unmodelled)
            {
                double magnitude = System.Math.Abs((double)row.Value);
                Assert.InRange(magnitude, System.Math.Pow(2, 83), System.Math.BitDecrement(System.Math.Pow(2, 86)));
                Assert.False(GenOptGoldenTrace.SameBits(Java8FloatText.ParseOfToString(row.Value), row.Java), "now matches: " + row.Value.ToString("R", CultureInfo.InvariantCulture));
            }
        }

        [Theory]
        [InlineData(2147483648f, "214748365", 1)]          // 2^31 -> 2.14748365E9 (integer path, one digit dropped)
        [InlineData(7.4505806E-9f, "74505806", -16)]       // 2^-27: single-bit margin -> 8 digits, not 7.450581E-9
        [InlineData(1.4E-45f, "14", -46)]                  // smallest subnormal: two-digit scientific rule
        [InlineData(1f, "1", 0)]
        [InlineData(0.3f, "3", -1)]
        [InlineData(16777216f, "16777216", 0)]             // 2^24, exact integer digits
        public void Decimal_ReproducesDocumentedExamples(float value, string digits, int exponent10)
        {
            Tuple<BigInteger, int> result = Java8FloatText.Decimal(value);

            Assert.Equal(digits, result.Item1.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(exponent10, result.Item2);
        }

        [Fact]
        public void ParseOfToString_HandlesSignZeroAndNonFinite()
        {
            Assert.True(GenOptGoldenTrace.SameBits(0.0, Java8FloatText.ParseOfToString(0f)));
            Assert.True(GenOptGoldenTrace.SameBits(-0.0, Java8FloatText.ParseOfToString(-0f)));
            Assert.True(double.IsNaN(Java8FloatText.ParseOfToString(float.NaN)));
            Assert.Equal(double.PositiveInfinity, Java8FloatText.ParseOfToString(float.PositiveInfinity));
            Assert.Equal(double.PositiveInfinity, Java8FloatText.Round(1e39));
            Assert.Equal(-Java8FloatText.ParseOfToString(0.1f), Java8FloatText.ParseOfToString(-0.1f));
            Assert.Equal(0.30000001192092896, Java8FloatText.Round(0.2 + 0.1), 0);
            Assert.Equal(Java8FloatText.Round(0.2 + 0.1), Java8FloatText.Round(0.4 - 0.1));
        }

        /// <summary>
        /// Spec §2.1: shortest-decimal agrees with Java between 7.45e-9 and 3.37e7. A deterministic sample checks that
        /// the model agrees with shortest-decimal there too.
        /// </summary>
        [Fact]
        public void Model_AgreesWithShortestDecimal_InTheMidRange()
        {
            Random random = new Random(20261007);
            int checkedCount = 0;
            while (checkedCount < 20000)
            {
                float value = BitConverter.Int32BitsToSingle(random.Next(int.MinValue, int.MaxValue));
                float magnitude = System.Math.Abs(value);
                if (!(magnitude > 7.46e-9f && magnitude < 3.36e7f))
                {
                    continue;
                }

                double shortest = double.Parse(value.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
                Assert.True(GenOptGoldenTrace.SameBits(shortest, Java8FloatText.ParseOfToString(value)), value.ToString("R", CultureInfo.InvariantCulture));
                checkedCount++;
            }
        }

        /// <summary>The kernel's own decimal-to-double conversion agrees with the correctly rounded .NET 8 parser.</summary>
        [Fact]
        public void ToDouble_IsCorrectlyRounded()
        {
            List<Tuple<string, int>> cases = new List<Tuple<string, int>>
            {
                Tuple.Create("9007199254740993", 0),           // 2^53 + 1: tie, rounds to even
                Tuple.Create("9007199254740995", 0),           // tie, rounds up to even
                Tuple.Create("49406564584124654", -340),       // smallest subnormal
                Tuple.Create("24703282292062328", -340),       // just above half the smallest subnormal
                Tuple.Create("22250738585072011", -324),       // around the smallest normal
                Tuple.Create("22250738585072014", -324),
                Tuple.Create("17976931348623157", 292),        // largest finite
                Tuple.Create("17976931348623159", 292),        // overflows to infinity
                Tuple.Create("1", 0),
                Tuple.Create("1", 23),
                Tuple.Create("3", -1),
            };

            Random random = new Random(7);
            for (int i = 0; i < 20000; i++)
            {
                int length = random.Next(1, 26);
                char[] digits = new char[length];
                for (int j = 0; j < length; j++)
                {
                    digits[j] = (char)('0' + random.Next(j == 0 ? 1 : 0, 10));
                }

                cases.Add(Tuple.Create(new string(digits), random.Next(-345, 300)));
            }

            foreach (Tuple<string, int> c in cases)
            {
                double expected = double.Parse(c.Item1 + "E" + c.Item2.ToString(CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
                double actual = Java8FloatText.ToDouble(BigInteger.Parse(c.Item1, CultureInfo.InvariantCulture), c.Item2);
                Assert.True(GenOptGoldenTrace.SameBits(expected, actual), c.Item1 + "E" + c.Item2 + ": expected " + expected.ToString("R", CultureInfo.InvariantCulture) + ", got " + actual.ToString("R", CultureInfo.InvariantCulture));
            }
        }
    }
}
