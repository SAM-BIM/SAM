// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Math;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace SAM.Tests.Helpers
{
    /// <summary>
    /// One GenOpt 3.1.1 golden trace from Golden/GenOpt (recorded from the real Java GenOpt by SAM.Tests.GenOptOracle),
    /// and the test-side adapter that runs the native SAM.Math kernel on the same case and compares it bit for bit.
    /// <para>
    /// Everything GenOpt-specific stays on this side and out of SAM.Math: the command-file number parsing, the
    /// keyword text, the synthetic objective functions and the listing comment text.
    /// </para>
    /// </summary>
    public sealed class GenOptGoldenTrace
    {
        public static readonly string Directory_GenOpt = Path.Combine(AppContext.BaseDirectory, "Golden", "GenOpt");

        private GenOptGoldenTrace()
        {
        }

        public string Name { get; private set; } = string.Empty;

        public string Algorithm { get; private set; } = string.Empty;

        public int MaxIte { get; private set; }

        public List<string> Keywords { get; } = new List<string>();

        public List<Parameter> Parameters { get; } = new List<Parameter>();

        public Function Objective { get; private set; } = new Function();

        public List<string> Termination { get; } = new List<string>();

        public List<Row> All { get; } = new List<Row>();

        public List<Row> Main { get; } = new List<Row>();

        public List<string> Footer { get; } = new List<string>();

        public sealed class Parameter
        {
            public string Name = string.Empty;
            public string Ini = "0";
            public string? Min;
            public string? Max;
            public string Step = "1";
        }

        public sealed class Row
        {
            public int Simulation;
            public int MainIteration;
            public int SubIteration;
            public List<string> F = new List<string>();
            public List<string> X = new List<string>();
            public string Comment = string.Empty;
        }

        /// <summary>The synthetic objective of the oracle's fake simulator (FunctionSpec), same IEEE expressions.</summary>
        public sealed class Function
        {
            public string Kind = "quadratic";
            public double[] Center = new double[0];
            public double[] Weight = new double[0];
            public double Offset;
            public double Quantum = 1;
            public List<string> Outputs = new List<string>();
            public List<int> FailAtSimulation = new List<int>();
            public List<int> FailOnceAtSimulation = new List<int>();

            public double[] Evaluate(IReadOnlyList<double> x)
            {
                double f;
                switch (Kind)
                {
                    case "constant":
                        f = Offset;
                        break;
                    case "quadratic":
                        f = Quadratic(x);
                        break;
                    case "quantisedQuadratic":
                        f = System.Math.Floor(Quadratic(x) / Quantum) * Quantum;
                        break;
                    case "linear":
                        f = Linear(x);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown function kind '" + Kind + "'.");
                }

                double sum = 0;
                for (int i = 0; i < x.Count; i++)
                {
                    sum += x[i];
                }

                double[] result = new double[Outputs.Count];
                result[0] = f;
                for (int k = 1; k < result.Length; k++)
                {
                    result[k] = k * sum;
                }

                return result;
            }

            private double Quadratic(IReadOnlyList<double> x)
            {
                double f = Offset;
                for (int i = 0; i < x.Count; i++)
                {
                    double d = x[i] - (i < Center.Length ? Center[i] : 0);
                    f += (i < Weight.Length ? Weight[i] : 1) * d * d;
                }

                return f;
            }

            private double Linear(IReadOnlyList<double> x)
            {
                double f = Offset;
                for (int i = 0; i < x.Count; i++)
                {
                    f += (i < Weight.Length ? Weight[i] : 1) * x[i];
                }

                return f;
            }
        }

        public static IEnumerable<string> Names()
        {
            return Directory.GetFiles(Directory_GenOpt, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(x => x, StringComparer.Ordinal)
                .Select(x => x!);
        }

        public static GenOptGoldenTrace Load(string name)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Directory_GenOpt, name + ".json")));
            JsonElement root = document.RootElement;
            JsonElement genOptCase = root.GetProperty("case");

            GenOptGoldenTrace trace = new GenOptGoldenTrace
            {
                Name = genOptCase.GetProperty("name").GetString()!,
                Algorithm = genOptCase.GetProperty("algorithm").GetString()!,
                MaxIte = genOptCase.GetProperty("maxIte").GetInt32(),
            };

            trace.Keywords.AddRange(genOptCase.GetProperty("algorithmKeywords").EnumerateArray().Select(x => x.GetString()!));
            foreach (JsonElement parameter in genOptCase.GetProperty("parameters").EnumerateArray())
            {
                trace.Parameters.Add(new Parameter
                {
                    Name = parameter.GetProperty("name").GetString()!,
                    Ini = parameter.GetProperty("ini").GetString()!,
                    Min = parameter.GetProperty("min").ValueKind == JsonValueKind.Null ? null : parameter.GetProperty("min").GetString(),
                    Max = parameter.GetProperty("max").ValueKind == JsonValueKind.Null ? null : parameter.GetProperty("max").GetString(),
                    Step = parameter.GetProperty("step").GetString()!,
                });
            }

            JsonElement function = genOptCase.GetProperty("function");
            trace.Objective = new Function
            {
                Kind = function.GetProperty("kind").GetString()!,
                Center = function.GetProperty("center").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                Weight = function.GetProperty("weight").EnumerateArray().Select(x => x.GetDouble()).ToArray(),
                Offset = function.GetProperty("offset").GetDouble(),
                Quantum = function.GetProperty("quantum").GetDouble(),
                Outputs = function.GetProperty("outputs").EnumerateArray().Select(x => x.GetString()!).ToList(),
                FailAtSimulation = function.GetProperty("failAtSimulation").EnumerateArray().Select(x => x.GetInt32()).ToList(),
                FailOnceAtSimulation = function.GetProperty("failOnceAtSimulation").EnumerateArray().Select(x => x.GetInt32()).ToList(),
            };

            trace.Termination.AddRange(root.GetProperty("termination").EnumerateArray().Select(x => x.GetString()!));
            trace.All.AddRange(root.GetProperty("all").EnumerateArray().Select(ReadRow));
            trace.Main.AddRange(root.GetProperty("main").EnumerateArray().Select(ReadRow));
            trace.Footer.AddRange(root.GetProperty("footer").EnumerateArray().Select(x => x.GetString()!));
            return trace;
        }

        private static Row ReadRow(JsonElement row)
        {
            return new Row
            {
                Simulation = row.GetProperty("simulation").GetInt32(),
                MainIteration = row.GetProperty("mainIteration").GetInt32(),
                SubIteration = row.GetProperty("subIteration").GetInt32(),
                F = row.GetProperty("f").EnumerateArray().Select(x => x.GetString()!).ToList(),
                X = row.GetProperty("x").EnumerateArray().Select(x => x.GetString()!).ToList(),
                Comment = row.GetProperty("comment").GetString()!,
            };
        }

        // ------------------------------------------------------------------ case -> kernel

        public OptimisationProblem CreateProblem()
        {
            return new OptimisationProblem(
                Parameters.Select(p => new OptimisationParameter(
                    p.Name,
                    ParseLikeStreamTokenizer(p.Ini),
                    p.Min is null ? double.NegativeInfinity : ParseLikeStreamTokenizer(p.Min),
                    p.Max is null ? double.PositiveInfinity : ParseLikeStreamTokenizer(p.Max),
                    ParseLikeStreamTokenizer(p.Step))),
                Objective.Outputs.Count);
        }

        public Optimiser CreateOptimiser()
        {
            switch (Algorithm)
            {
                case "GPSHookeJeeves":
                    return ConfigureGps(new HookeJeeves());
                case "GPSCoordinateSearch":
                    return ConfigureGps(new CoordinateSearch());
                case "GoldenSection":
                    GoldenSection goldenSection = new GoldenSection { MaximumSimulations = MaxIte };
                    string? keyword = Keywords.SingleOrDefault();
                    if (keyword is not null && keyword.StartsWith("AbsDiffFunction", StringComparison.Ordinal))
                    {
                        goldenSection.StoppingCriterion = GoldenSectionStoppingCriterion.AbsoluteDifference;
                        goldenSection.AbsoluteDifference = ParseLikeStreamTokenizer(KeywordValue(keyword));
                    }
                    else if (keyword is not null && keyword.StartsWith("IntervalReduction", StringComparison.Ordinal))
                    {
                        goldenSection.StoppingCriterion = GoldenSectionStoppingCriterion.IntervalReduction;
                        goldenSection.IntervalReduction = ParseLikeStreamTokenizer(KeywordValue(keyword));
                    }

                    return goldenSection;
                default:
                    throw new NotSupportedException(Algorithm);
            }
        }

        /// <summary>The fake simulator, including the injected failures (keyed by simulation number).</summary>
        public IObjectiveEvaluator CreateEvaluator()
        {
            HashSet<int> failedOnce = new HashSet<int>();
            return new DelegateObjectiveEvaluator((request, cancellationToken) =>
            {
                if (Objective.FailAtSimulation.Contains(request.Simulation))
                {
                    return ObjectiveEvaluation.Failure("injected failure");
                }

                if (Objective.FailOnceAtSimulation.Contains(request.Simulation) && failedOnce.Add(request.Simulation))
                {
                    return ObjectiveEvaluation.Failure("injected failure (once)");
                }

                return ObjectiveEvaluation.Success(Objective.Evaluate(request.Coordinates));
            });
        }

        public OptimisationResult Run(IProgress<OptimisationProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            return CreateOptimiser().Run(CreateProblem(), CreateEvaluator(), progress, cancellationToken);
        }

        private GeneralisedPatternSearch ConfigureGps(GeneralisedPatternSearch search)
        {
            int Value(string key) => int.Parse(KeywordValue(Keywords.Single(w => w.StartsWith(key + " ", StringComparison.Ordinal))), CultureInfo.InvariantCulture);
            search.MaximumSimulations = MaxIte;
            search.MeshSizeDivider = Value("MeshSizeDivider");
            search.InitialMeshSizeExponent = Value("InitialMeshSizeExponent");
            search.MeshSizeExponentIncrement = Value("MeshSizeExponentIncrement");
            search.NumberOfStepReduction = Value("NumberOfStepReduction");
            return search;
        }

        private static string KeywordValue(string keyword) => keyword.Split('=')[1].Trim().TrimEnd(';');

        /// <summary>
        /// GenOpt's command-file number reading (java.io.StreamTokenizer arithmetic, spec §1.1): v = v·10 + digit,
        /// divided by 10^d built by repeated multiplication; "E" applied as num *= Math.pow(10, exp); an integer
        /// value passes through (int), so -0 becomes 0.
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
                number *= System.Math.Pow(10, exponent);
            }

            if (number >= int.MinValue && number <= int.MaxValue && (int)number == number)
            {
                return (int)number;
            }

            return number;
        }

        // ------------------------------------------------------------------ kernel -> GenOpt listing text

        /// <summary>The GenOpt listing comment for a kernel trace entry.</summary>
        public string Comment(OptimisationTraceEntry entry)
        {
            string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
            string Move()
            {
                string name = Parameters[entry.ParameterIndex].Name;
                return " at " + name + (entry.Direction > 0 ? "+d" : "-d") + name + ".";
            }

            switch (entry.Event)
            {
                case OptimisationEvent.InitialPoint:
                    return "Initial point.";
                case OptimisationEvent.ExplorationBase:
                    return "Exploration base, Delta = " + Number(entry.Delta) + ".";
                case OptimisationEvent.CostReduced:
                    return "Cost reduced    " + Move();
                case OptimisationEvent.CostNotReduced:
                    return "Cost not reduced" + Move();
                case OptimisationEvent.LocalSearchReducedCost:
                    return "Local search reduced cost.";
                case OptimisationEvent.GlobalSearchReducedCost:
                    return "Global search reduced cost.";
                case OptimisationEvent.StepSizeReduced:
                    return "Iteration step did not reduce cost. Reduce step size to '" + Number(entry.Delta) + "'.";
                case OptimisationEvent.MaximumStepReductionsReached:
                    return "Iteration step did not reduce cost. Maximum number of step reductions reached.";
                case OptimisationEvent.MinimumPoint:
                    return "Minimum point.";
                case OptimisationEvent.CurrentLowestPoint:
                    return "Current lowest point.";
                case OptimisationEvent.LineSearch:
                    return "Linesearch.";
                default:
                    throw new NotSupportedException(entry.Event.ToString());
            }
        }

        /// <summary>The GenOpt outcome this trace recorded (from its termination text).</summary>
        public OptimisationOutcome ExpectedOutcome()
        {
            string text = string.Join("\n", Termination);
            if (text.StartsWith("Optimization completed successfully.", StringComparison.Ordinal))
            {
                return OptimisationOutcome.Success;
            }

            if (text.Contains("Maximum number of iteration exceeded.", StringComparison.Ordinal))
            {
                return OptimisationOutcome.MaximumSimulationsReached;
            }

            if (text.Contains("Nullspace in line search", StringComparison.Ordinal))
            {
                return OptimisationOutcome.Nullspace;
            }

            if (text.Contains("Following error was found", StringComparison.Ordinal))
            {
                return OptimisationOutcome.EvaluationFailed;
            }

            return OptimisationOutcome.AlgorithmError;
        }

        public int ExpectedRetries() => Termination.Count(l => l.Contains("Try to evaluate simulation a second time.", StringComparison.Ordinal));

        /// <summary>GoldenSection's result overview values (lower, upper, mid point, length, normalised length).</summary>
        public List<double> ExpectedFooter()
        {
            return Footer
                .Where(l => l.Contains(':', StringComparison.Ordinal) && !l.Contains("***", StringComparison.Ordinal))
                .Select(l => ParseJavaDouble(l.Substring(l.LastIndexOf(':') + 1).Trim()))
                .ToList();
        }

        /// <summary>Every difference between the kernel result and this Java trace (empty when bit-exact).</summary>
        public List<string> Compare(OptimisationResult result)
        {
            List<string> differences = new List<string>();
            CompareRows("all", All, result.Entries, includeSub: true, differences);
            CompareRows("main", Main, result.MainIterations, includeSub: false, differences);

            if (ExpectedOutcome() != result.Outcome)
            {
                differences.Add("outcome: java " + ExpectedOutcome() + ", kernel " + result.Outcome);
            }

            if (ExpectedRetries() != result.Retries)
            {
                differences.Add("retries: java " + ExpectedRetries() + ", kernel " + result.Retries);
            }

            List<double> footer = ExpectedFooter();
            List<double> actual = result.Interval is null
                ? new List<double>()
                : new List<double> { result.Interval.Lower, result.Interval.Upper, result.Interval.MidPoint, result.Interval.Length, result.Interval.NormalisedLength };
            if (footer.Count != actual.Count || footer.Where((v, i) => !SameBits(v, actual[i])).Any())
            {
                differences.Add("footer: java [" + string.Join(", ", footer) + "], kernel [" + string.Join(", ", actual) + "]");
            }

            return differences;
        }

        private void CompareRows(string listing, List<Row> expected, IReadOnlyList<OptimisationTraceEntry> actual, bool includeSub, List<string> differences)
        {
            if (expected.Count != actual.Count)
            {
                differences.Add(listing + ": java " + expected.Count + " rows, kernel " + actual.Count);
            }

            for (int i = 0; i < System.Math.Min(expected.Count, actual.Count); i++)
            {
                Row e = expected[i];
                OptimisationTraceEntry a = actual[i];
                string comment = Comment(a);
                bool same = e.Simulation == a.Simulation
                    && e.MainIteration == a.MainIteration
                    && (!includeSub || e.SubIteration == a.SubIteration)
                    && e.F.Count == a.Outputs.Count && e.F.Select((v, j) => SameBits(ParseJavaDouble(v), a.Outputs[j])).All(x => x)
                    && e.X.Count == a.Coordinates.Count && e.X.Select((v, j) => SameBits(ParseJavaDouble(v), a.Coordinates[j])).All(x => x)
                    && CommentsEqual(e.Comment, comment);
                if (!same)
                {
                    differences.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} row {1}: java [{2} {3} {4} f={5} x={6} '{7}'] kernel [{8} {9} {10} f={11} x={12} '{13}']",
                        listing, i,
                        e.Simulation, e.MainIteration, e.SubIteration, string.Join(",", e.F), string.Join(",", e.X), e.Comment,
                        a.Simulation, a.MainIteration, a.SubIteration, string.Join(",", a.Outputs.Select(v => v.ToString("R", CultureInfo.InvariantCulture))), string.Join(",", a.Coordinates.Select(v => v.ToString("R", CultureInfo.InvariantCulture))), comment));
                    return;
                }
            }
        }

        /// <summary>Equal text, with embedded numbers compared as doubles (Java and .NET print them differently).</summary>
        private static bool CommentsEqual(string expected, string actual)
        {
            const string NumberPattern = @"-?\d+(\.\d+)?(E-?\d+)?";
            if (Regex.Replace(expected, NumberPattern, "#") != Regex.Replace(actual, NumberPattern, "#"))
            {
                return false;
            }

            List<double> a = Regex.Matches(expected, NumberPattern).Select(m => ParseJavaDouble(m.Value)).ToList();
            List<double> b = Regex.Matches(actual, NumberPattern).Select(m => ParseJavaDouble(m.Value)).ToList();
            return a.Count == b.Count && a.Select((v, i) => SameBits(v, b[i])).All(x => x);
        }

        /// <summary>Parses a Java Double.toString value (e.g. "1.0E-5", "-0.0", "Infinity").</summary>
        public static double ParseJavaDouble(string text)
        {
            switch (text)
            {
                case "Infinity":
                    return double.PositiveInfinity;
                case "-Infinity":
                    return double.NegativeInfinity;
                case "NaN":
                    return double.NaN;
                default:
                    return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }

        public static bool SameBits(double a, double b) => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);
    }
}
