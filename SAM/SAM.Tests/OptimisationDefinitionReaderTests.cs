// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Reading the definition text (schema sam.optimisation/1): the proven Systems Demo examples, and every text and
    /// structure finding (OPT1xx) with its line and column. A definition that cannot be read is never returned.
    /// </summary>
    public class OptimisationDefinitionReaderTests
    {
        [Fact]
        public void SystemsDemo_GoldenSection_IsReadExactly()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.GoldenSection), out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas);

            Assert.Equal("tas-script", optimisationDefinition.Model.Engine);

            DesignVariable variable = Assert.Single(optimisationDefinition.Variables);
            Assert.Equal("Setpoint", variable.Name);
            Assert.Equal(DesignVariableType.Continuous, variable.Type);
            Assert.Equal(OptimisationQuantity.Unspecified, variable.Quantity);
            Assert.Null(variable.Unit);
            Assert.Equal(-5, variable.Minimum);
            Assert.Equal(35, variable.Maximum);
            Assert.Equal(3, variable.Start);
            Assert.Equal(1, variable.Step);

            Assert.Equal(new[] { "Result", "Cost", "CO2" }, optimisationDefinition.Outputs.Select(x => x.Name));
            Assert.Equal("GBP", optimisationDefinition.Output("Cost").Unit);
            Assert.Equal(OptimisationQuantity.Currency, optimisationDefinition.Output("Result").Quantity);
            Assert.Null(optimisationDefinition.Output("CO2").Unit);

            Assert.Equal("Result", optimisationDefinition.Objective.Output);
            Assert.Equal(ObjectiveSense.Minimise, optimisationDefinition.Objective.Sense);
            Assert.Equal(new[] { "Cost", "CO2" }, optimisationDefinition.RecordedOutputs().Select(x => x.Name));

            GoldenSectionMethod goldenSectionMethod = Assert.IsType<GoldenSectionMethod>(optimisationDefinition.Method);
            Assert.Equal(0.1, goldenSectionMethod.Tolerance);
            Assert.Equal(2000, optimisationDefinition.Stopping.MaximumSimulations);
            Assert.Empty(optimisationDefinition.Constraints);

            // Runnable on the Tas-like engine; the only note is that golden section does not use start and step.
            Assert.True(diagnostics.IsRunnable());
            Assert.Equal("OPT408", Assert.Single(diagnostics).Code);
        }

        [Fact]
        public void SystemsDemo_HookeJeeves_IsReadExactly()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves), out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas);

            HookeJeevesMethod hookeJeevesMethod = Assert.IsType<HookeJeevesMethod>(optimisationDefinition.Method);
            Assert.Equal(2, hookeJeevesMethod.StepReductionFactor);
            Assert.Equal(0, hookeJeevesMethod.InitialStepExponent);
            Assert.Equal(1, hookeJeevesMethod.StepExponentIncrement);
            Assert.Equal(4, hookeJeevesMethod.StepReductions);
            Assert.Equal(10, optimisationDefinition.Variables[0].Start);
            Assert.Equal(2, optimisationDefinition.Variables[0].Step);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData("", "OPT101")]
        [InlineData("   ", "OPT101")]
        [InlineData("[1, 2]", "OPT101")]
        [InlineData("\"text\"", "OPT101")]
        [InlineData("{ \"schema\": \"sam.optimisation/1\", }}", "OPT100")]
        [InlineData("{ \"schema\": \"sam.optimisation/1\"", "OPT100")]
        [InlineData("{ \"schema\": 'sam.optimisation/1' }", "OPT100")]
        [InlineData("{ \"minimum\": NaN }", "OPT100")]
        [InlineData("{ } { }", "OPT100")]
        public void InvalidText_IsNotRead(string text, string code)
        {
            OptimisationDefinition optimisationDefinition = Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics);

            Assert.Null(optimisationDefinition);
            OptimisationDiagnostic optimisationDiagnostic = Assert.Single(diagnostics);
            Assert.Equal(code, optimisationDiagnostic.Code);
            Assert.Equal(DiagnosticSeverity.Error, optimisationDiagnostic.Severity);
            Assert.NotNull(optimisationDiagnostic.Line);
        }

        [Fact]
        public void SyntaxError_HasItsLineAndColumn_AndNoInternalPositionText()
        {
            string text = "{\n  \"schema\": \"sam.optimisation/1\"\n  \"name\": \"x\"\n}";

            Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT100");
            Assert.Equal(3, optimisationDiagnostic.Line);
            Assert.Equal(3, optimisationDiagnostic.Column);
            Assert.DoesNotContain("LineNumber", optimisationDiagnostic.Message);
            Assert.StartsWith("The text is not valid JSON: ", optimisationDiagnostic.Message);
        }

        [Theory]
        [InlineData("\"schema\": \"sam.optimisation/1\",", "", "OPT102")]
        [InlineData("\"sam.optimisation/1\"", "\"sam.optimization/1\"", "OPT103")]
        [InlineData("\"sam.optimisation/1\"", "\"sam.optimisation\"", "OPT103")]
        [InlineData("\"sam.optimisation/1\"", "1", "OPT107")]
        [InlineData("\"sam.optimisation/1\"", "\"sam.optimisation/2\"", "OPT104")]
        public void Schema_IsChecked(string find, string replace, string code)
        {
            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.GoldenSectionWith(find, replace), out List<OptimisationDiagnostic> diagnostics));
            OptimisationFixtures.Single(diagnostics, code);
        }

        [Fact]
        public void UnknownProperty_IsAnError_WithASuggestion_AndItsPlace()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"maximum\": 35", "\"maxium\": 35");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic unknown = OptimisationFixtures.Single(diagnostics, "OPT105");
            Assert.Equal("$.variables[0].maxium", unknown.Path);
            Assert.Equal("Did you mean \"maximum\"?", unknown.Hint);
            Assert.Equal(15, unknown.Line);
            Assert.Equal(7, unknown.Column);

            // The real "maximum" is then missing.
            Assert.Equal("$.variables[0]", OptimisationFixtures.Single(diagnostics, "OPT110").Path);
        }

        [Fact]
        public void AnotherMethodsSetting_IsNamedAsSuch()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"tolerance\": 0.1", "\"stepReductions\": 4");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Contains("hooke-jeeves", OptimisationFixtures.Single(diagnostics, "OPT105").Message);
        }

        [Fact]
        public void RepeatedProperty_IsAnError()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"minimum\": -5,", "\"minimum\": -5,\n      \"minimum\": 0,");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal("$.variables[0].minimum", OptimisationFixtures.Single(diagnostics, "OPT106").Path);
        }

        [Theory]
        [InlineData("\"minimum\": -5", "\"minimum\": \"-5\"", "OPT108", "Remove the quotes: -5.")]
        [InlineData("\"minimum\": -5", "\"minimum\": \"NaN\"", "OPT108", "Write a number such as 35 or -5.5.")]
        [InlineData("\"minimum\": -5", "\"minimum\": true", "OPT107", null)]
        [InlineData("\"minimum\": -5", "\"minimum\": 1e400", "OPT109", null)]
        [InlineData("\"maximumSimulations\": 2000", "\"maximumSimulations\": 20.5", "OPT112", null)]
        [InlineData("\"maximumSimulations\": 2000", "\"maximumSimulations\": 1e10", "OPT112", null)]
        [InlineData("\"name\": \"Setpoint\"", "\"name\": 12", "OPT107", null)]
        [InlineData("\"variables\": [", "\"variables\": {}, \"x\": [", "OPT107", null)]
        public void WrongValueType_IsAnError(string find, string replace, string code, string hint)
        {
            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.GoldenSectionWith(find, replace), out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic optimisationDiagnostic = diagnostics.First(x => x.Code == code);
            if (hint != null)
            {
                Assert.Equal(hint, optimisationDiagnostic.Hint);
            }
        }

        [Theory]
        [InlineData("\"sense\": \"minimise\"", "", "$.objective")]
        [InlineData("\"output\": \"Result\",", "", "$.objective")]
        [InlineData("\"algorithm\": \"golden-section\",", "", "$.method")]
        [InlineData("\"engine\": \"tas-script\",", "", "$.model")]
        public void MissingRequiredProperty_IsAnError(string find, string replace, string path)
        {
            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.GoldenSectionWith(find, replace), out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal(path, OptimisationFixtures.Single(diagnostics, "OPT110").Path);
        }

        [Fact]
        public void UnknownEnumValue_IsAnError_WithTheAllowedValues()
        {
            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.GoldenSectionWith("\"sense\": \"minimise\"", "\"sense\": \"minimize\""), out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT111");
            Assert.Equal("Did you mean \"minimise\"? Allowed: \"minimise\", \"maximise\".", optimisationDiagnostic.Hint);
        }

        [Theory]
        [InlineData("\"golden-section\"", "\"Golden Section\"")]
        [InlineData("\"golden-section\"", "\"goldenSection\"")]
        [InlineData("\"golden-section\"", "\"GOLDEN_SECTION\"")]
        public void EnumValue_DifferingOnlyInCaseOrSeparators_IsNormalised(string find, string replace)
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.GoldenSectionWith(find, replace), out List<OptimisationDiagnostic> diagnostics);

            Assert.Equal(OptimisationAlgorithm.GoldenSection, optimisationDefinition.Method.Algorithm);
            Assert.Equal(DiagnosticSeverity.Info, OptimisationFixtures.Single(diagnostics, "OPT113").Severity);
            Assert.Contains("\"algorithm\": \"golden-section\"", optimisationDefinition.ToJson());
        }

        // ---------- Try every option ----------

        [Theory]
        [InlineData("\"try-every-option\"", false)]
        [InlineData("\"Try every option\"", true)]
        [InlineData("\"tryEveryOption\"", true)]
        public void TryEveryOption_IsRead_AndNormalised(string algorithm, bool normalised)
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.Choice, "\"try-every-option\"", algorithm);

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics);

            Assert.IsType<TryEveryOptionMethod>(optimisationDefinition.Method);
            Assert.Equal(OptimisationAlgorithm.TryEveryOption, optimisationDefinition.Method.Algorithm);
            Assert.Equal(normalised, diagnostics.Exists(x => x.Code == "OPT113"));
            Assert.Equal(OptimisationFixtures.Text(OptimisationFixtures.Choice).Replace("\r\n", "\n"), optimisationDefinition.ToJson());
        }

        [Theory]
        [InlineData("\"tolerance\": 0.1", "golden-section")]
        [InlineData("\"stepReductions\": 4", "hooke-jeeves")]
        public void TryEveryOption_HasNoSettings_AnotherMethodsSettingIsNamedAsSuch(string setting, string method)
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.Choice, "\"algorithm\": \"try-every-option\"", "\"algorithm\": \"try-every-option\",\n    " + setting);

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT105");
            Assert.Equal("$.method." + setting.Split('"')[1], optimisationDiagnostic.Path);
            Assert.Equal("\"" + setting.Split('"')[1] + "\" is a setting of the " + method + " method, not of this one.", optimisationDiagnostic.Message);
            Assert.Equal("Remove it, or change \"algorithm\".", optimisationDiagnostic.Hint);
        }

        [Fact]
        public void TryEveryOption_UnknownSetting_ListsWhatIsAllowed()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.Choice, "\"algorithm\": \"try-every-option\"", "\"algorithm\": \"try-every-option\",\n    \"order\": 1");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal("Allowed: algorithm.", OptimisationFixtures.Single(diagnostics, "OPT105").Hint);
        }

        [Fact]
        public void GoldenSection_ASettingOfTryEveryOptionsNeighbour_IsStillNamed()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"tolerance\": 0.1", "\"stepReductionFactor\": 2");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal("\"stepReductionFactor\" is a setting of the hooke-jeeves method, not of this one.", OptimisationFixtures.Single(diagnostics, "OPT105").Message);
        }

        [Fact]
        public void Algorithm_UnknownOrMissing_ListsTryEveryOption()
        {
            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.With(OptimisationFixtures.Choice, "\"try-every-option\"", "\"try-all\""), out List<OptimisationDiagnostic> unknown));
            Assert.EndsWith("Allowed: \"golden-section\", \"hooke-jeeves\", \"try-every-option\".", OptimisationFixtures.Single(unknown, "OPT111").Hint);

            Assert.Null(Create.OptimisationDefinition(OptimisationFixtures.With(OptimisationFixtures.Choice, "\"algorithm\": \"try-every-option\"", string.Empty), out List<OptimisationDiagnostic> missing));
            Assert.Equal("Add \"algorithm\": \"golden-section\" or \"hooke-jeeves\" or \"try-every-option\".", OptimisationFixtures.Single(missing, "OPT110").Hint);
        }

        [Fact]
        public void TryEveryOption_FindingsAreLocatedInTheText()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.Choice, "\"type\": \"discrete\"", "\"type\": \"continuous\"");

            Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics);

            OptimisationDiagnostic optimisationDiagnostic = OptimisationFixtures.Single(diagnostics, "OPT409");
            Assert.Equal(12, optimisationDiagnostic.Line);
            Assert.Equal(7, optimisationDiagnostic.Column);
        }

        [Fact]
        public void UnitSynonym_IsNormalised_WithANote()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.GoldenSectionWith("\"type\": \"continuous\",", "\"type\": \"continuous\",\n      \"unit\": \"degC\","), out List<OptimisationDiagnostic> diagnostics);

            Assert.Equal("°C", optimisationDefinition.Variables[0].Unit);
            Assert.Equal(DiagnosticSeverity.Info, OptimisationFixtures.Single(diagnostics, "OPT301").Severity);
        }

        [Fact]
        public void CommentsAndTrailingCommas_AreTolerated_WithAWarning()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"step\": 1", "\"step\": 1, // ignored by golden section\n");

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics);

            Assert.Equal(1, optimisationDefinition.Variables[0].Step);
            Assert.Equal(DiagnosticSeverity.Warning, OptimisationFixtures.Single(diagnostics, "OPT114").Severity);
            Assert.DoesNotContain("ignored", optimisationDefinition.ToJson());
        }

        [Fact]
        public void NullOptionalValue_IsTheSameAsAbsent()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.GoldenSectionWith("\"tolerance\": 0.1", "\"tolerance\": null"), out _);

            Assert.Null(((GoldenSectionMethod)optimisationDefinition.Method).Tolerance);
        }

        [Fact]
        public void ByteOrderMark_IsIgnored()
        {
            OptimisationFixtures.Read("﻿" + OptimisationFixtures.Text(OptimisationFixtures.GoldenSection), out _);
        }

        [Fact]
        public void SemanticFindings_PointAtTheirPlaceInTheText()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"minimum\": -5", "\"minimum\": 50");

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics);

            OptimisationDiagnostic range = OptimisationFixtures.Single(diagnostics, "OPT204");
            Assert.Equal("Setpoint range is invalid: minimum 50 is greater than maximum 35.", range.Message);
            Assert.Equal(14, range.Line);
            Assert.Equal(7, range.Column);
            Assert.False(diagnostics.IsRunnable());

            // Still read, so either view can show and fix it.
            Assert.Equal(50, optimisationDefinition.Variables[0].Minimum);
        }

        [Fact]
        public void Diagnostics_AreOrdered_ErrorsFirst_ThenByPlace()
        {
            string text = OptimisationFixtures.GoldenSectionWith("\"minimum\": -5", "\"minimum\": 50").Replace("\"output\": \"Result\"", "\"output\": \"Energy\"");

            OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas);

            List<DiagnosticSeverity> severities = diagnostics.Select(x => x.Severity).ToList();
            Assert.Equal(severities.OrderByDescending(x => x), severities);
            List<OptimisationDiagnostic> errors = diagnostics.FindAll(x => x.Severity == DiagnosticSeverity.Error);
            Assert.Equal(new[] { "OPT204", "OPT210" }, errors.Select(x => x.Code));
        }

        // ---------- Model bindings (targets and measures) ----------

        [Fact]
        public void UnboundFixtures_HaveNoTargetOrMeasure()
        {
            foreach (string fileName in new[] { OptimisationFixtures.GoldenSection, OptimisationFixtures.HookeJeeves })
            {
                OptimisationDefinition optimisationDefinition = OptimisationFixtures.Definition(fileName);

                Assert.All(optimisationDefinition.Variables, x => Assert.Null(x.Target));
                Assert.All(optimisationDefinition.Outputs, x => Assert.Null(x.Measure));
            }
        }

        [Fact]
        public void BoundSystemsDemo_IsReadExactly()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.BoundGoldenSection), out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());

            Assert.Equal("tas-model", optimisationDefinition.Model.Engine);

            OptimisationTarget target = Assert.Single(optimisationDefinition.Variables).Target;
            Assert.Equal("tpd.controller.setpoint", target.Kind);
            Assert.Equal(new Dictionary<string, string>() { { "controller", "HeatPumpController" }, { "plantRoom", "Plant Room 1" } }, target.Reference);
            Assert.Empty(target.Parameters);
            Assert.Empty(target.Options);

            Assert.Equal(new[] { "tpd.annual-cost", "tpd.annual-co2" }, optimisationDefinition.Outputs.Select(x => x.Measure.Kind));
            Assert.All(optimisationDefinition.Outputs, x => Assert.Empty(x.Measure.Reference));

            Assert.True(diagnostics.IsRunnable(), string.Join("\n", diagnostics));
            Assert.Equal("OPT408", Assert.Single(diagnostics).Code);
        }

        [Fact]
        public void BoundFixture_WithParametersAndReferences_IsReadExactly()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.BoundHookeJeeves), out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());

            Assert.Equal(new[] { "tbd.internal-condition.heating-setpoint", "tbd.internal-condition.cooling-setpoint", "tbd.glazing-construction.g-value" }, optimisationDefinition.Variables.Select(x => x.Target.Kind));
            Assert.Equal("Office", optimisationDefinition.Variables[1].Target.Reference["internalCondition"]);
            Assert.Equal("Office glazing", optimisationDefinition.Variables[2].Target.Reference["glazingConstruction"]);

            OptimisationMeasure overheating = optimisationDefinition.Output("Overheating").Measure;
            Assert.Equal("tsd.overheating-hours", overheating.Kind);
            Assert.Equal(28, overheating.Parameters["threshold"]);

            Assert.Empty(diagnostics);
        }

        [Fact]
        public void ChoiceFixture_IsRead_WithItsOptionsInOrder()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.Choice), out List<OptimisationDiagnostic> diagnostics);

            DesignVariable variable = Assert.Single(optimisationDefinition.Variables);
            Assert.Equal(DesignVariableType.Discrete, variable.Type);
            Assert.Equal((1.0, 3.0), (variable.Minimum, variable.Maximum));
            Assert.Null(variable.Start);
            Assert.Null(variable.Step);
            Assert.IsType<TryEveryOptionMethod>(optimisationDefinition.Method);
            Assert.Equal(new[] { "Double low-e", "Triple low-e", "Double solar control" }, variable.Target.Options);
            Assert.Equal(26.5, optimisationDefinition.Outputs[0].Measure.Parameters["threshold"]);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void Binding_UnknownField_IsRejected_WithASuggestion()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundGoldenSection, "\"kind\": \"tpd.controller.setpoint\"", "\"kinds\": \"tpd.controller.setpoint\"");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic unknown = OptimisationFixtures.Single(diagnostics, "OPT105");
            Assert.Equal("$.variables[0].target.kinds", unknown.Path);
            Assert.Equal("Did you mean \"kind\"?", unknown.Hint);
            Assert.Equal("$.variables[0].target", OptimisationFixtures.Single(diagnostics, "OPT110").Path);
        }

        [Fact]
        public void Measure_WithOptions_IsRejected()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundGoldenSection, "\"kind\": \"tpd.annual-cost\"", "\"kind\": \"tpd.annual-cost\", \"options\": [ \"A\" ]");

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal("$.outputs[0].measure.options", OptimisationFixtures.Single(diagnostics, "OPT105").Path);
        }

        [Theory]
        [InlineData("\"plantRoom\": \"Plant Room 1\"", "\"plantRoom\": 1", "OPT107", "$.variables[0].target.reference.plantRoom")]
        [InlineData("\"reference\": {", "\"reference\": \"Plant Room 1\", \"x\": {", "OPT107", "$.variables[0].target.reference")]
        [InlineData("\"controller\": \"HeatPumpController\",", "\"controller\": \"HeatPumpController\", \"controller\": \"Other\",", "OPT106", "$.variables[0].target.reference.controller")]
        public void Reference_StructureErrors_AreReported(string find, string replace, string code, string path)
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundGoldenSection, find, replace);

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal(path, diagnostics.First(x => x.Code == code).Path);
        }

        [Theory]
        [InlineData("\"threshold\": \"28\"", "OPT108", "Remove the quotes: 28.")]
        [InlineData("\"threshold\": true", "OPT107", null)]
        [InlineData("\"threshold\": 1e400", "OPT109", null)]
        [InlineData("\"threshold\": 28, \"threshold\": 26", "OPT106", "Keep one of them.")]
        public void Parameters_StructureErrors_AreReported(string replace, string code, string hint)
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundHookeJeeves, "\"threshold\": 28", replace);

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));

            OptimisationDiagnostic optimisationDiagnostic = diagnostics.First(x => x.Code == code);
            Assert.StartsWith("$.outputs[3].measure.parameters", optimisationDiagnostic.Path);
            Assert.Equal(hint, optimisationDiagnostic.Hint);
            Assert.NotNull(optimisationDiagnostic.Line);
        }

        [Theory]
        [InlineData("[\n          \"Double low-e\",", "\"Double low-e\", \"x\": [ \"y\",", "OPT107", "$.variables[0].target.options")]
        [InlineData("\"Triple low-e\",", "2,", "OPT107", "$.variables[0].target.options[1]")]
        public void Options_StructureErrors_AreReported(string find, string replace, string code, string path)
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.Choice, find, replace);

            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            Assert.Equal(path, diagnostics.First(x => x.Code == code).Path);
        }

        [Fact]
        public void Reference_NullValue_CountsAsAbsent()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundGoldenSection, "\"plantRoom\": \"Plant Room 1\"", "\"plantRoom\": null");

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel());

            Assert.False(optimisationDefinition.Variables[0].Target.Reference.ContainsKey("plantRoom"));
            Assert.Equal("The target of Setpoint does not say which plant room (\"plantRoom\" is missing from \"reference\").", OptimisationFixtures.Single(diagnostics, "OPT602").Message);
        }

        [Fact]
        public void BindingFindings_PointAtTheirPlaceInTheText()
        {
            string text = OptimisationFixtures.With(OptimisationFixtures.BoundGoldenSection, "\"plantRoom\": \"Plant Room 1\"", "\"plantRoom\": \"Plant Room 2\"");

            OptimisationFixtures.Read(text, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());

            OptimisationDiagnostic missing = OptimisationFixtures.Single(diagnostics, "OPT609");
            Assert.Equal("$.variables[0].target.reference.plantRoom", missing.Path);
            Assert.Equal(22, missing.Line);
            Assert.Equal(11, missing.Column);
            Assert.False(diagnostics.IsRunnable());
        }
    }
}
