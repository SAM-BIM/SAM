// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Units;
using System;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// The Energy (Wh, kWh, MWh) and Mass (kg, t) categories: appended to the enums without moving existing
    /// ordinals, convertible within their category only, and parsed from their abbreviations.
    /// </summary>
    public class EnergyMassUnitsTests
    {
        [Fact]
        public void NewUnitTypes_AreAppended_AfterRadian()
        {
            Assert.Equal((int)UnitType.Radian + 1, (int)UnitType.WattHour);
            Assert.Equal((int)UnitType.Radian + 2, (int)UnitType.KilowattHour);
            Assert.Equal((int)UnitType.Radian + 3, (int)UnitType.MegawattHour);
            Assert.Equal((int)UnitType.Radian + 4, (int)UnitType.Kilogram);
            Assert.Equal((int)UnitType.Radian + 5, (int)UnitType.Tonne);
            Assert.Equal(UnitType.Tonne, Enum.GetValues(typeof(UnitType)).Cast<UnitType>().Max());
        }

        [Fact]
        public void NewUnitCategories_AreAppended_AfterAngle()
        {
            Assert.Equal((int)UnitCategory.Angle + 1, (int)UnitCategory.Energy);
            Assert.Equal((int)UnitCategory.Angle + 2, (int)UnitCategory.Mass);
        }

        [Theory]
        [InlineData(UnitType.WattHour, UnitCategory.Energy, "Wh")]
        [InlineData(UnitType.KilowattHour, UnitCategory.Energy, "kWh")]
        [InlineData(UnitType.MegawattHour, UnitCategory.Energy, "MWh")]
        [InlineData(UnitType.Kilogram, UnitCategory.Mass, "kg")]
        [InlineData(UnitType.Tonne, UnitCategory.Mass, "t")]
        public void Category_Abbreviation_AndParsing(UnitType unitType, UnitCategory unitCategory, string abbreviation)
        {
            Assert.Equal(unitCategory, unitType.UnitCategory());
            Assert.Equal(abbreviation, unitType.Abbreviation());
            Assert.Equal(unitType, Units.Query.UnitType(abbreviation));
        }

        [Theory]
        [InlineData(1500.0, UnitType.WattHour, UnitType.KilowattHour, 1.5)]
        [InlineData(1.0, UnitType.KilowattHour, UnitType.WattHour, 1000.0)]
        [InlineData(12500.0, UnitType.KilowattHour, UnitType.MegawattHour, 12.5)]
        [InlineData(2.0, UnitType.MegawattHour, UnitType.KilowattHour, 2000.0)]
        [InlineData(4076.69276428223, UnitType.Kilogram, UnitType.Tonne, 4.07669276428223)]
        [InlineData(3.0, UnitType.Tonne, UnitType.Kilogram, 3000.0)]
        public void Convert_WithinCategory(double value, UnitType from, UnitType to, double expected)
        {
            double actual = Units.Convert.ByUnitType(value, from, to);
            Assert.True(System.Math.Abs(expected - actual) <= System.Math.Max(1, System.Math.Abs(expected)) * 1e-12, $"expected {expected}, got {actual}");
        }

        [Theory]
        [InlineData(UnitType.KilowattHour, UnitType.Jule)]
        [InlineData(UnitType.KilowattHour, UnitType.Kilojule)]
        [InlineData(UnitType.KilowattHour, UnitType.Kilowatt)]
        [InlineData(UnitType.Kilogram, UnitType.KilowattHour)]
        [InlineData(UnitType.Tonne, UnitType.CubicMeter)]
        public void Convert_AcrossCategories_IsNaN(UnitType from, UnitType to)
        {
            // Jule and Kilojule stay in the Enthaply family; Energy does not convert to J (documented, unchanged).
            Assert.True(double.IsNaN(Units.Convert.ByUnitType(1.0, from, to)));
        }

        [Theory]
        [InlineData(UnitStyle.SI)]
        [InlineData(UnitStyle.Imperial)]
        public void DefaultUnit_IsKilowattHourAndKilogram_InBothStyles(UnitStyle unitStyle)
        {
            Assert.Equal(UnitType.KilowattHour, unitStyle.UnitType(UnitCategory.Energy));
            Assert.Equal(UnitType.Kilogram, unitStyle.UnitType(UnitCategory.Mass));
        }

        [Fact]
        public void UnitTypes_ListEveryUnitOfTheCategory()
        {
            Assert.Equal(new[] { UnitType.WattHour, UnitType.KilowattHour, UnitType.MegawattHour }, UnitCategory.Energy.UnitTypes());
            Assert.Equal(new[] { UnitType.Kilogram, UnitType.Tonne }, UnitCategory.Mass.UnitTypes(UnitStyle.SI));
        }

        [Theory]
        [InlineData(2500.0, UnitType.WattHour, 2.5)]
        [InlineData(3.0, UnitType.MegawattHour, 3000.0)]
        public void ToSI_AndToImperial_GoToKilowattHour(double value, UnitType from, double expected)
        {
            Assert.Equal(expected, Units.Convert.ToSI(value, from), 9);
            Assert.Equal(expected, Units.Convert.ToImperial(value, from), 9);
        }

        [Fact]
        public void ToSI_AndToImperial_GoToKilogram()
        {
            Assert.Equal(2000.0, Units.Convert.ToSI(2.0, UnitType.Tonne), 9);
            Assert.Equal(2000.0, Units.Convert.ToImperial(2.0, UnitType.Tonne), 9);
        }

        [Fact]
        public void Quantity_ConvertTo_Energy()
        {
            Quantity quantity = new Quantity(7360.0, UnitType.KilowattHour);

            Assert.Equal(UnitCategory.Energy, quantity.Category);
            Assert.Equal(7.36, quantity.ConvertTo(UnitType.MegawattHour).Value, 12);
        }
    }
}
