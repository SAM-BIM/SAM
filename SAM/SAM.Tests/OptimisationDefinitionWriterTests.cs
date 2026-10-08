// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System;
using System.Collections.Generic;
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
        public void Fixture_IsCanonical(string fileName)
        {
            string text = OptimisationFixtures.Text(fileName).Replace("\r\n", "\n");

            Assert.Equal(text, OptimisationFixtures.Read(text, out _).ToJson());
        }

        [Theory]
        [InlineData(OptimisationFixtures.GoldenSection)]
        [InlineData(OptimisationFixtures.HookeJeeves)]
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
