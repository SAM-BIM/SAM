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
    }
}
