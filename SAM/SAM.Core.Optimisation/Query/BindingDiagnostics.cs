// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Core.Optimisation
{
    public static partial class Query
    {
        /// <summary>How many names a hint lists before "and n more".</summary>
        private const int hintNames = 8;

        /// <summary>
        /// OPT6xx checks a binding can have on its own (no engine needed): a kind is given, parameters are finite numbers
        /// (OPT214), options are named once each and sit on a "discrete" variable numbered 1 to n, and no model item is
        /// changed by two design variables.
        /// </summary>
        private static void BindingMeaning(OptimisationDefinition optimisationDefinition, List<OptimisationDiagnostic> result)
        {
            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                DesignVariable variable = variables[i];
                OptimisationTarget target = variable?.Target;
                if (target == null)
                {
                    continue;
                }

                string path = Path("variables", i) + ".target";
                string subject = Subject(variable, i);
                BindingMeaning(target, "target", subject, path, result);

                List<string> options = target.Options ?? new List<string>();
                if (options.Count == 0)
                {
                    continue;
                }

                HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < options.Count; j++)
                {
                    string path_Option = string.Format(CultureInfo.InvariantCulture, "{0}.options[{1}]", path, j);
                    if (string.IsNullOrWhiteSpace(options[j]))
                    {
                        result.Add(Error("OPT613", path_Option, string.Format(CultureInfo.InvariantCulture, "Option {0} of {1} has no name.", j + 1, subject), "Enter the name of the model item, or remove the option."));
                    }
                    else if (!names.Add(options[j]))
                    {
                        result.Add(Error("OPT613", path_Option, "Option " + Quote(options[j]) + " of " + subject + " is listed more than once.", "List each option once."));
                    }
                }

                if (variable.Type != DesignVariableType.Discrete)
                {
                    result.Add(Error("OPT612", Path("variables", i) + ".type", subject + " chooses between options, so its type must be \"discrete\"; it is \"" + OptimisationNames.Text(variable.Type) + "\".", "Use \"type\": \"discrete\"."));
                }
                else if (Finite(variable.Minimum) && Finite(variable.Maximum) && (variable.Minimum != 1 || variable.Maximum != options.Count))
                {
                    string count = options.Count.ToString(CultureInfo.InvariantCulture);
                    result.Add(Error("OPT615", Path("variables", i) + ".minimum", subject + " chooses between " + count + " options, numbered 1 to " + count + ", so its minimum must be 1 and its maximum " + count + "; they are " + Number(variable.Minimum, null) + " and " + Number(variable.Maximum, null) + ".", "Use \"minimum\": 1 and \"maximum\": " + count + "."));
                }
            }

            List<OptimisationOutput> outputs = optimisationDefinition.Outputs ?? new List<OptimisationOutput>();
            for (int i = 0; i < outputs.Count; i++)
            {
                OptimisationOutput output = outputs[i];
                if (output?.Measure != null)
                {
                    BindingMeaning(output.Measure, "measure", OutputSubject(output, i), Path("outputs", i) + ".measure", result);
                }
            }

            // A model item changed by two design variables: the engine could not tell which value to write.
            for (int i = 0; i < variables.Count; i++)
            {
                OptimisationTarget target = variables[i]?.Target;
                if (target == null || string.IsNullOrWhiteSpace(target.Kind))
                {
                    continue;
                }

                for (int j = 0; j < i; j++)
                {
                    if (target.SameItem(variables[j]?.Target))
                    {
                        result.Add(Error("OPT607", Path("variables", i) + ".target", Subject(variables[i], i) + " changes the same model item as " + Subject(variables[j], j) + ": " + BindingText(target, null) + ".", "Remove one of the two design variables: a model item can be changed by only one."));
                        break;
                    }
                }
            }
        }

        private static void BindingMeaning(OptimisationBinding optimisationBinding, string role, string subject, string path, List<OptimisationDiagnostic> result)
        {
            if (string.IsNullOrWhiteSpace(optimisationBinding.Kind))
            {
                result.Add(Error("OPT600", path + ".kind", "The " + role + " of " + subject + " has no \"kind\".", role == "target" ? "Choose what the design variable changes in the model." : "Choose what the output measures in the model's results."));
            }

            foreach (KeyValuePair<string, double> keyValuePair in (optimisationBinding.Parameters ?? new Dictionary<string, double>()).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                Finite(keyValuePair.Value, "The \"" + keyValuePair.Key + "\" of the " + role + " of " + subject, path + ".parameters." + keyValuePair.Key, result);
            }
        }

        /// <summary>
        /// OPT6xx checks against what the engine can change and measure: every binding's kind is one the engine lists
        /// (OPT601), with its reference keys (OPT602, OPT603), parameters (OPT604, OPT605), unit and quantity (OPT606) and
        /// options (OPT610, OPT611, and at most the kind's limit, OPT616); and when the engine takes targets or measures, every variable or output has one
        /// (OPT608). An engine that lists none (for example "tas-script") takes no bindings at all.
        /// </summary>
        private static void BindingCapability(OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities, List<OptimisationDiagnostic> result)
        {
            string engine = EngineText(capabilities);
            IReadOnlyList<OptimisationBindingCapability> targets = capabilities.Targets ?? new List<OptimisationBindingCapability>();
            IReadOnlyList<OptimisationBindingCapability> measures = capabilities.Measures ?? new List<OptimisationBindingCapability>();

            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                DesignVariable variable = variables[i];
                if (variable == null)
                {
                    continue;
                }

                string path = Path("variables", i) + ".target";
                string subject = Subject(variable, i);
                if (variable.Target == null)
                {
                    if (targets.Count != 0)
                    {
                        result.Add(Error("OPT608", Path("variables", i), subject + " does not say what it changes in the model (it has no \"target\"), so the " + engine + " engine cannot run it.", "Add a \"target\": one of the model items the engine can change."));
                    }

                    continue;
                }

                OptimisationBindingCapability optimisationBindingCapability = BindingKind(variable.Target, "target", subject, path, targets, measures, engine, result);
                if (optimisationBindingCapability == null)
                {
                    continue;
                }

                BindingCapability(variable.Target, optimisationBindingCapability, "target", subject, path, result);
                BindingUnit(variable.Quantity, variable.Unit, optimisationBindingCapability, subject, "sets", Path("variables", i), engine, result);

                List<string> options = variable.Target.Options ?? new List<string>();
                if (options.Count != 0 && !optimisationBindingCapability.AcceptsOptions)
                {
                    result.Add(Error("OPT610", path + ".options", KindText(optimisationBindingCapability, true) + " takes a value, not a choice between options, so the options of " + subject + " cannot be used.", "Remove \"options\"."));
                }
                else if (optimisationBindingCapability.AcceptsOptions && options.Count < 2)
                {
                    result.Add(Error("OPT611", path + ".options", KindText(optimisationBindingCapability, true) + " is a choice between options, and " + subject + (options.Count == 0 ? " lists none" : " lists only one") + "; at least two are needed.", "List the model items to choose between in \"options\"."));
                }
                else if (optimisationBindingCapability.MaximumOptions != null && options.Count > optimisationBindingCapability.MaximumOptions.Value)
                {
                    int maximum = optimisationBindingCapability.MaximumOptions.Value;
                    int surplus = options.Count - maximum;
                    result.Add(Error("OPT616", path + ".options", string.Format(CultureInfo.InvariantCulture, "{0} takes at most {1} options with the {2} engine (each option is one simulation), and {3} lists {4}, so this definition cannot run.", KindText(optimisationBindingCapability, true), maximum, engine, subject, options.Count), surplus == 1 ? "Remove 1 option." : string.Format(CultureInfo.InvariantCulture, "Remove {0} options.", surplus)));
                }
            }

            List<OptimisationOutput> outputs = optimisationDefinition.Outputs ?? new List<OptimisationOutput>();
            for (int i = 0; i < outputs.Count; i++)
            {
                OptimisationOutput output = outputs[i];
                if (output == null)
                {
                    continue;
                }

                string path = Path("outputs", i) + ".measure";
                string subject = OutputSubject(output, i);
                if (output.Measure == null)
                {
                    if (measures.Count != 0)
                    {
                        result.Add(Error("OPT608", Path("outputs", i), subject + " does not say what it measures in the model's results (it has no \"measure\"), so the " + engine + " engine cannot run it.", "Add a \"measure\": one of the results the engine can measure."));
                    }

                    continue;
                }

                OptimisationBindingCapability optimisationBindingCapability = BindingKind(output.Measure, "measure", subject, path, measures, targets, engine, result);
                if (optimisationBindingCapability == null)
                {
                    continue;
                }

                BindingCapability(output.Measure, optimisationBindingCapability, "measure", subject, path, result);
                BindingUnit(output.Quantity, output.Unit, optimisationBindingCapability, subject, "reports", Path("outputs", i), engine, result);
            }
        }

        /// <summary>The engine's capability for the binding's kind, or null with OPT601 (no kind is reported by OPT600).</summary>
        private static OptimisationBindingCapability BindingKind(OptimisationBinding optimisationBinding, string role, string subject, string path, IReadOnlyList<OptimisationBindingCapability> kinds, IReadOnlyList<OptimisationBindingCapability> kinds_Other, string engine, List<OptimisationDiagnostic> result)
        {
            if (string.IsNullOrWhiteSpace(optimisationBinding.Kind))
            {
                return null;
            }

            OptimisationBindingCapability optimisationBindingCapability = kinds.FirstOrDefault(x => x != null && string.Equals(x.Kind, optimisationBinding.Kind, StringComparison.Ordinal));
            if (optimisationBindingCapability != null)
            {
                return optimisationBindingCapability;
            }

            string action = role == "target" ? "change" : "measure";
            string notRunnable = ", so this definition cannot run.";
            if (kinds.Count == 0)
            {
                result.Add(Error("OPT601", path, subject + " has a " + role + ", but the " + engine + " engine takes no " + role + "s" + notRunnable, "Remove \"" + role + "\", or use an engine that can " + action + " " + (role == "target" ? "model items" : "model results") + "."));
                return null;
            }

            List<string> names = kinds.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Kind)).Select(x => x.Kind).ToList();
            OptimisationBindingCapability optimisationBindingCapability_Other = kinds_Other.FirstOrDefault(x => x != null && string.Equals(x.Kind, optimisationBinding.Kind, StringComparison.Ordinal));
            string message = optimisationBindingCapability_Other != null
                ? Quote(optimisationBinding.Kind) + " is a " + (role == "target" ? "measure" : "target") + ", not a " + role + ", so it cannot be the " + role + " of " + subject + "."
                : Quote(optimisationBinding.Kind) + " (the " + role + " of " + subject + ") is not a " + role + " the " + engine + " engine can " + action + notRunnable;
            string suggestion = OptimisationDefinitionReader.Suggestion(optimisationBinding.Kind, names);
            result.Add(Error("OPT601", path + ".kind", message, (suggestion == null ? string.Empty : "Did you mean \"" + suggestion + "\"? ") + Capitalise(role) + "s: " + ListText(names) + "."));
            return null;
        }

        private static void BindingCapability(OptimisationBinding optimisationBinding, OptimisationBindingCapability optimisationBindingCapability, string role, string subject, string path, List<OptimisationDiagnostic> result)
        {
            string kind = KindText(optimisationBindingCapability, false);

            // Reference keys.
            List<string> keys = optimisationBindingCapability.ReferenceKeys.Select(x => x.Name).ToList();
            Dictionary<string, string> reference = optimisationBinding.Reference ?? new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> keyValuePair in reference.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                OptimisationReferenceKey optimisationReferenceKey = optimisationBindingCapability.ReferenceKeys.FirstOrDefault(x => x.Name == keyValuePair.Key);
                string path_Key = path + ".reference." + keyValuePair.Key;
                if (optimisationReferenceKey == null)
                {
                    string suggestion = OptimisationDefinitionReader.Suggestion(keyValuePair.Key, keys);
                    result.Add(Error("OPT603", path_Key, "\"" + keyValuePair.Key + "\" is not a reference key of " + kind + " (the " + role + " of " + subject + ").", keys.Count == 0 ? "This " + role + " refers to the whole model: remove \"reference\"." : (suggestion == null ? string.Empty : "Did you mean \"" + suggestion + "\"? ") + "Keys: " + string.Join(", ", keys) + "."));
                }
                else if (string.IsNullOrWhiteSpace(keyValuePair.Value))
                {
                    result.Add(Error("OPT602", path_Key, "The " + role + " of " + subject + " names an empty " + KeyText(optimisationReferenceKey) + " (\"" + keyValuePair.Key + "\" is \"" + keyValuePair.Value + "\").", "Enter the name of the " + KeyText(optimisationReferenceKey) + " as it is in the model."));
                }
            }

            foreach (OptimisationReferenceKey optimisationReferenceKey in optimisationBindingCapability.ReferenceKeys)
            {
                if (optimisationReferenceKey.Required && !reference.ContainsKey(optimisationReferenceKey.Name))
                {
                    result.Add(Error("OPT602", path, "The " + role + " of " + subject + " does not say which " + KeyText(optimisationReferenceKey) + " (\"" + optimisationReferenceKey.Name + "\" is missing from \"reference\").", "Add \"" + optimisationReferenceKey.Name + "\" with the name of the " + KeyText(optimisationReferenceKey) + " as it is in the model."));
                }
            }

            // Parameters.
            List<string> names = optimisationBindingCapability.Parameters.Select(x => x.Name).ToList();
            foreach (KeyValuePair<string, double> keyValuePair in (optimisationBinding.Parameters ?? new Dictionary<string, double>()).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                string path_Parameter = path + ".parameters." + keyValuePair.Key;
                OptimisationBindingParameter optimisationBindingParameter = optimisationBindingCapability.Parameters.FirstOrDefault(x => x.Name == keyValuePair.Key);
                if (optimisationBindingParameter == null)
                {
                    string suggestion = OptimisationDefinitionReader.Suggestion(keyValuePair.Key, names);
                    result.Add(Error("OPT604", path_Parameter, "\"" + keyValuePair.Key + "\" is not a parameter of " + kind + " (the " + role + " of " + subject + ").", names.Count == 0 ? "This " + role + " takes no parameters: remove \"" + keyValuePair.Key + "\"." : (suggestion == null ? string.Empty : "Did you mean \"" + suggestion + "\"? ") + "Parameters: " + string.Join(", ", names) + "."));
                    continue;
                }

                double value = keyValuePair.Value;
                double? minimum = optimisationBindingParameter.Minimum;
                double? maximum = optimisationBindingParameter.Maximum;
                if (!Finite(value) || (minimum == null || value >= minimum.Value) && (maximum == null || value <= maximum.Value))
                {
                    continue;
                }

                string unit = optimisationBindingParameter.Unit;
                string accepted = minimum != null && maximum != null ? "from " + Number(minimum.Value, null) + " to " + Number(maximum.Value, unit) : minimum != null ? "at least " + Number(minimum.Value, unit) : "at most " + Number(maximum.Value, unit);
                result.Add(Error("OPT605", path_Parameter, "The \"" + keyValuePair.Key + "\" of the " + role + " of " + subject + " must be " + accepted + "; it is " + Number(value, unit) + ".", optimisationBindingParameter.Default == null ? null : "Leave it out to use " + Number(optimisationBindingParameter.Default.Value, unit) + "."));
            }
        }

        /// <summary>
        /// OPT606: a declared quantity or unit that is not the one the engine writes or reports for the kind. SAM converts
        /// nothing, so a value labelled MWh that the engine reports in kWh would be wrong by a factor of 1000.
        /// </summary>
        private static void BindingUnit(OptimisationQuantity quantity, string unit, OptimisationBindingCapability optimisationBindingCapability, string subject, string verb, string path, string engine, List<OptimisationDiagnostic> result)
        {
            OptimisationQuantity quantity_Kind = optimisationBindingCapability.Quantity;
            if (quantity != OptimisationQuantity.Unspecified && quantity_Kind != OptimisationQuantity.Unspecified && !SameQuantity(quantity, quantity_Kind))
            {
                result.Add(Error("OPT606", path + ".quantity", subject + " is declared as " + QuantityText(quantity) + ", but " + KindText(optimisationBindingCapability, false) + " is " + QuantityText(quantity_Kind) + ".", "Use \"quantity\": \"" + OptimisationNames.Text(quantity_Kind) + "\", or leave it out."));
            }

            if (string.IsNullOrWhiteSpace(unit) || string.IsNullOrWhiteSpace(optimisationBindingCapability.Unit))
            {
                return;
            }

            string symbol = OptimisationUnit(unit, out _)?.Symbol ?? unit.Trim();
            string symbol_Kind = OptimisationUnit(optimisationBindingCapability.Unit, out _)?.Symbol ?? optimisationBindingCapability.Unit.Trim();
            if (symbol != symbol_Kind)
            {
                result.Add(Error("OPT606", path + ".unit", subject + " is declared in " + symbol + ", but the " + engine + " engine " + verb + " " + KindText(optimisationBindingCapability, false) + " in " + symbol_Kind + ".", "Use \"unit\": \"" + symbol_Kind + "\", or leave it out."));
            }
        }

        /// <summary>
        /// OPT609 and OPT614: when the catalogue lists the model's items, every binding must be one of them (same kind and
        /// reference) and every option one the item offers. A binding whose kind the engine does not list is left to
        /// OPT601.
        /// </summary>
        private static void BindingCatalogue(OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities, OptimisationCatalogue catalogue, List<OptimisationDiagnostic> result)
        {
            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                DesignVariable variable = variables[i];
                OptimisationTarget target = variable?.Target;
                if (target == null || string.IsNullOrWhiteSpace(target.Kind) || (capabilities != null && !(capabilities.Targets ?? new List<OptimisationBindingCapability>()).Any(x => x?.Kind == target.Kind)))
                {
                    continue;
                }

                string path = Path("variables", i) + ".target";
                string subject = Subject(variable, i);
                OptimisationBindingCapability optimisationBindingCapability = capabilities?.Targets?.FirstOrDefault(x => x?.Kind == target.Kind);
                OptimisationCatalogueEntry entry = CatalogueEntry(target, catalogue.Variables.Where(x => x.Target != null).ToList(), optimisationBindingCapability, "changed", subject, path, result);
                if (entry == null || entry.Options.Count == 0)
                {
                    continue;
                }

                List<string> options = target.Options ?? new List<string>();
                for (int j = 0; j < options.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(options[j]) || entry.Options.Contains(options[j]))
                    {
                        continue;
                    }

                    string suggestion = Closest(options[j], entry.Options);
                    result.Add(Error("OPT614", string.Format(CultureInfo.InvariantCulture, "{0}.options[{1}]", path, j), "Option " + Quote(options[j]) + " of " + subject + " is not available for " + BindingText(target, optimisationBindingCapability) + ".", (suggestion == null ? string.Empty : "Did you mean " + Quote(suggestion) + "? ") + "Available: " + ListText(entry.Options.Select(Quote)) + "."));
                }
            }

            List<OptimisationOutput> outputs = optimisationDefinition.Outputs ?? new List<OptimisationOutput>();
            for (int i = 0; i < outputs.Count; i++)
            {
                OptimisationOutput output = outputs[i];
                OptimisationMeasure measure = output?.Measure;
                if (measure == null || string.IsNullOrWhiteSpace(measure.Kind) || (capabilities != null && !(capabilities.Measures ?? new List<OptimisationBindingCapability>()).Any(x => x?.Kind == measure.Kind)))
                {
                    continue;
                }

                OptimisationBindingCapability optimisationBindingCapability = capabilities?.Measures?.FirstOrDefault(x => x?.Kind == measure.Kind);
                CatalogueEntry(measure, catalogue.Outputs.Where(x => x.Measure != null).ToList(), optimisationBindingCapability, "measured", OutputSubject(output, i), Path("outputs", i) + ".measure", result);
            }
        }

        /// <summary>The catalogue entry with the binding's kind and reference, or null with OPT609 saying what is not in the model.</summary>
        private static OptimisationCatalogueEntry CatalogueEntry(OptimisationBinding optimisationBinding, List<OptimisationCatalogueEntry> entries, OptimisationBindingCapability optimisationBindingCapability, string participle, string subject, string path, List<OptimisationDiagnostic> result)
        {
            OptimisationCatalogueEntry entry = entries.Find(x => x.Binding.SameItem(optimisationBinding));
            if (entry != null)
            {
                return entry;
            }

            List<OptimisationCatalogueEntry> entries_Kind = entries.FindAll(x => x.Binding.Kind == optimisationBinding.Kind);
            if (entries_Kind.Count == 0)
            {
                List<string> available = entries.Select(x => x.Name).Distinct().ToList();
                result.Add(Error("OPT609", path + ".kind", Capitalise(KindText(optimisationBindingCapability, false, optimisationBinding.Kind)) + " is not available in this model, so " + subject + " cannot be " + participle + ".", available.Count == 0 ? null : "Choose one of: " + ListText(available) + "."));
                return null;
            }

            // The first reference key whose name is in no entry of this kind says best what is missing.
            foreach (KeyValuePair<string, string> keyValuePair in OptimisationBinding.References(optimisationBinding))
            {
                List<string> values = entries_Kind.Select(x => x.Binding.Reference != null && x.Binding.Reference.TryGetValue(keyValuePair.Key, out string value) ? value : null).Where(x => x != null).Distinct().ToList();
                if (values.Count == 0 || values.Contains(keyValuePair.Value))
                {
                    continue;
                }

                OptimisationReferenceKey optimisationReferenceKey = optimisationBindingCapability?.ReferenceKeys.FirstOrDefault(x => x.Name == keyValuePair.Key);
                string suggestion = Closest(keyValuePair.Value, values);
                string item = string.IsNullOrWhiteSpace(optimisationReferenceKey?.DisplayName) ? Quote(keyValuePair.Value) + " (\"" + keyValuePair.Key + "\")" : Capitalise(KeyText(optimisationReferenceKey)) + " " + Quote(keyValuePair.Value);
                result.Add(Error("OPT609", path + ".reference." + keyValuePair.Key, item + " is not in this model, so " + subject + " cannot be " + participle + ".",(suggestion == null ? string.Empty : "Did you mean " + Quote(suggestion) + "? ") + "Available: " + ListText(values.Select(Quote)) + "."));
                return null;
            }

            result.Add(Error("OPT609", path + ".reference", "This model has no " + BindingText(optimisationBinding, optimisationBindingCapability) + ", so " + subject + " cannot be " + participle + ".", "Available: " + ListText(entries_Kind.Select(x => BindingText(x.Binding, optimisationBindingCapability))) + "."));
            return null;
        }

        /// <summary>"Zone heating setpoint (internal condition “Office”)", for messages.</summary>
        private static string BindingText(OptimisationBinding optimisationBinding, OptimisationBindingCapability optimisationBindingCapability)
        {
            string result = KindText(optimisationBindingCapability, false, optimisationBinding.Kind);

            List<KeyValuePair<string, string>> reference = OptimisationBinding.References(optimisationBinding);
            if (optimisationBindingCapability != null)
            {
                // The engine's key order reads better than the ordinal one ("plant room …, controller …").
                List<string> keys = optimisationBindingCapability.ReferenceKeys.Select(x => x.Name).ToList();
                reference = reference.OrderBy(x => keys.IndexOf(x.Key) < 0 ? int.MaxValue : keys.IndexOf(x.Key)).ToList();
            }

            if (reference.Count == 0)
            {
                return result;
            }

            return result + " (" + string.Join(", ", reference.Select(x => KeyText(optimisationBindingCapability?.ReferenceKeys.FirstOrDefault(y => y.Name == x.Key), x.Key) + " " + Quote(x.Value))) + ")";
        }

        /// <summary>The kind's display name ("Zone heating setpoint"; lower-case first letter mid-sentence), else the kind in quotes.</summary>
        private static string KindText(OptimisationBindingCapability optimisationBindingCapability, bool sentenceStart, string kind = null)
        {
            string text = optimisationBindingCapability?.DisplayName;
            if (string.IsNullOrWhiteSpace(text))
            {
                return Quote(optimisationBindingCapability?.Kind ?? kind);
            }

            text = text.Trim();
            return sentenceStart ? Capitalise(text) : char.ToLowerInvariant(text[0]) + text.Substring(1);
        }

        /// <summary>"internal condition", else the key itself.</summary>
        private static string KeyText(OptimisationReferenceKey optimisationReferenceKey, string key = null)
        {
            return string.IsNullOrWhiteSpace(optimisationReferenceKey?.DisplayName) ? (optimisationReferenceKey?.Name ?? key) : optimisationReferenceKey.DisplayName.Trim();
        }

        private static string OutputSubject(OptimisationOutput output, int index)
        {
            return string.IsNullOrWhiteSpace(output?.Name) ? Label("Output", null, index) : output.Name;
        }

        private static string EngineText(IOptimisationCapabilities capabilities)
        {
            return string.IsNullOrWhiteSpace(capabilities.DisplayName) ? capabilities.Engine : capabilities.DisplayName;
        }

        private static bool SameQuantity(OptimisationQuantity quantity, OptimisationQuantity quantity_Other)
        {
            return quantity == quantity_Other
                || (quantity == OptimisationQuantity.Carbon && quantity_Other == OptimisationQuantity.Mass)
                || (quantity == OptimisationQuantity.Mass && quantity_Other == OptimisationQuantity.Carbon);
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>The name that differs only in case, else the closest likely misspelling, else null.</summary>
        private static string Closest(string text, IEnumerable<string> names)
        {
            List<string> list = names.ToList();
            return list.Find(x => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)) ?? OptimisationDefinitionReader.Suggestion(text, list);
        }

        /// <summary>The first <see cref="hintNames"/> items, then "and n more".</summary>
        private static string ListText(IEnumerable<string> items)
        {
            List<string> list = items.ToList();
            return list.Count <= hintNames ? string.Join(", ", list) : string.Join(", ", list.Take(hintNames)) + string.Format(CultureInfo.InvariantCulture, " and {0} more", list.Count - hintNames);
        }

        private static string Capitalise(string text)
        {
            return string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
