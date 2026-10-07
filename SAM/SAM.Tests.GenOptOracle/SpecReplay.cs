// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Globalization;
using System.Text.RegularExpressions;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// Executable form of the written behaviour specification (documentation/GenOpt-3.1.1-Behaviour.md,
    /// sections "Evaluation layer", "GPS main loop", "Golden section" and "Reporting"). It replays a
    /// golden case with the case's synthetic function and produces the rows GenOpt would write. A spec
    /// item counts as CONFIRMED only when this replay reproduces the Java traces bit for bit.
    /// This is test tooling that validates the specification; it is not the SAM optimisation kernel.
    /// </summary>
    internal sealed class SpecReplay
    {
        private const double MaxValue = double.MaxValue;

        private readonly GenOptCase genOptCase;
        private readonly int dim;
        private readonly int dimF;
        private readonly double[] lower;
        private readonly double[] upper;
        private readonly double[] initial;
        private readonly double[] step;
        private readonly ApproximateRedBlackMap<double[]> cache = new ApproximateRedBlackMap<double[]>(ComparePoints);
        private readonly List<Point> mainPoints = new List<Point>();
        private readonly List<Point> subPoints = new List<Point>();
        private readonly HashSet<int> failedOnce = new HashSet<int>();
        private int simulations;
        private int mainIteration = 1;
        private int subIteration = 1;
        private bool firstSimulations = true;

        public SpecReplay(GenOptCase genOptCase)
        {
            this.genOptCase = genOptCase;
            dim = genOptCase.Parameters.Count;
            dimF = genOptCase.Function.Outputs.Count;
            lower = genOptCase.Parameters.Select(p => p.Min is null ? double.NegativeInfinity : JavaNumbers.ParseLikeStreamTokenizer(p.Min)).ToArray();
            upper = genOptCase.Parameters.Select(p => p.Max is null ? double.PositiveInfinity : JavaNumbers.ParseLikeStreamTokenizer(p.Max)).ToArray();
            initial = genOptCase.Parameters.Select(p => JavaNumbers.ParseLikeStreamTokenizer(p.Ini)).ToArray();
            step = genOptCase.Parameters.Select(p => JavaNumbers.ParseLikeStreamTokenizer(p.Step)).ToArray();
        }

        public List<ListingRow> All { get; } = new List<ListingRow>();

        public List<ListingRow> Main { get; } = new List<ListingRow>();

        public List<double> Footer { get; } = new List<double>();

        /// <summary>success | maxIte | simulationError | nullspace | algorithmError</summary>
        public string Outcome { get; private set; } = string.Empty;

        public int Retries { get; private set; }

        public void Run()
        {
            try
            {
                switch (genOptCase.Algorithm)
                {
                    case "GPSHookeJeeves":
                        Outcome = RunGps(hookeJeeves: true) == 1 ? "success" : "maxIte";
                        break;
                    case "GPSCoordinateSearch":
                        Outcome = RunGps(hookeJeeves: false) == 1 ? "success" : "maxIte";
                        break;
                    case "GoldenSection":
                        Outcome = RunGoldenSection();
                        break;
                    default:
                        throw new NotSupportedException(genOptCase.Algorithm);
                }
            }
            catch (ReplayTermination termination)
            {
                Outcome = termination.Outcome;
            }
        }

        // ------------------------------------------------------------------ evaluation layer

        private sealed class Point
        {
            public double[] X = Array.Empty<double>();
            public double[] F = Array.Empty<double>();
            public int Simulation;
            public int MainIteration;
            public int SubIteration;
            public string Comment = string.Empty;

            public Point Clone() => new Point
            {
                X = (double[])X.Clone(),
                F = (double[])F.Clone(),
                Simulation = Simulation,
                MainIteration = MainIteration,
                SubIteration = SubIteration,
                Comment = Comment,
            };
        }

        private sealed class ReplayTermination : Exception
        {
            public ReplayTermination(string outcome)
            {
                Outcome = outcome;
            }

            public string Outcome { get; }
        }

        /// <summary>ModelGPS.getF: round to float-decimal, bounds rule, then the shared evaluation.</summary>
        private Point GetFGps(Point point)
        {
            Point rounded = point.Clone();
            for (int i = 0; i < dim; i++)
            {
                rounded.X[i] = Java8FloatTextModel.ParseOfToString((float)rounded.X[i]);
            }

            for (int i = 0; i < dim; i++)
            {
                if (rounded.X[i] > upper[i] || rounded.X[i] < lower[i])
                {
                    rounded.F = Enumerable.Repeat(MaxValue, dimF).ToArray();
                    return rounded;
                }
            }

            return GetF(new[] { rounded })[0];
        }

        /// <summary>Optimizer.getF(Point[]): result database lookup, numbering, simulation, retry rule.</summary>
        private Point[] GetF(Point[] points)
        {
            bool[] evaluate = new bool[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                double[]? known = Lookup(points[p].X);
                evaluate[p] = known is null;
                if (known is not null)
                {
                    points[p].F = (double[])known.Clone();
                }
            }

            int[] equalTo = new int[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                equalTo[p] = -1;
                for (int q = 0; q < p; q++)
                {
                    if (Equal(points[p].X, points[q].X))
                    {
                        equalTo[p] = q;
                        break;
                    }
                }

                if (equalTo[p] > -1)
                {
                    evaluate[p] = false;
                }
            }

            for (int p = 0; p < points.Length; p++)
            {
                if (evaluate[p])
                {
                    simulations++;
                }

                points[p].Simulation = simulations;
            }

            // Simulation numbers are assigned in array order before any simulation runs.
            int number = simulations - evaluate.Count(e => e) + 1;
            bool first = firstSimulations;
            string? failure = null;
            for (int p = 0; p < points.Length; p++)
            {
                if (!evaluate[p])
                {
                    continue;
                }

                points[p].Simulation = number++;
                if (!Simulate(points[p], allowRetry: !first) && failure is null)
                {
                    failure = "simulationError";
                }
            }

            firstSimulations = false;
            if (failure is not null)
            {
                throw new ReplayTermination(failure);
            }

            for (int p = 0; p < points.Length; p++)
            {
                if (equalTo[p] > -1)
                {
                    points[p].F = (double[])points[equalTo[p]].F.Clone();
                    points[p].Simulation = points[equalTo[p]].Simulation;
                }
            }

            return points.Select(p => p.Clone()).ToArray();
        }

        private bool Simulate(Point point, bool allowRetry)
        {
            if (!SimulateOnce(point))
            {
                if (!allowRetry)
                {
                    return false;
                }

                Retries++;
                if (!SimulateOnce(point))
                {
                    return false;
                }
            }

            cache.Put((double[])point.X.Clone(), (double[])point.F.Clone());
            return true;
        }

        private bool SimulateOnce(Point point)
        {
            int number = point.Simulation;
            if (genOptCase.Function.FailAtSimulation.Contains(number))
            {
                return false;
            }

            if (genOptCase.Function.FailOnceAtSimulation.Contains(number) && failedOnce.Add(number))
            {
                return false;
            }

            point.F = genOptCase.Function.Evaluate(point.X);
            return true;
        }

        /// <summary>Result database lookup (sorted map with the approximate Point.compareTo order).</summary>
        private double[]? Lookup(double[] x)
        {
            return cache.TryGetValue(x, out double[] f) ? f : null;
        }

        /// <summary>Point.compareTo for equal step numbers: per coordinate, approximate equality, else descending order.</summary>
        private static int ComparePoints(double[] query, double[] stored)
        {
            for (int i = 0; i < query.Length; i++)
            {
                if (!AreEqual(query[i], stored[i]))
                {
                    return query[i] > stored[i] ? -1 : 1;
                }
            }

            return 0;
        }

        private static bool Equal(double[] a, double[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (!AreEqual(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Point.areEqual: relative tolerance 1e-12; 0 matches only Â±0.</summary>
        private static bool AreEqual(double x1, double x2)
        {
            const double OneMinus = 1 - 1E-12;
            const double OnePlus = 1 + 1E-12;
            if (x1 > 0)
            {
                return OnePlus * x1 >= x2 && OneMinus * x1 <= x2;
            }

            return OneMinus * x1 >= x2 && OnePlus * x1 <= x2;
        }

        // ------------------------------------------------------------------ reporting

        private void Report(Point point, bool main, bool suppressMaxValue)
        {
            if (suppressMaxValue && point.F.All(f => f == MaxValue))
            {
                return;
            }

            Point stored = point.Clone();
            stored.Simulation = point.Simulation;
            stored.MainIteration = mainIteration;
            stored.SubIteration = subIteration;
            if (main)
            {
                mainPoints.Add(stored);
                Main.Add(Row(stored, includeSub: false));
                mainIteration++;
                subIteration = 1;
            }
            else
            {
                subPoints.Add(stored);
                All.Add(Row(stored, includeSub: true));
                subIteration++;
            }
        }

        private void ReportMinimum(string comment)
        {
            if (mainIteration <= 1)
            {
                return;
            }

            Point r = mainPoints[^1].Clone();
            double fMin = r.F[0];
            for (int j = mainPoints.Count - 1; j > -1; j--)
            {
                if (fMin > mainPoints[j].F[0])
                {
                    r = mainPoints[j].Clone();
                    fMin = r.F[0];
                }
            }

            for (int j = subPoints.Count - 1; j > -1; j--)
            {
                if (fMin > subPoints[j].F[0])
                {
                    r = subPoints[j].Clone();
                    fMin = r.F[0];
                }
            }

            r.Comment = comment;
            Main.Add(Row(r, includeSub: false));
            All.Add(Row(r, includeSub: true));
        }

        private static ListingRow Row(Point point, bool includeSub)
        {
            return new ListingRow
            {
                Simulation = point.Simulation,
                MainIteration = point.MainIteration,
                SubIteration = includeSub ? point.SubIteration : 0,
                StepNumber = 1,
                F = point.F.Select(v => v.ToString("R", CultureInfo.InvariantCulture)).ToList(),
                X = point.X.Select(v => v.ToString("R", CultureInfo.InvariantCulture)).ToList(),
                Comment = point.Comment,
            };
        }

        // ------------------------------------------------------------------ GPS (ModelGPS._run)

        private int[] directionPointer = Array.Empty<int>();
        private double delta;

        private int RunGps(bool hookeJeeves)
        {
            int[] mesh = GpsSettings();
            int divider = mesh[0];
            int initialExponent = mesh[1];
            int increment = mesh[2];
            int maxReductions = mesh[3];

            for (int i = 0; i < dim; i++)
            {
                if (initial[i] < lower[i] || initial[i] > upper[i])
                {
                    throw new ReplayTermination("algorithmError");
                }
            }

            directionPointer = new int[dim];
            int reductions = 0;
            int k = 0;
            bool iterate = true;
            int stepState = 0;
            int returnFlag = 0;
            List<Point> x = new List<Point>();
            Point[] global = Array.Empty<Point>();
            Point[] local = Array.Empty<Point>();
            int exponent = initialExponent;
            delta = 1.0 / Math.Pow(divider, initialExponent);

            do
            {
                bool runCase1 = stepState <= 1;
                bool runCase2 = stepState <= 2;
                if (stepState == 0)
                {
                    Point start = new Point { X = (double[])initial.Clone(), F = Enumerable.Repeat(MaxValue, dimF).ToArray(), Comment = "Initial point" };
                    x.Add(GetFGps(start));
                    x[k].Comment = "Initial point.";
                    Report(x[k], main: false, suppressMaxValue: true);
                    Report(x[k], main: true, suppressMaxValue: true);
                }

                bool skipToUpdate = false;
                if (runCase1)
                {
                    global = hookeJeeves ? GlobalSearchHookeJeeves(x) : Array.Empty<Point>();
                    if (global.Length != 0 && global[Lowest(global)].F[0] < x[k].F[0])
                    {
                        local = Array.Empty<Point>();
                        stepState = 3;
                        skipToUpdate = true; // 'break' out of the switch: the update runs on the next pass
                    }
                    else
                    {
                        stepState = 2;
                    }
                }

                if (skipToUpdate)
                {
                    continue;
                }

                if (runCase2)
                {
                    local = LocalSearch(x[^1]);
                }

                // case 3: parameter update
                Point[] searchSet = local.Concat(global).ToArray();
                int iLow = searchSet.Length == 0 ? -1 : Lowest(searchSet);
                if (iLow != -1 && searchSet[iLow].F[0] < x[k].F[0])
                {
                    Point next = searchSet[iLow].Clone();
                    next.Comment = iLow < local.Length ? "Local search reduced cost." : "Global search reduced cost.";
                    x.Add(next);
                    Report(next, main: false, suppressMaxValue: true);
                    Report(next, main: true, suppressMaxValue: true);
                }
                else
                {
                    if (local.Length <= dim && !MaxIterationReached())
                    {
                        throw new ReplayTermination("algorithmError");
                    }

                    Point next = x[k].Clone();
                    next.Comment = string.Empty;
                    x.Add(next);
                    exponent += increment;
                    delta = 1.0 / Math.Pow(divider, exponent);
                    if (reductions == maxReductions)
                    {
                        next.Comment = "Iteration step did not reduce cost. Maximum number of step reductions reached.";
                        Report(next, main: false, suppressMaxValue: true);
                        Report(next, main: true, suppressMaxValue: true);
                        iterate = false;
                        ReportMinimum("Minimum point.");
                        returnFlag = 1;
                    }
                    else
                    {
                        next.Comment = "Iteration step did not reduce cost. Reduce step size to '" + delta.ToString("R", CultureInfo.InvariantCulture) + "'.";
                        Report(next, main: false, suppressMaxValue: true);
                        Report(next, main: true, suppressMaxValue: true);
                        reductions++;
                    }
                }

                global = Array.Empty<Point>();
                local = Array.Empty<Point>();
                k++;
                stepState = 1;
            }
            while (iterate && !MaxIterationReached());

            if (iterate)
            {
                ReportMinimum("Current lowest point.");
                returnFlag = -1;
            }

            return returnFlag;
        }

        private int[] GpsSettings()
        {
            int Value(string key) => int.Parse(
                genOptCase.AlgorithmKeywords.Single(w => w.StartsWith(key + " ", StringComparison.Ordinal)).Split('=')[1].Trim().TrimEnd(';'),
                CultureInfo.InvariantCulture);
            return new[] { Value("MeshSizeDivider"), Value("InitialMeshSizeExponent"), Value("MeshSizeExponentIncrement"), Value("NumberOfStepReduction") };
        }

        private bool MaxIterationReached() => simulations >= genOptCase.MaxIte;

        private Point[] GlobalSearchHookeJeeves(List<Point> x)
        {
            int last = x.Count - 1;
            Point basePoint = x[last].Clone();
            basePoint.Comment = "Exploration base, Delta = " + delta.ToString("R", CultureInfo.InvariantCulture) + ".";
            if (last > 0)
            {
                for (int i = 0; i < dim; i++)
                {
                    basePoint.X[i] = (2 * x[last].X[i]) - x[last - 1].X[i];
                }

                basePoint = GetFGps(basePoint);
                Report(basePoint, main: false, suppressMaxValue: true);
            }

            return LocalSearch(basePoint);
        }

        private Point[] LocalSearch(Point start)
        {
            List<Point> points = new List<Point> { start.Clone() };
            int iLow = 0;
            for (int i = 0; i < dim; i++)
            {
                if (!MaxIterationReached())
                {
                    points.Add(Perturb(points[iLow], i));
                    if (points[^1].F[0] < points[iLow].F[0])
                    {
                        iLow = points.Count - 1;
                    }
                    else if (!MaxIterationReached())
                    {
                        points.Add(Perturb(points[iLow], i));
                        if (points[^1].F[0] < points[iLow].F[0])
                        {
                            iLow = points.Count - 1;
                        }
                    }
                }
            }

            return points.Skip(1).ToArray();
        }

        private Point Perturb(Point best, int i)
        {
            double direction = directionPointer[i] == 0 ? step[i] : -step[i];
            Point trial = best.Clone();
            trial.X[i] = best.X[i] + delta * direction;
            trial = GetFGps(trial);
            string name = genOptCase.Parameters[i].Name;
            string sign = directionPointer[i] == 0 ? "+d" : "-d";
            if (trial.F[0] < best.F[0])
            {
                trial.Comment = "Cost reduced     at " + name + sign + name + ".";
                Report(trial, main: false, suppressMaxValue: true);
                return trial;
            }

            trial.Comment = "Cost not reduced at " + name + sign + name + ".";
            Report(trial, main: false, suppressMaxValue: true);
            directionPointer[i] = directionPointer[i] == 0 ? 1 : 0;
            return best.Clone();
        }

        private static int Lowest(Point[] points)
        {
            int iLow = 0;
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].F[0] < points[iLow].F[0])
                {
                    iLow = i;
                }
            }

            return iLow;
        }

        // ------------------------------------------------------------------ golden section (IntervalDivider)

        private string RunGoldenSection()
        {
            double goldenRatio = (-1 + Math.Pow(5, 0.5)) / 2;
            int maxReductions;
            int criterion;
            double minimumDifference = 0;
            string? keyword = genOptCase.AlgorithmKeywords.SingleOrDefault();
            if (keyword is not null && keyword.StartsWith("AbsDiffFunction", StringComparison.Ordinal))
            {
                minimumDifference = JavaNumbers.ParseLikeStreamTokenizer(keyword.Split('=')[1].Trim().TrimEnd(';'));
                maxReductions = genOptCase.MaxIte > 1 ? genOptCase.MaxIte - 1 : 9;
                criterion = 1;
            }
            else if (keyword is not null && keyword.StartsWith("IntervalReduction", StringComparison.Ordinal))
            {
                double reduction = JavaNumbers.ParseLikeStreamTokenizer(keyword.Split('=')[1].Trim().TrimEnd(';'));
                int n = (int)MathF.Floor((float)((Math.Log(reduction) / Math.Log(goldenRatio)) + 1.49999) + 0.5f);
                maxReductions = n > 1 ? n - 1 : 9;
                criterion = 0;
            }
            else
            {
                maxReductions = genOptCase.MaxIte > 1 ? genOptCase.MaxIte - 1 : 9;
                criterion = 0;
            }

            Point Make(double value) => new Point { X = new[] { value }, F = new double[dimF], Comment = string.Empty };

            Point x0 = Make(lower[0]);
            Point x3 = Make(upper[0]);
            double dx = x3.X[0] - x0.X[0];
            double interval = 1.0;
            int reductionsDone = 0;
            interval *= goldenRatio;
            Point second = Make(x0.X[0] + (interval * dx));
            reductionsDone++;
            interval *= goldenRatio;
            Point firstPoint = Make(x0.X[0] + (interval * dx));

            Point[] batch = GetF(new[] { firstPoint, second });
            foreach (Point p in batch)
            {
                p.Comment = "Linesearch.";
                Report(p, main: false, suppressMaxValue: false);
            }

            Point x1 = batch[0].Clone();
            Point x2 = batch[1].Clone();
            double lowBorder = 0;
            bool terminate = false;
            bool equalBefore = false;

            bool DifferenceReached()
            {
                double fLow = x2.F[0] < x1.F[0] ? x2.F[0] : x1.F[0];
                return Math.Abs(lowBorder - fLow) < minimumDifference;
            }

            Point Evaluate(Point p)
            {
                Point r = GetF(new[] { p })[0];
                r.Comment = "Linesearch.";
                Report(r, main: false, suppressMaxValue: false);
                return r;
            }

            bool Iterate()
            {
                if (criterion == 1 && DifferenceReached())
                {
                    return false;
                }

                return reductionsDone != maxReductions;
            }

            do
            {
                reductionsDone++;
                interval *= goldenRatio;
                if (x2.F[0] < x1.F[0])
                {
                    lowBorder = x1.F[0];
                    x0 = x1.Clone();
                    x1 = x2.Clone();
                    x2 = Evaluate(Make(x3.X[0] - (interval * dx)));
                }
                else
                {
                    lowBorder = x2.F[0];
                    x3 = x2.Clone();
                    x2 = x1.Clone();
                    x1 = Evaluate(Make(x0.X[0] + (interval * dx)));
                }

                if (criterion != 1 && x1.F[0] == x2.F[0])
                {
                    if (equalBefore)
                    {
                        terminate = true;
                    }

                    equalBefore = true;
                }
                else
                {
                    equalBefore = false;
                }
            }
            while (Iterate() && !terminate);

            Point xLow;
            Point xUpp;
            if (x1.F[0] < x2.F[0])
            {
                xLow = x0;
                xUpp = x2;
            }
            else
            {
                xLow = x1;
                xUpp = x3;
            }

            int r = terminate ? -2 : (criterion == 1 && !DifferenceReached() ? -1 : 1);
            Footer.Add(xLow.X[0]);
            Footer.Add(xUpp.X[0]);
            Footer.Add((xUpp.X[0] + xLow.X[0]) / 2);
            Footer.Add(xUpp.X[0] - xLow.X[0]);
            Footer.Add((xUpp.X[0] - xLow.X[0]) / (upper[0] - lower[0]));
            return r == -2 ? "nullspace" : r == -1 ? "maxIte" : "success";
        }

        // ------------------------------------------------------------------ comparison with a golden trace

        public static List<string> Compare(GoldenTrace trace, SpecReplay replay)
        {
            List<string> differences = new List<string>();
            CompareRows("all", trace.All, replay.All, differences);
            CompareRows("main", trace.Main, replay.Main, differences);

            string expectedOutcome = OutcomeOf(trace);
            if (expectedOutcome != replay.Outcome)
            {
                differences.Add("outcome: java " + expectedOutcome + ", replay " + replay.Outcome);
            }

            int expectedRetries = trace.Termination.Count(l => l.Contains("Try to evaluate simulation a second time.", StringComparison.Ordinal));
            if (expectedRetries != replay.Retries)
            {
                differences.Add("retries: java " + expectedRetries.ToString(CultureInfo.InvariantCulture) + ", replay " + replay.Retries.ToString(CultureInfo.InvariantCulture));
            }

            List<double> footer = trace.Footer
                .Where(l => l.Contains(':', StringComparison.Ordinal) && !l.Contains("***", StringComparison.Ordinal))
                .Select(l => OutputListing.ParseJavaDouble(l.Substring(l.LastIndexOf(':') + 1).Trim()))
                .ToList();
            if (footer.Count != replay.Footer.Count || footer.Where((v, i) => !SameBits(v, replay.Footer[i])).Any())
            {
                differences.Add("footer: java [" + string.Join(", ", footer.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "], replay [" + string.Join(", ", replay.Footer.Select(v => v.ToString("R", CultureInfo.InvariantCulture))) + "]");
            }

            return differences;
        }

        private static string OutcomeOf(GoldenTrace trace)
        {
            string text = string.Join("\n", trace.Termination);
            if (text.StartsWith("Optimization completed successfully.", StringComparison.Ordinal))
            {
                return "success";
            }

            if (text.Contains("Maximum number of iteration exceeded.", StringComparison.Ordinal))
            {
                return "maxIte";
            }

            if (text.Contains("Nullspace in line search", StringComparison.Ordinal))
            {
                return "nullspace";
            }

            if (text.Contains("Following error was found", StringComparison.Ordinal))
            {
                return "simulationError";
            }

            return "algorithmError";
        }

        private static void CompareRows(string listing, List<ListingRow> expected, List<ListingRow> actual, List<string> differences)
        {
            int n = Math.Max(expected.Count, actual.Count);
            for (int i = 0; i < n; i++)
            {
                if (i >= expected.Count || i >= actual.Count)
                {
                    differences.Add(listing + " row " + i.ToString(CultureInfo.InvariantCulture) + ": count java " + expected.Count.ToString(CultureInfo.InvariantCulture) + ", replay " + actual.Count.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                ListingRow e = expected[i];
                ListingRow a = actual[i];
                bool same = e.Simulation == a.Simulation
                    && e.MainIteration == a.MainIteration
                    && e.SubIteration == a.SubIteration
                    && e.StepNumber == a.StepNumber
                    && e.F.Count == a.F.Count && e.F.Where((v, j) => !SameBits(OutputListing.ParseJavaDouble(v), OutputListing.ParseJavaDouble(a.F[j]))).Count() == 0
                    && e.X.Count == a.X.Count && e.X.Where((v, j) => !SameBits(OutputListing.ParseJavaDouble(v), OutputListing.ParseJavaDouble(a.X[j]))).Count() == 0
                    && CommentsEqual(e.Comment, a.Comment);
                if (!same)
                {
                    differences.Add(listing + " row " + i.ToString(CultureInfo.InvariantCulture) + ": java [" + Describe(e) + "] replay [" + Describe(a) + "]");
                    return;
                }
            }
        }

        private static string Describe(ListingRow row)
            => row.Simulation.ToString(CultureInfo.InvariantCulture) + " " + row.MainIteration.ToString(CultureInfo.InvariantCulture) + " " + row.SubIteration.ToString(CultureInfo.InvariantCulture)
               + " f=" + string.Join(",", row.F) + " x=" + string.Join(",", row.X) + " '" + row.Comment + "'";

        private static bool SameBits(double a, double b)
            => BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b);

        /// <summary>Equal text, with embedded numbers compared as doubles (Java and .NET print them differently).</summary>
        private static bool CommentsEqual(string expected, string actual)
        {
            const string NumberPattern = @"-?\d+(\.\d+)?(E-?\d+)?";
            if (Regex.Replace(expected, NumberPattern, "#") != Regex.Replace(actual, NumberPattern, "#"))
            {
                return false;
            }

            List<double> a = Regex.Matches(expected, NumberPattern).Select(m => OutputListing.ParseJavaDouble(m.Value)).ToList();
            List<double> b = Regex.Matches(actual, NumberPattern).Select(m => OutputListing.ParseJavaDouble(m.Value)).ToList();
            return a.Count == b.Count && a.Where((v, i) => !SameBits(v, b[i])).Count() == 0;
        }
    }
}
