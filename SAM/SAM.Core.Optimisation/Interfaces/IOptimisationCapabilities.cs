// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// What an engine can actually run. The engine (for example SAM_Tas for "tas-script") supplies it; validation
    /// (<see cref="Query.Diagnostics"/>) makes a definition that asks for more non-runnable with a clear message, and
    /// the AI exchange text (<see cref="Query.AIExchangeText"/>) offers only what is listed here.
    /// </summary>
    public interface IOptimisationCapabilities
    {
        /// <summary>The engine identifier a definition's <see cref="OptimisationModel.Engine"/> must match, for example "tas-script".</summary>
        string Engine { get; }

        /// <summary>A short engine name for messages, for example "Tas".</summary>
        string DisplayName { get; }

        /// <summary>The search methods the engine runs, each with how many design variables it accepts.</summary>
        IReadOnlyList<OptimisationAlgorithmCapability> Algorithms { get; }

        /// <summary>Minimise, maximise or both.</summary>
        IReadOnlyList<ObjectiveSense> Senses { get; }

        /// <summary>The design variable types the engine runs.</summary>
        IReadOnlyList<DesignVariableType> VariableTypes { get; }

        /// <summary>True when the engine enforces <see cref="OptimisationDefinition.Constraints"/>.</summary>
        bool SupportsConstraints { get; }

        /// <summary>
        /// The target kinds the engine can change in the model. Empty for an engine that takes no targets (its script
        /// changes the model); otherwise every design variable must have one of these as its target.
        /// </summary>
        IReadOnlyList<OptimisationBindingCapability> Targets { get; }

        /// <summary>
        /// The measure kinds the engine can read from the results. Empty for an engine that takes no measures (its script
        /// reports the outputs); otherwise every output must have one of these as its measure.
        /// </summary>
        IReadOnlyList<OptimisationBindingCapability> Measures { get; }
    }
}
