// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Units;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// Engineering display formatting added for optimisation results: energy and mass display units with group
    /// switching, significant figures for values with no known unit, per-category decimals overrides, and the public
    /// number formatting. Display only: the values themselves are never changed.
    /// </summary>
    public class QuantityFormatterEngineeringTests
    {
        private static QuantityFormatter Formatter(string culture = "en-GB", Dictionary<UnitCategory, int> decimals = null, UnitStyle unitStyle = UnitStyle.SI)
        {
            return new QuantityFormatter(new DocumentOptions() { UnitSystem = unitStyle, Culture = CultureInfo.GetCultureInfo(culture), Decimals = decimals });
        }

        private static ReportValue<Quantity> Value(double value, UnitType unitType)
        {
            return ReportValue<Quantity>.Available(new Quantity(value, unitType), ReportValueSource.SAM);
        }

        // ---------- Energy and mass ----------

        [Theory]
        [InlineData(UnitStyle.SI, 7360.04370117188, UnitType.KilowattHour, "7,360.0", "kWh")]
        [InlineData(UnitStyle.SI, 9999.94, UnitType.KilowattHour, "9,999.9", "kWh")]
        [InlineData(UnitStyle.SI, 10000.0, UnitType.KilowattHour, "10.00", "MWh")]
        [InlineData(UnitStyle.SI, 12.5, UnitType.MegawattHour, "12.50", "MWh")]
        [InlineData(UnitStyle.SI, 1500.0, UnitType.WattHour, "1.5", "kWh")]
        [InlineData(UnitStyle.Imperial, 7360.0, UnitType.KilowattHour, "7,360.0", "kWh")]
        [InlineData(UnitStyle.SI, 4076.69276428223, UnitType.Kilogram, "4,076.7", "kg")]
        [InlineData(UnitStyle.SI, 12346.0, UnitType.Kilogram, "12.35", "t")]
        [InlineData(UnitStyle.Imperial, 2.0, UnitType.Tonne, "2,000.0", "kg")]
        public void EnergyAndMass_StandAlone(UnitStyle unitStyle, double value, UnitType unitType, string text, string unit)
        {
            FormattedValue formattedValue = Formatter(unitStyle: unitStyle).Format(Value(value, unitType));

            Assert.Equal(text, formattedValue.Text);
            Assert.Equal(unit, formattedValue.Unit);
        }

        [Fact]
        public void EnergyGroup_SharesOneUnit_ChosenByItsLargestValue()
        {
            QuantityFormatter quantityFormatter = Formatter();
            Quantity[] quantities = { new Quantity(800, UnitType.KilowattHour), new Quantity(25000, UnitType.KilowattHour) };

            DisplayUnit displayUnit = quantityFormatter.SelectDisplayUnit(UnitCategory.Energy, quantities);

            Assert.Equal(UnitType.MegawattHour, displayUnit.UnitType);
            Assert.Equal("0.80", quantityFormatter.Format(Value(800, UnitType.KilowattHour), displayUnit).Text);
            Assert.Equal("25.00", quantityFormatter.Format(Value(25000, UnitType.KilowattHour), displayUnit).Text);
        }

        [Fact]
        public void PowerGroupSwitching_IsUnchanged()
        {
            QuantityFormatter si = Formatter();
            QuantityFormatter imperial = Formatter(unitStyle: UnitStyle.Imperial);

            Assert.Equal(UnitType.Watt, si.SelectDisplayUnit(UnitCategory.Power, new[] { new Quantity(9999, UnitType.Watt) }).UnitType);
            Assert.Equal(UnitType.Kilowatt, si.SelectDisplayUnit(UnitCategory.Power, new[] { new Quantity(10000, UnitType.Watt) }).UnitType);
            Assert.Equal(2, si.SelectDisplayUnit(UnitCategory.Power, new[] { new Quantity(10000, UnitType.Watt) }).Decimals);
            Assert.Equal(UnitType.KiloBtuPerHour, imperial.SelectDisplayUnit(UnitCategory.Power, new[] { new Quantity(100, UnitType.Kilowatt) }).UnitType);
        }

        [Fact]
        public void QuantitiesOfAnotherCategory_DoNotSwitchTheGroup()
        {
            DisplayUnit displayUnit = Formatter().SelectDisplayUnit(UnitCategory.Energy, new[] { new Quantity(1e9, UnitType.Watt), new Quantity(10, UnitType.KilowattHour) });

            Assert.Equal(UnitType.KilowattHour, displayUnit.UnitType);
        }

        // ---------- Decimals override ----------

        [Fact]
        public void DecimalsOverride_ReplacesTheCategoryDefault_Only()
        {
            QuantityFormatter quantityFormatter = Formatter(decimals: new Dictionary<UnitCategory, int> { { UnitCategory.Ratio, 1 } });

            Assert.Equal("12.3", quantityFormatter.Format(Value(0.1234, UnitType.Unitless)).Text);
            Assert.Equal("%", quantityFormatter.Format(Value(0.1234, UnitType.Unitless)).Unit);
            Assert.Equal("21.0", quantityFormatter.Format(Value(21, UnitType.Celsius)).Text);

            // Without the override the policy is unchanged.
            Assert.Equal("12", Formatter().Format(Value(0.1234, UnitType.Unitless)).Text);
        }

        [Fact]
        public void DecimalsOverride_AppliesToTheSwitchedUnitToo()
        {
            QuantityFormatter quantityFormatter = Formatter(decimals: new Dictionary<UnitCategory, int> { { UnitCategory.Energy, 0 } });

            Assert.Equal("7,360", quantityFormatter.Format(Value(7360.04, UnitType.KilowattHour)).Text);
            Assert.Equal("25", quantityFormatter.Format(Value(25000, UnitType.KilowattHour)).Text);
        }

        [Fact]
        public void DocumentOptions_CopyConstructor_CopiesDecimals()
        {
            DocumentOptions documentOptions = new DocumentOptions() { Decimals = new Dictionary<UnitCategory, int> { { UnitCategory.Ratio, 1 } } };
            DocumentOptions copy = new DocumentOptions(documentOptions);
            documentOptions.Decimals[UnitCategory.Ratio] = 3;

            Assert.Equal(1, copy.Decimals[UnitCategory.Ratio]);
            Assert.Null(new DocumentOptions(new DocumentOptions()).Decimals);
        }

        // ---------- Significant figures ----------

        [Theory]
        [InlineData(7360.04370117188, 4, "7,360")]
        [InlineData(4076.69276428223, 4, "4,077")]
        [InlineData(4.968943799848584, 4, "4.969")]
        [InlineData(4.968943799848584, 2, "5.0")]
        [InlineData(0.1, 4, "0.1000")]
        [InlineData(-5.0, 4, "-5.000")]
        [InlineData(123456.789, 4, "123,457")]
        [InlineData(0.0, 4, "0")]
        [InlineData(0.001, 4, "0.001000")]
        public void FormatSignificant(double value, int significantFigures, string expected)
        {
            Assert.Equal(expected, Formatter().FormatSignificant(value, significantFigures));
        }

        [Theory]
        [InlineData(0.000123, "1.230E-4")]
        [InlineData(-0.000123456, "-1.235E-4")]
        public void FormatSignificant_BelowTheThreshold_IsScientific(double value, string expected)
        {
            Assert.Equal(expected, Formatter().FormatSignificant(value));
        }

        [Fact]
        public void FormatSignificant_NeverPrintsMinusZero()
        {
            Assert.Equal("0", Formatter().FormatSignificant(-0.0));
            Assert.Equal("0", Formatter().FormatNumber(-0.00001, 0));
            Assert.Equal("0.0", Formatter().FormatNumber(-0.00001, 1));
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NotANumber_IsNotAvailable(double value)
        {
            QuantityFormatter quantityFormatter = Formatter();

            Assert.Equal(quantityFormatter.NotAvailableText, quantityFormatter.FormatSignificant(value));
            Assert.Equal(quantityFormatter.NotAvailableText, quantityFormatter.FormatNumber(value, 2));
        }

        [Fact]
        public void Culture_SetsTheSeparators()
        {
            Assert.Equal("7.360", Formatter("de-DE").FormatSignificant(7360.04370117188));
            Assert.Equal("4,969", Formatter("de-DE").FormatSignificant(4.968943799848584));
            Assert.Equal("1,230E-4", Formatter("de-DE").FormatSignificant(0.000123));
        }

        [Theory]
        [InlineData(new[] { 7360.04370117188, 7360.12, 7401.5 }, 4, 0)]
        [InlineData(new[] { 4.97, 5.0, 10.0 }, 4, 2)]
        [InlineData(new[] { 0.25, 0.5 }, 3, 3)]
        [InlineData(new[] { 0.0, 0.0 }, 4, 0)]
        [InlineData(new double[0], 4, 0)]
        [InlineData(new[] { 1e-9 }, 4, QuantityFormatter.MaximumDecimals)]
        [InlineData(new[] { double.NaN, 12.0 }, 4, 2)]
        public void DecimalsForSignificantFigures_FollowsTheLargestMagnitude(double[] values, int significantFigures, int expected)
        {
            Assert.Equal(expected, QuantityFormatter.DecimalsForSignificantFigures(values, significantFigures));
        }

        [Fact]
        public void AColumn_SharesItsDecimals()
        {
            QuantityFormatter quantityFormatter = Formatter();
            double[] column = { 7360.04370117188, 7361.9, 8012.25 };

            int decimals = QuantityFormatter.DecimalsForSignificantFigures(column);

            Assert.Equal(new[] { "7,360", "7,362", "8,012" }, Array.ConvertAll(column, x => quantityFormatter.FormatNumber(x, decimals)));
        }

        [Fact]
        public void InvalidArguments_Throw()
        {
            QuantityFormatter quantityFormatter = Formatter();

            Assert.Throws<ArgumentOutOfRangeException>(() => quantityFormatter.FormatNumber(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => quantityFormatter.FormatNumber(1, 16));
            Assert.Throws<ArgumentOutOfRangeException>(() => quantityFormatter.FormatSignificant(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => QuantityFormatter.DecimalsForSignificantFigures(new[] { 1.0 }, 16));
        }

        [Fact]
        public void Formatting_DoesNotChangeTheValue()
        {
            double value = 4.968943799848584;
            ReportValue<Quantity> reportValue = Value(value, UnitType.Celsius);

            Formatter().Format(reportValue);
            Formatter().FormatSignificant(value);

            Assert.True(reportValue.TryGetValue(out Quantity quantity));
            Assert.Equal(value, quantity.Value);
        }
    }
}
