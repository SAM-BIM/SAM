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
        /// <summary>
        /// Checks a definition's meaning, units and search method, in that order, and returns what is wrong in
        /// engineering terms (no text positions: <see cref="Create.OptimisationDefinition(string, out List{OptimisationDiagnostic}, IOptimisationCapabilities, bool)"/>
        /// adds them for a definition read from text).
        /// <list type="bullet">
        /// <item>OPT2xx meaning: names present and unique, the objective and constraints refer to outputs, every number is
        /// finite (OPT214), each range, start and step is valid, the simulation limit is at least 1.</item>
        /// <item>OPT3xx units: each unit is known and suits its declared quantity; a constraint's unit suits its output.
        /// Units are declarations: SAM never verifies them against the model.</item>
        /// <item>OPT4xx method: the method's own settings (always), then, when <paramref name="capabilities"/> is given,
        /// what the engine can run. A definition asking for more than the engine runs (for example maximise or a
        /// constraint) is reported as an error, so it stays valid to read and edit but is not runnable.</item>
        /// </list>
        /// </summary>
        public static List<OptimisationDiagnostic> Diagnostics(this OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities = null)
        {
            List<OptimisationDiagnostic> result = new List<OptimisationDiagnostic>();
            if (optimisationDefinition == null)
            {
                return result;
            }

            Meaning(optimisationDefinition, result);
            Units(optimisationDefinition, result);
            Method(optimisationDefinition, result);
            if (capabilities != null)
            {
                Capability(optimisationDefinition, capabilities, result);
            }

            return result;
        }

        /// <summary>True when no diagnostic is an error.</summary>
        public static bool IsRunnable(this IEnumerable<OptimisationDiagnostic> diagnostics)
        {
            return diagnostics != null && diagnostics.All(x => x == null || x.Severity != DiagnosticSeverity.Error);
        }

        /// <summary>True when the definition has no error for an engine with <paramref name="capabilities"/>.</summary>
        public static bool IsRunnable(this OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities)
        {
            if (capabilities == null)
            {
                throw new ArgumentNullException(nameof(capabilities));
            }

            return optimisationDefinition != null && optimisationDefinition.Diagnostics(capabilities).IsRunnable();
        }

        private static void Meaning(OptimisationDefinition optimisationDefinition, List<OptimisationDiagnostic> result)
        {
            if (optimisationDefinition.Model == null)
            {
                result.Add(Error("OPT110", "$", "\"model\" is required in the definition but is missing.", "Add \"model\": { \"engine\": \"tas-script\" }."));
            }
            else if (string.IsNullOrWhiteSpace(optimisationDefinition.Model.Engine))
            {
                result.Add(Error("OPT200", "$.model.engine", "The model engine is empty.", "Name the engine that runs the model, for example \"tas-script\"."));
            }

            // Design variables.
            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            if (variables.Count == 0)
            {
                result.Add(Error("OPT201", "$.variables", "The definition has no design variables, so there is nothing to optimise.", "Add at least one variable the model reads."));
            }

            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < variables.Count; i++)
            {
                DesignVariable variable = variables[i];
                string path = Path("variables", i);
                if (variable == null)
                {
                    continue;
                }

                string label = Label("Design variable", variable.Name, i);
                if (string.IsNullOrWhiteSpace(variable.Name))
                {
                    result.Add(Error("OPT202", path + ".name", label + " has no name.", "Enter the name the model reads, or remove the variable."));
                }
                else if (!names.Add(variable.Name))
                {
                    result.Add(Error("OPT203", path + ".name", "Two design variables are named " + Quote(variable.Name) + ".", "Each design variable needs its own name."));
                }

                // A value that is not a finite number cannot be run, and the text (JSON) cannot hold it either.
                bool finite = Finite(variable.Minimum, Subject(variable, i) + " minimum", path + ".minimum", result);
                finite &= Finite(variable.Maximum, Subject(variable, i) + " maximum", path + ".maximum", result);
                finite &= variable.Start == null || Finite(variable.Start.Value, Subject(variable, i) + " start", path + ".start", result);
                bool finite_Step = variable.Step == null || Finite(variable.Step.Value, Subject(variable, i) + " step", path + ".step", result);

                if (finite)
                {
                    if (variable.Minimum == variable.Maximum)
                    {
                        result.Add(Error("OPT204", path + ".minimum", Subject(variable, i) + " range is invalid: minimum and maximum are both " + Number(variable.Minimum, variable.Unit) + ", so it cannot change.", "Widen the range, or remove the variable."));
                    }
                    else if (variable.Minimum > variable.Maximum)
                    {
                        result.Add(Error("OPT204", path + ".minimum", Subject(variable, i) + " range is invalid: minimum " + Number(variable.Minimum, variable.Unit) + " is greater than maximum " + Number(variable.Maximum, variable.Unit) + ".", "Swap the two values."));
                    }
                    else if (variable.Start != null && (variable.Start.Value < variable.Minimum || variable.Start.Value > variable.Maximum))
                    {
                        result.Add(Error("OPT205", path + ".start", Subject(variable, i) + " start " + Number(variable.Start.Value, variable.Unit) + " is outside its range (" + Range(variable) + ").", "Choose a start within the range."));
                    }
                }

                if (finite_Step && variable.Step != null && !(variable.Step.Value > 0))
                {
                    result.Add(Error("OPT206", path + ".step", Subject(variable, i) + " step must be greater than 0; it is " + Number(variable.Step.Value, variable.Unit) + ".", null));
                }
            }

            // Outputs.
            List<OptimisationOutput> outputs = optimisationDefinition.Outputs ?? new List<OptimisationOutput>();
            if (outputs.Count == 0)
            {
                result.Add(Error("OPT207", "$.outputs", "The definition has no outputs, so there is nothing to optimise.", "Add the output the model reports for the objective."));
            }

            HashSet<string> names_Output = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < outputs.Count; i++)
            {
                OptimisationOutput output = outputs[i];
                if (output == null)
                {
                    continue;
                }

                string path = Path("outputs", i);
                if (string.IsNullOrWhiteSpace(output.Name))
                {
                    result.Add(Error("OPT208", path + ".name", Label("Output", output.Name, i) + " has no name.", "Enter the name the model writes, or remove the output."));
                }
                else if (!names_Output.Add(output.Name))
                {
                    result.Add(Error("OPT209", path + ".name", "Two outputs are named " + Quote(output.Name) + ".", "Each output needs its own name."));
                }
            }

            string available = names_Output.Count == 0 ? null : "Choose one of: " + string.Join(", ", names_Output) + ".";

            // Objective.
            OptimisationObjective objective = optimisationDefinition.Objective;
            if (objective == null)
            {
                result.Add(Error("OPT110", "$", "\"objective\" is required in the definition but is missing.", "Add \"objective\": { \"output\": …, \"sense\": \"minimise\" }."));
            }
            else if (string.IsNullOrWhiteSpace(objective.Output))
            {
                result.Add(Error("OPT210", "$.objective.output", "The objective does not name an output.", available));
            }
            else if (!names_Output.Contains(objective.Output))
            {
                result.Add(Error("OPT210", "$.objective.output", "The objective refers to output " + Quote(objective.Output) + ", which is not in the outputs.", "Add it to the outputs" + (available == null ? "." : ", or " + char.ToLowerInvariant(available[0]) + available.Substring(1))));
            }

            // Constraints.
            List<OptimisationConstraint> constraints = optimisationDefinition.Constraints ?? new List<OptimisationConstraint>();
            for (int i = 0; i < constraints.Count; i++)
            {
                OptimisationConstraint constraint = constraints[i];
                if (constraint == null)
                {
                    continue;
                }

                string path = Path("constraints", i);
                if (string.IsNullOrWhiteSpace(constraint.Output) || !names_Output.Contains(constraint.Output))
                {
                    result.Add(Error("OPT211", path + ".output", string.IsNullOrWhiteSpace(constraint.Output) ? "A constraint does not name an output." : "A constraint refers to output " + Quote(constraint.Output) + ", which is not in the outputs.", available));
                }

                string on = string.IsNullOrWhiteSpace(constraint.Output) ? "The constraint" : "The constraint on " + Quote(constraint.Output);
                if (constraint.AtMost != null)
                {
                    Finite(constraint.AtMost.Value, on + " limit \"atMost\"", path + ".atMost", result);
                }

                if (constraint.AtLeast != null)
                {
                    Finite(constraint.AtLeast.Value, on + " limit \"atLeast\"", path + ".atLeast", result);
                }

                if ((constraint.AtMost == null) == (constraint.AtLeast == null))
                {
                    result.Add(Error("OPT212", path, "A constraint" + (string.IsNullOrWhiteSpace(constraint.Output) ? string.Empty : " on " + Quote(constraint.Output)) + " must give exactly one limit: \"atMost\" or \"atLeast\".", "For a range, use two constraints."));
                }
            }

            // Stopping.
            int? maximumSimulations = optimisationDefinition.Stopping?.MaximumSimulations;
            if (maximumSimulations != null && maximumSimulations.Value < 1)
            {
                result.Add(Error("OPT213", "$.stopping.maximumSimulations", string.Format(CultureInfo.InvariantCulture, "The maximum number of simulations must be at least 1; it is {0}.", maximumSimulations.Value), null));
            }
        }

        private static void Units(OptimisationDefinition optimisationDefinition, List<OptimisationDiagnostic> result)
        {
            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i] != null)
                {
                    Unit(variables[i].Unit, variables[i].Quantity, Subject(variables[i], i), Path("variables", i), result);
                }
            }

            List<OptimisationOutput> outputs = optimisationDefinition.Outputs ?? new List<OptimisationOutput>();
            for (int i = 0; i < outputs.Count; i++)
            {
                if (outputs[i] != null)
                {
                    Unit(outputs[i].Unit, outputs[i].Quantity, string.IsNullOrWhiteSpace(outputs[i].Name) ? Label("Output", null, i) : outputs[i].Name, Path("outputs", i), result);
                }
            }

            List<OptimisationConstraint> constraints = optimisationDefinition.Constraints ?? new List<OptimisationConstraint>();
            for (int i = 0; i < constraints.Count; i++)
            {
                OptimisationConstraint constraint = constraints[i];
                if (constraint == null || string.IsNullOrWhiteSpace(constraint.Unit))
                {
                    continue;
                }

                string path = Path("constraints", i) + ".unit";
                OptimisationUnit optimisationUnit = OptimisationUnit(constraint.Unit, out bool synonym);
                if (optimisationUnit == null)
                {
                    result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Warning, "OPT300", path, "Unit " + Quote(constraint.Unit) + " is not recognised; it is shown as written and not checked.", KnownUnits()));
                    continue;
                }

                if (synonym)
                {
                    result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT301", path, "Unit " + Quote(constraint.Unit) + " is read as " + Quote(optimisationUnit.Symbol) + ".", null));
                }

                OptimisationOutput output = optimisationDefinition.Output(constraint.Output);
                if (output == null)
                {
                    continue;
                }

                OptimisationQuantity quantity = Quantity(output);
                if (quantity != OptimisationQuantity.Unspecified && !optimisationUnit.Suits(quantity))
                {
                    result.Add(Error("OPT303", path, "The constraint on " + Quote(output.Name) + " is in " + optimisationUnit.Symbol + ", but the output is " + QuantityText(quantity) + "; " + optimisationUnit.Symbol + " is a unit of " + QuantityNoun(optimisationUnit.Quantity) + ".", "Use one of: " + string.Join(", ", UnitSymbols(quantity)) + "."));
                }
                else if (!string.IsNullOrWhiteSpace(output.Unit) && OptimisationUnit(output.Unit, out _) is OptimisationUnit unit_Output && unit_Output.Symbol != optimisationUnit.Symbol && unit_Output.Suits(optimisationUnit.Quantity))
                {
                    result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT304", path, "The constraint limit is in " + optimisationUnit.Symbol + " while " + Quote(output.Name) + " is in " + unit_Output.Symbol + ".", null));
                }
            }
        }

        private static void Unit(string unit, OptimisationQuantity quantity, string subject, string path, List<OptimisationDiagnostic> result)
        {
            if (string.IsNullOrWhiteSpace(unit))
            {
                return;
            }

            OptimisationUnit optimisationUnit = OptimisationUnit(unit, out bool synonym);
            if (optimisationUnit == null)
            {
                result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Warning, "OPT300", path + ".unit", "Unit " + Quote(unit) + " of " + subject + " is not recognised; it is shown as written and not checked.", KnownUnits()));
                return;
            }

            if (synonym)
            {
                result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT301", path + ".unit", "Unit " + Quote(unit) + " of " + subject + " is read as " + Quote(optimisationUnit.Symbol) + ".", null));
            }

            if (!optimisationUnit.Suits(quantity))
            {
                result.Add(Error("OPT302", path + ".unit", subject + " is declared as " + QuantityText(quantity) + ", but " + optimisationUnit.Symbol + " is a unit of " + QuantityNoun(optimisationUnit.Quantity) + ".", "Use one of: " + string.Join(", ", UnitSymbols(quantity)) + "; or correct the quantity."));
            }
        }

        private static void Method(OptimisationDefinition optimisationDefinition, List<OptimisationDiagnostic> result)
        {
            OptimisationMethod method = optimisationDefinition.Method;
            if (method == null)
            {
                result.Add(Error("OPT110", "$", "\"method\" is required in the definition but is missing.", "Add \"method\": { \"algorithm\": … }."));
                return;
            }

            List<DesignVariable> variables = (optimisationDefinition.Variables ?? new List<DesignVariable>()).FindAll(x => x != null);

            if (method is GoldenSectionMethod goldenSectionMethod)
            {
                bool finite = goldenSectionMethod.Tolerance == null || Finite(goldenSectionMethod.Tolerance.Value, "The golden section objective tolerance", "$.method.tolerance", result);
                if (finite && goldenSectionMethod.Tolerance != null && !(goldenSectionMethod.Tolerance.Value > 0))
                {
                    result.Add(Error("OPT401", "$.method.tolerance", "The golden section objective tolerance must be greater than 0; it is " + Number(goldenSectionMethod.Tolerance.Value, null) + ".", null));
                }

                List<string> names = variables.FindAll(x => x.Start != null || x.Step != null).ConvertAll(x => Quote(x.Name));
                if (names.Count != 0)
                {
                    result.Add(new OptimisationDiagnostic(DiagnosticSeverity.Info, "OPT408", "$.method", "Golden section uses only the bounds; the start and step of " + string.Join(", ", names) + " are kept but not used.", null));
                }

                return;
            }

            if (method is HookeJeevesMethod hookeJeevesMethod)
            {
                Minimum(hookeJeevesMethod.StepReductionFactor, 2, "stepReductionFactor", "The Hooke–Jeeves step reduction factor", result, "OPT402");
                Minimum(hookeJeevesMethod.InitialStepExponent, 0, "initialStepExponent", "The Hooke–Jeeves initial step exponent", result, "OPT403");
                Minimum(hookeJeevesMethod.StepExponentIncrement, 1, "stepExponentIncrement", "The Hooke–Jeeves step exponent increment", result, "OPT404");
                Minimum(hookeJeevesMethod.StepReductions, 1, "stepReductions", "The number of Hooke–Jeeves step reductions", result, "OPT405");

                List<DesignVariable> variables_All = optimisationDefinition.Variables ?? new List<DesignVariable>();
                for (int i = 0; i < variables_All.Count; i++)
                {
                    DesignVariable variable = variables_All[i];
                    if (variable == null)
                    {
                        continue;
                    }

                    if (variable.Start == null)
                    {
                        result.Add(Error("OPT406", Path("variables", i), "Hooke–Jeeves needs a start value for " + Subject(variable, i) + ".", "Add \"start\" within the range (" + Range(variable) + ")."));
                    }

                    if (variable.Step == null)
                    {
                        result.Add(Error("OPT407", Path("variables", i), "Hooke–Jeeves needs a step for " + Subject(variable, i) + ".", "Add \"step\", the initial distance the search moves (greater than 0)."));
                    }
                }
            }
        }

        private static void Capability(OptimisationDefinition optimisationDefinition, IOptimisationCapabilities capabilities, List<OptimisationDiagnostic> result)
        {
            string engine = string.IsNullOrWhiteSpace(capabilities.DisplayName) ? capabilities.Engine : capabilities.DisplayName;
            string notRunnable = ", so this definition cannot run.";

            string engine_Definition = optimisationDefinition.Model?.Engine;
            if (!string.IsNullOrWhiteSpace(engine_Definition) && !string.Equals(engine_Definition, capabilities.Engine, StringComparison.Ordinal))
            {
                result.Add(Error("OPT410", "$.model.engine", "This definition is for the " + Quote(engine_Definition) + " engine, not " + Quote(capabilities.Engine) + notRunnable, "Use \"engine\": \"" + capabilities.Engine + "\" if the model is meant for " + engine + "."));
            }

            OptimisationMethod method = optimisationDefinition.Method;
            if (method != null)
            {
                OptimisationAlgorithmCapability optimisationAlgorithmCapability = capabilities.Algorithms?.FirstOrDefault(x => x.Algorithm == method.Algorithm);
                List<string> algorithms = (capabilities.Algorithms ?? new List<OptimisationAlgorithmCapability>()).Select(x => AlgorithmText(x.Algorithm)).ToList();
                if (optimisationAlgorithmCapability == null)
                {
                    result.Add(Error("OPT411", "$.method.algorithm", AlgorithmText(method.Algorithm, true) + " is not available with the " + engine + " engine" + notRunnable, algorithms.Count == 0 ? null : "Choose " + string.Join(" or ", algorithms) + "."));
                }
                else
                {
                    int count = (optimisationDefinition.Variables ?? new List<DesignVariable>()).Count(x => x != null);
                    int minimum = optimisationAlgorithmCapability.MinimumVariables;
                    int? maximum = optimisationAlgorithmCapability.MaximumVariables;
                    if (count != 0 && (count < minimum || (maximum != null && count > maximum.Value)))
                    {
                        string accepts = maximum == minimum ? "exactly " + Count(minimum) : maximum == null ? "at least " + Count(minimum) : string.Format(CultureInfo.InvariantCulture, "{0} to {1} design variables", minimum, maximum);
                        List<string> others = (capabilities.Algorithms ?? new List<OptimisationAlgorithmCapability>()).Where(x => x.Algorithm != method.Algorithm && count >= x.MinimumVariables && (x.MaximumVariables == null || count <= x.MaximumVariables)).Select(x => AlgorithmText(x.Algorithm)).ToList();
                        string hint = count > (maximum ?? int.MaxValue) ? "Remove " + (count - maximum.Value == 1 ? "a variable" : (count - maximum.Value).ToString(CultureInfo.InvariantCulture) + " variables") : "Add a variable";
                        result.Add(Error("OPT412", "$.variables", AlgorithmText(method.Algorithm, true) + " optimises " + accepts + "; " + Count(count, true) + " defined.", hint + (others.Count == 0 ? "." : ", or choose " + string.Join(" or ", others) + ".")));
                    }
                }
            }

            OptimisationObjective objective = optimisationDefinition.Objective;
            if (objective != null && capabilities.Senses != null && !capabilities.Senses.Contains(objective.Sense))
            {
                string sense = objective.Sense == ObjectiveSense.Maximise ? "Maximise" : "Minimise";
                result.Add(Error("OPT413", "$.objective.sense", sense + " is not available with the " + engine + " engine in this version" + notRunnable, objective.Sense == ObjectiveSense.Maximise && capabilities.Senses.Contains(ObjectiveSense.Minimise) ? "Use \"minimise\": for example, minimise an output the model writes as the negative or the reciprocal of the value to maximise." : null));
            }

            List<OptimisationConstraint> constraints = optimisationDefinition.Constraints ?? new List<OptimisationConstraint>();
            if (!capabilities.SupportsConstraints)
            {
                for (int i = 0; i < constraints.Count; i++)
                {
                    OptimisationConstraint constraint = constraints[i];
                    if (constraint == null)
                    {
                        continue;
                    }

                    string on = string.IsNullOrWhiteSpace(constraint.Output) ? "A constraint" : "The constraint on " + Quote(constraint.Output);
                    result.Add(Error("OPT414", Path("constraints", i), on + " cannot be enforced by the " + engine + " engine in this version" + notRunnable, "Remove the constraint to run" + (string.IsNullOrWhiteSpace(constraint.Output) ? "." : "; keep " + Quote(constraint.Output) + " as a recorded output and check it in the results.")));
                }
            }

            List<DesignVariable> variables = optimisationDefinition.Variables ?? new List<DesignVariable>();
            for (int i = 0; i < variables.Count; i++)
            {
                DesignVariable variable = variables[i];
                if (variable != null && capabilities.VariableTypes != null && !capabilities.VariableTypes.Contains(variable.Type))
                {
                    result.Add(Error("OPT415", Path("variables", i) + ".type", Subject(variable, i) + " is " + Article(OptimisationNames.Text(variable.Type)) + " variable, which the " + engine + " engine does not run in this version" + notRunnable, "Use " + string.Join(" or ", capabilities.VariableTypes.Select(x => "\"" + OptimisationNames.Text(x) + "\"")) + "."));
                }
            }
        }

        /// <summary>
        /// False, with OPT214, when <paramref name="value"/> is NaN or infinite: such a value cannot be run, and the
        /// definition text cannot hold it (JSON has no NaN or infinity, so the writer leaves it out).
        /// </summary>
        private static bool Finite(double value, string subject, string path, List<OptimisationDiagnostic> result)
        {
            if (!double.IsNaN(value) && !double.IsInfinity(value))
            {
                return true;
            }

            result.Add(Error("OPT214", path, subject + " must be a finite number; it is " + Number(value, null) + ".", "Enter a number such as 35 or -5.5."));
            return false;
        }

        private static void Minimum(int? value, int minimum, string name, string subject, List<OptimisationDiagnostic> result, string code)
        {
            if (value != null && value.Value < minimum)
            {
                result.Add(Error(code, "$.method." + name, string.Format(CultureInfo.InvariantCulture, "{0} must be a whole number of at least {1}; it is {2}.", subject, minimum, value.Value), null));
            }
        }

        /// <summary>The output's quantity: as declared, otherwise that of its unit.</summary>
        private static OptimisationQuantity Quantity(OptimisationOutput output)
        {
            if (output.Quantity != OptimisationQuantity.Unspecified)
            {
                return output.Quantity;
            }

            return OptimisationUnit(output.Unit, out _)?.Quantity ?? OptimisationQuantity.Unspecified;
        }

        private static OptimisationDiagnostic Error(string code, string path, string message, string hint)
        {
            return new OptimisationDiagnostic(DiagnosticSeverity.Error, code, path, message, hint);
        }

        private static string Path(string list, int index)
        {
            return string.Format(CultureInfo.InvariantCulture, "$.{0}[{1}]", list, index);
        }

        private static string Quote(string text)
        {
            return "“" + text + "”";
        }

        /// <summary>"Setpoint" or "Design variable 2", as the subject of a sentence.</summary>
        private static string Subject(DesignVariable variable, int index)
        {
            return string.IsNullOrWhiteSpace(variable?.Name) ? Label("Design variable", null, index) : variable.Name;
        }

        private static string Label(string kind, string name, int index)
        {
            return string.IsNullOrWhiteSpace(name) ? string.Format(CultureInfo.InvariantCulture, "{0} {1}", kind, index + 1) : kind + " " + Quote(name);
        }

        private static string Range(DesignVariable variable)
        {
            return Number(variable.Minimum, null) + " to " + Number(variable.Maximum, variable.Unit);
        }

        /// <summary>A value as written (shortest round-trip text) with its unit, for messages.</summary>
        internal static string Number(double value, string unit)
        {
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(unit) || unit == "-" ? text : text + " " + unit;
        }

        private static string Count(int count, bool verb = false)
        {
            string text = count == 1 ? "one design variable" : count.ToString(CultureInfo.InvariantCulture) + " design variables";
            if (!verb)
            {
                return text;
            }

            return count == 1 ? "1 is" : count.ToString(CultureInfo.InvariantCulture) + " are";
        }

        /// <summary>"golden section" / "Golden section" and "Hooke–Jeeves".</summary>
        internal static string AlgorithmText(OptimisationAlgorithm optimisationAlgorithm, bool sentenceStart = false)
        {
            switch (optimisationAlgorithm)
            {
                case OptimisationAlgorithm.GoldenSection:
                    return sentenceStart ? "Golden section" : "golden section";
                case OptimisationAlgorithm.HookeJeeves:
                    return "Hooke–Jeeves";
            }

            return optimisationAlgorithm.ToString();
        }

        private static string QuantityText(OptimisationQuantity quantity)
        {
            switch (quantity)
            {
                case OptimisationQuantity.TemperatureDifference:
                    return "a temperature difference";
                case OptimisationQuantity.Energy:
                    return "an energy";
                case OptimisationQuantity.Angle:
                    return "an angle";
                case OptimisationQuantity.Area:
                    return "an area";
                case OptimisationQuantity.Unspecified:
                    return "no stated quantity";
                case OptimisationQuantity.Dimensionless:
                    return "dimensionless";
                case OptimisationQuantity.Carbon:
                    return "carbon (CO2e)";
                default:
                    return "a " + OptimisationNames.Text(quantity);
            }
        }

        /// <summary>"temperature", "temperature difference", "carbon (CO2e)" and so on, after "a unit of".</summary>
        private static string QuantityNoun(OptimisationQuantity quantity)
        {
            switch (quantity)
            {
                case OptimisationQuantity.Carbon:
                    return "carbon (CO2e)";
                case OptimisationQuantity.Dimensionless:
                    return "a dimensionless value";
                case OptimisationQuantity.Unspecified:
                    return "no stated quantity";
                default:
                    return OptimisationNames.Text(quantity).Replace('-', ' ');
            }
        }

        private static string Article(string word)
        {
            return ("aeiou".IndexOf(char.ToLowerInvariant(word[0])) >= 0 ? "an " : "a ") + word;
        }

        private static string KnownUnits()
        {
            return "Known units: " + string.Join(", ", optimisationUnits.Select(x => x.Symbol)) + ".";
        }
    }
}
