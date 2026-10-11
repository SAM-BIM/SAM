// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A target kind an engine can change, or a measure kind it can read (<see cref="IOptimisationCapabilities.Targets"/>,
    /// <see cref="IOptimisationCapabilities.Measures"/>): its quantity and unit, the reference keys that name the model
    /// item, and its parameters with their defaults. SAM validates bindings against it and knows nothing else about the
    /// kind.
    /// </summary>
    public sealed class OptimisationBindingCapability
    {
        public OptimisationBindingCapability(string kind, string displayName, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null, IEnumerable<OptimisationReferenceKey> referenceKeys = null, IEnumerable<OptimisationBindingParameter> parameters = null, bool acceptsOptions = false)
            : this(kind, displayName, quantity, unit, referenceKeys, parameters, acceptsOptions, null)
        {
        }

        /// <summary>A kind with a limit on the number of options a choice target may list (<see cref="MaximumOptions"/>).</summary>
        public OptimisationBindingCapability(string kind, string displayName, OptimisationQuantity quantity, string unit, IEnumerable<OptimisationReferenceKey> referenceKeys, IEnumerable<OptimisationBindingParameter> parameters, bool acceptsOptions, int? maximumOptions)
        {
            Kind = kind;
            DisplayName = displayName;
            Quantity = quantity;
            Unit = unit;
            ReferenceKeys = (referenceKeys ?? Enumerable.Empty<OptimisationReferenceKey>()).Where(x => x != null).ToList().AsReadOnly();
            Parameters = (parameters ?? Enumerable.Empty<OptimisationBindingParameter>()).Where(x => x != null).ToList().AsReadOnly();
            AcceptsOptions = acceptsOptions;
            MaximumOptions = acceptsOptions ? maximumOptions : null;
        }

        /// <summary>The kind written in a binding, for example "tbd.internal-condition.heating-setpoint".</summary>
        public string Kind { get; }

        /// <summary>The kind in words, for the window, messages and the AI text, for example "Zone heating setpoint".</summary>
        public string DisplayName { get; }

        /// <summary>
        /// The quantity of the value the engine writes (target) or reports (measure); unspecified when it depends on the
        /// model (for example a plant controller whose sensor decides the unit).
        /// </summary>
        public OptimisationQuantity Quantity { get; }

        /// <summary>The unit of that value, for example "°C" or "kWh"; null when it depends on the model.</summary>
        public string Unit { get; }

        /// <summary>The keys that name the model item; empty when the kind refers to the whole model.</summary>
        public IReadOnlyList<OptimisationReferenceKey> ReferenceKeys { get; }

        /// <summary>The numeric settings a binding may give.</summary>
        public IReadOnlyList<OptimisationBindingParameter> Parameters { get; }

        /// <summary>
        /// True for a choice target: it takes <see cref="OptimisationTarget.Options"/> (named model items) and its
        /// variable is "discrete". Never true for a measure.
        /// </summary>
        public bool AcceptsOptions { get; }

        /// <summary>
        /// For a choice target: the most options it may list, because every option costs one simulation when the choice
        /// is tried in full (for example 8 for a glazing choice). Null for no limit, and always null when
        /// <see cref="AcceptsOptions"/> is false. A limit of the engine, not of the schema: a definition with more is
        /// readable and editable but not runnable on this engine (OPT616).
        /// </summary>
        public int? MaximumOptions { get; }
    }
}
