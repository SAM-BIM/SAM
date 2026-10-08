// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The canonical definition text: the Systems Demo fixtures are exactly the writer's output, writing is stable,
    /// and every number reads back as the same double (no rounding anywhere).
    /// </summary>
    public class OptimisationDefinitionWriterTests
    {
        [Theory]
        [InlineData(OptimisationFixtures.GoldenSection)]
        [InlineData(OptimisationFixtures.HookeJeeves)]
        [InlineData(OptimisationFixtures.BoundGoldenSection)]
        [InlineData(OptimisationFixtures.BoundHookeJeeves)]
        [InlineData(OptimisationFixtures.Choice)]
        public void Fixture_IsCanonical(string fileName)
        {
            string text = OptimisationFixtures.Text(fileName).Replace("\r\n", "\n");

            Assert.Equal(text, OptimisationFixtures.Read(text, out _).ToJson());
        }

        [Theory]
        [InlineData(OptimisationFixtures.GoldenSection)]
        [InlineData(OptimisationFixtures.HookeJeeves)]
        [InlineData(OptimisationFixtures.BoundGoldenSection)]
        [InlineData(OptimisationFixtures.BoundHookeJeeves)]
        [InlineData(OptimisationFixtures.Choice)]
        public void Writing_IsIdempotent(string fileName)
        {
            string once = OptimisationFixtures.Read(OptimisationFixtures.Text(fileName), out _).ToJson();
            string twice = OptimisationFixtures.Read(once, out _).ToJson();

            Assert.Equal(once, twice);
        }

        [Theory]
        [InlineData(4.968943799848584)]
        [InlineData(7360.04370117188)]
        [InlineData(4076.69276428223)]
        [InlineData(0.1)]
        [InlineData(-5.0)]
        [InlineData(1e-7)]
        [InlineData(0.30000000000000004)]
        [InlineData(double.MaxValue)]
        [InlineData(double.Epsilon)]
        [InlineData(-0.0)]
        public void Numbers_RoundTrip_BitForBit(double value)
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Minimum = value;
            optimisationDefinition.Variables[0].Maximum = value;
            optimisationDefinition.Variables[0].Start = value;
            optimisationDefinition.Variables[0].Step = value;

            OptimisationDefinition read = OptimisationFixtures.Read(optimisationDefinition.ToJson(), out _);

            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(read.Variables[0].Minimum));
            Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(read.Variables[0].Step.Value));
        }

        [Fact]
        public void Numbers_UseTheShortestText()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Minimum = 4.968943799848584;
            optimisationDefinition.Variables[0].Maximum = 35;

            string text = optimisationDefinition.ToJson();

            Assert.Contains("\"minimum\": 4.968943799848584,", text);
            Assert.Contains("\"maximum\": 35" + "\n", text);
        }

        [Fact]
        public void OptionalValues_AreLeftOut_AndTheLayoutIsFixed()
        {
            string expected =
                "{\n" +
                "  \"schema\": \"sam.optimisation/1\",\n" +
                "  \"model\": {\n" +
                "    \"engine\": \"tas-script\"\n" +
                "  },\n" +
                "  \"variables\": [\n" +
                "    {\n" +
                "      \"name\": \"x\",\n" +
                "      \"type\": \"continuous\",\n" +
                "      \"minimum\": 0,\n" +
                "      \"maximum\": 1\n" +
                "    }\n" +
                "  ],\n" +
                "  \"outputs\": [\n" +
                "    {\n" +
                "      \"name\": \"y\"\n" +
                "    }\n" +
                "  ],\n" +
                "  \"objective\": {\n" +
                "    \"output\": \"y\",\n" +
                "    \"sense\": \"minimise\"\n" +
                "  },\n" +
                "  \"method\": {\n" +
                "    \"algorithm\": \"golden-section\"\n" +
                "  }\n" +
                "}\n";

            Assert.Equal(expected, Definition().ToJson());
        }

        [Fact]
        public void EmptyLists_AreWrittenAsBrackets_ExceptConstraints()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables.Clear();

            string text = optimisationDefinition.ToJson();

            Assert.Contains("\"variables\": [],", text);
            Assert.DoesNotContain("constraints", text);
        }

        [Fact]
        public void Constraints_AndMaximise_AreWritten_AndReadBack()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Objective.Sense = ObjectiveSense.Maximise;
            optimisationDefinition.Constraints.Add(new OptimisationConstraint() { Output = "y", AtMost = 100, Unit = "h" });

            OptimisationDefinition read = OptimisationFixtures.Read(optimisationDefinition.ToJson(), out _);

            Assert.Equal(ObjectiveSense.Maximise, read.Objective.Sense);
            OptimisationConstraint constraint = Assert.Single(read.Constraints);
            Assert.Equal(100, constraint.AtMost);
            Assert.Null(constraint.AtLeast);
            Assert.Equal("h", constraint.Unit);
        }

        [Fact]
        public void Text_IsEscaped_ButNotAsciiOnly()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Name = "Setpoint \"A\" – °C \\ line\nbreak\ttab\u0001";

            string text = optimisationDefinition.ToJson();

            Assert.Contains("\"name\": \"Setpoint \\\"A\\\" – °C \\\\ line\\nbreak\\ttab\\u0001\",", text);
            Assert.Equal(optimisationDefinition.Name, OptimisationFixtures.Read(text, out _).Name);
        }

        [Fact]
        public void NonFiniteNumber_IsLeftOut_AndReadingReportsItMissing()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Maximum = double.NaN;

            string text = optimisationDefinition.ToJson();

            Assert.DoesNotContain("NaN", text);
            Assert.Null(Create.OptimisationDefinition(text, out List<OptimisationDiagnostic> diagnostics));
            OptimisationFixtures.Single(diagnostics, "OPT110");
        }

        [Fact]
        public void CopyConstructor_IsDeep()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves), out _);
            OptimisationDefinition copy = new OptimisationDefinition(optimisationDefinition);

            copy.Variables[0].Maximum = 99;
            copy.Outputs[0].Name = "Changed";
            ((HookeJeevesMethod)copy.Method).StepReductions = 9;
            copy.Objective.Output = "Changed";
            copy.Stopping.MaximumSimulations = 5;

            Assert.Equal(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves).Replace("\r\n", "\n"), optimisationDefinition.ToJson());
        }

        // ---------- Model bindings (targets and measures) ----------

        public static IEnumerable<object[]> Targets()
        {
            yield return new object[] { new OptimisationTarget("t.kind-only") };
            yield return new object[] { new OptimisationTarget("t.reference", OptimisationFixtures.Reference("internalCondition", "Office")) };
            yield return new object[] { new OptimisationTarget("t.two-keys", OptimisationFixtures.Reference("plantRoom", "Plant Room 1", "controller", "HeatPumpController")) };
            yield return new object[] { new OptimisationTarget("t.parameters", null, new Dictionary<string, double>() { { "b", 0.1 }, { "a", -5 } }) };
            yield return new object[] { new OptimisationTarget("t.options", OptimisationFixtures.Reference("glazingConstruction", "Office \"main\" glazing"), null, new[] { "Triple", "Double", "Double – solar °" }) };
            yield return new object[] { new OptimisationTarget("t.everything", OptimisationFixtures.Reference("k", "v"), new Dictionary<string, double>() { { "p", 4.968943799848584 } }, new[] { "A", "B" }) };
        }

        public static IEnumerable<object[]> Measures()
        {
            yield return new object[] { new OptimisationMeasure("m.kind-only") };
            yield return new object[] { new OptimisationMeasure("m.reference", OptimisationFixtures.Reference("zone", "Office 1")) };
            yield return new object[] { new OptimisationMeasure("m.parameters", null, new Dictionary<string, double>() { { "threshold", 28 } }) };
            yield return new object[] { new OptimisationMeasure("m.both", OptimisationFixtures.Reference("zone", "Office 1", "floor", "1"), new Dictionary<string, double>() { { "threshold", 26.5 }, { "hours", 1e-7 } }) };
        }

        [Theory]
        [MemberData(nameof(Targets))]
        public void Target_RoundTrips(OptimisationTarget target)
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Target = target;

            string text = optimisationDefinition.ToJson();
            OptimisationTarget read = OptimisationFixtures.Read(text, out _).Variables[0].Target;

            Assert.Equal(target.Kind, read.Kind);
            Assert.Equal(target.Reference.OrderBy(x => x.Key, StringComparer.Ordinal), read.Reference.OrderBy(x => x.Key, StringComparer.Ordinal));
            Assert.Equal(target.Parameters.OrderBy(x => x.Key, StringComparer.Ordinal), read.Parameters.OrderBy(x => x.Key, StringComparer.Ordinal));
            Assert.Equal(target.Options, read.Options);
            Assert.Equal(text, OptimisationFixtures.Read(text, out _).ToJson());
        }

        [Theory]
        [MemberData(nameof(Measures))]
        public void Measure_RoundTrips(OptimisationMeasure measure)
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Outputs[0].Measure = measure;

            string text = optimisationDefinition.ToJson();
            OptimisationMeasure read = OptimisationFixtures.Read(text, out _).Outputs[0].Measure;

            Assert.Equal(measure.Kind, read.Kind);
            Assert.Equal(measure.Reference.OrderBy(x => x.Key, StringComparer.Ordinal), read.Reference.OrderBy(x => x.Key, StringComparer.Ordinal));
            Assert.Equal(measure.Parameters.OrderBy(x => x.Key, StringComparer.Ordinal), read.Parameters.OrderBy(x => x.Key, StringComparer.Ordinal));
            Assert.Equal(text, OptimisationFixtures.Read(text, out _).ToJson());
        }

        [Fact]
        public void Binding_IsWrittenInTheCanonicalLayout_KeysInOrdinalOrder()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Target = new OptimisationTarget("k", OptimisationFixtures.Reference("plantRoom", "P", "controller", "C"), new Dictionary<string, double>() { { "z", 2 }, { "a", 0.5 } }, new[] { "B", "A" });
            optimisationDefinition.Outputs[0].Measure = new OptimisationMeasure("m");

            string text = optimisationDefinition.ToJson();

            Assert.Contains(
                "      \"maximum\": 1,\n" +
                "      \"target\": {\n" +
                "        \"kind\": \"k\",\n" +
                "        \"reference\": {\n" +
                "          \"controller\": \"C\",\n" +
                "          \"plantRoom\": \"P\"\n" +
                "        },\n" +
                "        \"parameters\": {\n" +
                "          \"a\": 0.5,\n" +
                "          \"z\": 2\n" +
                "        },\n" +
                "        \"options\": [\n" +
                "          \"B\",\n" +
                "          \"A\"\n" +
                "        ]\n" +
                "      }\n" +
                "    }\n", text);
            Assert.Contains(
                "      \"name\": \"y\",\n" +
                "      \"measure\": {\n" +
                "        \"kind\": \"m\"\n" +
                "      }\n", text);
        }

        [Fact]
        public void Binding_EmptyParts_AndNonFiniteParameters_AreLeftOut()
        {
            OptimisationDefinition optimisationDefinition = Definition();
            optimisationDefinition.Variables[0].Target = new OptimisationTarget("k") { Reference = null, Parameters = new Dictionary<string, double>() { { "nan", double.NaN }, { "infinity", double.PositiveInfinity } }, Options = null };

            string text = optimisationDefinition.ToJson();

            Assert.Contains("\"target\": {\n        \"kind\": \"k\"\n      }", text);
            Assert.Empty(OptimisationFixtures.Read(text, out _).Variables[0].Target.Parameters);
        }

        [Fact]
        public void Binding_Parameters_RoundTripBitForBit()
        {
            foreach (double value in new[] { 0.1, 26.5, 1e-7, 0.30000000000000004, -0.0, 4076.69276428223 })
            {
                OptimisationDefinition optimisationDefinition = Definition();
                optimisationDefinition.Outputs[0].Measure = new OptimisationMeasure("m", null, new Dictionary<string, double>() { { "p", value } });

                double read = OptimisationFixtures.Read(optimisationDefinition.ToJson(), out _).Outputs[0].Measure.Parameters["p"];

                Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(read));
            }
        }

        [Fact]
        public void CopyConstructor_CopiesBindingsDeeply()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Definition(OptimisationFixtures.Choice);
            OptimisationDefinition copy = new OptimisationDefinition(optimisationDefinition);

            copy.Variables[0].Target.Kind = "changed";
            copy.Variables[0].Target.Reference["glazingConstruction"] = "changed";
            copy.Variables[0].Target.Options.Add("changed");
            copy.Outputs[0].Measure.Parameters["threshold"] = 99;
            copy.Outputs[0].Measure.Reference["zone"] = "changed";

            Assert.Equal(OptimisationFixtures.Text(OptimisationFixtures.Choice).Replace("\r\n", "\n"), optimisationDefinition.ToJson());
        }

        private static OptimisationDefinition Definition()
        {
            OptimisationDefinition result = new OptimisationDefinition()
            {
                Model = new OptimisationModel("tas-script"),
                Objective = new OptimisationObjective("y", ObjectiveSense.Minimise),
                Method = new GoldenSectionMethod(),
            };

            result.Variables.Add(new DesignVariable("x", 0, 1));
            result.Outputs.Add(new OptimisationOutput("y"));
            return result;
        }
    }
}
