// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The definition text's vocabulary: property names per object, and the text of each enum value. The reader, the
    /// writer, the schema file and the AI exchange text all use these tables, so they cannot drift apart.
    /// </summary>
    internal static class OptimisationNames
    {
        // Property names per object, in canonical (written) order.
        internal static readonly string[] Definition = { "schema", "name", "description", "notes", "model", "variables", "outputs", "objective", "constraints", "method", "stopping" };
        internal static readonly string[] Model = { "engine", "description" };
        internal static readonly string[] Variable = { "name", "description", "type", "quantity", "unit", "minimum", "maximum", "start", "step", "target" };
        internal static readonly string[] Output = { "name", "description", "quantity", "unit", "aggregation", "measure" };
        internal static readonly string[] Target = { "kind", "reference", "parameters", "options" };
        internal static readonly string[] Measure = { "kind", "reference", "parameters" };
        internal static readonly string[] Objective = { "output", "sense" };
        internal static readonly string[] Constraint = { "output", "atMost", "atLeast", "unit" };
        internal static readonly string[] GoldenSection = { "algorithm", "tolerance" };
        internal static readonly string[] HookeJeeves = { "algorithm", "stepReductionFactor", "initialStepExponent", "stepExponentIncrement", "stepReductions" };
        internal static readonly string[] Stopping = { "maximumSimulations" };

        /// <summary>Every object of the schema by its name in the JSON Schema file's "$defs" (the root is "definition").</summary>
        internal static readonly IReadOnlyDictionary<string, string[]> Objects = new Dictionary<string, string[]>()
        {
            { "definition", Definition },
            { "model", Model },
            { "variable", Variable },
            { "output", Output },
            { "target", Target },
            { "measure", Measure },
            { "objective", Objective },
            { "constraint", Constraint },
            { "goldenSection", GoldenSection },
            { "hookeJeeves", HookeJeeves },
            { "stopping", Stopping },
        };

        private static readonly Dictionary<ObjectiveSense, string> senses = new Dictionary<ObjectiveSense, string>()
        {
            { ObjectiveSense.Minimise, "minimise" },
            { ObjectiveSense.Maximise, "maximise" },
        };

        private static readonly Dictionary<OptimisationAlgorithm, string> algorithms = new Dictionary<OptimisationAlgorithm, string>()
        {
            { OptimisationAlgorithm.GoldenSection, "golden-section" },
            { OptimisationAlgorithm.HookeJeeves, "hooke-jeeves" },
        };

        private static readonly Dictionary<DesignVariableType, string> variableTypes = new Dictionary<DesignVariableType, string>()
        {
            { DesignVariableType.Continuous, "continuous" },
            { DesignVariableType.Integer, "integer" },
            { DesignVariableType.Discrete, "discrete" },
        };

        private static readonly Dictionary<OptimisationQuantity, string> quantities = new Dictionary<OptimisationQuantity, string>()
        {
            { OptimisationQuantity.Dimensionless, "dimensionless" },
            { OptimisationQuantity.Temperature, "temperature" },
            { OptimisationQuantity.TemperatureDifference, "temperature-difference" },
            { OptimisationQuantity.Percent, "percent" },
            { OptimisationQuantity.Time, "time" },
            { OptimisationQuantity.Power, "power" },
            { OptimisationQuantity.Energy, "energy" },
            { OptimisationQuantity.Mass, "mass" },
            { OptimisationQuantity.Carbon, "carbon" },
            { OptimisationQuantity.Currency, "currency" },
            { OptimisationQuantity.Angle, "angle" },
            { OptimisationQuantity.Length, "length" },
            { OptimisationQuantity.Area, "area" },
        };

        internal static string Text(ObjectiveSense value) => senses[value];

        internal static string Text(OptimisationAlgorithm value) => algorithms[value];

        internal static string Text(DesignVariableType value) => variableTypes[value];

        /// <summary>The text of a declared quantity; null for <see cref="OptimisationQuantity.Unspecified"/>, which is never written.</summary>
        internal static string Text(OptimisationQuantity value) => quantities.TryGetValue(value, out string text) ? text : null;

        internal static IReadOnlyList<string> Texts<T>() where T : struct, Enum
        {
            return Map<T>().Values.ToList();
        }

        /// <summary>
        /// Reads an enum value from its text. An exact match is <paramref name="exact"/>; a match that differs only in
        /// case, spaces, underscores or hyphens (for example "Golden Section" or "goldenSection") is accepted with
        /// <paramref name="exact"/> false so the reader can say it was normalised.
        /// </summary>
        internal static bool TryParse<T>(string text, out T value, out bool exact) where T : struct, Enum
        {
            value = default;
            exact = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            Dictionary<T, string> map = Map<T>();
            foreach (KeyValuePair<T, string> keyValuePair in map)
            {
                if (keyValuePair.Value == text)
                {
                    value = keyValuePair.Key;
                    exact = true;
                    return true;
                }
            }

            string key = Key(text);
            foreach (KeyValuePair<T, string> keyValuePair in map)
            {
                if (Key(keyValuePair.Value) == key)
                {
                    value = keyValuePair.Key;
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<T, string> Map<T>() where T : struct, Enum
        {
            object result = null;
            if (typeof(T) == typeof(ObjectiveSense))
            {
                result = senses;
            }
            else if (typeof(T) == typeof(OptimisationAlgorithm))
            {
                result = algorithms;
            }
            else if (typeof(T) == typeof(DesignVariableType))
            {
                result = variableTypes;
            }
            else if (typeof(T) == typeof(OptimisationQuantity))
            {
                result = quantities;
            }

            return (Dictionary<T, string>)result ?? throw new NotSupportedException(typeof(T).Name);
        }

        private static string Key(string text)
        {
            return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }
    }
}
