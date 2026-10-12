// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// The units <paramref name="analyticalModelSimulationResult"/> holds its consumptions and peak loads in.
        /// <list type="bullet">
        /// <item>A <see cref="AnalyticalModelSimulationResultParameter.ResultUnitsVersion"/> marker decides: version 1 is
        /// <see cref="ResultUnitsState.Marked"/> (kWh, kW), whatever the producer; any other value is
        /// <see cref="ResultUnitsState.UnsupportedVersion"/>.</item>
        /// <item>Without a marker the producer decides, by an exact match of <see cref="Core.Result.Source"/>:
        /// <see cref="AnalyticalModelSimulationResult.TasSource"/> is <see cref="ResultUnitsState.LegacyTas"/> (Wh, W),
        /// <see cref="AnalyticalModelSimulationResult.OpenStudioSource"/> is <see cref="ResultUnitsState.LegacyOpenStudio"/>
        /// (kWh, kW), and anything else is <see cref="ResultUnitsState.UnknownProducer"/>.</item>
        /// </list>
        /// The size of a value never decides.
        /// </summary>
        public static ResultUnitsState ResultUnitsState(this AnalyticalModelSimulationResult analyticalModelSimulationResult)
        {
            if (analyticalModelSimulationResult == null)
            {
                return Analytical.ResultUnitsState.Undefined;
            }

            if (analyticalModelSimulationResult.TryGetValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion, out object value))
            {
                return TryGetWholeNumber(value, out long version) && version == AnalyticalModelSimulationResult.CurrentResultUnitsVersion
                    ? Analytical.ResultUnitsState.Marked
                    : Analytical.ResultUnitsState.UnsupportedVersion;
            }

            string source = analyticalModelSimulationResult.Source;
            if (string.Equals(source, AnalyticalModelSimulationResult.TasSource, StringComparison.Ordinal))
            {
                return Analytical.ResultUnitsState.LegacyTas;
            }

            if (string.Equals(source, AnalyticalModelSimulationResult.OpenStudioSource, StringComparison.Ordinal))
            {
                return Analytical.ResultUnitsState.LegacyOpenStudio;
            }

            return Analytical.ResultUnitsState.UnknownProducer;
        }

        /// <summary>
        /// Reads a consumption in kWh (<see cref="AnalyticalModelSimulationResultParameter.ConsumptionHeating"/>,
        /// <see cref="AnalyticalModelSimulationResultParameter.ConsumptionCooling"/>) or a peak load in kW
        /// (<see cref="AnalyticalModelSimulationResultParameter.PeakHeatingLoad"/>,
        /// <see cref="AnalyticalModelSimulationResultParameter.PeakCoolingLoad"/>), whichever units the result was written in:
        /// a <see cref="ResultUnitsState.LegacyTas"/> value is divided by 1000 here, exactly once; a
        /// <see cref="ResultUnitsState.Marked"/> or <see cref="ResultUnitsState.LegacyOpenStudio"/> value is returned as
        /// stored. Callers must not convert the value again.
        /// <para>
        /// Returns false, with <paramref name="value"/> NaN, when the result is null, the parameter is not one of the four,
        /// the value is missing, or the units are not known (<see cref="ResultUnitsState.UnsupportedVersion"/>,
        /// <see cref="ResultUnitsState.UnknownProducer"/>). Nothing is guessed and the result is not changed.
        /// </para>
        /// </summary>
        public static bool TryGetConsumptionOrPeakLoad(this AnalyticalModelSimulationResult analyticalModelSimulationResult, AnalyticalModelSimulationResultParameter analyticalModelSimulationResultParameter, out double value)
        {
            value = double.NaN;

            if (analyticalModelSimulationResult == null || !IsConsumptionOrPeakLoad(analyticalModelSimulationResultParameter))
            {
                return false;
            }

            double factor;
            switch (ResultUnitsState(analyticalModelSimulationResult))
            {
                case Analytical.ResultUnitsState.Marked:
                case Analytical.ResultUnitsState.LegacyOpenStudio:
                    factor = 1;
                    break;

                case Analytical.ResultUnitsState.LegacyTas:
                    factor = 1000;
                    break;

                default:
                    return false;
            }

            if (!analyticalModelSimulationResult.TryGetValue(analyticalModelSimulationResultParameter, out double stored) || double.IsNaN(stored))
            {
                return false;
            }

            value = stored / factor;
            return true;
        }

        /// <summary>
        /// <see cref="TryGetConsumptionOrPeakLoad"/> as a value: the consumption in kWh or the peak load in kW, or NaN when it
        /// cannot be read.
        /// </summary>
        public static double ConsumptionOrPeakLoad(this AnalyticalModelSimulationResult analyticalModelSimulationResult, AnalyticalModelSimulationResultParameter analyticalModelSimulationResultParameter)
        {
            return TryGetConsumptionOrPeakLoad(analyticalModelSimulationResult, analyticalModelSimulationResultParameter, out double value) ? value : double.NaN;
        }

        private static bool IsConsumptionOrPeakLoad(AnalyticalModelSimulationResultParameter analyticalModelSimulationResultParameter)
        {
            switch (analyticalModelSimulationResultParameter)
            {
                case AnalyticalModelSimulationResultParameter.ConsumptionHeating:
                case AnalyticalModelSimulationResultParameter.ConsumptionCooling:
                case AnalyticalModelSimulationResultParameter.PeakHeatingLoad:
                case AnalyticalModelSimulationResultParameter.PeakCoolingLoad:
                    return true;

                default:
                    return false;
            }
        }

        // A marker read back from JSON or set by hand may be any number type or text; only an exact whole number counts.
        private static bool TryGetWholeNumber(object value, out long result)
        {
            result = 0;

            switch (value)
            {
                case int @int:
                    result = @int;
                    return true;

                case long @long:
                    result = @long;
                    return true;

                case short @short:
                    result = @short;
                    return true;

                case double @double:
                    if (double.IsNaN(@double) || double.IsInfinity(@double) || @double != System.Math.Truncate(@double) || System.Math.Abs(@double) > long.MaxValue)
                    {
                        return false;
                    }

                    result = (long)@double;
                    return true;

                case string @string:
                    return long.TryParse(@string.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);

                default:
                    return false;
            }
        }
    }
}
