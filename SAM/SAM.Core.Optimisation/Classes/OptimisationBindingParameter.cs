// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A numeric setting of a binding kind, as an engine defines it, for example the "threshold" of an overheating
    /// measure (°C, default 28). A binding that leaves it out uses <see cref="Default"/>.
    /// </summary>
    public sealed class OptimisationBindingParameter
    {
        public OptimisationBindingParameter(string name, double? @default = null, double? minimum = null, double? maximum = null, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null, string displayName = null)
        {
            Name = name;
            Default = @default;
            Minimum = minimum;
            Maximum = maximum;
            Quantity = quantity;
            Unit = unit;
            DisplayName = displayName;
        }

        /// <summary>The key written in the binding's parameters, for example "threshold".</summary>
        public string Name { get; }

        /// <summary>The value the engine uses when the binding leaves the parameter out; null when it has none.</summary>
        public double? Default { get; }

        /// <summary>The smallest accepted value (inclusive); null for no limit.</summary>
        public double? Minimum { get; }

        /// <summary>The largest accepted value (inclusive); null for no limit.</summary>
        public double? Maximum { get; }

        public OptimisationQuantity Quantity { get; }

        /// <summary>The unit of the value, for example "°C". Optional.</summary>
        public string Unit { get; }

        /// <summary>The setting in words, for messages, for example "overheating threshold". Optional.</summary>
        public string DisplayName { get; }
    }
}
