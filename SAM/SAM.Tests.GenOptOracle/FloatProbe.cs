// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Experiment E-F. GenOpt's GPS rounds every coordinate before evaluating it:
    /// x' = Double.parseDouble(Float.toString((float)x)). The result depends only on f = (float)x,
    /// so the probe feeds floats as initial points (one GPS run with MaxIte = 1 evaluates exactly
    /// the rounded initial point) and reads Java's rounded values back from OutputListingAll.txt.
    /// Each Java value is compared bit-for-bit with <see cref="Java8FloatTextModel"/>; differences from the plain\n    /// shortest-decimal rule are counted for reference.
    /// </summary>
    internal static class FloatProbe
    {
        public static int Run(JavaGenOpt java, string simulatorExe, string workDirectory, int seed, int randomCount, int focusedCount, int perRun, string reportPath)
        {
            List<float> floats = Samples(seed, randomCount, focusedCount);
            Console.WriteLine("Probe floats: " + floats.Count.ToString(CultureInfo.InvariantCulture));

            List<(float F, string Text, double JavaIni)> inputs = new List<(float, string, double)>();
            int unrepresentable = 0;
            foreach (float f in floats)
            {
                string text = JavaNumbers.FloatToCommandFileText(f);
                double javaIni = JavaNumbers.ParseLikeStreamTokenizer(text);
                // -0 is read back as +0 by GenOpt's parser (modelled); every other value must reproduce the float.
                if (f != 0 && BitConverter.SingleToInt32Bits((float)javaIni) != BitConverter.SingleToInt32Bits(f))
                {
                    unrepresentable++;
                    continue;
                }

                inputs.Add((f, text, javaIni));
            }

            Console.WriteLine("Skipped (text does not reproduce the float through the StreamTokenizer model): " + unrepresentable.ToString(CultureInfo.InvariantCulture));

            int mismatches = 0;
            int naiveDifferences = 0;
            int compared = 0;
            StringBuilder mismatchLines = new StringBuilder();
            mismatchLines.AppendLine("floatBits\tcommandText\tjava\tmodel");
            StringBuilder allLines = new StringBuilder();
            allLines.AppendLine("floatBits\tjava");

            for (int start = 0, batch = 0; start < inputs.Count; start += perRun, batch++)
            {
                List<(float F, string Text, double JavaIni)> chunk = inputs.GetRange(start, Math.Min(perRun, inputs.Count - start));
                GenOptCase probeCase = new GenOptCase
                {
                    Name = "float-probe-" + batch.ToString("D4", CultureInfo.InvariantCulture),
                    MaxIte = 1,
                    Function = new FunctionSpec { Kind = "constant", Offset = 0 },
                };
                for (int i = 0; i < chunk.Count; i++)
                {
                    probeCase.Parameters.Add(new ParameterSpec { Name = "x" + (i + 1).ToString("D4", CultureInfo.InvariantCulture), Ini = chunk[i].Text, Step = "1" });
                }

                string directory = Path.Combine(workDirectory, probeCase.Name);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }

                probeCase.Write(directory, simulatorExe);
                JavaRunResult result = java.Run(directory, TimeSpan.FromMinutes(10));
                string listingPath = Path.Combine(directory, "OutputListingAll.txt");
                if (!File.Exists(listingPath))
                {
                    Console.Error.WriteLine(result.StandardOutput);
                    Console.Error.WriteLine(result.StandardError);
                    throw new InvalidOperationException("GenOpt produced no OutputListingAll.txt in " + directory);
                }

                OutputListing listing = OutputListing.Read(listingPath, 1);
                ListingRow initial = listing.Rows.First(r => r.Simulation == 1);
                for (int i = 0; i < chunk.Count; i++)
                {
                    double javaValue = OutputListing.ParseJavaDouble(initial.X[i]);
                    double model = Java8FloatTextModel.ParseOfToString((float)chunk[i].JavaIni);
                    compared++;
                    allLines.Append(BitConverter.SingleToInt32Bits(chunk[i].F).ToString("X8", CultureInfo.InvariantCulture)).Append('\t').AppendLine(initial.X[i]);
                    if (BitConverter.DoubleToInt64Bits(javaValue) != BitConverter.DoubleToInt64Bits(model))
                    {
                        mismatches++;
                        mismatchLines.Append(BitConverter.SingleToInt32Bits(chunk[i].F).ToString("X8", CultureInfo.InvariantCulture)).Append('\t')
                            .Append(chunk[i].Text).Append('\t').Append(initial.X[i]).Append('\t')
                            .AppendLine(model.ToString("R", CultureInfo.InvariantCulture));
                    }

                    if (BitConverter.DoubleToInt64Bits(javaValue) != BitConverter.DoubleToInt64Bits(JavaNumbers.RoundToFloatShortestDecimal(chunk[i].JavaIni)))
                    {
                        naiveDifferences++;
                    }
                }

                Console.WriteLine("batch " + batch.ToString(CultureInfo.InvariantCulture) + ": compared " + compared.ToString(CultureInfo.InvariantCulture) + ", mismatches " + mismatches.ToString(CultureInfo.InvariantCulture));
                Directory.Delete(directory, recursive: true);
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("E-F float rounding probe");
            report.AppendLine("seed\t" + seed.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("randomFloats\t" + randomCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("focusedRandomFloats\t" + focusedCount.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("probeFloats\t" + floats.Count.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("skippedUnrepresentable\t" + unrepresentable.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("compared\t" + compared.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("modelMismatches\t" + mismatches.ToString(CultureInfo.InvariantCulture));
            report.AppendLine("javaDiffersFromShortestDecimal\t" + naiveDifferences.ToString(CultureInfo.InvariantCulture));
            report.AppendLine();
            report.Append(mismatchLines);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
            File.WriteAllText(reportPath, report.ToString());
            File.WriteAllText(Path.ChangeExtension(reportPath, ".all.tsv"), allLines.ToString());
            Console.WriteLine(report.ToString(0, Math.Min(report.Length, 4000)));
            return mismatches == 0 ? 0 : 1;
        }

        /// <summary>Deterministic sample set: special values, lattice values GPS produces, dense runs, random bit patterns.</summary>
        public static List<float> Samples(int seed, int randomCount, int focusedCount)
        {
            HashSet<int> bits = new HashSet<int>();
            List<float> result = new List<float>();
            void Add(float f)
            {
                if (float.IsNaN(f) || float.IsInfinity(f))
                {
                    return;
                }

                if (bits.Add(BitConverter.SingleToInt32Bits(f)))
                {
                    result.Add(f);
                }
            }

            // Special values: signed zeros, smallest subnormals, largest subnormal, smallest normal, largest finite.
            foreach (float f in new[] { 0f, -0f, float.Epsilon, -float.Epsilon, 2 * float.Epsilon, BitConverter.Int32BitsToSingle(0x007FFFFF), BitConverter.Int32BitsToSingle(0x00800000), float.MaxValue, -float.MaxValue })
            {
                Add(f);
            }

            // Powers of two and ten, and their float neighbours.
            for (int k = -149; k <= 127; k++)
            {
                float p = (float)Math.Pow(2, k);
                Add(p);
                Add(-p);
                Add(MathF.BitIncrement(p));
                Add(MathF.BitDecrement(p));
            }

            // Floats with few significant bits (m·2^k for small odd m): the shapes of round-off residues.
            for (int k = -149; k <= 120; k++)
            {
                for (int m = 3; m <= 31; m += 2)
                {
                    Add((float)(m * Math.Pow(2, k)));
                }
            }

            // Residues of subtracting nearby decimal lattice values (what a search crossing zero produces).
            double[] decimals = { 0.1, 0.2, 0.3, 0.05, 0.15, 0.025, 0.075, 0.01, 0.07, 0.35, 1.1, 2.2, 0.0125 };
            foreach (double a in decimals)
            {
                foreach (double b in decimals)
                {
                    foreach (double scale in new[] { 1.0, 0.5, 0.25, 0.125, 2.0, 3.0 })
                    {
                        Add((float)(a - b * scale));
                        Add((float)((a + b) - (a + b * scale)));
                        Add((float)(a * scale - b));
                    }
                }
            }

            for (int k = -45; k <= 38; k++)
            {
                float p = (float)Math.Pow(10, k);
                Add(p);
                Add(MathF.BitIncrement(p));
                Add(MathF.BitDecrement(p));
            }

            // Values a GPS search over typical building-parameter inputs produces: x0 + kÂ·Î”Â·step.
            double[] origins = { 0, 1, 3, -5, 0.4, 0.9, 20.5, 35, 100, 0.05, 1e-3, 1234.5678 };
            double[] steps = { 1, 0.5, 0.1, 0.05, 0.01, 0.2, 0.25, 2.5, 15, 0.3, 1.0 / 3.0, 0.001 };
            foreach (double x0 in origins)
            {
                foreach (double step in steps)
                {
                    for (int j = 0; j <= 10; j++)
                    {
                        double delta = 1.0 / Math.Pow(2, j);
                        for (int k = -40; k <= 40; k++)
                        {
                            Add((float)(x0 + k * (delta * step)));
                        }
                    }
                }
            }

            // Dense consecutive floats around common magnitudes.
            foreach (float centre in new[] { 0.1f, 0.3f, 1f, 3.3f, 10f, 100.1f, 1e-5f, 7e6f, 1.6777216e7f })
            {
                float f = centre;
                for (int i = 0; i < 300; i++)
                {
                    f = MathF.BitDecrement(f);
                }

                for (int i = 0; i < 600; i++)
                {
                    Add(f);
                    f = MathF.BitIncrement(f);
                }
            }

            // Random floats with magnitude in [1e-6, 1e7], the range building-parameter inputs live in.
            Random focusedRandom = new Random(unchecked(seed * 31 + 7));
            int low = BitConverter.SingleToInt32Bits(1e-6f);
            int high = BitConverter.SingleToInt32Bits(1e7f);
            int focused = focusedCount;
            while (focused > 0)
            {
                float f = BitConverter.Int32BitsToSingle(focusedRandom.Next(low, high + 1));
                if (focusedRandom.Next(2) == 1)
                {
                    f = -f;
                }

                int before = result.Count;
                Add(f);
                if (result.Count > before)
                {
                    focused--;
                }
            }

            // Uniform random bit patterns (all exponents, both signs).
            Random random = new Random(seed);
            while (randomCount > 0)
            {
                float f = BitConverter.Int32BitsToSingle(random.Next(int.MinValue, int.MaxValue));
                if (float.IsNaN(f) || float.IsInfinity(f))
                {
                    continue;
                }

                int before = result.Count;
                Add(f);
                if (result.Count > before)
                {
                    randomCount--;
                }
            }

            return result;
        }
    }
}
