// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SAM.Tests.GenOptOracle
{
    /// <summary>
    /// A deterministic synthetic objective. The first output is the objective GenOpt minimises;
    /// further outputs are recorded only (output k &gt; 0 is k times the sum of the coordinates).
    /// The same definition is used by the fake simulator (Java side) and by the native replay
    /// tests (C# side), so both sides evaluate identical IEEE double expressions.
    /// </summary>
    public sealed class FunctionSpec
    {
        /// <summary>constant | quadratic | quantisedQuadratic | linear</summary>
        public string Kind { get; set; } = "quadratic";

        public double[] Center { get; set; } = Array.Empty<double>();

        public double[] Weight { get; set; } = Array.Empty<double>();

        public double Offset { get; set; }

        /// <summary>Step of the floor applied by quantisedQuadratic (creates equal-value regions).</summary>
        public double Quantum { get; set; } = 1;

        public List<string> Outputs { get; set; } = new List<string> { "f" };

        public List<int> FailAtSimulation { get; set; } = new List<int>();

        public List<int> FailOnceAtSimulation { get; set; } = new List<int>();

        public double[] Evaluate(IReadOnlyList<double> x)
        {
            double f = Kind switch
            {
                "constant" => Offset,
                "quadratic" => Quadratic(x),
                "quantisedQuadratic" => Math.Floor(Quadratic(x) / Quantum) * Quantum,
                "linear" => Linear(x),
                _ => throw new InvalidOperationException("Unknown function kind '" + Kind + "'."),
            };

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
                double d = x[i] - CenterAt(i);
                f += WeightAt(i) * d * d;
            }

            return f;
        }

        private double Linear(IReadOnlyList<double> x)
        {
            double f = Offset;
            for (int i = 0; i < x.Count; i++)
            {
                f += WeightAt(i) * x[i];
            }

            return f;
        }

        private double CenterAt(int i) => i < Center.Length ? Center[i] : 0;

        private double WeightAt(int i) => i < Weight.Length ? Weight[i] : 1;
    }

    internal static class Json
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
    }
}
