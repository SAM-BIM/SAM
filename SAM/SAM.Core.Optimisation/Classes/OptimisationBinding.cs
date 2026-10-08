// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// What a design variable changes in the model (<see cref="OptimisationTarget"/>) or what an output measures
    /// (<see cref="OptimisationMeasure"/>), so that an engine can change and read the model itself instead of running a
    /// hand-written script. SAM holds no engine knowledge: <see cref="Kind"/>, the <see cref="Reference"/> keys and the
    /// <see cref="Parameters"/> are defined by the engine, which lists them in its capabilities
    /// (<see cref="IOptimisationCapabilities.Targets"/>, <see cref="IOptimisationCapabilities.Measures"/>); the
    /// diagnostics check a binding only against those.
    /// </summary>
    public abstract class OptimisationBinding
    {
        protected OptimisationBinding()
        {
        }

        protected OptimisationBinding(string kind, IDictionary<string, string> reference, IDictionary<string, double> parameters)
        {
            Kind = kind;
            Reference = reference == null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(reference, StringComparer.Ordinal);
            Parameters = parameters == null ? new Dictionary<string, double>(StringComparer.Ordinal) : new Dictionary<string, double>(parameters, StringComparer.Ordinal);
        }

        protected OptimisationBinding(OptimisationBinding optimisationBinding)
            : this(optimisationBinding?.Kind, optimisationBinding?.Reference, optimisationBinding?.Parameters)
        {
        }

        /// <summary>The engine-defined kind, for example "tbd.internal-condition.heating-setpoint". Required.</summary>
        public string Kind { get; set; }

        /// <summary>
        /// The model item, by the engine's keys and the model's own names, for example
        /// { "internalCondition": "Office" }. Empty when the kind refers to the whole model. Keys are written in
        /// ordinal order.
        /// </summary>
        public Dictionary<string, string> Reference { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Numeric settings of the kind, for example { "threshold": 28 }. A setting left out uses the engine's default.
        /// Keys are written in ordinal order.
        /// </summary>
        public Dictionary<string, double> Parameters { get; set; } = new Dictionary<string, double>(StringComparer.Ordinal);

        /// <summary>True when both bindings have the same kind and refer to the same model item (parameters aside).</summary>
        public bool SameItem(OptimisationBinding optimisationBinding)
        {
            if (optimisationBinding == null || !string.Equals(Kind, optimisationBinding.Kind, StringComparison.Ordinal))
            {
                return false;
            }

            List<KeyValuePair<string, string>> reference = References(this);
            List<KeyValuePair<string, string>> reference_Other = References(optimisationBinding);
            return reference.Count == reference_Other.Count && reference.Zip(reference_Other, (x, y) => x.Key == y.Key && x.Value == y.Value).All(x => x);
        }

        /// <summary>The reference entries with a value, in ordinal key order.</summary>
        internal static List<KeyValuePair<string, string>> References(OptimisationBinding optimisationBinding)
        {
            return (optimisationBinding?.Reference ?? new Dictionary<string, string>()).Where(x => x.Key != null && x.Value != null).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
        }
    }
}
