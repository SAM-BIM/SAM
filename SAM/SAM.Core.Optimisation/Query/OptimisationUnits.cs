// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core.Optimisation
{
    public static partial class Query
    {
        private static readonly List<OptimisationUnit> optimisationUnits = new List<OptimisationUnit>()
        {
            new OptimisationUnit("-", OptimisationQuantity.Dimensionless, "Unitless"),
            new OptimisationUnit("%", OptimisationQuantity.Percent, "Percent"),
            new OptimisationUnit("°C", OptimisationQuantity.Temperature, "Celsius", "degC", "deg C", "C"),
            new OptimisationUnit("°F", OptimisationQuantity.Temperature, "Fahrenheit", "degF", "deg F", "F"),
            // Engineering convention: a temperature difference is stated in K.
            new OptimisationUnit("K", OptimisationQuantity.TemperatureDifference, "KelvinDifference"),
            new OptimisationUnit("h", OptimisationQuantity.Time, "Hour", "hr", "hrs", "hours"),
            new OptimisationUnit("W", OptimisationQuantity.Power, "Watt"),
            new OptimisationUnit("kW", OptimisationQuantity.Power, "Kilowatt"),
            new OptimisationUnit("Wh", OptimisationQuantity.Energy, "WattHour"),
            new OptimisationUnit("kWh", OptimisationQuantity.Energy, "KilowattHour", "kW.h", "kW h"),
            new OptimisationUnit("MWh", OptimisationQuantity.Energy, "MegawattHour", "MW.h", "MW h"),
            new OptimisationUnit("kg", OptimisationQuantity.Mass, "Kilogram"),
            new OptimisationUnit("t", OptimisationQuantity.Mass, "Tonne", "tonne", "tonnes"),
            new OptimisationUnit("kgCO2e", OptimisationQuantity.Carbon, "Kilogram", "kgCO2", "kg CO2e", "kg CO2", "kgCO₂e", "kgCO₂"),
            new OptimisationUnit("tCO2e", OptimisationQuantity.Carbon, "Tonne", "tCO2", "t CO2e", "t CO2", "tCO₂e", "tCO₂"),
            new OptimisationUnit("GBP", OptimisationQuantity.Currency, null, "£"),
            new OptimisationUnit("EUR", OptimisationQuantity.Currency, null, "€"),
            new OptimisationUnit("USD", OptimisationQuantity.Currency, null, "$"),
            new OptimisationUnit("°", OptimisationQuantity.Angle, "Degree", "deg", "degrees"),
            new OptimisationUnit("m", OptimisationQuantity.Length, "Meter"),
            new OptimisationUnit("m²", OptimisationQuantity.Area, "SquareMeter", "m2", "sqm"),
        };

        /// <summary>
        /// The unit symbols an optimisation definition accepts, each with the quantity it measures. Units describe
        /// values; SAM never verifies them against the model.
        /// </summary>
        public static IReadOnlyList<OptimisationUnit> OptimisationUnits()
        {
            return optimisationUnits.AsReadOnly();
        }

        /// <summary>
        /// The unit written as <paramref name="text"/>: its canonical symbol (exact, case-sensitive: "MWh" is not
        /// "mWh"), or one of its synonyms, in which case <paramref name="synonym"/> is true. Null when unknown.
        /// </summary>
        public static OptimisationUnit OptimisationUnit(string text, out bool synonym)
        {
            synonym = false;
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string value = text.Trim();
            OptimisationUnit result = optimisationUnits.Find(x => x.Symbol == value);
            if (result != null)
            {
                synonym = value != text;
                return result;
            }

            result = optimisationUnits.Find(x => x.Synonyms.Contains(value));
            if (result != null)
            {
                synonym = true;
            }

            return result;
        }

        /// <summary>True when a value of <paramref name="quantity"/> may be stated in <paramref name="optimisationUnit"/>: the same quantity, or carbon and mass for each other.</summary>
        internal static bool Suits(this OptimisationUnit optimisationUnit, OptimisationQuantity quantity)
        {
            if (optimisationUnit == null || quantity == OptimisationQuantity.Unspecified)
            {
                return true;
            }

            if (optimisationUnit.Quantity == quantity)
            {
                return true;
            }

            // A carbon quantity is a mass of CO2(e): either kind of unit states it.
            return (quantity == OptimisationQuantity.Carbon && optimisationUnit.Quantity == OptimisationQuantity.Mass)
                || (quantity == OptimisationQuantity.Mass && optimisationUnit.Quantity == OptimisationQuantity.Carbon);
        }

        /// <summary>The symbols of <paramref name="quantity"/>, for messages.</summary>
        internal static List<string> UnitSymbols(OptimisationQuantity quantity)
        {
            return optimisationUnits.Where(x => x.Suits(quantity)).Select(x => x.Symbol).ToList();
        }
    }
}
