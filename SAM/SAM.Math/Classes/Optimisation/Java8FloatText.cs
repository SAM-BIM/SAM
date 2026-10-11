// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Numerics;

namespace SAM.Math
{
    /// <summary>
    /// Behavioural model, in exact rational arithmetic, of the value Java 8 produces for
    /// Double.parseDouble(Float.toString(f)). Pattern-search coordinates are snapped through it before
    /// every evaluation (documentation/GenOpt-3.1.1-Behaviour.md §2.1).
    /// <para>
    /// It differs from "shortest round-trip decimal" in three observed ways:
    /// 1. Integer-valued floats with 1 ≤ |f| &lt; 2^63 are written as exact integer digits. When the binary
    ///    exponent e exceeds 24, floor((e - 25)·log10 2) trailing digits are dropped (for e - 25 ≥ 2), with
    ///    round-half-up on the dropped part.
    /// 2. Other floats use free-format digit generation with a symmetric margin of half the float spacing,
    ///    halved again when the significand has a single set bit.
    /// 3. In scientific form (decimal exponent &lt; -3 or ≥ 8) at least two digits are generated before the
    ///    stopping test applies.
    /// </para>
    /// <para>
    /// Provenance: the structure was informed by the design of JDK 8's FloatingDecimal. This is an independent
    /// implementation, validated only by black-box comparison with Java (240,138 probed floats). No JDK source
    /// was copied. Known difference: 42 probed values with 2^83 ≤ |f| &lt; 2^86 differ in the last printed digit.
    /// The owner accepted that band as outside the parity domain (decision D1).
    /// </para>
    /// <para>
    /// The decimal is converted to double by this class's own exact conversion, so the result does not depend on the
    /// runtime's double.Parse: the model's result is defined by this arithmetic alone, whichever runtime hosts
    /// SAM.Math (a netstandard2.0 library; parsing has not been correctly rounded on every .NET runtime).
    /// </para>
    /// </summary>
    public static class Java8FloatText
    {
        private static readonly BigInteger TwoTo52 = BigInteger.One << 52;
        private static readonly BigInteger TwoTo53 = BigInteger.One << 53;

        /// <summary>Returns Double.parseDouble(Float.toString((float)x)) as predicted by the model.</summary>
        public static double Round(double x)
        {
            return ParseOfToString((float)x);
        }

        /// <summary>Returns Double.parseDouble(Float.toString(f)) as predicted by the model.</summary>
        public static double ParseOfToString(float f)
        {
            if (float.IsNaN(f) || float.IsInfinity(f) || f == 0)
            {
                return f;
            }

            Tuple<BigInteger, int> @decimal = Decimal(System.Math.Abs(f));
            double value = ToDouble(@decimal.Item1, @decimal.Item2);
            return f < 0 ? -value : value;
        }

        /// <summary>
        /// The model's decimal for a positive finite float: value = Item1 × 10^Item2.
        /// </summary>
        public static Tuple<BigInteger, int> Decimal(float f)
        {
            if (!(f > 0) || float.IsInfinity(f))
            {
                throw new ArgumentOutOfRangeException(nameof(f), "A positive finite float is required.");
            }

            int bits = BitConverter.ToInt32(BitConverter.GetBytes(f), 0);
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

            int highestBit = HighestSetBit(significand);
            int binExp = binaryExponentOfLsb + highestBit;              // 2^binExp ≤ f < 2^(binExp+1)
            int trailingZeros = TrailingZeroCount(significand);
            int nFractBits = highestBit - trailingZeros + 1;           // significant bits

            // Rule 1: integer fast path.
            bool isInteger = binaryExponentOfLsb + trailingZeros >= 0;
            if (isInteger && binExp >= 0 && binExp <= 62)
            {
                BigInteger integer = binaryExponentOfLsb < 0
                    ? new BigInteger(significand) >> -binaryExponentOfLsb
                    : new BigInteger(significand) << binaryExponentOfLsb;

                int insignificant = 0;
                if (binExp > 24)
                {
                    int p2 = binExp - 24 - 1;
                    insignificant = p2 > 1 ? (int)System.Math.Floor(p2 * System.Math.Log10(2)) : 0;
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

                return Tuple.Create(integer, insignificant);
            }

            // Rules 2 and 3: free-format digit generation on exact rationals.
            // value = v / d and margin = margin / d, all positive integers. The margin is half the spacing,
            // 2^(lsb-1), or 2^(lsb-2) for a single significant bit.
            BigInteger v = new BigInteger(significand);
            BigInteger d = BigInteger.One;
            BigInteger margin = BigInteger.One;
            int lsb = binaryExponentOfLsb;
            int marginShift = nFractBits == 1 ? 2 : 1;
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

            d <<= marginShift;

            // Decimal exponent: a first-order estimate of log10 that can be one too high just below a power of
            // ten. The first-digit step then drops a leading zero, or keeps it when the margin reaches 10^k.
            int k = EstimateDecimalExponent(significand, highestBit, binExp);
            if (Compare(v, d, k + 1) >= 0)
            {
                throw new InvalidOperationException("Decimal exponent estimate too low.");
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

            // Scientific form (decimal exponent k < -3 or k ≥ 8) forces another digit.
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
                        roundUp = (digits[digits.Count - 1] & 1) != 0;
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

            return Tuple.Create(number, k - (digits.Count - 1));
        }

        /// <summary>
        /// Correctly rounded (round-half-even) conversion of digits × 10^exponent10 to double, in exact integer
        /// arithmetic.
        /// </summary>
        public static double ToDouble(BigInteger digits, int exponent10)
        {
            if (digits.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(digits), "Digits must not be negative.");
            }

            if (digits.IsZero)
            {
                return 0;
            }

            BigInteger numerator = digits;
            BigInteger denominator = BigInteger.One;
            if (exponent10 >= 0)
            {
                numerator *= BigInteger.Pow(10, exponent10);
            }
            else
            {
                denominator = BigInteger.Pow(10, -exponent10);
            }

            // Find e with 2^52 ≤ numerator / (denominator · 2^e) < 2^53, or e = -1074 for a subnormal.
            int e = System.Math.Max(BitLength(numerator) - BitLength(denominator) - 53, -1074);
            BigInteger remainder;
            BigInteger divisor;
            BigInteger quotient = Divide(numerator, denominator, e, out remainder, out divisor);
            while (quotient >= TwoTo53)
            {
                e++;
                quotient = Divide(numerator, denominator, e, out remainder, out divisor);
            }

            while (quotient < TwoTo52 && e > -1074)
            {
                e--;
                quotient = Divide(numerator, denominator, e, out remainder, out divisor);
            }

            int half = (remainder << 1).CompareTo(divisor);
            if (half > 0 || (half == 0 && !quotient.IsEven))
            {
                quotient += 1;
                if (quotient == TwoTo53)
                {
                    quotient >>= 1;
                    e++;
                }
            }

            if (quotient < TwoTo52)
            {
                // Subnormal (e == -1074), or the smallest normal after rounding up.
                return BitConverter.Int64BitsToDouble((long)quotient);
            }

            int biasedExponent = e + 52 + 1023;
            if (biasedExponent >= 2047)
            {
                return double.PositiveInfinity;
            }

            long bits = ((long)biasedExponent << 52) | (long)(quotient - TwoTo52);
            return BitConverter.Int64BitsToDouble(bits);
        }

        private static BigInteger Divide(BigInteger numerator, BigInteger denominator, int e, out BigInteger remainder, out BigInteger divisor)
        {
            if (e >= 0)
            {
                divisor = denominator << e;
                return BigInteger.DivRem(numerator, divisor, out remainder);
            }

            divisor = denominator;
            return BigInteger.DivRem(numerator << -e, divisor, out remainder);
        }

        /// <summary>Number of bits of a positive integer.</summary>
        private static int BitLength(BigInteger value)
        {
            byte[] bytes = value.ToByteArray();
            int length = bytes.Length;
            byte top = bytes[length - 1];
            if (top == 0)
            {
                length--;
                top = bytes[length - 1];
            }

            int bits = (length - 1) * 8;
            while (top != 0)
            {
                bits++;
                top >>= 1;
            }

            return bits;
        }

        private static int HighestSetBit(long value)
        {
            int result = -1;
            while (value != 0)
            {
                result++;
                value >>= 1;
            }

            return result;
        }

        private static int TrailingZeroCount(long value)
        {
            int result = 0;
            while ((value & 1) == 0)
            {
                result++;
                value >>= 1;
            }

            return result;
        }

        /// <summary>
        /// floor((m - 1.5)·0.289529654 + 0.176091259 + binExp·0.301029995663981), where m ∈ [1, 2) is the
        /// normalised significand: a first-order estimate of log10 of the value.
        /// </summary>
        private static int EstimateDecimalExponent(long significand, int highestBit, int binExp)
        {
            // Normalised significand in [1, 2): exact, at most 24 significant bits.
            double normalised = significand / System.Math.Pow(2, highestBit);
            double estimate = (normalised - 1.5D) * 0.289529654D + 0.176091259 + binExp * 0.301029995663981;
            return (int)System.Math.Floor(estimate);
        }

        /// <summary>Compares v/d with 10^k.</summary>
        private static int Compare(BigInteger v, BigInteger d, int k)
        {
            return k >= 0 ? v.CompareTo(d * BigInteger.Pow(10, k)) : (v * BigInteger.Pow(10, -k)).CompareTo(d);
        }
    }
}
