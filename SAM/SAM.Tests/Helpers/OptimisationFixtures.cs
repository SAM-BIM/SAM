// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Optimisation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>Shared inputs for the SAM.Core.Optimisation tests.</summary>
    internal static class OptimisationFixtures
    {
        public const string GoldenSection = "systems-demo-golden-section.json";
        public const string HookeJeeves = "systems-demo-hooke-jeeves.json";

        /// <summary>
        /// Capabilities shaped like the V1 Tas engine (the real ones come from SAM_Tas in a later PR): golden section on
        /// exactly one variable, Hooke-Jeeves on one or more, minimise only, continuous variables, no constraints.
        /// </summary>
        public static readonly IOptimisationCapabilities Tas = new OptimisationCapabilities(
            "tas-script",
            "Tas",
            new[] { new OptimisationAlgorithmCapability(OptimisationAlgorithm.GoldenSection, 1, 1), new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null) },
            new[] { ObjectiveSense.Minimise },
            new[] { DesignVariableType.Continuous },
            false);

        /// <summary>Capabilities of an engine that runs everything the schema can express.</summary>
        public static readonly IOptimisationCapabilities Everything = new OptimisationCapabilities(
            "tas-script",
            "Future",
            new[] { new OptimisationAlgorithmCapability(OptimisationAlgorithm.GoldenSection, 1, 1), new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null), new OptimisationAlgorithmCapability(OptimisationAlgorithm.TryEveryOption, 1, 1) },
            new[] { ObjectiveSense.Minimise, ObjectiveSense.Maximise },
            new[] { DesignVariableType.Continuous, DesignVariableType.Integer, DesignVariableType.Discrete },
            true);

        // ---------- Model bindings (targets and measures) ----------

        public const string BoundGoldenSection = "systems-demo-bound-golden-section.json";
        public const string BoundHookeJeeves = "zone-setpoints-glazing-hooke-jeeves.json";
        public const string Choice = "glazing-choice.json";

        /// <summary>
        /// Capabilities shaped like the planned "tas-model" engine (the real ones come from SAM_Tas in PR7b): the V1
        /// targets and measures of the plan, plus a glazing choice target of at most 8 options (the owner's default), and
        /// the Tas search methods. <paramref name="discrete"/> adds what a choice needs: "discrete" variables and "try
        /// every option" on exactly one variable (a choice runs on its own in V1).
        /// </summary>
        public static OptimisationCapabilities TasModel(bool discrete = false)
        {
            OptimisationReferenceKey internalCondition = new OptimisationReferenceKey("internalCondition", "internal condition");
            OptimisationReferenceKey glazingConstruction = new OptimisationReferenceKey("glazingConstruction", "glazing construction");

            List<OptimisationAlgorithmCapability> algorithms = new List<OptimisationAlgorithmCapability>() { new OptimisationAlgorithmCapability(OptimisationAlgorithm.GoldenSection, 1, 1), new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null) };
            if (discrete)
            {
                algorithms.Add(new OptimisationAlgorithmCapability(OptimisationAlgorithm.TryEveryOption, 1, 1));
            }

            return new OptimisationCapabilities(
                "tas-model",
                "Tas",
                algorithms,
                new[] { ObjectiveSense.Minimise },
                discrete ? new[] { DesignVariableType.Continuous, DesignVariableType.Discrete } : new[] { DesignVariableType.Continuous },
                false,
                new[]
                {
                    new OptimisationBindingCapability("tbd.internal-condition.heating-setpoint", "Zone heating setpoint", OptimisationQuantity.Temperature, "°C", new[] { internalCondition }),
                    new OptimisationBindingCapability("tbd.internal-condition.cooling-setpoint", "Zone cooling setpoint", OptimisationQuantity.Temperature, "°C", new[] { internalCondition }),
                    new OptimisationBindingCapability("tbd.glazing-construction.g-value", "Glazing g-value", OptimisationQuantity.Dimensionless, "-", new[] { glazingConstruction }),
                    new OptimisationBindingCapability("tpd.controller.setpoint", "Plant controller setpoint", referenceKeys: new[] { new OptimisationReferenceKey("plantRoom", "plant room"), new OptimisationReferenceKey("controller", "controller") }),
                    new OptimisationBindingCapability("tbd.glazing-construction.choice", "Glazing construction choice", OptimisationQuantity.Unspecified, null, new[] { glazingConstruction }, null, true, 8),
                },
                new[]
                {
                    new OptimisationBindingCapability("tsd.annual-heating-demand", "Annual heating demand", OptimisationQuantity.Energy, "kWh"),
                    new OptimisationBindingCapability("tsd.annual-cooling-demand", "Annual cooling demand", OptimisationQuantity.Energy, "kWh"),
                    new OptimisationBindingCapability("tsd.overheating-hours", "Overheating hours", OptimisationQuantity.Time, "h", parameters: new[] { new OptimisationBindingParameter("threshold", 28, 20, 40, OptimisationQuantity.Temperature, "°C", "resultant temperature threshold") }),
                    new OptimisationBindingCapability("tpd.annual-energy", "Annual plant energy", OptimisationQuantity.Energy, "kWh"),
                    new OptimisationBindingCapability("tpd.annual-cost", "Annual plant cost", OptimisationQuantity.Currency, "GBP"),
                    new OptimisationBindingCapability("tpd.annual-co2", "Annual plant CO2", OptimisationQuantity.Carbon, "kgCO2e"),
                });
        }

        public static Dictionary<string, string> Reference(params string[] keysAndValues)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i + 1 < keysAndValues.Length; i += 2)
            {
                result[keysAndValues[i]] = keysAndValues[i + 1];
            }

            return result;
        }

        /// <summary>A catalogue as the "tas-model" engine would read it from a model (names and values illustrative).</summary>
        public static OptimisationCatalogue ModelCatalogue()
        {
            return new OptimisationCatalogue(
                new[]
                {
                    new OptimisationCatalogueEntry("Setpoint", new OptimisationTarget("tpd.controller.setpoint", Reference("plantRoom", "Plant Room 1", "controller", "HeatPumpController")), "HeatPumpController setpoint"),
                    new OptimisationCatalogueEntry("Office heating setpoint", new OptimisationTarget("tbd.internal-condition.heating-setpoint", Reference("internalCondition", "Office")), quantity: OptimisationQuantity.Temperature, unit: "°C", value: 21, minimum: 16, maximum: 24),
                    new OptimisationCatalogueEntry("Office cooling setpoint", new OptimisationTarget("tbd.internal-condition.cooling-setpoint", Reference("internalCondition", "Office")), quantity: OptimisationQuantity.Temperature, unit: "°C", value: 24, minimum: 21, maximum: 28),
                    new OptimisationCatalogueEntry("Meeting room heating setpoint", new OptimisationTarget("tbd.internal-condition.heating-setpoint", Reference("internalCondition", "Meeting room")), quantity: OptimisationQuantity.Temperature, unit: "°C", value: 20),
                    new OptimisationCatalogueEntry("Office glazing g-value", new OptimisationTarget("tbd.glazing-construction.g-value", Reference("glazingConstruction", "Office glazing")), unit: "-", value: 0.42, minimum: 0.2, maximum: 0.7),
                    new OptimisationCatalogueEntry("Office glazing", new OptimisationTarget("tbd.glazing-construction.choice", Reference("glazingConstruction", "Office glazing")), options: new[] { "Double low-e", "Triple low-e", "Double solar control" }),
                },
                new[]
                {
                    new OptimisationCatalogueEntry("Annual heating demand", new OptimisationMeasure("tsd.annual-heating-demand"), unit: "kWh", value: 41200.5),
                    new OptimisationCatalogueEntry("Annual cooling demand", new OptimisationMeasure("tsd.annual-cooling-demand"), unit: "kWh", value: 18750),
                    new OptimisationCatalogueEntry("Overheating hours", new OptimisationMeasure("tsd.overheating-hours", parameters: new Dictionary<string, double>() { { "threshold", 28 } }), unit: "h", value: 37),
                    new OptimisationCatalogueEntry("Annual plant energy", new OptimisationMeasure("tpd.annual-energy"), unit: "kWh"),
                    new OptimisationCatalogueEntry("Annual plant cost", new OptimisationMeasure("tpd.annual-cost"), unit: "GBP"),
                    new OptimisationCatalogueEntry("Annual plant CO2", new OptimisationMeasure("tpd.annual-co2"), unit: "kgCO2e"),
                },
                "read from the Tas model");
        }

        public static string Text(string fileName)
        {
            return File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "Optimisation", fileName));
        }

        /// <summary>Reads text that must load; returns the definition and its diagnostics.</summary>
        public static OptimisationDefinition Read(string text, out List<OptimisationDiagnostic> diagnostics, IOptimisationCapabilities capabilities = null, bool extract = false)
        {
            OptimisationDefinition result = Create.OptimisationDefinition(text, out diagnostics, capabilities, extract);
            Assert.True(result != null, "Not read: " + string.Join(Environment.NewLine, diagnostics));
            return result;
        }

        /// <summary>Reads text that must load, checking its bindings against <paramref name="catalogue"/> too.</summary>
        public static OptimisationDefinition Read(string text, out List<OptimisationDiagnostic> diagnostics, IOptimisationCapabilities capabilities, OptimisationCatalogue catalogue, bool extract = false)
        {
            OptimisationDefinition result = Create.OptimisationDefinition(text, out diagnostics, capabilities, catalogue, extract);
            Assert.True(result != null, "Not read: " + string.Join(Environment.NewLine, diagnostics));
            return result;
        }

        /// <summary>The single diagnostic with <paramref name="code"/>.</summary>
        public static OptimisationDiagnostic Single(IEnumerable<OptimisationDiagnostic> diagnostics, string code)
        {
            List<OptimisationDiagnostic> list = diagnostics.Where(x => x.Code == code).ToList();
            Assert.True(list.Count == 1, "Expected one " + code + ", got: " + string.Join(Environment.NewLine, diagnostics));
            return list[0];
        }

        /// <summary>The golden-section fixture with <paramref name="find"/> replaced by <paramref name="replace"/> (which must occur).</summary>
        public static string GoldenSectionWith(string find, string replace)
        {
            return With(GoldenSection, find, replace);
        }

        /// <summary>The fixture <paramref name="fileName"/> with <paramref name="find"/> replaced by <paramref name="replace"/> (which must occur).</summary>
        public static string With(string fileName, string find, string replace)
        {
            string text = Text(fileName);
            Assert.Contains(find, text);
            return text.Replace(find, replace);
        }

        /// <summary>The fixture <paramref name="fileName"/> read (it must load).</summary>
        public static OptimisationDefinition Definition(string fileName)
        {
            return Read(Text(fileName), out _);
        }

        /// <summary>The codes of the errors, in order.</summary>
        public static List<string> Errors(IEnumerable<OptimisationDiagnostic> diagnostics)
        {
            return diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => x.Code).ToList();
        }
    }
}
