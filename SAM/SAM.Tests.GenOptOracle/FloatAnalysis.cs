// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>Scores rounding models against a saved probe result file (floatBits, Java value).</summary>
    internal static class FloatAnalysis
    {
        public static int Run(string resultsPath, int show)
        {
            int total = 0;
            int shortestMismatch = 0;
            int modelMismatch = 0;
            int shortestMismatchInRange = 0;
            List<string> examples = new List<string>();
            foreach (string line in File.ReadLines(resultsPath).Skip(1))
            {
                string[] cells = line.Split('\t');
                int bits = int.Parse(cells[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (bits == unchecked((int)0x80000000))
                {
                    continue; // "-0" is read as 0 by GenOpt's command-file parser; it never reaches the rounding
                }

                float f = BitConverter.Int32BitsToSingle(bits);
                double java = OutputListing.ParseJavaDouble(cells[1]);
                total++;

                double shortest = JavaNumbers.RoundToFloatShortestDecimal(f);
                if (!Same(java, shortest))
                {
                    shortestMismatch++;
                    double magnitude = Math.Abs(f);
                    if (magnitude >= 1e-6 && magnitude <= 1e7)
                    {
                        shortestMismatchInRange++;
                    }
                }

                double model = Java8FloatTextModel.ParseOfToString(f);
                if (!Same(java, model))
                {
                    modelMismatch++;
                    if (examples.Count < show)
                    {
                        examples.Add(cells[0] + "\tjava " + cells[1] + "\tmodel " + model.ToString("R", CultureInfo.InvariantCulture) + "\tshortest " + shortest.ToString("R", CultureInfo.InvariantCulture));
                    }
                }
            }

            Console.WriteLine("samples                         " + total.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("shortest-decimal mismatches     " + shortestMismatch.ToString(CultureInfo.InvariantCulture) + " (of which |f| in [1e-6,1e7]: " + shortestMismatchInRange.ToString(CultureInfo.InvariantCulture) + ")");
            Console.WriteLine("Java8FloatTextModel mismatches  " + modelMismatch.ToString(CultureInfo.InvariantCulture));
            foreach (string example in examples)
            {
                Console.WriteLine(example);
            }

            return modelMismatch == 0 ? 0 : 1;
        }

        /// <summary>Bitwise equality.</summary>
        private static bool Same(double java, double model)
        {
            return BitConverter.DoubleToInt64Bits(java) == BitConverter.DoubleToInt64Bits(model);
        }
    }
}
