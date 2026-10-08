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
            new[] { new OptimisationAlgorithmCapability(OptimisationAlgorithm.GoldenSection, 1, 1), new OptimisationAlgorithmCapability(OptimisationAlgorithm.HookeJeeves, 1, null) },
            new[] { ObjectiveSense.Minimise, ObjectiveSense.Maximise },
            new[] { DesignVariableType.Continuous, DesignVariableType.Integer, DesignVariableType.Discrete },
            true);

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
            string text = Text(GoldenSection);
            Assert.Contains(find, text);
            return text.Replace(find, replace);
        }
    }
}
