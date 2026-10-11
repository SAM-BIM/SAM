// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SAM.Core.Optimisation
{
    public static partial class Convert
    {
        /// <summary>
        /// The definition's canonical text: one JSON object of schema <see cref="OptimisationDefinition.Schema"/>, with
        /// properties in a fixed order, two-space indentation, "\n" line ends and invariant numbers in their shortest
        /// round-trip form, so that reading the text gives back exactly the same values (no rounding) and writing an
        /// unchanged definition gives exactly the same text. Optional values that are not set are left out, and so are an
        /// empty constraint list and a number that is not finite. Non-ASCII text (for example "°C") is written as it is.
        /// A binding's reference and parameters are written in ordinal key order, and empty ones are left out; options
        /// keep their order.
        /// </summary>
        public static string ToJson(this OptimisationDefinition optimisationDefinition)
        {
            if (optimisationDefinition == null)
            {
                return null;
            }

            JsonTextWriter writer = new JsonTextWriter();
            writer.BeginObject();
            writer.Property("schema", OptimisationDefinition.Schema);
            writer.Property("name", optimisationDefinition.Name);
            writer.Property("description", optimisationDefinition.Description);
            writer.Property("notes", optimisationDefinition.Notes);

            if (optimisationDefinition.Model != null)
            {
                writer.BeginObject("model");
                writer.Property("engine", optimisationDefinition.Model.Engine);
                writer.Property("description", optimisationDefinition.Model.Description);
                writer.EndObject();
            }

            writer.BeginArray("variables");
            foreach (DesignVariable variable in optimisationDefinition.Variables ?? new List<DesignVariable>())
            {
                if (variable == null)
                {
                    continue;
                }

                writer.BeginObject();
                writer.Property("name", variable.Name);
                writer.Property("description", variable.Description);
                writer.Property("type", OptimisationNames.Text(variable.Type));
                writer.Property("quantity", OptimisationNames.Text(variable.Quantity));
                writer.Property("unit", variable.Unit);
                writer.Property("minimum", variable.Minimum);
                writer.Property("maximum", variable.Maximum);
                writer.Property("start", variable.Start);
                writer.Property("step", variable.Step);
                Binding(writer, "target", variable.Target);
                writer.EndObject();
            }

            writer.EndArray();

            writer.BeginArray("outputs");
            foreach (OptimisationOutput output in optimisationDefinition.Outputs ?? new List<OptimisationOutput>())
            {
                if (output == null)
                {
                    continue;
                }

                writer.BeginObject();
                writer.Property("name", output.Name);
                writer.Property("description", output.Description);
                writer.Property("quantity", OptimisationNames.Text(output.Quantity));
                writer.Property("unit", output.Unit);
                writer.Property("aggregation", output.Aggregation);
                Binding(writer, "measure", output.Measure);
                writer.EndObject();
            }

            writer.EndArray();

            if (optimisationDefinition.Objective != null)
            {
                writer.BeginObject("objective");
                writer.Property("output", optimisationDefinition.Objective.Output);
                writer.Property("sense", OptimisationNames.Text(optimisationDefinition.Objective.Sense));
                writer.EndObject();
            }

            List<OptimisationConstraint> constraints = optimisationDefinition.Constraints?.FindAll(x => x != null);
            if (constraints != null && constraints.Count != 0)
            {
                writer.BeginArray("constraints");
                foreach (OptimisationConstraint constraint in constraints)
                {
                    writer.BeginObject();
                    writer.Property("output", constraint.Output);
                    writer.Property("atMost", constraint.AtMost);
                    writer.Property("atLeast", constraint.AtLeast);
                    writer.Property("unit", constraint.Unit);
                    writer.EndObject();
                }

                writer.EndArray();
            }

            if (optimisationDefinition.Method != null)
            {
                writer.BeginObject("method");
                writer.Property("algorithm", OptimisationNames.Text(optimisationDefinition.Method.Algorithm));
                if (optimisationDefinition.Method is GoldenSectionMethod goldenSectionMethod)
                {
                    writer.Property("tolerance", goldenSectionMethod.Tolerance);
                }
                else if (optimisationDefinition.Method is HookeJeevesMethod hookeJeevesMethod)
                {
                    writer.Property("stepReductionFactor", hookeJeevesMethod.StepReductionFactor);
                    writer.Property("initialStepExponent", hookeJeevesMethod.InitialStepExponent);
                    writer.Property("stepExponentIncrement", hookeJeevesMethod.StepExponentIncrement);
                    writer.Property("stepReductions", hookeJeevesMethod.StepReductions);
                }

                writer.EndObject();
            }

            if (optimisationDefinition.Stopping?.MaximumSimulations != null)
            {
                writer.BeginObject("stopping");
                writer.Property("maximumSimulations", optimisationDefinition.Stopping.MaximumSimulations);
                writer.EndObject();
            }

            writer.EndObject();
            return writer.ToString();
        }

        /// <summary>
        /// A binding as one line of JSON, for example { "kind": "tsd.overheating-hours", "parameters": { "threshold": 28 } },
        /// with the same keys, order and numbers as <see cref="ToJson"/> writes, so it can be copied into a definition.
        /// </summary>
        internal static string ToCompactJson(OptimisationBinding optimisationBinding)
        {
            if (optimisationBinding == null)
            {
                return null;
            }

            List<string> properties = new List<string>();
            if (optimisationBinding.Kind != null)
            {
                properties.Add("\"kind\": " + ToJsonString(optimisationBinding.Kind));
            }

            List<KeyValuePair<string, string>> reference = OptimisationBinding.References(optimisationBinding);
            if (reference.Count != 0)
            {
                properties.Add("\"reference\": { " + string.Join(", ", reference.Select(x => ToJsonString(x.Key) + ": " + ToJsonString(x.Value))) + " }");
            }

            List<KeyValuePair<string, double>> parameters = Parameters(optimisationBinding);
            if (parameters.Count != 0)
            {
                properties.Add("\"parameters\": { " + string.Join(", ", parameters.Select(x => ToJsonString(x.Key) + ": " + x.Value.ToString("R", CultureInfo.InvariantCulture))) + " }");
            }

            List<string> options = (optimisationBinding as OptimisationTarget)?.Options?.FindAll(x => x != null);
            if (options != null && options.Count != 0)
            {
                properties.Add("\"options\": [ " + string.Join(", ", options.Select(ToJsonString)) + " ]");
            }

            return "{ " + string.Join(", ", properties) + " }";
        }

        /// <summary>The text as a JSON string, quoted and escaped as <see cref="ToJson"/> writes it.</summary>
        internal static string ToJsonString(string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            JsonTextWriter.Escape(stringBuilder, value ?? string.Empty);
            return stringBuilder.ToString();
        }

        /// <summary>The binding's finite parameters in ordinal key order: the ones the text holds.</summary>
        private static List<KeyValuePair<string, double>> Parameters(OptimisationBinding optimisationBinding)
        {
            // A parameter that is not finite is left out, like any other number: reading the text then uses the default.
            return (optimisationBinding.Parameters ?? new Dictionary<string, double>()).Where(x => x.Key != null && !double.IsNaN(x.Value) && !double.IsInfinity(x.Value)).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        }

        private static void Binding(JsonTextWriter writer, string name, OptimisationBinding optimisationBinding)
        {
            if (optimisationBinding == null)
            {
                return;
            }

            writer.BeginObject(name);
            writer.Property("kind", optimisationBinding.Kind);

            List<KeyValuePair<string, string>> reference = OptimisationBinding.References(optimisationBinding);
            if (reference.Count != 0)
            {
                writer.BeginObject("reference");
                foreach (KeyValuePair<string, string> keyValuePair in reference)
                {
                    writer.Property(keyValuePair.Key, keyValuePair.Value);
                }

                writer.EndObject();
            }

            List<KeyValuePair<string, double>> parameters = Parameters(optimisationBinding);
            if (parameters.Count != 0)
            {
                writer.BeginObject("parameters");
                foreach (KeyValuePair<string, double> keyValuePair in parameters)
                {
                    writer.Property(keyValuePair.Key, keyValuePair.Value);
                }

                writer.EndObject();
            }

            List<string> options = (optimisationBinding as OptimisationTarget)?.Options?.FindAll(x => x != null);
            if (options != null && options.Count != 0)
            {
                writer.BeginArray("options");
                foreach (string option in options)
                {
                    writer.Item(option);
                }

                writer.EndArray();
            }

            writer.EndObject();
        }

        /// <summary>
        /// A small pretty-printing JSON writer with the canonical layout: every object and list on its own lines, two
        /// spaces per level, "key": value with one space, an empty list as [].
        /// </summary>
        private sealed class JsonTextWriter
        {
            private readonly StringBuilder stringBuilder = new StringBuilder();
            private readonly Stack<bool> first = new Stack<bool>();
            private readonly Stack<bool> array = new Stack<bool>();

            public void BeginObject(string name = null)
            {
                Begin(name, '{', false);
            }

            public void EndObject()
            {
                End('}');
            }

            public void BeginArray(string name)
            {
                Begin(name, '[', true);
            }

            public void EndArray()
            {
                End(']');
            }

            public void Property(string name, string value)
            {
                if (value == null)
                {
                    return;
                }

                Key(name);
                String(value);
            }

            /// <summary>A text item of the current list.</summary>
            public void Item(string value)
            {
                Separator();
                String(value);
            }

            public void Property(string name, double? value)
            {
                // JSON has no NaN or infinity: such a value is left out, and reading the text reports it as missing.
                if (value == null || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
                {
                    return;
                }

                Key(name);
                // "R" is the shortest text that reads back as the same double on .NET Core 3.0 and later.
                stringBuilder.Append(value.Value.ToString("R", CultureInfo.InvariantCulture));
            }

            public void Property(string name, int? value)
            {
                if (value == null)
                {
                    return;
                }

                Key(name);
                stringBuilder.Append(value.Value.ToString(CultureInfo.InvariantCulture));
            }

            public override string ToString()
            {
                return stringBuilder.ToString() + "\n";
            }

            private void Begin(string name, char bracket, bool isArray)
            {
                if (name != null)
                {
                    Key(name);
                }
                else if (first.Count != 0)
                {
                    Separator();
                }

                stringBuilder.Append(bracket);
                first.Push(true);
                array.Push(isArray);
            }

            private void End(char bracket)
            {
                bool empty = first.Pop();
                array.Pop();
                if (!empty)
                {
                    NewLine();
                }

                stringBuilder.Append(bracket);
            }

            private void Key(string name)
            {
                Separator();
                String(name);
                stringBuilder.Append(": ");
            }

            private void Separator()
            {
                if (!first.Peek())
                {
                    stringBuilder.Append(',');
                }

                first.Pop();
                first.Push(false);
                NewLine();
            }

            private void NewLine()
            {
                stringBuilder.Append('\n').Append(' ', first.Count * 2);
            }

            private void String(string value)
            {
                Escape(stringBuilder, value);
            }

            /// <summary>Appends <paramref name="value"/> as a quoted JSON string: control characters escaped, other text as it is.</summary>
            internal static void Escape(StringBuilder stringBuilder, string value)
            {
                stringBuilder.Append('"');
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"':
                            stringBuilder.Append("\\\"");
                            break;
                        case '\\':
                            stringBuilder.Append("\\\\");
                            break;
                        case '\n':
                            stringBuilder.Append("\\n");
                            break;
                        case '\r':
                            stringBuilder.Append("\\r");
                            break;
                        case '\t':
                            stringBuilder.Append("\\t");
                            break;
                        default:
                            if (c < 0x20 || c == (char)0x2028 || c == (char)0x2029)
                            {
                                stringBuilder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                stringBuilder.Append(c);
                            }

                            break;
                    }
                }

                stringBuilder.Append('"');
            }
        }
    }
}
