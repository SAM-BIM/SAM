// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Meaning (OPT2xx), unit (OPT3xx) and method/capability (OPT4xx) findings, worded for engineers. A definition that
    /// asks for more than the engine runs (maximise, constraints, other variable types) stays readable and editable but
    /// is not runnable.
    /// </summary>
    public class OptimisationDiagnosticsTests
    {
        private static OptimisationDefinition GoldenSection()
        {
            return OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.GoldenSection), out _);
        }

        private static OptimisationDefinition HookeJeeves()
        {
            return OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves), out _);
        }

        [Fact]
        public void Fixtures_AreRunnable_OnTheTasLikeEngine()
        {
            Assert.True(GoldenSection().IsRunnable(OptimisationFixtures.Tas));
            Assert.True(HookeJeeves().IsRunnable(OptimisationFixtures.Tas));
        }

        // ---------- Meaning ----------

        [Fact]
        public void Range_MinimumAboveMaximum_NamesTheVariableAndUnit()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables[0].Unit = "°C";
            optimisationDefinition.Variables[0].Minimum = 35;
            optimisationDefinition.Variables[0].Maximum = 25;

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT204");

            Assert.Equal("Setpoint range is invalid: minimum 35 °C is greater than maximum 25 °C.", optimisationDiagnostic.Message);
            Assert.Equal("$.variables[0].minimum", optimisationDiagnostic.Path);
            Assert.Equal(DiagnosticSeverity.Error, optimisationDiagnostic.Severity);
        }

        [Fact]
        public void Range_Empty_IsReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables[0].Minimum = 20;
            optimisationDefinition.Variables[0].Maximum = 20;

            Assert.Equal("Setpoint range is invalid: minimum and maximum are both 20, so it cannot change.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT204").Message);
        }

        [Fact]
        public void Start_OutsideTheRange_IsReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables[0].Start = 40;

            Assert.Equal("Setpoint start 40 is outside its range (-5 to 35).", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT205").Message);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-1.0)]
        public void Step_NotPositive_IsReported(double step)
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables[0].Step = step;

            OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT206");
        }

        [Theory]
        [InlineData("minimum", double.NaN)]
        [InlineData("minimum", double.NegativeInfinity)]
        [InlineData("maximum", double.PositiveInfinity)]
        [InlineData("start", double.NaN)]
        [InlineData("step", double.PositiveInfinity)]
        public void NonFiniteVariableNumber_IsReported_AndNotRunnable(string field, double value)
        {
            //A definition built in code (not read from text) can hold NaN or infinity; the text cannot.
            OptimisationDefinition optimisationDefinition = HookeJeeves();
            DesignVariable variable = optimisationDefinition.Variables[0];
            switch (field)
            {
                case "minimum": variable.Minimum = value; break;
                case "maximum": variable.Maximum = value; break;
                case "start": variable.Start = value; break;
                default: variable.Step = value; break;
            }

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.Tas);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT214");
            Assert.Equal("$.variables[0]." + field, optimisationDiagnostic.Path);
            Assert.StartsWith("Setpoint " + field + " must be a finite number; it is ", optimisationDiagnostic.Message);
            Assert.DoesNotContain(diagnostics, x => x.Code == "OPT204" || x.Code == "OPT205" || x.Code == "OPT206");
            Assert.False(diagnostics.IsRunnable());
            Assert.False(optimisationDefinition.IsRunnable(OptimisationFixtures.Tas));
        }

        [Fact]
        public void NonFiniteToleranceOrConstraintLimit_IsReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            ((GoldenSectionMethod)optimisationDefinition.Method).Tolerance = double.PositiveInfinity;
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "Cost", AtMost = double.NaN });

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics();

            Assert.Equal(new[] { "$.method.tolerance", "$.constraints[0].atMost" },diagnostics.Where(x => x.Code == "OPT214").Select(x => x.Path).OrderByDescending(x => x));
            Assert.DoesNotContain(diagnostics, x => x.Code == "OPT401");
            Assert.False(diagnostics.IsRunnable());
        }

        [Fact]
        public void Names_MissingOrRepeated_AreReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables.Add(new DesignVariable("Setpoint", 0, 1));
            optimisationDefinition.Variables.Add(new DesignVariable(" ", 0, 1));
            optimisationDefinition.Outputs.Add(new OptimisationOutput("Cost"));
            optimisationDefinition.Outputs.Add(new OptimisationOutput(null));

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics();

            Assert.Equal("Two design variables are named “Setpoint”.", OptimisationFixtures.Single(diagnostics, "OPT203").Message);
            Assert.Equal("Design variable 3 has no name.", OptimisationFixtures.Single(diagnostics, "OPT202").Message);
            Assert.Equal("Two outputs are named “Cost”.", OptimisationFixtures.Single(diagnostics, "OPT209").Message);
            Assert.Equal("Output 5 has no name.", OptimisationFixtures.Single(diagnostics, "OPT208").Message);
        }

        [Fact]
        public void NoVariablesOrOutputs_AreReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables.Clear();
            optimisationDefinition.Outputs.Clear();

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics();

            OptimisationFixtures.Single(diagnostics, "OPT201");
            OptimisationFixtures.Single(diagnostics, "OPT207");
        }

        [Fact]
        public void Objective_ReferringToAMissingOutput_ListsTheOutputs()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Objective.Output = "Energy";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT210");

            Assert.Equal("The objective refers to output “Energy”, which is not in the outputs.", optimisationDiagnostic.Message);
            Assert.Equal("Add it to the outputs, or choose one of: Result, Cost, CO2.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void Constraint_Errors_AreReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "OverheatingHours", AtMost = 100 });
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "Cost", AtMost = 100, AtLeast = 10 });
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "Cost" });

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics();

            OptimisationFixtures.Single(diagnostics, "OPT211");
            Assert.Equal(2, diagnostics.Count(x => x.Code == "OPT212"));
        }

        [Fact]
        public void MaximumSimulations_BelowOne_IsReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Stopping.MaximumSimulations = 0;

            OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT213");
        }

        // ---------- Units ----------

        [Fact]
        public void UnknownUnit_IsAWarning_NotAnError()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Outputs[2].Unit = "furlongs";

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.Tas);

            Assert.Equal(DiagnosticSeverity.Warning, OptimisationFixtures.Single(diagnostics, "OPT300").Severity);
            Assert.True(diagnostics.IsRunnable());
        }

        [Fact]
        public void Unit_NotSuitingTheDeclaredQuantity_IsAnError()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables[0].Quantity = OptimisationQuantity.Temperature;
            optimisationDefinition.Variables[0].Unit = "kWh";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT302");

            Assert.Equal("Setpoint is declared as a temperature, but kWh is a unit of energy.", optimisationDiagnostic.Message);
            Assert.StartsWith("Use one of: °C, °F;", optimisationDiagnostic.Hint);
        }

        [Theory]
        [InlineData(OptimisationQuantity.Carbon, "kg")]
        [InlineData(OptimisationQuantity.Mass, "kgCO2e")]
        [InlineData(OptimisationQuantity.Currency, "GBP")]
        [InlineData(OptimisationQuantity.Unspecified, "kWh")]
        public void Unit_SuitingTheQuantity_IsAccepted(OptimisationQuantity quantity, string unit)
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Outputs[2].Quantity = quantity;
            optimisationDefinition.Outputs[2].Unit = unit;

            Assert.DoesNotContain(optimisationDefinition.Diagnostics(), x => x.Code.StartsWith("OPT30"));
        }

        [Fact]
        public void ConstraintUnit_IncompatibleWithTheOutput_IsAnError()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Outputs.Add(new OptimisationOutput("OverheatingHours") { Unit = "h" });
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "OverheatingHours", AtMost = 100, Unit = "kWh" });

            Assert.Contains("is in kWh, but the output is a time; kWh is a unit of energy.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT303").Message);
        }

        [Fact]
        public void ConstraintUnit_DifferentButCompatible_IsANote()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Outputs.Add(new OptimisationOutput("AnnualEnergy") { Unit = "kWh" });
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "AnnualEnergy", AtMost = 10, Unit = "MWh" });

            Assert.Equal(DiagnosticSeverity.Info, OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT304").Severity);
        }

        [Fact]
        public void UnitCatalogue_KnowsTheEngineeringUnits()
        {
            Assert.Equal(OptimisationQuantity.Temperature, Core.Optimisation.Query.OptimisationUnit("°C", out bool synonym).Quantity);
            Assert.False(synonym);
            Assert.Equal("°C", Core.Optimisation.Query.OptimisationUnit("degC", out synonym).Symbol);
            Assert.True(synonym);
            Assert.Equal(Units.UnitType.Celsius, Core.Optimisation.Query.OptimisationUnit("°C", out _).UnitType);
            Assert.Equal(Units.UnitType.Undefined, Core.Optimisation.Query.OptimisationUnit("GBP", out _).UnitType);
            Assert.Null(Core.Optimisation.Query.OptimisationUnit("mWh", out _));
            Assert.Equal(OptimisationQuantity.Energy, Core.Optimisation.Query.OptimisationUnit("MWh", out _).Quantity);
        }

        [Theory]
        [InlineData("kWh", Units.UnitType.KilowattHour)]
        [InlineData("MWh", Units.UnitType.MegawattHour)]
        [InlineData("kg", Units.UnitType.Kilogram)]
        [InlineData("t", Units.UnitType.Tonne)]
        [InlineData("kgCO2e", Units.UnitType.Kilogram)]
        public void UnitCatalogue_ResolvesTheEnergyAndMassUnitTypes_OfSAMUnits(string symbol, Units.UnitType unitType)
        {
            //Resolved by name at run time: these exist since SAM#186 (energy and mass units).
            Assert.Equal(unitType, Core.Optimisation.Query.OptimisationUnit(symbol, out _).UnitType);
        }

        // ---------- Method ----------

        [Fact]
        public void GoldenSection_ToleranceNotPositive_IsReported()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            ((GoldenSectionMethod)optimisationDefinition.Method).Tolerance = 0;

            Assert.Equal("The golden section objective tolerance must be greater than 0; it is 0.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT401").Message);
        }

        [Fact]
        public void GoldenSection_StartAndStep_AreANote()
        {
            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(GoldenSection().Diagnostics(), "OPT408");

            Assert.Equal(DiagnosticSeverity.Info, optimisationDiagnostic.Severity);
            Assert.Equal("Golden section uses only the bounds; the start and step of “Setpoint” are kept but not used.", optimisationDiagnostic.Message);
        }

        [Theory]
        [InlineData(1, 0, 1, 4, "OPT402")]
        [InlineData(2, -1, 1, 4, "OPT403")]
        [InlineData(2, 0, 0, 4, "OPT404")]
        [InlineData(2, 0, 1, 0, "OPT405")]
        public void HookeJeeves_SettingDomains(int stepReductionFactor, int initialStepExponent, int stepExponentIncrement, int stepReductions, string code)
        {
            OptimisationDefinition optimisationDefinition = HookeJeeves();
            optimisationDefinition.Method = new HookeJeevesMethod() { StepReductionFactor = stepReductionFactor, InitialStepExponent = initialStepExponent, StepExponentIncrement = stepExponentIncrement, StepReductions = stepReductions };

            Assert.Equal(DiagnosticSeverity.Error, OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), code).Severity);
        }

        [Fact]
        public void HookeJeeves_NeedsStartAndStep()
        {
            OptimisationDefinition optimisationDefinition = HookeJeeves();
            optimisationDefinition.Variables[0].Start = null;
            optimisationDefinition.Variables[0].Step = null;

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics();

            Assert.Equal("Hooke–Jeeves needs a start value for Setpoint.", OptimisationFixtures.Single(diagnostics, "OPT406").Message);
            OptimisationFixtures.Single(diagnostics, "OPT407");
        }

        [Fact]
        public void HookeJeeves_DefaultSettings_AreValid()
        {
            OptimisationDefinition optimisationDefinition = HookeJeeves();
            optimisationDefinition.Method = new HookeJeevesMethod();

            Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.Tas));
        }

        // ---------- Capability ----------

        [Fact]
        public void GoldenSection_WithTwoVariables_IsNotRunnable_WithTheEngineeringMessage()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Variables.Add(new DesignVariable("Other", 0, 1));

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.Tas), "OPT412");

            Assert.Equal("Golden section optimises exactly one design variable; 2 are defined.", optimisationDiagnostic.Message);
            Assert.Equal("Remove a variable, or choose Hooke–Jeeves.", optimisationDiagnostic.Hint);
            Assert.False(optimisationDefinition.IsRunnable(OptimisationFixtures.Tas));
        }

        [Fact]
        public void Maximise_IsReadable_ButNotRunnable_OnAMinimiseOnlyEngine()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.GoldenSectionWith("\"sense\": \"minimise\"", "\"sense\": \"maximise\""), out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT413");
            Assert.Equal("Maximise is not available with the Tas engine in this version, so this definition cannot run.", optimisationDiagnostic.Message);
            Assert.Equal(ObjectiveSense.Maximise, optimisationDefinition.Objective.Sense);
            Assert.False(diagnostics.IsRunnable());
            Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.Everything));
        }

        [Fact]
        public void Constraint_IsReadable_ButNotRunnable_OnAnEngineWithoutConstraints()
        {
            string text = OptimisationFixtures.GoldenSectionWith("  \"method\": {", "  \"constraints\": [\n    {\n      \"output\": \"CO2\",\n      \"atMost\": 5000\n    }\n  ],\n  \"method\": {");

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT414");
            Assert.Equal("The constraint on “CO2” cannot be enforced by the Tas engine in this version, so this definition cannot run.", optimisationDiagnostic.Message);
            Assert.Equal("Remove the constraint to run; keep “CO2” as a recorded output and check it in the results.", optimisationDiagnostic.Hint);
            Assert.Equal(43, optimisationDiagnostic.Line);
            Assert.Single(optimisationDefinition.Constraints);
            Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.Everything));
        }

        [Fact]
        public void OtherVariableType_IsNotRunnable()
        {
            OptimisationDefinition optimisationDefinition = HookeJeeves();
            optimisationDefinition.Variables[0].Type = DesignVariableType.Integer;

            Assert.Equal("Setpoint is an integer variable, which the Tas engine does not run in this version, so this definition cannot run.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.Tas), "OPT415").Message);
        }

        [Fact]
        public void OtherEngine_IsNotRunnable()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Model.Engine = "energyplus";

            OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.Tas), "OPT410");
        }

        [Fact]
        public void UnsupportedAlgorithm_IsNotRunnable()
        {
            OptimisationCapabilities capabilities = new OptimisationCapabilities("tas-script", "Tas", new[] { new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null) }, new[] { ObjectiveSense.Minimise }, new[] { DesignVariableType.Continuous }, false);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(GoldenSection().Diagnostics(capabilities), "OPT411");

            Assert.Equal("Golden section is not available with the Tas engine, so this definition cannot run.", optimisationDiagnostic.Message);
            Assert.Equal("Choose Hooke–Jeeves.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void WithoutCapabilities_NoCapabilityFindings()
        {
            OptimisationDefinition optimisationDefinition = GoldenSection();
            optimisationDefinition.Objective.Sense = ObjectiveSense.Maximise;
            optimisationDefinition.Variables.Add(new DesignVariable("Other", 0, 1));

            Assert.DoesNotContain(optimisationDefinition.Diagnostics(), x => x.Code.StartsWith("OPT41"));
        }

        [Fact]
        public void Diagnostic_ToString_IsReadable()
        {
            OptimisationDiagnostic optimisationDiagnostic = new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT204", "$.variables[0].minimum", "Setpoint range is invalid.", "Swap the two values.", 14, 7);

            Assert.Equal("Error OPT204 at $.variables[0].minimum (line 14, column 7): Setpoint range is invalid. Swap the two values.", optimisationDiagnostic.ToString());
        }
    }
}
