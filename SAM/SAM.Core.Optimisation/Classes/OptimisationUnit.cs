// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Units;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// A unit symbol the definition accepts, the quantity it measures, and the <see cref="Units.UnitType"/> SAM knows
    /// it as (Undefined for units SAM.Units has no type for, such as currencies). The list is
    /// <see cref="Query.OptimisationUnits"/>.
    /// </summary>
    public sealed class OptimisationUnit
    {
        internal OptimisationUnit(string symbol, OptimisationQuantity quantity, string unitTypeText = null, params string[] synonyms)
        {
            Symbol = symbol;
            Quantity = quantity;
            UnitType = string.IsNullOrEmpty(unitTypeText) ? Units.UnitType.Undefined : Units.Query.UnitType(unitTypeText);
            Synonyms = (synonyms ?? new string[0]).ToList().AsReadOnly();
        }

        /// <summary>The canonical symbol written in the definition, for example "°C".</summary>
        public string Symbol { get; }

        public OptimisationQuantity Quantity { get; }

        /// <summary>
        /// SAM's unit type for this unit when SAM.Units has one (resolved by name at run time, so a newer SAM.Units adds
        /// types without a change here); otherwise <see cref="Units.UnitType.Undefined"/>.
        /// </summary>
        public UnitType UnitType { get; }

        /// <summary>Other spellings accepted with a note, for example "degC" for "°C".</summary>
        public IReadOnlyList<string> Synonyms { get; }

        public override string ToString()
        {
            return Symbol;
        }
    }
}
