// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>A sanitised golden trace: the case definition plus what Java GenOpt 3.1.1 did with it.</summary>
    public sealed class GoldenTrace
    {
        public string Oracle { get; set; } = "GenOpt 3.1.1 (genopt.jar 2016-03-30), Java 8";

        public GenOptCase Case { get; set; } = new GenOptCase();

        public int ExitCode { get; set; }

        /// <summary>GenOpt.log text after its last divider line, with paths removed.</summary>
        public List<string> Termination { get; set; } = new List<string>();

        /// <summary>
        /// GenOpt console lines after the start banner, with paths removed. Compared for determinism only; not stored
        /// in the golden traces (owner decision D4).
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public List<string> Console { get; set; } = new List<string>();

        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>OutputListingAll.txt data rows.</summary>
        public List<ListingRow> All { get; set; } = new List<ListingRow>();

        /// <summary>OutputListingMain.txt data rows.</summary>
        public List<ListingRow> Main { get; set; } = new List<ListingRow>();

        /// <summary>Text after the data rows of OutputListingAll.txt (e.g. GoldenSection's result overview).</summary>
        public List<string> Footer { get; set; } = new List<string>();
    }

    internal static class CaseRunner
    {
        public static int Run(JavaGenOpt java, string simulatorExe, string workDirectory, string? only, string? goldenDirectory)
        {
            int failures = 0;
            foreach (GenOptCase genOptCase in CaseCatalogue.All())
            {
                if (only is not null && only != "all" && genOptCase.Name != only)
                {
                    continue;
                }

                GoldenTrace first = RunOnce(java, simulatorExe, Path.Combine(workDirectory, genOptCase.Name, "run-a"), genOptCase);
                GoldenTrace second = RunOnce(java, simulatorExe, Path.Combine(workDirectory, genOptCase.Name, "run-b"), genOptCase);

                string a = JsonSerializer.Serialize(first, Json.Options);
                string b = JsonSerializer.Serialize(second, Json.Options);
                bool deterministic = a == b && first.Console.SequenceEqual(second.Console);
                if (!deterministic)
                {
                    failures++;
                    File.WriteAllText(Path.Combine(workDirectory, genOptCase.Name, "run-a.json"), a);
                    File.WriteAllText(Path.Combine(workDirectory, genOptCase.Name, "run-b.json"), b);
                }

                System.Console.WriteLine(
                    genOptCase.Name.PadRight(32) +
                    " exit " + first.ExitCode.ToString(CultureInfo.InvariantCulture) +
                    "  sims " + (first.All.Count == 0 ? 0 : first.All.Max(r => r.Simulation)).ToString(CultureInfo.InvariantCulture).PadLeft(4) +
                    "  rows " + first.All.Count.ToString(CultureInfo.InvariantCulture).PadLeft(4) +
                    "  " + (deterministic ? "deterministic" : "NOT DETERMINISTIC") +
                    "  | " + string.Join(" / ", first.Termination.Take(2)));

                if (goldenDirectory is not null && deterministic)
                {
                    Directory.CreateDirectory(goldenDirectory);
                    File.WriteAllText(Path.Combine(goldenDirectory, genOptCase.Name + ".json"), a.ReplaceLineEndings("\n") + "\n");
                }
            }

            return failures == 0 ? 0 : 1;
        }

        private static GoldenTrace RunOnce(JavaGenOpt java, string simulatorExe, string directory, GenOptCase genOptCase)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            genOptCase.Write(directory, simulatorExe);
            JavaRunResult result = java.Run(directory, TimeSpan.FromMinutes(20));
            File.WriteAllText(Path.Combine(directory, "java-stdout.txt"), result.StandardOutput);
            File.WriteAllText(Path.Combine(directory, "java-stderr.txt"), result.StandardError);

            GoldenTrace trace = new GoldenTrace { Case = genOptCase, ExitCode = result.ExitCode };
            int outputs = genOptCase.Function.Outputs.Count;

            string allPath = Path.Combine(directory, "OutputListingAll.txt");
            if (File.Exists(allPath))
            {
                OutputListing all = OutputListing.Read(allPath, outputs);
                trace.Columns = all.Columns;
                trace.All = all.Rows;
                trace.Footer = all.Footer.Select(l => Sanitise(l, directory)).ToList();
            }

            string mainPath = Path.Combine(directory, "OutputListingMain.txt");
            if (File.Exists(mainPath))
            {
                trace.Main = OutputListing.Read(mainPath, outputs).Rows;
            }

            string logPath = Path.Combine(directory, "GenOpt.log");
            if (File.Exists(logPath))
            {
                string[] log = File.ReadAllLines(logPath);
                int divider = Array.FindLastIndex(log, l => l.StartsWith("_____", StringComparison.Ordinal));
                trace.Termination = log.Skip(divider + 1)
                    .Select(l => Sanitise(l, directory))
                    .Where(l => l.Trim().Length > 0)
                    .ToList();
            }

            trace.Console = ConsoleLines(result.StandardOutput + result.StandardError, directory);
            return trace;
        }

        /// <summary>Console output after GenOpt's banner (the banner holds the LBNL copyright text).</summary>
        private static List<string> ConsoleLines(string text, string directory)
        {
            string[] lines = text.ReplaceLineEndings("\n").Split('\n');
            int dividers = 0;
            int start = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("_____", StringComparison.Ordinal))
                {
                    dividers++;
                    if (dividers == 2)
                    {
                        start = i + 1;
                        break;
                    }
                }
            }

            return lines.Skip(start)
                .Select(l => Sanitise(l, directory))
                .Where(l => l.Trim().Length > 0)
                .Where(l => !l.StartsWith("Optimization started", StringComparison.Ordinal))
                .Where(l => !l.StartsWith("Optimization finished", StringComparison.Ordinal))
                .Where(l => !l.StartsWith("Execution time", StringComparison.Ordinal))
                .ToList();
        }

        /// <summary>Removes the case directory, simulator path and any other absolute path.</summary>
        public static string Sanitise(string line, string directory)
        {
            string result = line;
            foreach (string variant in PathVariants(directory))
            {
                result = result.Replace(variant, "<case>", StringComparison.OrdinalIgnoreCase);
            }

            result = Regex.Replace(result, @"[A-Za-z]:[\\/][^\s'""]*", "<path>");
            result = Regex.Replace(result, @"(Start time|started at|finished at)\s*:?.*", "$1: <time>");
            return result.TrimEnd();
        }

        private static IEnumerable<string> PathVariants(string directory)
        {
            string full = Path.GetFullPath(directory).TrimEnd('\\');
            yield return full;
            yield return full.Replace('\\', '/');
            yield return full.Replace("\\", "\\\\");
        }
    }
}
