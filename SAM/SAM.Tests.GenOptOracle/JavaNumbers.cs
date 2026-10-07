// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// C# models of the two Java number conversions that decide which doubles GenOpt works with.
    /// Both are hypotheses tested against the real Java GenOpt by the probes in this tool.
    /// </summary>
    public static class JavaNumbers
    {
        /// <summary>
        /// Candidate model of GenOpt's coordinate rounding (ModelGPS.getF → Optimizer.roundCoordinates):
        /// Double.parseDouble(Float.toString((float)x)). Model: the shortest decimal that round-trips
        /// the float (.NET Core 3.0+ "R"), parsed back to double with correct rounding.
        /// </summary>
        public static double RoundToFloatShortestDecimal(double x)
        {
            float f = (float)x;
            if (float.IsNaN(f) || float.IsInfinity(f) || f == 0)
            {
                return f;
            }

            return double.Parse(f.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Model of how GenOpt reads a numeric keyword value from its command file
        /// (genopt.io.Token with java.io.StreamTokenizer): the mantissa is accumulated as
        /// v = v*10 + digit and divided by 10^(digits after the point) built by repeated
        /// multiplication; an "E&lt;exp&gt;" suffix is applied as num *= Math.pow(10, exp).
        /// Exact for at most 15 significant digits with at most 22 decimal places and no exponent.
        /// </summary>
        public static double ParseLikeStreamTokenizer(string text)
        {
            int i = 0;
            bool negative = false;
            if (i < text.Length && text[i] == '-')
            {
                negative = true;
                i++;
            }

            double v = 0;
            int decexp = 0;
            int seenDot = 0;
            for (; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '.' && seenDot == 0)
                {
                    seenDot = 1;
                }
                else if (c >= '0' && c <= '9')
                {
                    v = v * 10 + (c - '0');
                    decexp += seenDot;
                }
                else
                {
                    break;
                }
            }

            if (decexp != 0)
            {
                double denominator = 10;
                decexp--;
                while (decexp > 0)
                {
                    denominator *= 10;
                    decexp--;
                }

                v /= denominator;
            }

            double number = negative ? -v : v;

            if (i < text.Length && char.ToUpperInvariant(text[i]) == 'E')
            {
                int exponent = int.Parse(text.Substring(i + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                number *= Math.Pow(10, exponent);
            }

            // genopt.io.Token passes integer-valued numbers through Integer.toString((int)num),
            // otherwise Double.toString(num); the parameter parser then re-reads that text.
            // The (int) round trip turns -0.0 into 0.
            if (number >= int.MinValue && number <= int.MaxValue && (int)number == number)
            {
                return (int)number;
            }

            return number;
        }

        /// <summary>
        /// Writes a float as command-file text that <see cref="ParseLikeStreamTokenizer"/> reads back
        /// to a double which rounds to the same float: plain decimal when that is exact
        /// (≤ 15 integer digits, ≤ 22 decimal places), otherwise "d.ddd…E&lt;exp&gt;".
        /// </summary>
        public static string FloatToCommandFileText(float f)
        {
            if (f == 0)
            {
                return float.IsNegative(f) ? "-0" : "0";
            }

            string shortest = Math.Abs(f).ToString("R", CultureInfo.InvariantCulture);

            // Decompose the shortest representation into digits D and exponent q: |f| ≈ D × 10^q.
            string mantissa = shortest;
            int exponent = 0;
            int e = shortest.IndexOfAny(new[] { 'E', 'e' });
            if (e >= 0)
            {
                mantissa = shortest.Substring(0, e);
                exponent = int.Parse(shortest.Substring(e + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }

            int dot = mantissa.IndexOf('.');
            string digits = dot >= 0 ? mantissa.Remove(dot, 1) : mantissa;
            int fractionDigits = dot >= 0 ? mantissa.Length - dot - 1 : 0;
            int q = exponent - fractionDigits;
            digits = digits.TrimStart('0');
            while (digits.Length > 1 && digits.EndsWith('0'))
            {
                digits = digits.Substring(0, digits.Length - 1);
                q++;
            }

            string sign = f < 0 ? "-" : string.Empty;

            if (q >= 0 && digits.Length + q <= 15)
            {
                return sign + digits + new string('0', q);
            }

            if (q < 0 && -q <= 22)
            {
                int places = -q;
                string padded = digits.PadLeft(places + 1, '0');
                return sign + padded.Substring(0, padded.Length - places) + "." + padded.Substring(padded.Length - places);
            }

            // Scientific: d.ddd × 10^(q + digits.Length - 1)
            int scientificExponent = q + digits.Length - 1;
            string scientificMantissa = digits.Length == 1 ? digits : digits.Substring(0, 1) + "." + digits.Substring(1);
            return sign + scientificMantissa + "E" + scientificExponent.ToString(CultureInfo.InvariantCulture);
        }
    }
}
