// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Writes a compact, reviewable subset of a float-probe result file as a golden fixture:
    /// every value where Java differs from the shortest decimal outside the large-integer band,
    /// one in ten inside it, one in a hundred of the values where Java equals the shortest decimal,
    /// and every value the behavioural model does not reproduce (marked "unmodelled").
    /// </summary>
    internal static class FloatFixtureExport
    {
        public static int Run(string resultsPath, string outputPath)
        {
            StringBuilder text = new StringBuilder();
            text.Append("# GenOpt 3.1.1 coordinate rounding under Java 8: Double.parseDouble(Float.toString(f)).\n");
            text.Append("# Values observed through the real Java GenOpt (experiment E-F, documentation/GenOpt-3.1.1-Behaviour.md).\n");
            text.Append("# floatBits = IEEE-754 single bits (hex); java = GenOpt's printed rounded value; note = class.\n");
            text.Append("floatBits\tjava\tnote\n");

            int agreeing = 0;
            int largeInteger = 0;
            int written = 0;
            foreach (string line in File.ReadLines(resultsPath).Skip(1))
            {
                string[] cells = line.Split('\t');
                int bits = int.Parse(cells[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                float f = BitConverter.Int32BitsToSingle(bits);
                if (bits == unchecked((int)0x80000000))
                {
                    continue; // -0 never reaches the rounding (GenOpt's parser reads "-0" as 0)
                }

                double java = OutputListing.ParseJavaDouble(cells[1]);
                double shortest = JavaNumbers.RoundToFloatShortestDecimal(f);
                double model = Java8FloatTextModel.ParseOfToString(f);
                bool javaIsShortest = BitConverter.DoubleToInt64Bits(java) == BitConverter.DoubleToInt64Bits(shortest);
                bool modelled = BitConverter.DoubleToInt64Bits(java) == BitConverter.DoubleToInt64Bits(model);
                double magnitude = Math.Abs((double)f);

                string? note = null;
                if (!modelled)
                {
                    note = "unmodelled";
                }
                else if (!javaIsShortest)
                {
                    bool integerBand = magnitude >= Math.Pow(2, 25) && magnitude < Math.Pow(2, 63);
                    if (!integerBand)
                    {
                        note = "not-shortest";
                    }
                    else if (largeInteger++ % 10 == 0)
                    {
                        note = "not-shortest-integer";
                    }
                }
                else if (agreeing++ % 100 == 0)
                {
                    note = "shortest";
                }

                if (note is not null)
                {
                    text.Append(cells[0]).Append('\t').Append(cells[1]).Append('\t').Append(note).Append('\n');
                    written++;
                }
            }

            File.WriteAllText(outputPath, text.ToString());
            Console.WriteLine("wrote " + written.ToString(CultureInfo.InvariantCulture) + " rows to " + outputPath);
            return 0;
        }
    }
}
