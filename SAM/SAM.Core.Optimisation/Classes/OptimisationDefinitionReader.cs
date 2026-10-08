// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// Binds the parsed definition text to an <see cref="OptimisationDefinition"/>, strictly: every OPT1xx structure
    /// finding is reported with its place, and the place of every value is remembered (by JSON path) so that later
    /// findings about the definition can point at the text too.
    /// </summary>
    internal sealed class OptimisationDefinitionReader
    {
        private readonly List<OptimisationDiagnostic> diagnostics;
        private readonly Dictionary<string, KeyValuePair<int, int>> positions = new Dictionary<string, KeyValuePair<int, int>>(StringComparer.Ordinal);

        public OptimisationDefinitionReader(List<OptimisationDiagnostic> diagnostics)
        {
            this.diagnostics = diagnostics;
        }

        public OptimisationDefinition Read(OptimisationJsonNode node)
        {
            const string path = "$";

            Remember(path, node);

            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Definition);

            if (!Schema(properties, node))
            {
                return null;
            }

            OptimisationDefinition result = new OptimisationDefinition()
            {
                Name = Text(properties, "name", path, false),
                Description = Text(properties, "description", path, false),
                Notes = Text(properties, "notes", path, false),
            };

            OptimisationJsonNode node_Model = Object(properties, "model", path, node, true);
            if (node_Model != null)
            {
                result.Model = Model(node_Model, path + ".model");
            }

            result.Variables = List(properties, "variables", path, node, true, Variable);
            result.Outputs = List(properties, "outputs", path, node, true, Output);

            OptimisationJsonNode node_Objective = Object(properties, "objective", path, node, true);
            if (node_Objective != null)
            {
                result.Objective = Objective(node_Objective, path + ".objective");
            }

            result.Constraints = List(properties, "constraints", path, node, false, Constraint);

            OptimisationJsonNode node_Method = Object(properties, "method", path, node, true);
            if (node_Method != null)
            {
                result.Method = Method(node_Method, path + ".method");
            }

            OptimisationJsonNode node_Stopping = Object(properties, "stopping", path, node, false);
            if (node_Stopping != null)
            {
                string path_Stopping = path + ".stopping";
                Dictionary<string, OptimisationJsonProperty> properties_Stopping = Properties(node_Stopping, path_Stopping, OptimisationNames.Stopping);
                result.Stopping = new StoppingCriteria(Integer(properties_Stopping, "maximumSimulations", path_Stopping, node_Stopping, false));
            }

            return result;
        }

        /// <summary>The same diagnostic with the line and column of its path (or of the nearest enclosing value).</summary>
        public OptimisationDiagnostic Locate(OptimisationDiagnostic optimisationDiagnostic)
        {
            string path = optimisationDiagnostic.Path ?? "$";
            while (true)
            {
                if (positions.TryGetValue(path, out KeyValuePair<int, int> position))
                {
                    return new OptimisationDiagnostic(optimisationDiagnostic, position.Key, position.Value);
                }

                int index = System.Math.Max(path.LastIndexOf('.'), path.LastIndexOf('['));
                if (index <= 0)
                {
                    return optimisationDiagnostic;
                }

                path = path.Substring(0, index);
            }
        }

        private bool Schema(Dictionary<string, OptimisationJsonProperty> properties, OptimisationJsonNode node)
        {
            const string path = "$.schema";
            string hint = "Use \"schema\": \"" + OptimisationDefinition.Schema + "\".";

            if (!properties.TryGetValue("schema", out OptimisationJsonProperty property) || property.Value.Kind == OptimisationJsonKind.Null)
            {
                Error("OPT102", "$", "The definition does not say which schema it follows (\"schema\" is missing).", hint, node);
                return false;
            }

            if (property.Value.Kind != OptimisationJsonKind.String)
            {
                Error("OPT107", path, "\"schema\" must be text, not " + property.Value.KindText + ".", hint, property.Value);
                return false;
            }

            string text = property.Value.Text?.Trim() ?? string.Empty;
            int index = text.IndexOf('/');
            string name = index < 0 ? text : text.Substring(0, index);
            if (index < 0 || !string.Equals(name, OptimisationDefinition.SchemaName, StringComparison.OrdinalIgnoreCase) || !int.TryParse(text.Substring(index + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int version) || version < 1)
            {
                Error("OPT103", path, "\"" + text + "\" is not a SAM optimisation definition schema.", hint, property.Value);
                return false;
            }

            if (version > OptimisationDefinition.SchemaVersion)
            {
                Error("OPT104", path, string.Format(CultureInfo.InvariantCulture, "The definition uses schema version {0}, which is newer than this version of SAM reads ({1}).", version, OptimisationDefinition.SchemaVersion), "Update SAM, or rewrite the definition for " + OptimisationDefinition.Schema + ".", property.Value);
                return false;
            }

            return true;
        }

        private OptimisationModel Model(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Model);
            return new OptimisationModel(Text(properties, "engine", path, true, node), Text(properties, "description", path, false));
        }

        private DesignVariable Variable(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Variable);

            DesignVariable result = new DesignVariable()
            {
                Name = Text(properties, "name", path, true, node),
                Description = Text(properties, "description", path, false),
                Unit = Unit(properties, path),
                Start = Number(properties, "start", path, node, false),
                Step = Number(properties, "step", path, node, false),
            };

            result.Type = Enum(properties, "type", path, DesignVariableType.Continuous);
            result.Quantity = Enum(properties, "quantity", path, OptimisationQuantity.Unspecified);
            result.Minimum = Number(properties, "minimum", path, node, true) ?? double.NaN;
            result.Maximum = Number(properties, "maximum", path, node, true) ?? double.NaN;

            OptimisationJsonNode node_Target = Object(properties, "target", path, node, false);
            if (node_Target != null)
            {
                string path_Target = path + ".target";
                Dictionary<string, OptimisationJsonProperty> properties_Target = Properties(node_Target, path_Target, OptimisationNames.Target);
                result.Target = new OptimisationTarget()
                {
                    Kind = Text(properties_Target, "kind", path_Target, true, node_Target),
                    Reference = Reference(properties_Target, path_Target),
                    Parameters = Parameters(properties_Target, path_Target),
                    Options = Options(properties_Target, path_Target),
                };
            }

            return result;
        }

        private OptimisationOutput Output(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Output);

            OptimisationOutput result = new OptimisationOutput()
            {
                Name = Text(properties, "name", path, true, node),
                Description = Text(properties, "description", path, false),
                Quantity = Enum(properties, "quantity", path, OptimisationQuantity.Unspecified),
                Unit = Unit(properties, path),
                Aggregation = Text(properties, "aggregation", path, false),
            };

            OptimisationJsonNode node_Measure = Object(properties, "measure", path, node, false);
            if (node_Measure != null)
            {
                string path_Measure = path + ".measure";
                Dictionary<string, OptimisationJsonProperty> properties_Measure = Properties(node_Measure, path_Measure, OptimisationNames.Measure);
                result.Measure = new OptimisationMeasure()
                {
                    Kind = Text(properties_Measure, "kind", path_Measure, true, node_Measure),
                    Reference = Reference(properties_Measure, path_Measure),
                    Parameters = Parameters(properties_Measure, path_Measure),
                };
            }

            return result;
        }

        private OptimisationObjective Objective(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Objective);

            OptimisationObjective result = new OptimisationObjective() { Output = Text(properties, "output", path, true, node) };
            if (!properties.ContainsKey("sense") || properties["sense"].Value.Kind == OptimisationJsonKind.Null)
            {
                Error("OPT110", path, "The objective does not say whether to minimise or maximise (\"sense\" is missing).", "Add \"sense\": \"minimise\" or \"maximise\".", node);
            }
            else
            {
                result.Sense = Enum(properties, "sense", path, ObjectiveSense.Minimise);
            }

            return result;
        }

        private OptimisationConstraint Constraint(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Constraint);

            return new OptimisationConstraint()
            {
                Output = Text(properties, "output", path, true, node),
                AtMost = Number(properties, "atMost", path, node, false),
                AtLeast = Number(properties, "atLeast", path, node, false),
                Unit = Unit(properties, path),
            };
        }

        private OptimisationMethod Method(OptimisationJsonNode node, string path)
        {
            if (node.Kind != OptimisationJsonKind.Object)
            {
                return null;
            }

            OptimisationJsonProperty property = node.Properties.Find(x => x.Name == "algorithm");
            if (property == null || property.Value.Kind == OptimisationJsonKind.Null)
            {
                Error("OPT110", path, "The method does not say which algorithm to use (\"algorithm\" is missing).", "Add \"algorithm\": " + string.Join(" or ", OptimisationNames.Texts<OptimisationAlgorithm>().Select(x => "\"" + x + "\"")) + ".", node);
                Properties(node, path, OptimisationNames.Methods.Values.SelectMany(x => x).Distinct().ToArray());
                return null;
            }

            Dictionary<string, OptimisationJsonProperty> properties_Algorithm = new Dictionary<string, OptimisationJsonProperty>() { { "algorithm", property } };
            if (!TryEnum(properties_Algorithm, "algorithm", path, out OptimisationAlgorithm optimisationAlgorithm))
            {
                return null;
            }

            Dictionary<string, OptimisationJsonProperty> properties = Properties(node, path, OptimisationNames.Methods[optimisationAlgorithm], optimisationAlgorithm);
            switch (optimisationAlgorithm)
            {
                case OptimisationAlgorithm.GoldenSection:
                    return new GoldenSectionMethod() { Tolerance = Number(properties, "tolerance", path, node, false) };

                case OptimisationAlgorithm.HookeJeeves:
                    return new HookeJeevesMethod()
                    {
                        StepReductionFactor = Integer(properties, "stepReductionFactor", path, node, false),
                        InitialStepExponent = Integer(properties, "initialStepExponent", path, node, false),
                        StepExponentIncrement = Integer(properties, "stepExponentIncrement", path, node, false),
                        StepReductions = Integer(properties, "stepReductions", path, node, false),
                    };

                default:
                    return new TryEveryOptionMethod();
            }
        }

        /// <summary>
        /// The object's properties by name. Unknown properties (OPT105, with a suggestion) and repeated ones (OPT106)
        /// are reported; the first of a repeated property is kept. In a "method" object (<paramref name="algorithm"/>
        /// given), a property of another method is reported as belonging to it.
        /// </summary>
        private Dictionary<string, OptimisationJsonProperty> Properties(OptimisationJsonNode node, string path, string[] names, OptimisationAlgorithm? algorithm = null)
        {
            Dictionary<string, OptimisationJsonProperty> result = new Dictionary<string, OptimisationJsonProperty>(StringComparer.Ordinal);
            if (node == null || node.Kind != OptimisationJsonKind.Object)
            {
                return result;
            }

            foreach (OptimisationJsonProperty property in node.Properties)
            {
                string path_Property = path + "." + property.Name;

                if (result.ContainsKey(property.Name))
                {
                    Error("OPT106", path_Property, "\"" + property.Name + "\" is given more than once.", "Keep one of them.", property);
                    continue;
                }

                if (!names.Contains(property.Name))
                {
                    string other = algorithm == null ? null : OptimisationNames.Methods.Where(x => x.Key != algorithm.Value && x.Value.Contains(property.Name)).Select(x => OptimisationNames.Text(x.Key)).FirstOrDefault();
                    if (other != null)
                    {
                        Error("OPT105", path_Property, "\"" + property.Name + "\" is a setting of the " + other + " method, not of this one.", "Remove it, or change \"algorithm\".", property);
                        continue;
                    }

                    string suggestion = Suggestion(property.Name, names);
                    Error("OPT105", path_Property, "\"" + property.Name + "\" is not a known field here.", suggestion == null ? "Allowed: " + string.Join(", ", names) + "." : "Did you mean \"" + suggestion + "\"?", property);
                    continue;
                }

                result[property.Name] = property;
                Remember(path_Property, property);
            }

            return result;
        }

        /// <summary>
        /// A binding's "reference": an object whose keys the engine defines (checked later against its capabilities) and
        /// whose values are model item names (text). A key given as null counts as absent.
        /// </summary>
        private Dictionary<string, string> Reference(Dictionary<string, OptimisationJsonProperty> properties, string path)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);

            OptimisationJsonNode value = Object(properties, "reference", path, null, false);
            if (value == null)
            {
                return result;
            }

            string path_Reference = path + ".reference";
            Dictionary<string, OptimisationJsonProperty> properties_Reference = Map(value, path_Reference);
            foreach (string name in properties_Reference.Keys)
            {
                string text = Text(properties_Reference, name, path_Reference, false);
                if (text != null)
                {
                    result[name] = text;
                }
            }

            return result;
        }

        /// <summary>
        /// A binding's "parameters": an object whose keys the engine defines (checked later against its capabilities) and
        /// whose values are numbers. A key given as null counts as absent (the engine's default).
        /// </summary>
        private Dictionary<string, double> Parameters(Dictionary<string, OptimisationJsonProperty> properties, string path)
        {
            Dictionary<string, double> result = new Dictionary<string, double>(StringComparer.Ordinal);

            OptimisationJsonNode value = Object(properties, "parameters", path, null, false);
            if (value == null)
            {
                return result;
            }

            string path_Parameters = path + ".parameters";
            Dictionary<string, OptimisationJsonProperty> properties_Parameters = Map(value, path_Parameters);
            foreach (string name in properties_Parameters.Keys)
            {
                double? number = Number(properties_Parameters, name, path_Parameters, value, false);
                if (number != null)
                {
                    result[name] = number.Value;
                }
            }

            return result;
        }

        /// <summary>A choice target's "options": a list of model item names (text), in order.</summary>
        private List<string> Options(Dictionary<string, OptimisationJsonProperty> properties, string path)
        {
            List<string> result = new List<string>();

            OptimisationJsonNode value = Value(properties, "options", path, null, false);
            if (value == null)
            {
                return result;
            }

            string path_Options = path + ".options";
            if (value.Kind != OptimisationJsonKind.Array)
            {
                Error("OPT107", path_Options, "\"options\" must be a list [ … ] of names, not " + value.KindText + ".", null, value);
                return result;
            }

            for (int i = 0; i < value.Items.Count; i++)
            {
                OptimisationJsonNode item = value.Items[i];
                string path_Item = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", path_Options, i);
                Remember(path_Item, item);

                if (item.Kind != OptimisationJsonKind.String)
                {
                    Error("OPT107", path_Item, string.Format(CultureInfo.InvariantCulture, "Option {0} must be a name in quotes, not {1}.", i + 1, item.KindText), null, item);
                    continue;
                }

                result.Add(item.Text);
            }

            return result;
        }

        /// <summary>
        /// The properties of an object whose keys the engine defines (a reference or parameters): any key is accepted
        /// here (the engine's capabilities check it), and a repeated one is reported (OPT106); the first is kept.
        /// </summary>
        private Dictionary<string, OptimisationJsonProperty> Map(OptimisationJsonNode node, string path)
        {
            Dictionary<string, OptimisationJsonProperty> result = new Dictionary<string, OptimisationJsonProperty>(StringComparer.Ordinal);
            foreach (OptimisationJsonProperty property in node.Properties)
            {
                string path_Property = path + "." + property.Name;
                if (result.ContainsKey(property.Name))
                {
                    Error("OPT106", path_Property, "\"" + property.Name + "\" is given more than once.", "Keep one of them.", property);
                    continue;
                }

                result[property.Name] = property;
                Remember(path_Property, property);
            }

            return result;
        }

        private OptimisationJsonNode Object(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, OptimisationJsonNode node, bool required)
        {
            OptimisationJsonNode value = Value(properties, name, path, node, required);
            if (value == null)
            {
                return null;
            }

            if (value.Kind != OptimisationJsonKind.Object)
            {
                Error("OPT107", path + "." + name, "\"" + name + "\" must be an object { … }, not " + value.KindText + ".", null, value);
                return null;
            }

            return value;
        }

        private List<T> List<T>(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, OptimisationJsonNode node, bool required, Func<OptimisationJsonNode, string, T> read) where T : class
        {
            List<T> result = new List<T>();

            OptimisationJsonNode value = Value(properties, name, path, node, required);
            if (value == null)
            {
                return result;
            }

            string path_List = path + "." + name;
            if (value.Kind != OptimisationJsonKind.Array)
            {
                Error("OPT107", path_List, "\"" + name + "\" must be a list [ … ], not " + value.KindText + ".", null, value);
                return result;
            }

            for (int i = 0; i < value.Items.Count; i++)
            {
                OptimisationJsonNode item = value.Items[i];
                string path_Item = string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", path_List, i);
                Remember(path_Item, item);

                if (item.Kind != OptimisationJsonKind.Object)
                {
                    Error("OPT107", path_Item, string.Format(CultureInfo.InvariantCulture, "Item {0} of \"{1}\" must be an object {{ … }}, not {2}.", i + 1, name, item.KindText), null, item);
                    continue;
                }

                result.Add(read(item, path_Item));
            }

            return result;
        }

        private string Text(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, bool required, OptimisationJsonNode node = null)
        {
            OptimisationJsonNode value = Value(properties, name, path, node, required);
            if (value == null)
            {
                return null;
            }

            if (value.Kind != OptimisationJsonKind.String)
            {
                Error("OPT107", path + "." + name, "\"" + name + "\" must be text in quotes, not " + value.KindText + ".", null, value);
                return null;
            }

            return value.Text;
        }

        /// <summary>The unit text; a known synonym (for example "degC") is replaced by its symbol ("°C") with a note (OPT301).</summary>
        private string Unit(Dictionary<string, OptimisationJsonProperty> properties, string path)
        {
            string result = Text(properties, "unit", path, false);
            OptimisationUnit optimisationUnit = Query.OptimisationUnit(result, out bool synonym);
            if (optimisationUnit == null || !synonym)
            {
                return result;
            }

            OptimisationJsonNode value = properties["unit"].Value;
            diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT301", path + ".unit", "Unit \"" + result + "\" was read as \"" + optimisationUnit.Symbol + "\".", null, value.Line, value.Column));
            return optimisationUnit.Symbol;
        }

        private double? Number(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, OptimisationJsonNode node, bool required)
        {
            OptimisationJsonNode value = Value(properties, name, path, node, required);
            if (value == null)
            {
                return null;
            }

            string path_Value = path + "." + name;
            if (value.Kind == OptimisationJsonKind.String)
            {
                bool numeric = double.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && !double.IsNaN(number) && !double.IsInfinity(number);
                Error("OPT108", path_Value, "\"" + name + "\" is written as text (\"" + value.Text + "\"); it must be a number.", numeric ? "Remove the quotes: " + value.Text.Trim() + "." : "Write a number such as 35 or -5.5.", value);
                return null;
            }

            if (value.Kind != OptimisationJsonKind.Number)
            {
                Error("OPT107", path_Value, "\"" + name + "\" must be a number, not " + value.KindText + ".", null, value);
                return null;
            }

            // The runtime's parser is correctly rounded, so a value read and written again is the same double.
            if (!double.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) || double.IsNaN(result) || double.IsInfinity(result))
            {
                Error("OPT109", path_Value, "\"" + name + "\" (" + value.Text + ") is too large to use.", null, value);
                return null;
            }

            return result;
        }

        private int? Integer(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, OptimisationJsonNode node, bool required)
        {
            double? value = Number(properties, name, path, node, required);
            if (value == null)
            {
                return null;
            }

            if (value.Value != System.Math.Floor(value.Value) || value.Value < int.MinValue || value.Value > int.MaxValue)
            {
                Error("OPT112", path + "." + name, "\"" + name + "\" must be a whole number; " + properties[name].Value.Text + " is not.", null, properties[name].Value);
                return null;
            }

            return (int)value.Value;
        }

        private T Enum<T>(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, T @default) where T : struct, System.Enum
        {
            return TryEnum(properties, name, path, out T result) ? result : @default;
        }

        private bool TryEnum<T>(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, out T result) where T : struct, System.Enum
        {
            result = default;

            OptimisationJsonNode value = Value(properties, name, path, null, false);
            if (value == null)
            {
                return false;
            }

            string path_Value = path + "." + name;
            IReadOnlyList<string> texts = OptimisationNames.Texts<T>();
            string allowed = string.Join(", ", texts.Select(x => "\"" + x + "\""));

            if (value.Kind != OptimisationJsonKind.String)
            {
                Error("OPT107", path_Value, "\"" + name + "\" must be text in quotes, not " + value.KindText + ".", "Allowed: " + allowed + ".", value);
                return false;
            }

            if (!OptimisationNames.TryParse(value.Text, out result, out bool exact))
            {
                string suggestion = Suggestion(value.Text, texts);
                Error("OPT111", path_Value, "\"" + value.Text + "\" is not a known value of \"" + name + "\".", (suggestion == null ? string.Empty : "Did you mean \"" + suggestion + "\"? ") + "Allowed: " + allowed + ".", value);
                return false;
            }

            if (!exact)
            {
                diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT113", path_Value, "\"" + value.Text + "\" was read as \"" + Text(result) + "\".", null, value.Line, value.Column));
            }

            return true;
        }

        /// <summary>The property's value; null (with OPT110 when <paramref name="required"/>) when it is absent or null.</summary>
        private OptimisationJsonNode Value(Dictionary<string, OptimisationJsonProperty> properties, string name, string path, OptimisationJsonNode node, bool required)
        {
            if (properties.TryGetValue(name, out OptimisationJsonProperty property) && property.Value.Kind != OptimisationJsonKind.Null)
            {
                return property.Value;
            }

            if (required)
            {
                Error("OPT110", path, "\"" + name + "\" is required " + Where(path) + " but is missing.", null, (object)property ?? node);
            }

            return null;
        }

        private static string Text<T>(T value) where T : struct, System.Enum
        {
            switch (value)
            {
                case ObjectiveSense objectiveSense:
                    return OptimisationNames.Text(objectiveSense);
                case OptimisationAlgorithm optimisationAlgorithm:
                    return OptimisationNames.Text(optimisationAlgorithm);
                case DesignVariableType designVariableType:
                    return OptimisationNames.Text(designVariableType);
                case OptimisationQuantity optimisationQuantity:
                    return OptimisationNames.Text(optimisationQuantity);
            }

            return value.ToString();
        }

        /// <summary>"in the definition", "in variables[1]" and so on, for messages.</summary>
        private static string Where(string path)
        {
            return path == "$" ? "in the definition" : "in " + path.Substring(2);
        }

        private void Remember(string path, OptimisationJsonNode node)
        {
            if (node != null && !positions.ContainsKey(path))
            {
                positions[path] = new KeyValuePair<int, int>(node.Line, node.Column);
            }
        }

        private void Remember(string path, OptimisationJsonProperty property)
        {
            if (property != null && !positions.ContainsKey(path))
            {
                positions[path] = new KeyValuePair<int, int>(property.Line, property.Column);
            }
        }

        private void Error(string code, string path, string message, string hint, object place)
        {
            int? line = null;
            int? column = null;
            if (place is OptimisationJsonNode node)
            {
                line = node.Line;
                column = node.Column;
            }
            else if (place is OptimisationJsonProperty property)
            {
                line = property.Line;
                column = property.Column;
            }

            diagnostics.Add(new OptimisationDiagnostic(DiagnosticSeverity.Error, code, path, message, hint, line, column));
        }

        /// <summary>The closest allowed name, when it is close enough to be a likely typing mistake or a different case.</summary>
        internal static string Suggestion(string text, IEnumerable<string> names)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            string result = null;
            int best = int.MaxValue;
            foreach (string name in names)
            {
                int distance = Distance(text.ToLowerInvariant(), name.ToLowerInvariant());
                if (distance < best)
                {
                    best = distance;
                    result = name;
                }
            }

            return best <= System.Math.Max(2, text.Length / 3) ? result : null;
        }

        /// <summary>Levenshtein distance.</summary>
        private static int Distance(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = System.Math.Min(System.Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                }

                int[] swap = previous;
                previous = current;
                current = swap;
            }

            return previous[b.Length];
        }
    }
}
