// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// One available design variable or output. A name-only entry is a name a script reads or writes ("tas-script"). An
    /// entry with a <see cref="Target"/> is a model item an engine can change, and one with a <see cref="Measure"/> a
    /// result it can read, each with the model's current value and, for a target, a suggested range or the options to
    /// choose from.
    /// </summary>
    public sealed class OptimisationCatalogueEntry
    {
        public OptimisationCatalogueEntry(string name, string description = null, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null)
        {
            Name = name;
            Description = description;
            Quantity = quantity;
            Unit = unit;
            Options = new List<string>().AsReadOnly();
        }

        /// <summary>A model item an engine can change.</summary>
        /// <param name="name">A suggested design variable name, for example "Office heating setpoint".</param>
        /// <param name="target">The target; its options are ignored (the available ones are <paramref name="options"/>).</param>
        /// <param name="value">The item's current value in the model, in <paramref name="unit"/>; null when unknown.</param>
        /// <param name="minimum">The lower end of a suggested range; null when none is suggested.</param>
        /// <param name="maximum">The upper end of a suggested range; null when none is suggested.</param>
        /// <param name="options">For a choice target, the model items it may choose between.</param>
        public OptimisationCatalogueEntry(string name, OptimisationTarget target, string description = null, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null, double? value = null, double? minimum = null, double? maximum = null, IEnumerable<string> options = null)
            : this(name, description, quantity, unit)
        {
            Target = target == null ? null : new OptimisationTarget(target.Kind, target.Reference, target.Parameters);
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            Options = (options ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList().AsReadOnly();
        }

        /// <summary>A result an engine can read.</summary>
        /// <param name="name">A suggested output name, for example "Annual heating demand".</param>
        /// <param name="measure">The measure, with the parameters it is offered with (for example a threshold).</param>
        /// <param name="value">The current value in the model's existing results, in <paramref name="unit"/>; null when unknown.</param>
        public OptimisationCatalogueEntry(string name, OptimisationMeasure measure, string description = null, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null, double? value = null)
            : this(name, description, quantity, unit)
        {
            Measure = measure == null ? null : new OptimisationMeasure(measure);
            Value = value;
        }

        public string Name { get; }

        public string Description { get; }

        public OptimisationQuantity Quantity { get; }

        public string Unit { get; }

        /// <summary>The model item a design variable can change; null for a name-only entry or an output.</summary>
        public OptimisationTarget Target { get; }

        /// <summary>The result an output can measure; null for a name-only entry or a design variable.</summary>
        public OptimisationMeasure Measure { get; }

        /// <summary>The current value in the model (a target) or in its existing results (a measure); null when unknown.</summary>
        public double? Value { get; }

        /// <summary>The lower end of a suggested range for a target; null when none is suggested.</summary>
        public double? Minimum { get; }

        /// <summary>The upper end of a suggested range for a target; null when none is suggested.</summary>
        public double? Maximum { get; }

        /// <summary>For a choice target, the model items it may choose between; otherwise empty.</summary>
        public IReadOnlyList<string> Options { get; }

        /// <summary>The target or the measure; null for a name-only entry.</summary>
        internal OptimisationBinding Binding => (OptimisationBinding)Target ?? Measure;
    }
}
