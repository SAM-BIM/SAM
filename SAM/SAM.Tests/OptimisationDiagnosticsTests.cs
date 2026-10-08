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

        // ---------- Model bindings (OPT6xx) ----------

        private static OptimisationDefinition Bound()
        {
            return OptimisationFixtures.Definition(OptimisationFixtures.BoundGoldenSection);
        }

        private static OptimisationDefinition Zones()
        {
            return OptimisationFixtures.Definition(OptimisationFixtures.BoundHookeJeeves);
        }

        private static OptimisationDefinition Choice()
        {
            return OptimisationFixtures.Definition(OptimisationFixtures.Choice);
        }

        [Fact]
        public void BoundFixtures_AreRunnable_OnATasModelEngine_WithTheModelCatalogue()
        {
            Assert.True(Bound().IsRunnable(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()));
            Assert.True(Zones().IsRunnable(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()));
            Assert.Empty(Zones().Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()));
        }

        [Fact]
        public void ChoiceFixture_IsAValidShape_ButNotRunnable_WhileNoEngineRunsDiscreteVariables()
        {
            Assert.Equal(new[] { "OPT415" }, OptimisationFixtures.Errors(Choice().Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue())));
            Assert.Empty(Choice().Diagnostics(OptimisationFixtures.TasModel(true), OptimisationFixtures.ModelCatalogue()));
        }

        [Fact]
        public void UnboundFixtures_HaveExactlyTheirOldFindings_OnTasScript()
        {
            OptimisationCatalogue names = new OptimisationCatalogue(new[] { new OptimisationCatalogueEntry("Setpoint") }, new[] { new OptimisationCatalogueEntry("Result") }, "Tas script");

            Assert.Equal(new[] { "OPT408" }, GoldenSection().Diagnostics(OptimisationFixtures.Tas).Select(x => x.Code));
            Assert.Equal(new[] { "OPT408" }, GoldenSection().Diagnostics(OptimisationFixtures.Tas, names).Select(x => x.Code));
            Assert.Empty(HookeJeeves().Diagnostics(OptimisationFixtures.Tas, OptimisationFixtures.ModelCatalogue()));
            Assert.True(HookeJeeves().IsRunnable(OptimisationFixtures.Tas, names));
        }

        [Fact]
        public void UnboundDefinition_OnATasModelEngine_NeedsEveryBinding()
        {
            List<OptimisationDiagnostic> diagnostics = HookeJeeves().Diagnostics(OptimisationFixtures.TasModel());

            Assert.Equal(new[] { "OPT410", "OPT608", "OPT608", "OPT608", "OPT608" }, OptimisationFixtures.Errors(diagnostics));
            OptimisationDiagnostic variable = diagnostics.First(x => x.Code == "OPT608");
            Assert.Equal("$.variables[0]", variable.Path);
            Assert.Equal("Setpoint does not say what it changes in the model (it has no \"target\"), so the Tas engine cannot run it.", variable.Message);
            Assert.Equal("CO2 does not say what it measures in the model's results (it has no \"measure\"), so the Tas engine cannot run it.", diagnostics.Last(x => x.Code == "OPT608").Message);
        }

        [Fact]
        public void BoundDefinition_OnTasScript_TakesNoBindings()
        {
            List<OptimisationDiagnostic> diagnostics = Bound().Diagnostics(OptimisationFixtures.Tas);

            Assert.Equal(new[] { "OPT410", "OPT601", "OPT601", "OPT601" }, OptimisationFixtures.Errors(diagnostics));
            OptimisationDiagnostic target = diagnostics.First(x => x.Code == "OPT601");
            Assert.Equal("$.variables[0].target", target.Path);
            Assert.Equal("Setpoint has a target, but the Tas engine takes no targets, so this definition cannot run.", target.Message);
            Assert.Equal("Remove \"target\", or use an engine that can change model items.", target.Hint);
            Assert.Equal("Cost has a measure, but the Tas engine takes no measures, so this definition cannot run.", diagnostics.First(x => x.Path == "$.outputs[0].measure").Message);
        }

        [Fact]
        public void OPT600_ABindingWithoutKind()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Kind = " ";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT600");

            Assert.Equal("$.variables[0].target.kind", optimisationDiagnostic.Path);
            Assert.Equal("The target of Setpoint has no \"kind\".", optimisationDiagnostic.Message);
            Assert.DoesNotContain(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()), x => x.Code == "OPT601" || x.Code == "OPT609");
        }

        [Fact]
        public void OPT601_UnknownKind_SuggestsTheClosest()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Kind = "tpd.controler.setpoint";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()), "OPT601");

            Assert.Equal("$.variables[0].target.kind", optimisationDiagnostic.Path);
            Assert.Equal("“tpd.controler.setpoint” (the target of Setpoint) is not a target the Tas engine can change, so this definition cannot run.", optimisationDiagnostic.Message);
            Assert.Equal("Did you mean \"tpd.controller.setpoint\"? Targets: tbd.internal-condition.heating-setpoint, tbd.internal-condition.cooling-setpoint, tbd.glazing-construction.g-value, tpd.controller.setpoint, tbd.glazing-construction.choice.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT601_AMeasureUsedAsATarget()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Kind = "tpd.annual-cost";

            Assert.Equal("“tpd.annual-cost” is a measure, not a target, so it cannot be the target of Setpoint.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT601").Message);
        }

        [Fact]
        public void OPT602_MissingOrEmptyReference()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Reference.Remove("controller");

            OptimisationDiagnostic missing = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT602");
            Assert.Equal("$.variables[0].target", missing.Path);
            Assert.Equal("The target of Setpoint does not say which controller (\"controller\" is missing from \"reference\").", missing.Message);
            Assert.Equal("Add \"controller\" with the name of the controller as it is in the model.", missing.Hint);

            optimisationDefinition.Variables[0].Target.Reference["controller"] = " ";
            OptimisationDiagnostic empty = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT602");
            Assert.Equal("$.variables[0].target.reference.controller", empty.Path);
            Assert.Equal("The target of Setpoint names an empty controller (\"controller\" is \" \").", empty.Message);
        }

        [Fact]
        public void OPT603_UnknownReferenceKey()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Reference["plantroom"] = "Plant Room 1";
            optimisationDefinition.Outputs[0].Measure.Reference["zone"] = "Office";

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()).FindAll(x => x.Code == "OPT603");

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal("$.variables[0].target.reference.plantroom", diagnostics[0].Path);
            Assert.Equal("\"plantroom\" is not a reference key of plant controller setpoint (the target of Setpoint).", diagnostics[0].Message);
            Assert.Equal("Did you mean \"plantRoom\"? Keys: plantRoom, controller.", diagnostics[0].Hint);
            Assert.Equal("This measure refers to the whole model: remove \"reference\".", diagnostics[1].Hint);
        }

        [Fact]
        public void OPT604_UnknownParameter()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Output("Overheating").Measure.Parameters["treshold"] = 26;
            optimisationDefinition.Output("Plant energy").Measure.Parameters["x"] = 1;

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()).FindAll(x => x.Code == "OPT604");

            Assert.Equal(2, diagnostics.Count);
            Assert.Equal("$.outputs[0].measure.parameters.x", diagnostics[0].Path);
            Assert.Equal("This measure takes no parameters: remove \"x\".", diagnostics[0].Hint);
            Assert.Equal("\"treshold\" is not a parameter of overheating hours (the measure of Overheating).", diagnostics[1].Message);
            Assert.Equal("Did you mean \"threshold\"? Parameters: threshold.", diagnostics[1].Hint);
        }

        [Theory]
        [InlineData(50.0, "The \"threshold\" of the measure of Overheating must be from 20 to 40 °C; it is 50 °C.")]
        [InlineData(19.5, "The \"threshold\" of the measure of Overheating must be from 20 to 40 °C; it is 19.5 °C.")]
        public void OPT605_ParameterOutOfRange(double value, string message)
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Output("Overheating").Measure.Parameters["threshold"] = value;

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT605");

            Assert.Equal("$.outputs[3].measure.parameters.threshold", optimisationDiagnostic.Path);
            Assert.Equal(message, optimisationDiagnostic.Message);
            Assert.Equal("Leave it out to use 28 °C.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT605_TheRangeLimitsThemselvesAreAccepted()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            foreach (double value in new[] { 20.0, 40.0 })
            {
                optimisationDefinition.Output("Overheating").Measure.Parameters["threshold"] = value;
                Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.TasModel()));
            }
        }

        [Fact]
        public void OPT214_ANonFiniteParameter()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Output("Overheating").Measure.Parameters["threshold"] = double.NaN;

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel());

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT214");
            Assert.Equal("$.outputs[3].measure.parameters.threshold", optimisationDiagnostic.Path);
            Assert.Equal("The \"threshold\" of the measure of Overheating must be a finite number; it is NaN.", optimisationDiagnostic.Message);
            Assert.DoesNotContain(diagnostics, x => x.Code == "OPT605");
        }

        [Fact]
        public void OPT606_QuantityOfTheKind()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[0].Quantity = OptimisationQuantity.Energy;

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT606");

            Assert.Equal("$.variables[0].quantity", optimisationDiagnostic.Path);
            Assert.Equal("Heating setpoint is declared as an energy, but zone heating setpoint is a temperature.", optimisationDiagnostic.Message);
            Assert.Equal("Use \"quantity\": \"temperature\", or leave it out.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT606_UnitOfTheKind_NoConversion()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Output("Plant energy").Unit = "MWh";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT606");

            Assert.Equal("$.outputs[0].unit", optimisationDiagnostic.Path);
            Assert.Equal("Plant energy is declared in MWh, but the Tas engine reports annual plant energy in kWh.", optimisationDiagnostic.Message);
            Assert.Equal("Use \"unit\": \"kWh\", or leave it out.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT606_ASynonymOrNoUnit_IsTheKindsUnit()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[0].Unit = "degC";
            optimisationDefinition.Variables[1].Unit = null;
            optimisationDefinition.Variables[1].Quantity = OptimisationQuantity.Unspecified;

            Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.TasModel()));
            Assert.DoesNotContain(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), x => x.Code == "OPT606");
        }

        [Fact]
        public void OPT606_CarbonDeclaredAsAMass_IsAccepted()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Output("CO2").Quantity = OptimisationQuantity.Mass;

            Assert.DoesNotContain(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), x => x.Code == "OPT606");
        }

        [Fact]
        public void OPT607_TheSameTargetTwice()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[1].Target = new OptimisationTarget("tbd.internal-condition.heating-setpoint", OptimisationFixtures.Reference("internalCondition", "Office"));

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT607");

            Assert.Equal("$.variables[1].target", optimisationDiagnostic.Path);
            Assert.Equal("Cooling setpoint changes the same model item as Heating setpoint: “tbd.internal-condition.heating-setpoint” (internalCondition “Office”).", optimisationDiagnostic.Message);
            Assert.Equal("Cooling setpoint changes the same model item as Heating setpoint: “tbd.internal-condition.heating-setpoint” (internalCondition “Office”).", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT607").Message);
        }

        [Fact]
        public void OPT607_ReferenceKeyOrderDoesNotMatter_ParametersAreIgnored()
        {
            OptimisationTarget target = new OptimisationTarget("k", OptimisationFixtures.Reference("a", "1", "b", "2"), new Dictionary<string, double>() { { "p", 1 } });

            Assert.True(target.SameItem(new OptimisationTarget("k", OptimisationFixtures.Reference("b", "2", "a", "1"))));
            Assert.False(target.SameItem(new OptimisationTarget("k", OptimisationFixtures.Reference("a", "1"))));
            Assert.False(target.SameItem(new OptimisationTarget("k", OptimisationFixtures.Reference("a", "1", "b", "3"))));
            Assert.False(target.SameItem(new OptimisationTarget("K", OptimisationFixtures.Reference("a", "1", "b", "2"))));
        }

        [Fact]
        public void OPT607_TheSameMeasureTwice_IsAllowed()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Outputs.Add(new OptimisationOutput("Overheating 26") { Measure = new OptimisationMeasure("tsd.overheating-hours", null, new Dictionary<string, double>() { { "threshold", 26 } }) });

            Assert.True(optimisationDefinition.IsRunnable(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()));
        }

        [Fact]
        public void OPT609_AKindTheModelDoesNotOffer()
        {
            OptimisationCatalogue catalogue = OptimisationFixtures.ModelCatalogue();
            OptimisationCatalogue withoutPlant = new OptimisationCatalogue(catalogue.Variables, catalogue.Outputs.Where(x => !x.Measure.Kind.StartsWith("tpd.")), catalogue.Source);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(Zones().Diagnostics(OptimisationFixtures.TasModel(), withoutPlant), "OPT609");

            Assert.Equal("$.outputs[0].measure.kind", optimisationDiagnostic.Path);
            Assert.Equal("Annual plant energy is not available in this model, so Plant energy cannot be measured.", optimisationDiagnostic.Message);
            Assert.Equal("Choose one of: Annual heating demand, Annual cooling demand, Overheating hours.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT609_AModelItemThatIsNotInTheModel_SuggestsTheName()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[0].Target.Reference["internalCondition"] = "office";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue()), "OPT609");

            Assert.Equal("$.variables[0].target.reference.internalCondition", optimisationDiagnostic.Path);
            Assert.Equal("Internal condition “office” is not in this model, so Heating setpoint cannot be changed.", optimisationDiagnostic.Message);
            Assert.Equal("Did you mean “Office”? Available: “Office”, “Meeting room”.", optimisationDiagnostic.Hint);

            // Without the engine's capabilities the key has no display name.
            Assert.Equal("“office” (\"internalCondition\") is not in this model, so Heating setpoint cannot be changed.", OptimisationFixtures.Single(optimisationDefinition.Diagnostics(null, OptimisationFixtures.ModelCatalogue()), "OPT609").Message);
        }

        [Fact]
        public void OPT609_ACombinationThatIsNotInTheModel()
        {
            OptimisationCatalogue catalogue = new OptimisationCatalogue(
                new[]
                {
                    new OptimisationCatalogueEntry("A", new OptimisationTarget("tpd.controller.setpoint", OptimisationFixtures.Reference("plantRoom", "P1", "controller", "C1"))),
                    new OptimisationCatalogueEntry("B", new OptimisationTarget("tpd.controller.setpoint", OptimisationFixtures.Reference("plantRoom", "P2", "controller", "C2"))),
                },
                OptimisationFixtures.ModelCatalogue().Outputs);
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Reference = OptimisationFixtures.Reference("plantRoom", "P1", "controller", "C2");

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(), catalogue), "OPT609");

            Assert.Equal("$.variables[0].target.reference", optimisationDiagnostic.Path);
            Assert.Equal("This model has no plant controller setpoint (plant room “P1”, controller “C2”), so Setpoint cannot be changed.", optimisationDiagnostic.Message);
            Assert.Equal("Available: plant controller setpoint (plant room “P1”, controller “C1”), plant controller setpoint (plant room “P2”, controller “C2”).", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT609_IsLeftToOPT601_ForAKindTheEngineDoesNotList()
        {
            OptimisationDefinition optimisationDefinition = Bound();
            optimisationDefinition.Variables[0].Target.Kind = "tbd.unknown";

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());

            OptimisationFixtures.Single(diagnostics, "OPT601");
            Assert.DoesNotContain(diagnostics, x => x.Code == "OPT609");
        }

        [Fact]
        public void OPT610_OptionsOnAValueTarget()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[0].Target.Options = new List<string>() { "A", "B" };

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel()), "OPT610");

            Assert.Equal("$.variables[0].target.options", optimisationDiagnostic.Path);
            Assert.Equal("Zone heating setpoint takes a value, not a choice between options, so the options of Heating setpoint cannot be used.", optimisationDiagnostic.Message);
        }

        [Theory]
        [InlineData(0, "Glazing construction choice is a choice between options, and Glazing lists none; at least two are needed.")]
        [InlineData(1, "Glazing construction choice is a choice between options, and Glazing lists only one; at least two are needed.")]
        public void OPT611_AChoiceNeedsTwoOptions(int count, string message)
        {
            OptimisationDefinition optimisationDefinition = Choice();
            optimisationDefinition.Variables[0].Target.Options = optimisationDefinition.Variables[0].Target.Options.Take(count).ToList();

            Assert.Equal(message, OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(true)), "OPT611").Message);
        }

        [Fact]
        public void OPT612_OptionsNeedADiscreteVariable()
        {
            OptimisationDefinition optimisationDefinition = Choice();
            optimisationDefinition.Variables[0].Type = DesignVariableType.Continuous;

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT612");

            Assert.Equal("$.variables[0].type", optimisationDiagnostic.Path);
            Assert.Equal("Glazing chooses between options, so its type must be \"discrete\"; it is \"continuous\".", optimisationDiagnostic.Message);
        }

        [Fact]
        public void OPT613_AnOptionRepeatedOrWithoutName()
        {
            OptimisationDefinition optimisationDefinition = Choice();
            optimisationDefinition.Variables[0].Target.Options = new List<string>() { "Double low-e", "Double low-e", " " };

            List<OptimisationDiagnostic> diagnostics = optimisationDefinition.Diagnostics().FindAll(x => x.Code == "OPT613");

            Assert.Equal(new[] { "$.variables[0].target.options[1]", "$.variables[0].target.options[2]" }, diagnostics.Select(x => x.Path));
            Assert.Equal("Option “Double low-e” of Glazing is listed more than once.", diagnostics[0].Message);
            Assert.Equal("Option 3 of Glazing has no name.", diagnostics[1].Message);
        }

        [Fact]
        public void OPT614_AnOptionTheModelDoesNotOffer()
        {
            OptimisationDefinition optimisationDefinition = Choice();
            optimisationDefinition.Variables[0].Target.Options[1] = "Triple low e";

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(OptimisationFixtures.TasModel(true), OptimisationFixtures.ModelCatalogue()), "OPT614");

            Assert.Equal("$.variables[0].target.options[1]", optimisationDiagnostic.Path);
            Assert.Equal("Option “Triple low e” of Glazing is not available for glazing construction choice (glazing construction “Office glazing”).", optimisationDiagnostic.Message);
            Assert.Equal("Did you mean “Triple low-e”? Available: “Double low-e”, “Triple low-e”, “Double solar control”.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void OPT615_AChoiceIsNumberedOneToTheNumberOfOptions()
        {
            OptimisationDefinition optimisationDefinition = Choice();
            optimisationDefinition.Variables[0].Maximum = 4;

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(optimisationDefinition.Diagnostics(), "OPT615");

            Assert.Equal("$.variables[0].minimum", optimisationDiagnostic.Path);
            Assert.Equal("Glazing chooses between 3 options, numbered 1 to 3, so its minimum must be 1 and its maximum 3; they are 1 and 4.", optimisationDiagnostic.Message);
            Assert.Equal("Use \"minimum\": 1 and \"maximum\": 3.", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void WithoutCapabilities_NoBindingCapabilityFindings()
        {
            OptimisationDefinition optimisationDefinition = Zones();
            optimisationDefinition.Variables[0].Target.Kind = "tbd.unknown";
            optimisationDefinition.Variables[0].Target.Reference["x"] = "y";
            optimisationDefinition.Output("Overheating").Measure.Parameters["threshold"] = 99;
            optimisationDefinition.Output("Plant energy").Unit = "MWh";

            Assert.Empty(optimisationDefinition.Diagnostics());
        }

        [Fact]
        public void IsRunnable_WithACatalogue_RequiresCapabilities()
        {
            Assert.Throws<System.ArgumentNullException>(() => Zones().IsRunnable(null, OptimisationFixtures.ModelCatalogue()));
        }

        [Fact]
        public void Diagnostic_ToString_IsReadable()
        {
            OptimisationDiagnostic optimisationDiagnostic = new OptimisationDiagnostic(DiagnosticSeverity.Error, "OPT204", "$.variables[0].minimum", "Setpoint range is invalid.", "Swap the two values.", 14, 7);

            Assert.Equal("Error OPT204 at $.variables[0].minimum (line 14, column 7): Setpoint range is invalid. Swap the two values.", optimisationDiagnostic.ToString());
        }
    }
}
