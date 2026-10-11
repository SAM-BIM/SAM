// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SAM.Core.Optimisation
{
    public static partial class Query
    {
        /// <summary>
        /// A self-contained text to give an AI assistant (for example by copy and paste into a chat) so that it returns
        /// an edited optimisation definition: the strict reply contract (exactly one raw JSON object, nothing else), the
        /// rules of the schema, the names the model makes available, the current definition and the user's request.
        /// <para>
        /// It describes only what <paramref name="capabilities"/> can run: a method, objective sense, variable type or
        /// constraint the engine cannot run is not offered. For an engine that changes and reads the model itself (it
        /// lists targets or measures), every design variable and output must be bound, and only the catalogue's model
        /// items whose kind the engine lists are offered, each with its current value and unit; a choice target (and the
        /// rules of a choice) only when the engine runs "discrete" variables and "try-every-option", with the options the
        /// model offers and the kind's limit on their number. It holds no file path or machine detail: the definition is
        /// portable by design and nothing else is added. The reply is read back with
        /// <see cref="Create.OptimisationDefinition(string, out List{OptimisationDiagnostic}, IOptimisationCapabilities, bool)"/>
        /// (with extract set, in case the assistant adds a code fence anyway) and validated like any other definition.
        /// </para>
        /// </summary>
        /// <param name="optimisationDefinition">The current definition; null asks for a new one.</param>
        /// <param name="capabilities">What the engine runs. Required.</param>
        /// <param name="catalogue">The available design variables and outputs; null or empty when none is known.</param>
        /// <param name="task">The user's request in their own words; null leaves a place for it.</param>
        public static string AIExchangeText(OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities, OptimisationCatalogue catalogue = null, string task = null)
        {
            if (capabilities == null)
            {
                throw new ArgumentNullException(nameof(capabilities));
            }

            // A choice needs both "discrete" variables and "try-every-option": neither is offered without the other.
            bool choices = RunsChoices(capabilities);
            List<OptimisationAlgorithmCapability> algorithms = (capabilities.Algorithms ?? new List<OptimisationAlgorithmCapability>()).Where(x => x != null && (choices || x.Algorithm != OptimisationAlgorithm.TryEveryOption)).ToList();
            List<string> senses = (capabilities.Senses ?? new List<ObjectiveSense>()).Select(x => "\"" + OptimisationNames.Text(x) + "\"").ToList();
            List<string> variableTypes = (capabilities.VariableTypes ?? new List<DesignVariableType>()).Where(x => choices || x != DesignVariableType.Discrete).Select(x => "\"" + OptimisationNames.Text(x) + "\"").ToList();

            // An engine that lists target or measure kinds runs only bound definitions; the others take names.
            bool bindings = (capabilities.Targets?.Count ?? 0) != 0 || (capabilities.Measures?.Count ?? 0) != 0;
            List<OptimisationCatalogueEntry> targets = OfferedTargets(capabilities, catalogue);
            List<OptimisationCatalogueEntry> measures = OfferedMeasures(capabilities, catalogue);
            bool options = targets.Exists(x => x.Options.Count != 0);
            bool limited = targets.Exists(x => x.Options.Count != 0 && capabilities.Targets.First(y => y != null && y.Kind == x.Target.Kind).MaximumOptions != null);

            StringBuilder stringBuilder = new StringBuilder();
            void Line(string text = "") => stringBuilder.Append(text).Append('\n');

            Line("You are editing a SAM optimisation definition (schema \"" + OptimisationDefinition.Schema + "\").");
            Line();
            Line("OUTPUT FORMAT (strict)");
            Line("Reply with exactly one JSON object and nothing else: no Markdown, no code fence, no explanation before or");
            Line("after it. The first character of your reply must be \"{\" and the last must be \"}\".");
            Line();
            Line("RULES");
            if (bindings)
            {
                Line("- Give every design variable a \"target\" and every output a \"measure\", copied exactly from AVAILABLE. Do not");
                Line("  invent targets, measures or model item names. Names are short labels of your choice, each unique.");
            }
            else
            {
                Line("- Use only the design variable and output names listed under AVAILABLE. Do not invent names.");
            }

            Line("- Do not write code, scripts or expressions. Do not add fields that are not listed here.");
            Line("- Keep \"schema\": \"" + OptimisationDefinition.Schema + "\", and keep every field you were not asked to change.");
            Line("- If the request cannot be expressed with these rules, do not approximate it: return the current definition");
            Line("  unchanged and explain why in \"notes\".");
            Line("- Top level: schema, name (optional), description (optional), notes (optional), model, variables, outputs,");
            Line("  objective, method, stopping (optional).");
            Line("- model: { \"engine\": \"" + capabilities.Engine + "\", \"description\" (optional) }");
            Line("- variables[]: { \"name\", \"description\" (optional), \"type\": " + Either(variableTypes) + ", \"quantity\" (optional),");
            Line("  \"unit\" (optional), \"minimum\", \"maximum\", \"start\", \"step\"" + (bindings ? ", \"target\"" : string.Empty) + " }: minimum < maximum, start within [minimum, maximum],");
            Line("  step > 0. Numbers are plain JSON numbers, never text in quotes.");
            if (choices)
            {
                Line("- A \"discrete\" variable is a choice between options numbered 1 to n: \"minimum\": 1, \"maximum\": n (1 is the first");
                Line("  option), no \"start\" or \"step\". Only \"try-every-option\" searches a choice, and it searches only a choice.");
            }

            if (bindings)
            {
                Line("- target: { \"kind\", \"reference\", \"parameters\" } exactly as under AVAILABLE (reference and parameters only when");
                Line("  shown there). Keep the range within the suggested range when one is shown.");
                if (options)
                {
                    Line("- A choice target also has \"options\": two or more of the names listed for it under AVAILABLE, copied exactly, in");
                    Line("  the order to number them" + (limited ? " (at most as many as shown there)" : string.Empty) + ". Its variable is \"discrete\" with \"maximum\": the number");
                    Line("  of options.");
                }
            }

            Line("- outputs[]: { \"name\", \"description\" (optional), \"quantity\" (optional), \"unit\" (optional)" + (bindings ? ", \"measure\"" : string.Empty) + " }. Every output is");
            Line("  reported for each simulation; one of them is the objective.");
            if (bindings)
            {
                Line("- measure: { \"kind\", \"reference\", \"parameters\" } exactly as under AVAILABLE; a parameter may be changed within");
                Line("  the range shown, or left out to use its default.");
            }

            Line("- objective: { \"output\": <one of outputs[].name>, \"sense\": " + Either(senses) + " }");
            Line("- method: one of");
            foreach (OptimisationAlgorithmCapability optimisationAlgorithmCapability in algorithms)
            {
                Line("  " + MethodRule(optimisationAlgorithmCapability));
            }

            Line("  Method settings other than \"algorithm\" may be left out to use the engine's defaults.");
            Line("- stopping: { \"maximumSimulations\": whole number >= 1 } (each simulation is one full model run, often minutes)");
            if (capabilities.SupportsConstraints)
            {
                Line("- constraints[] (optional): { \"output\": <one of outputs[].name>, \"atMost\" or \"atLeast\": number, \"unit\" (optional) }");
            }

            Line("- quantity values: " + string.Join(", ", OptimisationNames.Texts<OptimisationQuantity>().Select(x => "\"" + x + "\"")));
            if (bindings)
            {
                Line("- units: " + string.Join(", ", optimisationUnits.Select(x => x.Symbol)) + ". A bound value is in the unit shown under");
                Line("  AVAILABLE: use that unit, or leave \"unit\" out.");
            }
            else
            {
                Line("- units (they describe values; they are not checked against the model): " + string.Join(", ", optimisationUnits.Select(x => x.Symbol)));
            }

            Line();

            Line("AVAILABLE" + (string.IsNullOrWhiteSpace(catalogue?.Source) ? string.Empty : " (" + catalogue.Source + ")"));
            if (bindings)
            {
                if (targets.Count == 0 && measures.Count == 0)
                {
                    Line("- No list of model items is available: keep the targets and measures already in the current definition.");
                }
                else
                {
                    Line("Can change (design variable targets):");
                    Bound(targets, capabilities.Targets, Line);
                    Line("Can measure (output measures):");
                    Bound(measures, capabilities.Measures, Line);
                }
            }
            else if (catalogue == null || catalogue.IsEmpty)
            {
                Line("- No list of names is available: use only the names already in the current definition.");
            }
            else
            {
                Line("- design variables: " + Names(catalogue.Variables));
                Line("- outputs: " + Names(catalogue.Outputs));
            }

            Line();
            Line("CURRENT DEFINITION");
            Line(optimisationDefinition == null ? "(none yet: create a complete definition)" : optimisationDefinition.ToJson().TrimEnd('\n'));
            Line();
            Line("TASK");
            Line(string.IsNullOrWhiteSpace(task) ? "<describe the change you want here>" : task.Trim());

            return stringBuilder.ToString();
        }

        private static string MethodRule(OptimisationAlgorithmCapability optimisationAlgorithmCapability)
        {
            string variables;
            int minimum = optimisationAlgorithmCapability.MinimumVariables;
            int? maximum = optimisationAlgorithmCapability.MaximumVariables;
            if (maximum == minimum)
            {
                variables = minimum == 1 ? "exactly 1 design variable" : string.Format(CultureInfo.InvariantCulture, "exactly {0} design variables", minimum);
            }
            else if (maximum == null)
            {
                variables = string.Format(CultureInfo.InvariantCulture, "{0} or more design variables", minimum);
            }
            else
            {
                variables = string.Format(CultureInfo.InvariantCulture, "{0} to {1} design variables", minimum, maximum);
            }

            switch (optimisationAlgorithmCapability.Algorithm)
            {
                case OptimisationAlgorithm.GoldenSection:
                    return "{ \"algorithm\": \"golden-section\", \"tolerance\": number > 0 } (" + variables + "; uses only minimum and maximum)";
                case OptimisationAlgorithm.HookeJeeves:
                    return "{ \"algorithm\": \"hooke-jeeves\", \"stepReductionFactor\": whole number >= 2, \"initialStepExponent\": whole number >= 0,\n" +
                           "    \"stepExponentIncrement\": whole number >= 1, \"stepReductions\": whole number >= 1 } (" + variables + "; each needs start and step)";
                case OptimisationAlgorithm.TryEveryOption:
                    return "{ \"algorithm\": \"try-every-option\" } (" + variables + ", a \"discrete\" choice; one simulation per option, in order;\n" +
                           "    \"maximumSimulations\", if given, at least the number of options)";
            }

            return "{ \"algorithm\": \"" + OptimisationNames.Text(optimisationAlgorithmCapability.Algorithm) + "\" } (" + variables + ")";
        }

        /// <summary>True when the engine runs a choice: "discrete" variables and the "try-every-option" method.</summary>
        private static bool RunsChoices(IOptimisationCapabilities capabilities)
        {
            return capabilities.VariableTypes != null && capabilities.VariableTypes.Contains(DesignVariableType.Discrete)
                && capabilities.Algorithms != null && capabilities.Algorithms.Any(x => x != null && x.Algorithm == OptimisationAlgorithm.TryEveryOption);
        }

        /// <summary>
        /// The catalogue's targets whose kind the engine lists; a choice target only when the engine runs a choice
        /// ("discrete" variables and "try-every-option", <see cref="RunsChoices"/>).
        /// </summary>
        private static List<OptimisationCatalogueEntry> OfferedTargets(IOptimisationCapabilities capabilities, OptimisationCatalogue catalogue)
        {
            List<OptimisationCatalogueEntry> result = new List<OptimisationCatalogueEntry>();
            foreach (OptimisationCatalogueEntry entry in catalogue?.Variables ?? new List<OptimisationCatalogueEntry>())
            {
                OptimisationBindingCapability optimisationBindingCapability = entry.Target == null ? null : capabilities.Targets?.FirstOrDefault(x => x != null && x.Kind == entry.Target.Kind);
                if (optimisationBindingCapability == null)
                {
                    continue;
                }

                if (optimisationBindingCapability.AcceptsOptions && (entry.Options.Count < 2 || !RunsChoices(capabilities)))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        /// <summary>The catalogue's measures whose kind the engine lists.</summary>
        private static List<OptimisationCatalogueEntry> OfferedMeasures(IOptimisationCapabilities capabilities, OptimisationCatalogue catalogue)
        {
            return (catalogue?.Outputs ?? new List<OptimisationCatalogueEntry>()).Where(x => x.Measure != null && capabilities.Measures != null && capabilities.Measures.Any(y => y != null && y.Kind == x.Measure.Kind)).ToList();
        }

        /// <summary>
        /// One entry per model item: the suggested name and the binding as one line of JSON to copy, then what it is,
        /// its unit, current value, suggested range, options and parameters.
        /// </summary>
        private static void Bound(List<OptimisationCatalogueEntry> entries, IReadOnlyList<OptimisationBindingCapability> kinds, Action<string> line)
        {
            if (entries.Count == 0)
            {
                line("- (none in this model)");
                return;
            }

            foreach (OptimisationCatalogueEntry entry in entries)
            {
                OptimisationBinding optimisationBinding = entry.Binding;
                OptimisationBindingCapability optimisationBindingCapability = kinds.First(x => x != null && x.Kind == optimisationBinding.Kind);
                string unit = string.IsNullOrWhiteSpace(entry.Unit) ? optimisationBindingCapability.Unit : entry.Unit.Trim();

                line("- " + entry.Name + ": " + Convert.ToCompactJson(optimisationBinding));

                List<string> details = new List<string>();
                string what = string.IsNullOrWhiteSpace(entry.Description) ? optimisationBindingCapability.DisplayName : entry.Description;
                if (!string.IsNullOrWhiteSpace(what))
                {
                    details.Add(what.Trim());
                }

                details.Add(string.IsNullOrWhiteSpace(unit) ? "no stated unit" : "unit " + unit);
                if (entry.Value != null)
                {
                    details.Add("now " + Number(entry.Value.Value, unit));
                }

                if (entry.Minimum != null && entry.Maximum != null)
                {
                    details.Add("suggested range " + Number(entry.Minimum.Value, null) + " to " + Number(entry.Maximum.Value, unit));
                }

                if (entry.Options.Count != 0)
                {
                    string limit = optimisationBindingCapability.MaximumOptions == null ? string.Empty : " (at most " + optimisationBindingCapability.MaximumOptions.Value.ToString(CultureInfo.InvariantCulture) + ")";
                    details.Add("options" + limit + ": " + string.Join(", ", entry.Options.Select(Convert.ToJsonString)));
                }

                foreach (OptimisationBindingParameter optimisationBindingParameter in optimisationBindingCapability.Parameters)
                {
                    List<string> parameter = new List<string>();
                    if (!string.IsNullOrWhiteSpace(optimisationBindingParameter.DisplayName))
                    {
                        parameter.Add(optimisationBindingParameter.DisplayName.Trim());
                    }

                    if (optimisationBindingParameter.Default != null)
                    {
                        parameter.Add("default " + Number(optimisationBindingParameter.Default.Value, optimisationBindingParameter.Unit));
                    }

                    if (optimisationBindingParameter.Minimum != null || optimisationBindingParameter.Maximum != null)
                    {
                        parameter.Add(optimisationBindingParameter.Minimum != null && optimisationBindingParameter.Maximum != null
                            ? Number(optimisationBindingParameter.Minimum.Value, null) + " to " + Number(optimisationBindingParameter.Maximum.Value, optimisationBindingParameter.Unit)
                            : optimisationBindingParameter.Minimum != null ? "at least " + Number(optimisationBindingParameter.Minimum.Value, optimisationBindingParameter.Unit) : "at most " + Number(optimisationBindingParameter.Maximum.Value, optimisationBindingParameter.Unit));
                    }

                    details.Add("parameter \"" + optimisationBindingParameter.Name + "\"" + (parameter.Count == 0 ? string.Empty : " (" + string.Join(", ", parameter) + ")"));
                }

                line("  " + string.Join("; ", details));
            }
        }

        private static string Either(List<string> values)
        {
            return values.Count == 0 ? "(none)" : string.Join(" or ", values);
        }

        private static string Names(IReadOnlyList<OptimisationCatalogueEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "(none found)";
            }

            return string.Join(", ", entries.Select(x =>
            {
                List<string> details = new List<string>();
                if (!string.IsNullOrWhiteSpace(x.Description))
                {
                    details.Add(x.Description.Trim());
                }

                if (!string.IsNullOrWhiteSpace(x.Unit))
                {
                    details.Add(x.Unit.Trim());
                }

                return details.Count == 0 ? x.Name : x.Name + " (" + string.Join(", ", details) + ")";
            }));
        }
    }
}
