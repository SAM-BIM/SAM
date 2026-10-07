// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Verbs:
    ///   sim                      fake simulator (GenOpt calls this; not for manual use)
    ///   probe-float  --java J --genopt-jar G --work DIR [--seed N] [--random N] [--focused N] [--per-run N] [--report FILE]
    ///   cases        --java J --genopt-jar G --work DIR [--case NAME|all] [--golden DIR]
    ///                runs each catalogue case twice (determinism check); with --golden writes NAME.json traces
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: SAM.Tests.GenOptOracle <sim|probe-float|cases> [options]");
                return 2;
            }

            Dictionary<string, string> options = Options(args.Skip(1).ToArray());
            switch (args[0])
            {
                case "sim":
                    return FakeSimulator.Run();

                case "probe-float":
                    {
                        string work = Required(options, "work");
                        JavaGenOpt java = new JavaGenOpt(Required(options, "java"), Required(options, "genopt-jar"), Path.Combine(work, "java-user-home"));
                        return FloatProbe.Run(
                            java,
                            SimulatorExe(),
                            work,
                            Int(options, "seed", 20261007),
                            Int(options, "random", 100000),
                            Int(options, "focused", 100000),
                            Int(options, "per-run", 1000),
                            options.TryGetValue("report", out string? report) ? report : Path.Combine(work, "float-probe-report.tsv"));
                    }

                case "replay":
                    {
                        int failures = 0;
                        foreach (string file in Directory.GetFiles(Required(options, "golden"), "*.json").OrderBy(f => f, StringComparer.Ordinal))
                        {
                            GoldenTrace trace = System.Text.Json.JsonSerializer.Deserialize<GoldenTrace>(File.ReadAllText(file), Json.Options)
                                ?? throw new InvalidOperationException("Empty trace " + file);
                            SpecReplay replay = new SpecReplay(trace.Case);
                            replay.Run();
                            List<string> differences = SpecReplay.Compare(trace, replay);
                            Console.WriteLine(trace.Case.Name.PadRight(32) + (differences.Count == 0 ? "MATCH" : "DIFF"));
                            foreach (string difference in differences)
                            {
                                Console.WriteLine("    " + difference);
                            }

                            failures += differences.Count == 0 ? 0 : 1;
                        }

                        return failures == 0 ? 0 : 1;
                    }

                case "export-float-fixture":
                    return FloatFixtureExport.Run(Required(options, "results"), Required(options, "out"));

                case "analyse-float":
                    return FloatAnalysis.Run(Required(options, "results"), Int(options, "show", 30));

                case "cases":
                    {
                        string work = Required(options, "work");
                        JavaGenOpt java = new JavaGenOpt(Required(options, "java"), Required(options, "genopt-jar"), Path.Combine(work, "java-user-home"));
                        return CaseRunner.Run(
                            java,
                            SimulatorExe(),
                            work,
                            options.TryGetValue("case", out string? only) ? only : null,
                            options.TryGetValue("golden", out string? golden) ? golden : null);
                    }

                default:
                    Console.Error.WriteLine("unknown verb " + args[0]);
                    return 2;
            }
        }

        private static string SimulatorExe()
        {
            return Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine the simulator executable path.");
        }

        private static Dictionary<string, string> Options(string[] args)
        {
            Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException("Expected an option, got " + args[i]);
                }

                options[args[i].Substring(2)] = args[i + 1];
            }

            return options;
        }

        private static string Required(Dictionary<string, string> options, string name)
        {
            return options.TryGetValue(name, out string? value) ? value : throw new ArgumentException("--" + name + " is required");
        }

        private static int Int(Dictionary<string, string> options, string name, int fallback)
        {
            return options.TryGetValue(name, out string? value) ? int.Parse(value, CultureInfo.InvariantCulture) : fallback;
        }
    }
}
