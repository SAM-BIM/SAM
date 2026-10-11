// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// The synthetic experiments of the GenOpt 3.1.1 oracle (documentation/GenOpt-3.1.1-Behaviour.md).
    /// Every case is deterministic, uses only synthetic functions and contains no machine-specific data.
    /// </summary>
    internal static class CaseCatalogue
    {
        private static readonly string[] GpsDefault =
        {
            "MeshSizeDivider = 2;",
            "InitialMeshSizeExponent = 0;",
            "MeshSizeExponentIncrement = 1;",
            "NumberOfStepReduction = 4;",
        };

        public static IReadOnlyList<GenOptCase> All()
        {
            List<GenOptCase> cases = new List<GenOptCase>();

            // E1 - full GPS Hooke-Jeeves traces: direction memory and pattern sequencing.
            cases.Add(Gps("e1-hj-quad-1d", "1-D shifted quadratic, Tas-like bounds and step.",
                new[] { P("x1", "3", "-5", "35", "1") },
                Quadratic(new[] { 0.3 })));
            cases.Add(Gps("e1-hj-quad-2d", "2-D shifted quadratic from GenOpt's own quad example settings.",
                new[] { P("x1", "0.9", "-1", "1", "0.05"), P("x2", "0.9", "-1", "1", "0.05") },
                Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })));
            cases.Add(Gps("e1-hj-quad-3d", "3-D weighted quadratic, unequal steps, long pattern moves.",
                new[] { P("x1", "5", "-20", "20", "1"), P("x2", "-3", "-20", "20", "0.5"), P("x3", "2", "-20", "20", "0.25") },
                Quadratic(new[] { 1.3, -0.7, 0.2 }, new[] { 1.0, 3.0, 0.5 })));

            // E2 - bounds.
            cases.Add(Gps("e2-optimum-on-bound", "Linear objective, optimum at the lower bounds.",
                new[] { P("x1", "3", "0", "10", "1"), P("x2", "4", "0", "10", "1") },
                Linear(new[] { 1.0, 2.0 })));
            cases.Add(Gps("e2-start-on-bound", "Start on the upper bound, optimum interior.",
                new[] { P("x1", "10", "0", "10", "1"), P("x2", "0", "0", "10", "1") },
                Quadratic(new[] { 6.2, 3.7 })));
            cases.Add(Gps("e2-infeasible-pattern", "Pattern point beyond the upper bound, then exploration from it.",
                new[] { P("x1", "0", "0", "10", "3"), P("x2", "0", "-1", "1", "1") },
                Linear(new[] { -1.0, 0.0 })));

            // E3 / E4 - flat objective; MaxEqualResults.
            cases.Add(Gps("e3-flat", "Constant objective: every trial is a tie; ties never move.",
                new[] { P("x1", "1", "-5", "5", "1"), P("x2", "-1", "-5", "5", "1") },
                new FunctionSpec { Kind = "constant", Offset = 4.5 }));
            GenOptCase maxEqual = Gps("e4-flat-maxequalresults-2", "Constant objective with MaxEqualResults = 2 (lowest allowed).",
                new[] { P("x1", "1", "-5", "5", "1"), P("x2", "-1", "-5", "5", "1") },
                new FunctionSpec { Kind = "constant", Offset = 4.5 });
            maxEqual.MaxEqualResults = 2;
            cases.Add(maxEqual);

            // E5 - MaxIte cuts a search.
            GenOptCase maxIte = Gps("e5-maxite-7", "MaxIte = 7 stops the search mid-exploration.",
                new[] { P("x1", "0.9", "-1", "1", "0.05"), P("x2", "0.9", "-1", "1", "0.05") },
                Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 }));
            maxIte.MaxIte = 7;
            cases.Add(maxIte);

            // E7 - simulation failures.
            cases.Add(WithFailures(Gps("e7-fail-first", "Simulation 1 always fails.", TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })), atSimulation: new[] { 1 }));
            cases.Add(WithFailures(Gps("e7-fail-once-first", "Simulation 1 fails once (no retry for the first evaluation batch is expected).", TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })), onceAtSimulation: new[] { 1 }));
            cases.Add(WithFailures(Gps("e7-fail-later", "Simulation 4 always fails.", TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })), atSimulation: new[] { 4 }));
            cases.Add(WithFailures(Gps("e7-fail-once-later", "Simulation 4 fails once; the retry succeeds.", TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })), onceAtSimulation: new[] { 4 }));

            // E8 - unusual steps.
            cases.Add(Gps("e8-step-zero", "x2 has Step = 0.",
                new[] { P("x1", "0.9", "-1", "1", "0.05"), P("x2", "0.9", "-1", "1", "0") },
                Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })));
            cases.Add(Gps("e8-step-negative", "x1 has a negative Step.",
                new[] { P("x1", "0.9", "-1", "1", "-0.05"), P("x2", "0.9", "-1", "1", "0.05") },
                Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 })));

            // E9 - mesh parameters and the step-reduction boundary.
            cases.Add(Gps("e9-mesh-3-1-2-1", "MeshSizeDivider 3, InitialMeshSizeExponent 1, MeshSizeExponentIncrement 2, NumberOfStepReduction 1.",
                TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 }),
                new[] { "MeshSizeDivider = 3;", "InitialMeshSizeExponent = 1;", "MeshSizeExponentIncrement = 2;", "NumberOfStepReduction = 1;" }));
            cases.Add(Gps("e9-step-reductions-1", "NumberOfStepReduction = 1 on a 1-D problem.",
                new[] { P("x1", "3", "-5", "35", "1") }, Quadratic(new[] { 0.3 }),
                new[] { "MeshSizeDivider = 2;", "InitialMeshSizeExponent = 0;", "MeshSizeExponentIncrement = 1;", "NumberOfStepReduction = 1;" }));
            cases.Add(Gps("e9-step-reductions-2", "NumberOfStepReduction = 2 on a 1-D problem.",
                new[] { P("x1", "3", "-5", "35", "1") }, Quadratic(new[] { 0.3 }),
                new[] { "MeshSizeDivider = 2;", "InitialMeshSizeExponent = 0;", "MeshSizeExponentIncrement = 1;", "NumberOfStepReduction = 2;" }));

            // E10 / E16 - golden section.
            cases.Add(Golden("e10-gs-absdiff", "GoldenSection with AbsDiffFunction (structure of the Tas Systems Demo run, synthetic objective).",
                P("x1", "3", "-5", "35", "1"), Quadratic(new[] { 5.13 }, new[] { 3.0 }, 7360), "AbsDiffFunction = 1;"));
            cases.Add(Golden("e10-gs-intervalreduction", "GoldenSection with IntervalReduction = 0.01.",
                P("x1", "3", "-5", "35", "1"), Quadratic(new[] { 5.13 }, new[] { 3.0 }, 7360), "IntervalReduction = 0.01;"));
            GenOptCase noKeyword = Golden("e10-gs-no-keyword", "GoldenSection without a stopping keyword (MaxIte = 12).",
                P("x1", "3", "-5", "35", "1"), Quadratic(new[] { 5.13 }, new[] { 3.0 }, 7360), null);
            noKeyword.MaxIte = 12;
            cases.Add(noKeyword);
            cases.Add(Golden("e10-gs-nullspace", "GoldenSection on a flat objective (IntervalReduction mode, nullspace expected).",
                P("x1", "3", "-5", "35", "1"), new FunctionSpec { Kind = "constant", Offset = 2 }, "IntervalReduction = 0.0001;"));

            // E11 - unbounded parameters.
            cases.Add(Gps("e11-unbounded", "No Min/Max (SMALL/BIG).",
                new[] { P("x1", "4", null, null, "1"), P("x2", "-2.5", null, null, "0.5") },
                Quadratic(new[] { -7.3, 12.1 })));

            // E12 - multiple outputs: only the first drives the search.
            FunctionSpec multi = Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 });
            multi.Outputs = new List<string> { "Result", "Cost", "CO2" };
            cases.Add(Gps("e12-multi-output", "Three outputs; Result is minimised, the others are recorded.", TwoD(), multi));

            // E13 - coordinate search.
            GenOptCase coordinate = Gps("e13-coordinate-search", "GPSCoordinateSearch on the 2-D quadratic.", TwoD(), Quadratic(new[] { 0.1, -0.37 }, new[] { 1.0, 2.0 }));
            coordinate.Algorithm = "GPSCoordinateSearch";
            cases.Add(coordinate);

            // E-C - result database / cache.
            cases.Add(Gps("ec-float-equal-points", "Step 0.1 from 0.2: 0.2+0.1 and 0.4-0.1 differ as doubles but round to the same float decimal.",
                new[] { P("x1", "0.2", "-1", "2", "0.1") }, Quadratic(new[] { 0.73 })));
            cases.Add(Gps("ec-cross-zero", "Coordinates step through zero from positive and negative sides.",
                new[] { P("x1", "0.3", "-1", "1", "0.1"), P("x2", "-0.2", "-1", "1", "0.1") },
                Quadratic(new[] { -0.05, 0.04 })));
            cases.Add(Gps("ec-negative-zero-start", "Initial value -0 (signed zero).",
                new[] { P("x1", "-0", "-1", "1", "0.25") }, Quadratic(new[] { 0.6 })));
            cases.Add(Gps("ec-tiny-values", "Coordinates near 1e-30 (scientific notation, float subnormal range not reached).",
                new[] { P("x1", "3E-30", "-1E-28", "1E-28", "1E-30") }, Quadratic(new[] { -2.2e-30 }, new[] { 1e60 })));
            cases.Add(Gps("ec-plateau", "Quantised objective: equal values, repeated pattern points, cache hits.",
                new[] { P("x1", "0", "-10", "10", "1"), P("x2", "0", "-10", "10", "1") },
                new FunctionSpec { Kind = "quantisedQuadratic", Center = new[] { 3.4, -2.6 }, Weight = new[] { 1.0, 1.0 }, Quantum = 2 }));
            GenOptCase collapse = Golden("ec-gs-collapse", "GoldenSection run until interior points are closer than 1e-12 relative (approximate point equality).",
                P("x1", "1.5", "1", "2", "1"), Quadratic(new[] { 1.37 }), "AbsDiffFunction = 0.0000000000000000000000000000001;");
            collapse.MaxIte = 90;
            cases.Add(collapse);

            return cases;
        }

        private static ParameterSpec[] TwoD() => new[] { P("x1", "0.9", "-1", "1", "0.05"), P("x2", "0.9", "-1", "1", "0.05") };

        private static ParameterSpec P(string name, string ini, string? min, string? max, string step)
            => new ParameterSpec { Name = name, Ini = ini, Min = min, Max = max, Step = step };

        private static FunctionSpec Quadratic(double[] center, double[]? weight = null, double offset = 0)
            => new FunctionSpec { Kind = "quadratic", Center = center, Weight = weight ?? Array.Empty<double>(), Offset = offset };

        private static FunctionSpec Linear(double[] weight)
            => new FunctionSpec { Kind = "linear", Weight = weight };

        private static GenOptCase Gps(string name, string purpose, ParameterSpec[] parameters, FunctionSpec function, string[]? keywords = null)
            => new GenOptCase
            {
                Name = name,
                Purpose = purpose,
                Parameters = parameters.ToList(),
                Algorithm = "GPSHookeJeeves",
                AlgorithmKeywords = (keywords ?? GpsDefault).ToList(),
                Function = function,
            };

        private static GenOptCase Golden(string name, string purpose, ParameterSpec parameter, FunctionSpec function, string? keyword)
            => new GenOptCase
            {
                Name = name,
                Purpose = purpose,
                Parameters = new List<ParameterSpec> { parameter },
                Algorithm = "GoldenSection",
                AlgorithmKeywords = keyword is null ? new List<string>() : new List<string> { keyword },
                Function = function,
            };

        private static GenOptCase WithFailures(GenOptCase genOptCase, int[]? atSimulation = null, int[]? onceAtSimulation = null)
        {
            genOptCase.Function.FailAtSimulation = (atSimulation ?? Array.Empty<int>()).ToList();
            genOptCase.Function.FailOnceAtSimulation = (onceAtSimulation ?? Array.Empty<int>()).ToList();
            return genOptCase;
        }
    }
}
