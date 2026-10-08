// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>One available design variable or output: its name and, when known, its meaning and unit.</summary>
    public sealed class OptimisationCatalogueEntry
    {
        public OptimisationCatalogueEntry(string name, string description = null, OptimisationQuantity quantity = OptimisationQuantity.Unspecified, string unit = null)
        {
            Name = name;
            Description = description;
            Quantity = quantity;
            Unit = unit;
        }

        public string Name { get; }

        public string Description { get; }

        public OptimisationQuantity Quantity { get; }

        public string Unit { get; }
    }
}
