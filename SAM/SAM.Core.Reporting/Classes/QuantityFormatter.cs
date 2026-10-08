// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Units;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Core.Reporting
{
    /// <summary>
    /// Default <see cref="IQuantityFormatter"/>: a display policy per <see cref="UnitCategory"/> and
    /// <see cref="UnitStyle"/>. Conversions go through <see cref="Quantity.ConvertTo(UnitType)"/>.
    /// </summary>
    public sealed class QuantityFormatter : IQuantityFormatter
    {
        /// <summary>
        /// SI power switches from W to kW when the group's largest value reaches 10 kW.
        /// </summary>
        public const double KilowattThreshold_W = 10000;

        /// <summary>
        /// Imperial power switches from Btu/h to kBtu/h when the group's largest value reaches 100 kBtu/h.
        /// </summary>
        public const double KiloBtuPerHourThreshold_BtuPerHour = 100000;

        /// <summary>
        /// Energy switches from kWh to MWh when the group's largest value reaches 10 MWh.
        /// </summary>
        public const double MegawattHourThreshold_kWh = 10000;

        /// <summary>
        /// Mass switches from kg to t when the group's largest value reaches 10 t.
        /// </summary>
        public const double TonneThreshold_kg = 10000;

        /// <summary>
        /// Values whose magnitude is below this (and not zero) are shown in scientific notation by
        /// <see cref="FormatSignificant(double, int)"/>.
        /// </summary>
        public const double ScientificThreshold = 1e-3;

        /// <summary>
        /// The most decimals <see cref="DecimalsForSignificantFigures"/> returns.
        /// </summary>
        public const int MaximumDecimals = 6;

        private readonly CultureInfo cultureInfo;
        private readonly AirFlowDisplay airFlowDisplay;
        private readonly Dictionary<UnitCategory, int> decimalsByCategory;

        public QuantityFormatter(DocumentOptions documentOptions = null)
        {
            documentOptions = documentOptions ?? new DocumentOptions();

            UnitSystem = documentOptions.UnitSystem == UnitStyle.Imperial ? UnitStyle.Imperial : UnitStyle.SI;
            cultureInfo = documentOptions.Culture ?? CultureInfo.InvariantCulture;
            airFlowDisplay = documentOptions.SIAirFlow;
            decimalsByCategory = documentOptions.Decimals == null ? new Dictionary<UnitCategory, int>() : new Dictionary<UnitCategory, int>(documentOptions.Decimals);
        }

        public UnitStyle UnitSystem { get; }

        public string NotAvailableText => "—";

        public string NotApplicableText => "n/a";

        /// <summary>
        /// The category's default display unit, with the decimals of <see cref="DocumentOptions.Decimals"/> when the
        /// host overrides them for that category.
        /// </summary>
        public DisplayUnit DisplayUnit(UnitCategory unitCategory)
        {
            return WithDecimals(unitCategory, DefaultDisplayUnit(unitCategory));
        }

        private DisplayUnit DefaultDisplayUnit(UnitCategory unitCategory)
        {
            bool si = UnitSystem == UnitStyle.SI;

            switch (unitCategory)
            {
                case UnitCategory.Length:
                    return si ? new DisplayUnit(UnitType.Meter, "m", 2) : new DisplayUnit(UnitType.Feet, "ft", 1);

                case UnitCategory.Area:
                    return si ? new DisplayUnit(UnitType.SquareMeter, "m²", 1) : new DisplayUnit(UnitType.SquareFoot, "ft²", 0);

                case UnitCategory.Volume:
                    return si ? new DisplayUnit(UnitType.CubicMeter, "m³", 1) : new DisplayUnit(UnitType.CubicFoot, "ft³", 0);

                case UnitCategory.Temperature:
                    return si ? new DisplayUnit(UnitType.Celsius, "°C", 1) : new DisplayUnit(UnitType.Fahrenheit, "°F", 1);

                case UnitCategory.TemperatureDifference:
                    return si ? new DisplayUnit(UnitType.KelvinDifference, "K", 1) : new DisplayUnit(UnitType.FahrenheitDifference, "Δ°F", 1);

                case UnitCategory.AirFlow:
                    if (!si)
                    {
                        return new DisplayUnit(UnitType.CubicFootPerMinute, "cfm", 0);
                    }

                    return airFlowDisplay == AirFlowDisplay.CubicMetersPerSecond ? new DisplayUnit(UnitType.CubicMeterPerSecond, "m³/s", 3) : new DisplayUnit(UnitType.LitersPerSecond, "L/s", 0);

                case UnitCategory.Power:
                    return si ? new DisplayUnit(UnitType.Watt, "W", 0) : new DisplayUnit(UnitType.BtuPerHour, "Btu/h", 0);

                case UnitCategory.SpecificPower:
                    return si ? new DisplayUnit(UnitType.WattPerSquareMeter, "W/m²", 1) : new DisplayUnit(UnitType.BtuPerHourSquareFoot, "Btu/h·ft²", 2);

                case UnitCategory.PowerPerPerson:
                    return si ? new DisplayUnit(UnitType.WattPerPerson, "W/person", 0) : new DisplayUnit(UnitType.BtuPerHourPerPerson, "Btu/h/person", 0);

                case UnitCategory.AreaPerPerson:
                    return si ? new DisplayUnit(UnitType.SquareMeterPerPerson, "m²/person", 1) : new DisplayUnit(UnitType.SquareFootPerPerson, "ft²/person", 0);

                case UnitCategory.AirChangeRate:
                    return new DisplayUnit(UnitType.AirChangesPerHour, "ac/h", 2);

                case UnitCategory.RelativeHumidity:
                case UnitCategory.Ratio:
                case UnitCategory.Efficiency:
                    return new DisplayUnit(UnitType.Percent, "%", 0);

                case UnitCategory.HumidityRatio:
                    return new DisplayUnit(UnitType.GramPerKilogram, "g/kg", 1);

                case UnitCategory.Illuminance:
                    return si ? new DisplayUnit(UnitType.Lux, "lx", 0) : new DisplayUnit(UnitType.FootCandle, "fc", 1);

                case UnitCategory.ThermalTransmittance:
                    return si ? new DisplayUnit(UnitType.WattPerSquareMeterKelvin, "W/m²K", 2) : new DisplayUnit(UnitType.BtuPerHourSquareFootFahrenheit, "Btu/h·ft²·°F", 3);

                case UnitCategory.Count:
                    return new DisplayUnit(UnitType.Person, "persons", 1);

                case UnitCategory.Time:
                    return new DisplayUnit(UnitType.Hour, "h", 0);

                case UnitCategory.Angle:
                    return new DisplayUnit(UnitType.Degree, "°", 1);

                case UnitCategory.Pressure:
                    return si ? new DisplayUnit(UnitType.Pascal, "Pa", 0) : new DisplayUnit(UnitType.PoundPerSquareInch, "psi", 3);

                // Energy and mass have one unit system in SAM, so both styles show kWh and kg.
                case UnitCategory.Energy:
                    return new DisplayUnit(UnitType.KilowattHour, "kWh", 1);

                case UnitCategory.Mass:
                    return new DisplayUnit(UnitType.Kilogram, "kg", 1);
            }

            UnitType unitType = UnitSystem.UnitType(unitCategory);
            return new DisplayUnit(unitType, unitType == UnitType.Undefined ? null : unitType.Abbreviation(), 2);
        }

        public DisplayUnit SelectDisplayUnit(UnitCategory unitCategory, IEnumerable<Quantity> quantities)
        {
            DisplayUnit result = DefaultDisplayUnit(unitCategory);
            if (quantities != null && TryGetLargerDisplayUnit(unitCategory, out double threshold, out DisplayUnit displayUnit_Larger))
            {
                double max = 0;
                foreach (Quantity quantity in quantities)
                {
                    if (!quantity.IsValid || quantity.Category != unitCategory)
                    {
                        continue;
                    }

                    double value = System.Math.Abs(quantity.ConvertTo(result.UnitType).Value);
                    if (value > max)
                    {
                        max = value;
                    }
                }

                if (max >= threshold)
                {
                    result = displayUnit_Larger;
                }
            }

            return WithDecimals(unitCategory, result);
        }

        /// <summary>
        /// The larger unit a group of a category switches to, and the threshold (in the category's default display
        /// unit) at which its largest value makes the switch: W to kW, Btu/h to kBtu/h, kWh to MWh, kg to t.
        /// </summary>
        private bool TryGetLargerDisplayUnit(UnitCategory unitCategory, out double threshold, out DisplayUnit displayUnit)
        {
            switch (unitCategory)
            {
                case UnitCategory.Power:
                    if (UnitSystem == UnitStyle.SI)
                    {
                        threshold = KilowattThreshold_W;
                        displayUnit = new DisplayUnit(UnitType.Kilowatt, "kW", 2);
                    }
                    else
                    {
                        threshold = KiloBtuPerHourThreshold_BtuPerHour;
                        displayUnit = new DisplayUnit(UnitType.KiloBtuPerHour, "kBtu/h", 1);
                    }

                    return true;

                case UnitCategory.Energy:
                    threshold = MegawattHourThreshold_kWh;
                    displayUnit = new DisplayUnit(UnitType.MegawattHour, "MWh", 2);
                    return true;

                case UnitCategory.Mass:
                    threshold = TonneThreshold_kg;
                    displayUnit = new DisplayUnit(UnitType.Tonne, "t", 2);
                    return true;
            }

            threshold = double.NaN;
            displayUnit = null;
            return false;
        }

        private DisplayUnit WithDecimals(UnitCategory unitCategory, DisplayUnit displayUnit)
        {
            if (displayUnit == null || !decimalsByCategory.TryGetValue(unitCategory, out int value) || value == displayUnit.Decimals)
            {
                return displayUnit;
            }

            return new DisplayUnit(displayUnit.UnitType, displayUnit.Symbol, value);
        }

        public FormattedValue Format(ReportValue<Quantity> reportValue, DisplayUnit displayUnit)
        {
            if (reportValue == null)
            {
                throw new ArgumentNullException(nameof(reportValue));
            }

            if (!reportValue.TryGetValue(out Quantity quantity))
            {
                return Placeholder(reportValue);
            }

            if (displayUnit == null)
            {
                throw new ArgumentNullException(nameof(displayUnit));
            }

            double value = quantity.ConvertTo(displayUnit.UnitType).Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new InvalidOperationException(string.Format("Cannot display {0} in {1}.", quantity, displayUnit));
            }

            return new FormattedValue(FormatNumber(value, displayUnit.Decimals), displayUnit.Symbol, reportValue.Availability, reportValue.Freshness, reportValue.Source, reportValue.SourceTimestamp, reportValue.Note);
        }

        public FormattedValue Format(ReportValue<Quantity> reportValue)
        {
            if (reportValue == null)
            {
                throw new ArgumentNullException(nameof(reportValue));
            }

            if (!reportValue.TryGetValue(out Quantity quantity))
            {
                return Placeholder(reportValue);
            }

            return Format(reportValue, SelectDisplayUnit(quantity.Category, new[] { quantity }));
        }

        public FormattedValue Format(ReportValue<string> reportValue)
        {
            if (reportValue == null)
            {
                throw new ArgumentNullException(nameof(reportValue));
            }

            if (!reportValue.TryGetValue(out string text))
            {
                return Placeholder(reportValue);
            }

            return new FormattedValue(text, null, reportValue.Availability, reportValue.Freshness, reportValue.Source, reportValue.SourceTimestamp, reportValue.Note);
        }

        public FormattedValue Format(ReportValue<DateTime> reportValue)
        {
            if (reportValue == null)
            {
                throw new ArgumentNullException(nameof(reportValue));
            }

            if (!reportValue.TryGetValue(out DateTime dateTime))
            {
                return Placeholder(reportValue);
            }

            return new FormattedValue(FormatDateTime(dateTime), null, reportValue.Availability, reportValue.Freshness, reportValue.Source, reportValue.SourceTimestamp, reportValue.Note);
        }

        /// <summary>
        /// Formats a number with a fixed number of decimals and the culture's separators. A value that rounds to zero
        /// prints as zero, never as "-0.0". NaN and infinity print as <see cref="NotAvailableText"/>.
        /// </summary>
        /// <param name="value">The value, unchanged by the caller: rounding happens here, for display only.</param>
        /// <param name="decimals">Decimals, 0 to 15.</param>
        public string FormatNumber(double value, int decimals)
        {
            if (decimals < 0 || decimals > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "Decimals must be between 0 and 15.");
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return NotAvailableText;
            }

            double rounded = System.Math.Round(value, decimals, MidpointRounding.AwayFromZero);
            if (rounded == 0)
            {
                rounded = 0;
            }

            return rounded.ToString("N" + decimals.ToString(CultureInfo.InvariantCulture), cultureInfo);
        }

        /// <summary>
        /// Formats a value that has no known unit to <paramref name="significantFigures"/> significant figures, with the
        /// culture's separators: 7360.04370117188 is "7,360" and 4.968943799848584 is "4.969" at 4 figures. A value
        /// whose magnitude is below <see cref="ScientificThreshold"/> (and not zero) is shown in scientific notation;
        /// NaN and infinity print as <see cref="NotAvailableText"/>. Values in a column should share their decimals:
        /// use <see cref="DecimalsForSignificantFigures"/> and <see cref="FormatNumber"/> instead.
        /// </summary>
        public string FormatSignificant(double value, int significantFigures = 4)
        {
            if (significantFigures < 1 || significantFigures > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(significantFigures), significantFigures, "Significant figures must be between 1 and 15.");
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return NotAvailableText;
            }

            if (value != 0 && System.Math.Abs(value) < ScientificThreshold)
            {
                string format = significantFigures == 1 ? "0E+0" : "0." + new string('0', significantFigures - 1) + "E+0";
                return value.ToString(format, cultureInfo);
            }

            return FormatNumber(value, DecimalsForSignificantFigures(new[] { value }, significantFigures));
        }

        /// <summary>
        /// The decimals that show the group's largest magnitude to <paramref name="significantFigures"/> significant
        /// figures, so that comparable values (a table column) share one precision. NaN and infinity are ignored; an
        /// empty or all-zero group gives 0. The result is between 0 and <see cref="MaximumDecimals"/>.
        /// </summary>
        public static int DecimalsForSignificantFigures(IEnumerable<double> values, int significantFigures = 4)
        {
            if (significantFigures < 1 || significantFigures > 15)
            {
                throw new ArgumentOutOfRangeException(nameof(significantFigures), significantFigures, "Significant figures must be between 1 and 15.");
            }

            double max = 0;
            if (values != null)
            {
                foreach (double value in values)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        continue;
                    }

                    double absolute = System.Math.Abs(value);
                    if (absolute > max)
                    {
                        max = absolute;
                    }
                }
            }

            if (max == 0)
            {
                return 0;
            }

            int result = significantFigures - 1 - (int)System.Math.Floor(System.Math.Log10(max));
            return System.Math.Max(0, System.Math.Min(MaximumDecimals, result));
        }

        /// <summary>
        /// Formats a date and time as "25 Sep 2026 14:02". Month names are invariant (English) on purpose: culture
        /// month abbreviations come from ICU data that differs between machines, which would make documents and their
        /// snapshots machine-dependent.
        /// </summary>
        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        private FormattedValue Placeholder(IReportValue reportValue)
        {
            string text = reportValue.Availability == Availability.NotApplicable ? NotApplicableText : NotAvailableText;
            return new FormattedValue(text, null, reportValue.Availability, note: reportValue.Note);
        }
    }
}
