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
        /// constraint the engine cannot run is not offered. It holds no file path or machine detail: the definition is
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

            List<OptimisationAlgorithmCapability> algorithms = (capabilities.Algorithms ?? new List<OptimisationAlgorithmCapability>()).ToList();
            List<string> senses = (capabilities.Senses ?? new List<ObjectiveSense>()).Select(x => "\"" + OptimisationNames.Text(x) + "\"").ToList();
            List<string> variableTypes = (capabilities.VariableTypes ?? new List<DesignVariableType>()).Select(x => "\"" + OptimisationNames.Text(x) + "\"").ToList();

            StringBuilder stringBuilder = new StringBuilder();
            void Line(string text = "") => stringBuilder.Append(text).Append('\n');

            Line("You are editing a SAM optimisation definition (schema \"" + OptimisationDefinition.Schema + "\").");
            Line();
            Line("OUTPUT FORMAT (strict)");
            Line("Reply with exactly one JSON object and nothing else: no Markdown, no code fence, no explanation before or");
            Line("after it. The first character of your reply must be \"{\" and the last must be \"}\".");
            Line();
            Line("RULES");
            Line("- Use only the design variable and output names listed under AVAILABLE. Do not invent names.");
            Line("- Do not write code, scripts or expressions. Do not add fields that are not listed here.");
            Line("- Keep \"schema\": \"" + OptimisationDefinition.Schema + "\", and keep every field you were not asked to change.");
            Line("- If the request cannot be expressed with these rules, do not approximate it: return the current definition");
            Line("  unchanged and explain why in \"notes\".");
            Line("- Top level: schema, name (optional), description (optional), notes (optional), model, variables, outputs,");
            Line("  objective, method, stopping (optional).");
            Line("- model: { \"engine\": \"" + capabilities.Engine + "\", \"description\" (optional) }");
            Line("- variables[]: { \"name\", \"description\" (optional), \"type\": " + Either(variableTypes) + ", \"quantity\" (optional),");
            Line("  \"unit\" (optional), \"minimum\", \"maximum\", \"start\", \"step\" }: minimum < maximum, start within [minimum, maximum],");
            Line("  step > 0. Numbers are plain JSON numbers, never text in quotes.");
            Line("- outputs[]: { \"name\", \"description\" (optional), \"quantity\" (optional), \"unit\" (optional) }. Every output is");
            Line("  reported for each simulation; one of them is the objective.");
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
            Line("- units (they describe values; they are not checked against the model): " + string.Join(", ", optimisationUnits.Select(x => x.Symbol)));
            Line();

            Line("AVAILABLE" + (string.IsNullOrWhiteSpace(catalogue?.Source) ? string.Empty : " (" + catalogue.Source + ")"));
            if (catalogue == null || catalogue.IsEmpty)
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
            }

            return "{ \"algorithm\": \"" + OptimisationNames.Text(optimisationAlgorithmCapability.Algorithm) + "\" } (" + variables + ")";
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
