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

        // ---------- AI exchange text with model bindings ----------

        [Fact]
        public void AIExchangeText_ForABindingEngine_AsksForEveryBinding_AndKeepsTheNoCodeRule()
        {
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue(), "Lowest annual energy.");

            Assert.Contains("- Give every design variable a \"target\" and every output a \"measure\", copied exactly from AVAILABLE. Do not\n  invent targets, measures or model item names. Names are short labels of your choice, each unique.\n", text);
            Assert.Contains("- Do not write code, scripts or expressions. Do not add fields that are not listed here.", text);
            Assert.Contains("\"minimum\", \"maximum\", \"start\", \"step\", \"target\" }", text);
            Assert.Contains("\"unit\" (optional), \"measure\" }", text);
            Assert.Contains("- target: { \"kind\", \"reference\", \"parameters\" } exactly as under AVAILABLE", text);
            Assert.Contains("- measure: { \"kind\", \"reference\", \"parameters\" } exactly as under AVAILABLE", text);
            Assert.Contains("- model: { \"engine\": \"tas-model\"", text);
            Assert.DoesNotContain("Use only the design variable and output names listed under AVAILABLE", text);
            Assert.DoesNotContain("they are not checked against the model", text);
        }

        [Fact]
        public void AIExchangeText_OffersTheCatalogueItems_WithValuesAndUnits()
        {
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());

            Assert.Contains(
                "AVAILABLE (read from the Tas model)\n" +
                "Can change (design variable targets):\n" +
                "- Setpoint: { \"kind\": \"tpd.controller.setpoint\", \"reference\": { \"controller\": \"HeatPumpController\", \"plantRoom\": \"Plant Room 1\" } }\n" +
                "  HeatPumpController setpoint; no stated unit\n" +
                "- Office heating setpoint: { \"kind\": \"tbd.internal-condition.heating-setpoint\", \"reference\": { \"internalCondition\": \"Office\" } }\n" +
                "  Zone heating setpoint; unit °C; now 21 °C; suggested range 16 to 24 °C\n" +
                "- Office cooling setpoint: { \"kind\": \"tbd.internal-condition.cooling-setpoint\", \"reference\": { \"internalCondition\": \"Office\" } }\n" +
                "  Zone cooling setpoint; unit °C; now 24 °C; suggested range 21 to 28 °C\n" +
                "- Meeting room heating setpoint: { \"kind\": \"tbd.internal-condition.heating-setpoint\", \"reference\": { \"internalCondition\": \"Meeting room\" } }\n" +
                "  Zone heating setpoint; unit °C; now 20 °C\n" +
                "- Office glazing g-value: { \"kind\": \"tbd.glazing-construction.g-value\", \"reference\": { \"glazingConstruction\": \"Office glazing\" } }\n" +
                "  Glazing g-value; unit -; now 0.42; suggested range 0.2 to 0.7\n" +
                "Can measure (output measures):\n" +
                "- Annual heating demand: { \"kind\": \"tsd.annual-heating-demand\" }\n" +
                "  Annual heating demand; unit kWh; now 41200.5 kWh\n" +
                "- Annual cooling demand: { \"kind\": \"tsd.annual-cooling-demand\" }\n" +
                "  Annual cooling demand; unit kWh; now 18750 kWh\n" +
                "- Overheating hours: { \"kind\": \"tsd.overheating-hours\", \"parameters\": { \"threshold\": 28 } }\n" +
                "  Overheating hours; unit h; now 37 h; parameter \"threshold\" (resultant temperature threshold, default 28 °C, 20 to 40 °C)\n" +
                "- Annual plant energy: { \"kind\": \"tpd.annual-energy\" }\n" +
                "  Annual plant energy; unit kWh\n" +
                "- Annual plant cost: { \"kind\": \"tpd.annual-cost\" }\n" +
                "  Annual plant cost; unit GBP\n" +
                "- Annual plant CO2: { \"kind\": \"tpd.annual-co2\" }\n" +
                "  Annual plant CO2; unit kgCO2e\n" +
                "\n" +
                "CURRENT DEFINITION\n", text);
        }

        [Fact]
        public void AIExchangeText_OffersAChoice_OnlyWhenTheEngineRunsDiscreteVariables()
        {
            string continuous = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());
            string discrete = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(true), OptimisationFixtures.ModelCatalogue());

            Assert.DoesNotContain("tbd.glazing-construction.choice", continuous);
            Assert.DoesNotContain("options", continuous);
            Assert.Contains("- Office glazing: { \"kind\": \"tbd.glazing-construction.choice\", \"reference\": { \"glazingConstruction\": \"Office glazing\" } }\n  Glazing construction choice; no stated unit; options: \"Double low-e\", \"Triple low-e\", \"Double solar control\"\n", discrete);
            Assert.Contains("- A choice target also has \"options\": two or more of the names listed for it under AVAILABLE. Its variable has\n  \"type\": \"discrete\", \"minimum\": 1 and \"maximum\": the number of options (1 is the first option).\n", discrete);
        }

        [Fact]
        public void AIExchangeText_OffersOnlyKindsTheEngineLists()
        {
            OptimisationCatalogue catalogue = OptimisationFixtures.ModelCatalogue();
            OptimisationCatalogue withUnknown = new OptimisationCatalogue(
                catalogue.Variables.Concat(new[] { new OptimisationCatalogueEntry("Shading", new OptimisationTarget("tbd.shading.depth")) }),
                catalogue.Outputs.Concat(new[] { new OptimisationCatalogueEntry("Daylight", new OptimisationMeasure("tsd.daylight-factor")) }),
                catalogue.Source);

            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), withUnknown);

            Assert.DoesNotContain("tbd.shading.depth", text);
            Assert.DoesNotContain("tsd.daylight-factor", text);
            Assert.Contains("tpd.annual-co2", text);
        }

        [Fact]
        public void AIExchangeText_ForABindingEngine_WithoutModelItems_SaysSo()
        {
            OptimisationCatalogue names = new OptimisationCatalogue(new[] { new OptimisationCatalogueEntry("Setpoint") }, null, "Tas script");

            Assert.Contains("AVAILABLE (Tas script)\n- No list of model items is available: keep the targets and measures already in the current definition.\n", Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), names));
            Assert.Contains("AVAILABLE\n- No list of model items is available", Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel()));
        }

        [Fact]
        public void AIExchangeText_ForTasScript_OffersNoBindings_EvenWithAModelCatalogue()
        {
            string text = Core.Optimisation.Query.AIExchangeText(OptimisationFixtures.Definition(OptimisationFixtures.GoldenSection), OptimisationFixtures.Tas, SystemsDemoCatalogue());

            Assert.DoesNotContain("target", text);
            Assert.DoesNotContain("measure", text);
            Assert.Contains("- Use only the design variable and output names listed under AVAILABLE. Do not invent names.", text);
            Assert.DoesNotContain("tbd.", Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.Tas, OptimisationFixtures.ModelCatalogue()));
        }

        [Fact]
        public void AIReply_CopyingTheOfferedItems_ReadsBackBoundAndRunnable()
        {
            // An assistant builds a definition from the offered lines, copying each binding's JSON exactly.
            string text = Core.Optimisation.Query.AIExchangeText(null, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());
            string Offered(string name)
            {
                string start = "- " + name + ": ";
                int index = text.IndexOf(start, StringComparison.Ordinal) + start.Length;
                return text.Substring(index, text.IndexOf('\n', index) - index);
            }

            string reply = "Here you are:\n{ \"schema\": \"sam.optimisation/1\", \"model\": { \"engine\": \"tas-model\" },\n" +
                "  \"variables\": [ { \"name\": \"Heating\", \"minimum\": 18, \"maximum\": 23, \"target\": " + Offered("Office heating setpoint") + " } ],\n" +
                "  \"outputs\": [ { \"name\": \"Heating demand\", \"unit\": \"kWh\", \"measure\": " + Offered("Annual heating demand") + " },\n" +
                "    { \"name\": \"Overheating\", \"measure\": " + Offered("Overheating hours") + " } ],\n" +
                "  \"objective\": { \"output\": \"Heating demand\", \"sense\": \"minimise\" },\n" +
                "  \"method\": { \"algorithm\": \"golden-section\" } }";

            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Read(reply, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue(), true);

            Assert.True(diagnostics.IsRunnable(), string.Join("\n", diagnostics));
            Assert.Equal("Office", optimisationDefinition.Variables[0].Target.Reference["internalCondition"]);
            Assert.Equal(28, optimisationDefinition.Output("Overheating").Measure.Parameters["threshold"]);
        }

        [Fact]
        public void AIReply_InventingAModelItem_IsNotRunnable()
        {
            string reply = OptimisationFixtures.With(OptimisationFixtures.BoundHookeJeeves, "\"internalCondition\": \"Office\"\n        }\n      }\n    },\n    {\n      \"name\": \"Cooling setpoint\"", "\"internalCondition\": \"Boardroom\"\n        }\n      }\n    },\n    {\n      \"name\": \"Cooling setpoint\"");

            OptimisationFixtures.Read(reply, out List<OptimisationDiagnostic> diagnostics, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue(), true);

            Assert.False(diagnostics.IsRunnable());
            Assert.Equal("Internal condition “Boardroom” is not in this model, so Heating setpoint cannot be changed.", OptimisationFixtures.Single(diagnostics, "OPT609").Message);
        }

        [Fact]
        public void AIExchangeText_EmbedsABoundDefinition_AndItReadsBackUnchanged()
        {
            OptimisationDefinition optimisationDefinition = OptimisationFixtures.Definition(OptimisationFixtures.BoundHookeJeeves);
            string text = Core.Optimisation.Query.AIExchangeText(optimisationDefinition, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue());
            string start = "CURRENT DEFINITION\n";
            string reply = text.Substring(text.IndexOf(start, StringComparison.Ordinal) + start.Length);
            reply = reply.Substring(0, reply.IndexOf("\nTASK\n", StringComparison.Ordinal));

            Assert.Equal(optimisationDefinition.ToJson(), OptimisationFixtures.Read(reply, out _, OptimisationFixtures.TasModel(), OptimisationFixtures.ModelCatalogue(), true).ToJson());
        }

        [Fact]
        public void AIExchangeText_ForABindingEngine_HoldsNoPathOrMachineDetail()
        {
            string text = Core.Optimisation.Query.AIExchangeText(OptimisationFixtures.Definition(OptimisationFixtures.BoundHookeJeeves), OptimisationFixtures.TasModel(true), OptimisationFixtures.ModelCatalogue());

            Assert.DoesNotContain(":\\", text);
            Assert.DoesNotContain(Environment.UserName, text);
            Assert.DoesNotContain(Environment.MachineName, text);
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
        public void Schema_Bindings_AreEngineDefinedMaps()
        {
            using (JsonDocument jsonDocument = JsonDocument.Parse(Core.Optimisation.Query.SchemaText()))
            {
                JsonElement defs = jsonDocument.RootElement.GetProperty("$defs");

                Assert.Equal("#/$defs/target", defs.GetProperty("variable").GetProperty("properties").GetProperty("target").GetProperty("$ref").GetString());
                Assert.Equal("#/$defs/measure", defs.GetProperty("output").GetProperty("properties").GetProperty("measure").GetProperty("$ref").GetString());
                Assert.Equal("string", defs.GetProperty("reference").GetProperty("additionalProperties").GetProperty("type").GetString());
                Assert.Equal("number", defs.GetProperty("parameters").GetProperty("additionalProperties").GetProperty("type").GetString());
                Assert.Equal(new[] { "kind" }, defs.GetProperty("target").GetProperty("required").EnumerateArray().Select(x => x.GetString()));
                Assert.Equal(new[] { "kind" }, defs.GetProperty("measure").GetProperty("required").EnumerateArray().Select(x => x.GetString()));
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
