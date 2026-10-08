// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The AI round trip: the text given to an assistant (strict raw-JSON contract, only what the engine runs, no
    /// paths), reading an assistant's reply defensively (fences and prose removed with a warning), and the JSON Schema
    /// resource agreeing with the reader.
    /// </summary>
    public class OptimisationExchangeTests
    {
        private static OptimisationCatalogue SystemsDemoCatalogue()
        {
            return new OptimisationCatalogue(
                new[] { new OptimisationCatalogueEntry("Setpoint") },
                new[] { new OptimisationCatalogueEntry("Result"), new OptimisationCatalogueEntry("Cost", unit: "GBP"), new OptimisationCatalogueEntry("CO2") },
                "found in the Tas script text");
        }

        // ---------- Extracting the JSON object ----------

        [Fact]
        public void Extract_RawObject_IsUnchanged()
        {
            string text = OptimisationFixtures.Text(OptimisationFixtures.GoldenSection);

            Assert.Equal(text.Trim(), Core.Optimisation.Query.ExtractJsonObject(text, out bool trimmed));
            Assert.False(trimmed);
        }

        [Theory]
        [InlineData("```json\n{TEXT}\n```")]
        [InlineData("Here is the updated definition:\n\n```\n{TEXT}\n```\nI changed the maximum.")]
        [InlineData("Note {braces} in prose first.\n{TEXT}")]
        public void Extract_FencedOrWithProse_FindsTheObject(string template)
        {
            string text = OptimisationFixtures.Text(OptimisationFixtures.GoldenSection).Trim();

            Assert.Equal(text, Core.Optimisation.Query.ExtractJsonObject(template.Replace("{TEXT}", text), out bool trimmed));
            Assert.True(trimmed);
        }

        [Fact]
        public void Extract_BracesInStringsAndComments_AreNotCounted()
        {
            string text = "{ \"name\": \"a } b { c\", // }\n \"x\": 1 }";

            Assert.Equal(text, Core.Optimisation.Query.ExtractJsonObject(text, out _));
        }

        [Theory]
        [InlineData("")]
        [InlineData("no object here")]
        [InlineData("{ \"unterminated\": 1")]
        public void Extract_NoBalancedObject_IsNull(string text)
        {
            Assert.Null(Core.Optimisation.Query.ExtractJsonObject(text, out _));
        }

        [Fact]
        public void Paste_WithAFence_IsRead_WithAWarning()
        {
            string reply = "```json\n" + OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves) + "```";

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(reply, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas, true);

            Assert.Equal(OptimisationAlgorithm.HookeJeeves, optimisationDefinition.Method.Algorithm);
            Assert.Equal(DiagnosticSeverity.Warning, OptimisationFixtures.Single(diagnostics, "OPT115").Severity);
            Assert.True(diagnostics.IsRunnable());
        }

        [Fact]
        public void Paste_WithoutExtract_RejectsAFence()
        {
            string reply = "```json\n" + OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves) + "```";

            Assert.Null(Create.OptimisationDefinition(reply, out List<OptimisationDiagnostic> diagnostics));
            OptimisationFixtures.Single(diagnostics, "OPT100");
        }

        [Theory]
        [InlineData("I could not do that.")]
        [InlineData("")]
        public void Paste_WithNoObject_IsNotRead(string reply)
        {
            Assert.Null(Create.OptimisationDefinition(reply, out List<OptimisationDiagnostic> diagnostics, extract: true));
            OptimisationFixtures.Single(diagnostics, "OPT101");
        }

        [Fact]
        public void Paste_InvalidAIOutput_IsNeverAccepted()
        {
            // An assistant inventing a field and writing a number as text: nothing is returned to apply.
            string reply = OptimisationFixtures.Text(OptimisationFixtures.GoldenSection).Replace("\"maximum\": 35", "\"maximum\": \"35\", \"weight\": 2");

            Assert.Null(Create.OptimisationDefinition(reply, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.Tas, true));
            OptimisationFixtures.Single(diagnostics, "OPT105");
            OptimisationFixtures.Single(diagnostics, "OPT108");
        }

        // ---------- AI exchange text ----------

        [Fact]
        public void AIExchangeText_StatesTheStrictRawJsonContract()
        {
            string text = Core.Optimisation.Query.AIExchangeText(OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.GoldenSection), out _), OptimisationFixtures.Tas, SystemsDemoCatalogue(), "Use Hooke-Jeeves.");

            Assert.StartsWith("You are editing a SAM optimisation definition (schema \"sam.optimisation/1\").", text);
            Assert.Contains("Reply with exactly one JSON object and nothing else: no Markdown, no code fence, no explanation before or", text);
            Assert.Contains("The first character of your reply must be \"{\" and the last must be \"}\".", text);
            Assert.Contains("Do not invent names.", text);
            Assert.Contains("Do not write code, scripts or expressions.", text);
            Assert.EndsWith("TASK\nUse Hooke-Jeeves.\n", text);
        }

        [Fact]
        public void AIExchangeText_EmbedsTheCanonicalDefinition_WithoutAFence()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.GoldenSection), out _);

            string text = Core.Optimisation.Query.AIExchangeText(optimisationDefinition, OptimisationFixtures.Tas, SystemsDemoCatalogue());

            Assert.Contains("CURRENT DEFINITION\n" + optimisationDefinition.ToJson() + "\nTASK\n", text);
            Assert.DoesNotContain("```", text);
        }

        [Fact]
        public void AIExchangeText_OffersOnlyWhatTheEngineRuns()
        {
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.Tas, SystemsDemoCatalogue());

            Assert.Contains("\"sense\": \"minimise\" }", text);
            Assert.DoesNotContain("maximise", text);
            Assert.DoesNotContain("constraint", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("atMost", text);
            Assert.DoesNotContain("\"integer\"", text);
            Assert.Contains("\"type\": \"continuous\"", text);
            Assert.Contains("\"algorithm\": \"golden-section\"", text);
            Assert.Contains("(exactly 1 design variable; uses only minimum and maximum)", text);
            Assert.Contains("\"algorithm\": \"hooke-jeeves\"", text);
            Assert.Contains("(1 or more design variables; each needs start and step)", text);
            Assert.Contains("(none yet: create a complete definition)", text);
        }

        [Fact]
        public void AIExchangeText_OffersMoreWhenTheEngineRunsMore()
        {
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.Everything);

            Assert.Contains("\"minimise\" or \"maximise\"", text);
            Assert.Contains("constraints[] (optional)", text);
        }

        [Fact]
        public void AIExchangeText_OnlyOneAlgorithm_IsOffered_WhenTheEngineRunsOne()
        {
            OptimisationCapabilities capabilities = new OptimisationCapabilities("tas-script", "Tas", new[] { new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null) }, new[] { ObjectiveSense.Minimise }, new[] { DesignVariableType.Continuous }, false);

            Assert.DoesNotContain("golden-section", Core.Optimisation.Query.AIExchangeText(null, capabilities));
        }

        [Fact]
        public void AIExchangeText_ListsTheAvailableNames()
        {
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.Tas, SystemsDemoCatalogue());

            Assert.Contains("AVAILABLE (found in the Tas script text)\n- design variables: Setpoint\n- outputs: Result, Cost (GBP), CO2\n", text);
            Assert.Contains("- No list of names is available", Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.Tas));
        }

        [Fact]
        public void AIExchangeText_HoldsNoPathOrMachineDetail()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves), out _);

            string text = Core.Optimisation.Query.AIExchangeText(optimisationDefinition, OptimisationFixtures.Tas, SystemsDemoCatalogue());

            Assert.DoesNotContain(":\\", text);
            Assert.DoesNotContain("\\\\", text);
            Assert.DoesNotContain(Environment.UserName, text);
            Assert.DoesNotContain(Environment.MachineName, text);
        }

        [Fact]
        public void AIExchangeText_RequiresCapabilities()
        {
            Assert.Throws<ArgumentNullException>(() => Core.Optimisation.Query.AIExchangeText(null, null));
        }

        [Fact]
        public void RoundTrip_TheCurrentDefinitionInThePackage_ReadsBackUnchanged()
        {
            // An assistant asked for no change returns the embedded definition: it reads back to the same canonical text.
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(OptimisationFixtures.Text(OptimisationFixtures.HookeJeeves), out _);
            string text = Core.Optimisation.Query.AIExchangeText(optimisationDefinition, OptimisationFixtures.Tas);
            string start = "CURRENT DEFINITION\n";
            string reply = text.Substring(text.IndexOf(start, StringComparison.Ordinal) + start.Length);
            reply = reply.Substring(0, reply.IndexOf("\nTASK\n", StringComparison.Ordinal));

            Assert.Equal(optimisationDefinition.ToJson(), OptimisationFixtures.Read(reply, out _, OptimisationFixtures.Tas, true).ToJson());
        }

        // ---------- JSON Schema resource ----------

        [Fact]
        public void Schema_IsEmbedded_AndValidJson()
        {
            string text = Core.Optimisation.Query.SchemaText();

            Assert.False(string.IsNullOrWhiteSpace(text));
            using (JsonDocument jsonDocument = JsonDocument.Parse(text))
            {
                Assert.Equal("https://json-schema.org/draft/2020-12/schema", jsonDocument.RootElement.GetProperty("$schema").GetString());
                Assert.Equal(OptimisationDefinition.Schema, jsonDocument.RootElement.GetProperty("properties").GetProperty("schema").GetProperty("const").GetString());
            }
        }

        [Fact]
        public void Schema_DescribesExactlyTheReadersFields()
        {
            using (JsonDocument jsonDocument = JsonDocument.Parse(Core.Optimisation.Query.SchemaText()))
            {
                JsonElement root = jsonDocument.RootElement;
                JsonElement defs = root.GetProperty("$defs");

                foreach (KeyValuePair<string, string[]> keyValuePair in OptimisationNames.Objects)
                {
                    JsonElement element = keyValuePair.Key == "definition" ? root : defs.GetProperty(keyValuePair.Key);
                    List<string> names = element.GetProperty("properties").EnumerateObject().Select(x => x.Name).ToList();

                    Assert.Equal(keyValuePair.Value.OrderBy(x => x, StringComparer.Ordinal), names.OrderBy(x => x, StringComparer.Ordinal));
                    Assert.False(element.GetProperty("additionalProperties").GetBoolean(), keyValuePair.Key);
                }
            }
        }

        [Fact]
        public void Schema_EnumValues_AreTheReadersValues()
        {
            using (JsonDocument jsonDocument = JsonDocument.Parse(Core.Optimisation.Query.SchemaText()))
            {
                JsonElement defs = jsonDocument.RootElement.GetProperty("$defs");

                Assert.Equal(OptimisationNames.Texts<OptimisationQuantity>(), defs.GetProperty("quantity").GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
                Assert.Equal(OptimisationNames.Texts<ObjectiveSense>(), defs.GetProperty("objective").GetProperty("properties").GetProperty("sense").GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
                Assert.Equal(OptimisationNames.Texts<DesignVariableType>(), defs.GetProperty("variable").GetProperty("properties").GetProperty("type").GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
                Assert.Equal("golden-section", defs.GetProperty("goldenSection").GetProperty("properties").GetProperty("algorithm").GetProperty("const").GetString());
                Assert.Equal("hooke-jeeves", defs.GetProperty("hookeJeeves").GetProperty("properties").GetProperty("algorithm").GetProperty("const").GetString());

                List<string> symbols = Core.Optimisation.Query.OptimisationUnits().Select(x => x.Symbol).ToList();
                string unitDescription = defs.GetProperty("unit").GetProperty("description").GetString();
                Assert.All(symbols, x => Assert.Contains(x, unitDescription));
            }
        }
    }
}
