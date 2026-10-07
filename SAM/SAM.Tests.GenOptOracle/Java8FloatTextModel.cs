// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Numerics;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Behavioural model (exact rational arithmetic) of the decimal value Java 8 produces for
    /// Double.parseDouble(Float.toString(f)). It differs from "shortest round-trip decimal" in three
    /// observed ways, each confirmed by the E-F probe against the real Java GenOpt:
    ///  1. Integer-valued floats with 1 ≤ |f| &lt; 2^63 are written as exact integer digits; when the
    ///     binary exponent e exceeds 24, floor((e - 25)·log10 2) trailing digits are dropped with
    ///     round-half-up on the dropped part (for e - 25 ≥ 2).
    ///  2. Other floats are written by free-format digit generation with a symmetric margin of half
    ///     the float spacing, halved again when the significand has a single set bit.
    ///  3. When the result is written in scientific form (|f| &lt; 1e-3 or the decimal exponent ≥ 8),
    ///     at least two significant digits are generated before the stopping test applies.
    /// The structure of this model was informed by the design of JDK 8's FloatingDecimal; it is an
    /// independent implementation validated only by black-box comparison (see the PR1 record).
    /// </summary>
    public static class Java8FloatTextModel
    {
        /// <summary>Returns Double.parseDouble(Float.toString(f)) as predicted by the model.</summary>
        public static double ParseOfToString(float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f) || f == 0)
            {
                return f;
            }

            (BigInteger digits, int exponent10) = Decimal(Math.Abs(f));
            double value = DecimalToDouble(digits, exponent10);
            return f < 0 ? -value : value;
        }

        /// <summary>The model's decimal for a positive finite float: value = digits × 10^exponent10.</summary>
        public static (BigInteger Digits, int Exponent10) Decimal(float f)
        {
            int bits = BitConverter.SingleToInt32Bits(f);
            int biased = (bits >> 23) & 0xFF;
            long fraction = bits & 0x7FFFFF;

            long significand;
            int binaryExponentOfLsb;     // value = significand × 2^binaryExponentOfLsb
            if (biased == 0)
            {
                significand = fraction;
                binaryExponentOfLsb = -149;
            }
            else
            {
                significand = fraction | 0x800000;
                binaryExponentOfLsb = biased - 150;
            }

            int highestBit = 63 - BitOperations.LeadingZeroCount((ulong)significand);
            int binExp = binaryExponentOfLsb + highestBit;              // 2^binExp ≤ f < 2^(binExp+1)
            int trailingZeros = BitOperations.TrailingZeroCount(significand);
            int nFractBits = highestBit - trailingZeros + 1;           // significant bits

            // Rule 1: integer fast path.
            bool isInteger = binaryExponentOfLsb + trailingZeros >= 0;
            if (isInteger && binExp >= 0 && binExp <= 62)
            {
                BigInteger integer = new BigInteger(significand) << binaryExponentOfLsb;
                if (binaryExponentOfLsb < 0)
                {
                    integer = new BigInteger(significand) >> -binaryExponentOfLsb;
                }

                int insignificant = 0;
                if (binExp > 24)
                {
                    int p2 = binExp - 24 - 1;
                    insignificant = p2 > 1 ? (int)Math.Floor(p2 * Math.Log10(2)) : 0;
                }

                if (insignificant > 0)
                {
                    BigInteger pow10 = BigInteger.Pow(10, insignificant);
                    BigInteger residue = integer % pow10;
                    integer /= pow10;
                    if (residue >= pow10 / 2)
                    {
                        integer += 1;
                    }
                }

                return (integer, insignificant);
            }

            // Rule 2/3: free-format digit generation on exact rationals.
            // value = V / D, margin = Mg / D, all positive integers.
            BigInteger v = new BigInteger(significand);
            BigInteger d = BigInteger.One;
            BigInteger margin = BigInteger.One;            // half the spacing: 2^(lsb-1) relative to V = significand·2^lsb
            int lsb = binaryExponentOfLsb;
            // Express value = significand·2^lsb, margin = 2^(lsb-1) [or 2^(lsb-2) for a single significant bit].
            int marginShift = nFractBits == 1 ? 2 : 1;
            // Common scale: multiply everything by 2^(marginShift - lsb) when lsb is negative.
            // value·2^(marginShift) = significand·2^(lsb+marginShift); margin·2^(marginShift) = 2^lsb.
            v <<= marginShift;
            if (lsb >= 0)
            {
                v <<= lsb;
                margin <<= lsb;
            }
            else
            {
                d <<= -lsb;
            }

            // Now value = v / (d·2^marginShift)… keep the 2^marginShift in d.
            d <<= marginShift;

            // Decimal exponent: an approximate estimate (a linear approximation of log10 around 1.5),
            // which can be one too high just below a power of ten; the first-digit step below then
            // either drops a leading zero or, when the rounding margin already reaches 10^k, keeps it.
            int k = EstimateDecimalExponent(significand, highestBit, binExp);
            if (Compare(v, d, k + 1) >= 0)
            {
                throw new InvalidOperationException("Decimal exponent estimate too low for " + f.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }

            // b / s = value / 10^k ∈ [1, 10); m / s = margin / 10^k.
            BigInteger b = v;
            BigInteger s = d;
            BigInteger m = margin;
            if (k >= 0)
            {
                s *= BigInteger.Pow(10, k);
            }
            else
            {
                BigInteger scale = BigInteger.Pow(10, -k);
                b *= scale;
                m *= scale;
            }

            BigInteger tens = s * 10;
            List<int> digits = new List<int>();

            int q = (int)(b / s);
            b = 10 * (b % s);
            m *= 10;
            bool low = b < m;
            bool high = b + m > tens;
            if (q == 0 && !high)
            {
                k--;
            }
            else
            {
                digits.Add(q);
            }

            // Java's decExp at this point is k; scientific form when k < -3 or k ≥ 8 forces another digit.
            if (k < -3 || k >= 8)
            {
                low = false;
                high = false;
            }

            while (!low && !high)
            {
                q = (int)(b / s);
                b = 10 * (b % s);
                m *= 10;
                low = b < m;
                high = b + m > tens;
                digits.Add(q);
            }

            BigInteger lowDigitDifference = (b << 1) - tens;
            bool roundUp = false;
            if (high)
            {
                if (low)
                {
                    if (lowDigitDifference.IsZero)
                    {
                        roundUp = (digits[^1] & 1) != 0;
                    }
                    else if (lowDigitDifference.Sign > 0)
                    {
                        roundUp = true;
                    }
                }
                else
                {
                    roundUp = true;
                }
            }

            BigInteger number = BigInteger.Zero;
            foreach (int digit in digits)
            {
                number = number * 10 + digit;
            }

            if (roundUp)
            {
                number += 1;
            }

            int exponent10 = k - (digits.Count - 1);
            return (number, exponent10);
        }

        /// <summary>
        /// floor((m - 1.5)·0.289529654 + 0.176091259 + binExp·0.301029995663981), where m ∈ [1, 2) is the
        /// normalised significand: a first-order estimate of log10 of the value.
        /// </summary>
        private static int EstimateDecimalExponent(long significand, int highestBit, int binExp)
        {
            // Normalised significand as a double in [1, 2): exact, at most 24 significant bits.
            double normalised = significand / Math.Pow(2, highestBit);
            double estimate = (normalised - 1.5D) * 0.289529654D + 0.176091259 + binExp * 0.301029995663981;
            return (int)Math.Floor(estimate);
        }

        /// <summary>Compares v/d with 10^k.</summary>
        private static int Compare(BigInteger v, BigInteger d, int k)
        {
            return k >= 0 ? v.CompareTo(d * BigInteger.Pow(10, k)) : (v * BigInteger.Pow(10, -k)).CompareTo(d);
        }

        /// <summary>Correctly rounded conversion of digits × 10^exponent10 to double (.NET Core 3.0+ parser).</summary>
        private static double DecimalToDouble(BigInteger digits, int exponent10)
        {
            string text = digits.ToString(System.Globalization.CultureInfo.InvariantCulture) + "E" + exponent10.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return double.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
