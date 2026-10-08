// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
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
