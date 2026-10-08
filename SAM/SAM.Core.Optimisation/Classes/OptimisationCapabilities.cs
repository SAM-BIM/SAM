// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>A plain <see cref="IOptimisationCapabilities"/>: an engine builds one to describe what it runs.</summary>
    public sealed class OptimisationCapabilities : IOptimisationCapabilities
    {
        public OptimisationCapabilities(string engine, string displayName, IEnumerable<OptimisationAlgorithmCapability> algorithms, IEnumerable<ObjectiveSense> senses, IEnumerable<DesignVariableType> variableTypes, bool supportsConstraints)
        {
            Engine = engine;
            DisplayName = displayName;
            Algorithms = (algorithms ?? Enumerable.Empty<OptimisationAlgorithmCapability>()).Where(x => x != null).ToList().AsReadOnly();
            Senses = (senses ?? Enumerable.Empty<ObjectiveSense>()).Distinct().ToList().AsReadOnly();
            VariableTypes = (variableTypes ?? Enumerable.Empty<DesignVariableType>()).Distinct().ToList().AsReadOnly();
            SupportsConstraints = supportsConstraints;
        }

        public string Engine { get; }

        public string DisplayName { get; }

        public IReadOnlyList<OptimisationAlgorithmCapability> Algorithms { get; }

        public IReadOnlyList<ObjectiveSense> Senses { get; }

        public IReadOnlyList<DesignVariableType> VariableTypes { get; }

        public bool SupportsConstraints { get; }
    }
}
