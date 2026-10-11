// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// The simulation program GenOpt starts once per evaluation. GenOpt runs it with the working
    /// directory set to "&lt;ini dir&gt;\tmp-genopt-run-N", having written the substituted input file
    /// (Variables.txt, one "name=value" line per variable) there. It reads the function definition
    /// from "..\fakesim.json", writes "Output.txt" (one "name::value" line per output, round-trip
    /// formatted) and "Error.txt" (the GenOpt log file; contains "Error" only on injected failures).
    /// </summary>
    internal static class FakeSimulator
    {
        public const string SpecFileName = "fakesim.json";
        public const string InputFileName = "Variables.txt";
        public const string OutputFileName = "Output.txt";
        public const string LogFileName = "Error.txt";

        public static int Run()
        {
            string directory = Directory.GetCurrentDirectory();
            string? parent = Directory.GetParent(directory)?.FullName;
            if (parent is null)
            {
                return 2;
            }

            FunctionSpec spec = JsonSerializer.Deserialize<FunctionSpec>(File.ReadAllText(Path.Combine(parent, SpecFileName)), Json.Options)
                ?? throw new InvalidOperationException("Empty function specification.");

            int simulation = SimulationNumber(directory);

            List<double> x = new List<double>();
            foreach (string line in File.ReadAllLines(Path.Combine(directory, InputFileName)))
            {
                int index = line.IndexOf('=');
                if (index < 0)
                {
                    continue;
                }

                x.Add(double.Parse(line.Substring(index + 1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture));
            }

            string log = "fake simulator: simulation " + simulation.ToString(CultureInfo.InvariantCulture) + Environment.NewLine;
            if (spec.FailAtSimulation.Contains(simulation))
            {
                File.WriteAllText(Path.Combine(directory, LogFileName), log + "Error: injected failure" + Environment.NewLine);
                File.WriteAllText(Path.Combine(directory, OutputFileName), string.Empty);
                return 0;
            }

            if (spec.FailOnceAtSimulation.Contains(simulation))
            {
                string marker = Path.Combine(parent, "failed-once-" + simulation.ToString(CultureInfo.InvariantCulture) + ".marker");
                if (!File.Exists(marker))
                {
                    File.WriteAllText(marker, string.Empty);
                    File.WriteAllText(Path.Combine(directory, LogFileName), log + "Error: injected failure (once)" + Environment.NewLine);
                    File.WriteAllText(Path.Combine(directory, OutputFileName), string.Empty);
                    return 0;
                }
            }

            double[] values = spec.Evaluate(x);
            using (StreamWriter writer = new StreamWriter(Path.Combine(directory, OutputFileName)))
            {
                for (int i = 0; i < spec.Outputs.Count; i++)
                {
                    writer.WriteLine(spec.Outputs[i] + "::" + values[i].ToString("R", CultureInfo.InvariantCulture));
                }
            }

            File.WriteAllText(Path.Combine(directory, LogFileName), log);
            return 0;
        }

        private static int SimulationNumber(string directory)
        {
            Match match = Regex.Match(Path.GetFileName(directory), "tmp-genopt-run-([0-9]+)$");
            return match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        }
    }
}
