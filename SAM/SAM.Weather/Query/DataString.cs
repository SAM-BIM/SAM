// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SAM.Weather
{
    public static partial class Query
    {
        /// <summary>
        /// Generates a string representation of the given WeatherData object.
        /// </summary>
        /// <param name="weatherData">The WeatherData object to generate a string representation of.</param>
        /// <returns>A string representation of the given WeatherData object.</returns>
        public static string DataString(this WeatherData weatherData)
        {
            if (weatherData == null)
                return null;

            IEnumerable<int> years = weatherData?.Years;
            if (years == null)
                return null;

            double timeZone = weatherData.EPWTimeZone();

            List<string> values = new List<string>();
            foreach (int year in years)
                values.Add(DataString(year, weatherData[year], weatherData.Elevtion, weatherData.Latitude, weatherData.Longitude, timeZone));

            return string.Join("\n", values);
        }

        /// <summary>
        /// Standard-atmosphere pressure [Pa] at the given elevation (ISA troposphere lapse,
        /// valid 0–11000 m); NaN elevation falls back to sea level (101325 Pa).
        /// </summary>
        private static double StandardAtmosphericPressure(double elevation)
        {
            if (double.IsNaN(elevation) || elevation <= 0)
                return 101325.0;

            if (elevation > 11000)
                elevation = 11000;

            return 101325.0 * Math.Pow(1.0 - 2.25577e-5 * elevation, 5.25588);
        }

        /// <summary>
        /// Generates a string of data for a given year and WeatherYear object.
        /// Without a location the direct-normal radiation cannot be derived from the horizontal
        /// components and is written as missing; use the overload with location to derive it.
        /// </summary>
        /// <param name="year">The year to generate data for.</param>
        /// <param name="weatherYear">The WeatherYear object to generate data from.</param>
        /// <returns>A string of data for the given year and WeatherYear object.</returns>
        public static string DataString(int year, WeatherYear weatherYear, double elevation = double.NaN)
        {
            return DataString(year, weatherYear, elevation, double.NaN, double.NaN, double.NaN);
        }

        /// <summary>
        /// Generates a string of data for a given year and WeatherYear object.
        /// </summary>
        /// <param name="year">The year to generate data for.</param>
        /// <param name="weatherYear">The WeatherYear object to generate data from.</param>
        /// <param name="elevation">Site elevation [m] used for the standard-atmosphere pressure fallback; NaN falls back to sea level.</param>
        /// <param name="latitude">Site latitude [degrees], north positive.</param>
        /// <param name="longitude">Site longitude [degrees], east positive.</param>
        /// <param name="timeZone">Standard time zone [hours from UTC], east positive.</param>
        /// <returns>A string of data for the given year and WeatherYear object.</returns>
        public static string DataString(int year, WeatherYear weatherYear, double elevation, double latitude, double longitude, double timeZone)
        {
            if (weatherYear == null)
                return null;

            string[] values = Enumerable.Repeat(string.Empty, 365).ToArray();
            for (int i = 0; i < 365; i++)
            {
                WeatherDay weatherDay = weatherYear[i];
                if (weatherDay == null)
                    continue;

                DateTime dateTime = new DateTime(year, 1, 1);
                dateTime = dateTime.AddDays(i);

                values[i] = DataString(year, dateTime.Month, dateTime.Day, weatherDay, elevation, latitude, longitude, timeZone);
            }

            return string.Join("\n", values);
        }

        /// <summary>
        /// Generates a string of data for a given day and weatherDay.
        /// Without a location the direct-normal radiation is written as missing.
        /// </summary>
        /// <param name="year">The year of the data.</param>
        /// <param name="month">The month of the data.</param>
        /// <param name="day">The day of the data.</param>
        /// <param name="weatherDay">The weatherDay of the data.</param>
        /// <returns>A string of data for the given day and weatherDay.</returns>
        public static string DataString(int year, int month, int day, WeatherDay weatherDay, double elevation = double.NaN)
        {
            return DataString(year, month, day, weatherDay, elevation, double.NaN, double.NaN, double.NaN);
        }

        /// <summary>
        /// Generates a string of data for a given day and weatherDay.
        /// </summary>
        /// <param name="year">The year of the data.</param>
        /// <param name="month">The month of the data.</param>
        /// <param name="day">The day of the data.</param>
        /// <param name="weatherDay">The weatherDay of the data.</param>
        /// <param name="elevation">Site elevation [m] used for the standard-atmosphere pressure fallback; NaN falls back to sea level.</param>
        /// <param name="latitude">Site latitude [degrees], north positive.</param>
        /// <param name="longitude">Site longitude [degrees], east positive.</param>
        /// <param name="timeZone">Standard time zone [hours from UTC], east positive.</param>
        /// <returns>A string of data for the given day and weatherDay.</returns>
        public static string DataString(int year, int month, int day, WeatherDay weatherDay, double elevation, double latitude, double longitude, double timeZone)
        {
            if (weatherDay == null)
                return null;

            List<string> values = new List<string>();
            for (int i = 0; i < 24; i++)
            {
                string value = DataString(year, month, day, i, weatherDay, elevation, latitude, longitude, timeZone);
                values.Add(value == null ? string.Empty : value);
            }

            return string.Join("\n", values);
        }

        /// <summary>
        /// Generates a data string from the given year, month, day, hour, and WeatherDay object.
        /// Without a location the direct-normal radiation is written as missing.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="weatherDay">The WeatherDay object.</param>
        /// <param name="elevation">Site elevation [m] used for the standard-atmosphere pressure fallback; NaN falls back to sea level.</param>
        /// <returns>A data string.</returns>
        public static string DataString(int year, int month, int day, int hour, WeatherDay weatherDay, double elevation = double.NaN)
        {
            return DataString(year, month, day, hour, weatherDay, elevation, double.NaN, double.NaN, double.NaN);
        }

        /// <summary>
        /// Generates the EPW data record for one hour.
        /// <para>
        /// Radiation: SAM stores horizontal components (global = direct + diffuse on the horizontal,
        /// see <see cref="WeatherHour.CalculatedGlobalSolarRadiation"/>). The record carries global
        /// horizontal, direct normal and diffuse horizontal radiation; direct normal is the horizontal
        /// beam divided by the hour-mean cosine of the solar zenith at the site (EPW hour = the hour
        /// ending at <c>hour + 1</c>, local standard time), so it needs latitude, longitude and time
        /// zone. It never exceeds what a pure Rayleigh atmosphere transmits, which only matters with the
        /// sun near the horizon, where global - diffuse is ill-conditioned; the stated global and
        /// diffuse are written unchanged. The extraterrestrial fields are astronomical. A component that
        /// is neither stated nor derivable is written as the EPW missing value 9999, never as 0.
        /// </para>
        /// <para>
        /// Sky: SAM does not hold horizontal infrared radiation, so it is written as missing (9999);
        /// EnergyPlus then derives it from dry-bulb, dew-point and opaque sky cover. SAM cloud cover
        /// [0-1] is written in tenths to total and opaque sky cover (SAM holds one cloud amount; the
        /// opaque value is that amount, not a separate observation). A dew point that is not stated is
        /// derived from dry-bulb and relative humidity.
        /// </para>
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="hour">The hour.</param>
        /// <param name="weatherDay">The WeatherDay object.</param>
        /// <param name="elevation">Site elevation [m] used for the standard-atmosphere pressure fallback; NaN falls back to sea level.</param>
        /// <param name="latitude">Site latitude [degrees], north positive.</param>
        /// <param name="longitude">Site longitude [degrees], east positive.</param>
        /// <param name="timeZone">Standard time zone [hours from UTC], east positive.</param>
        /// <returns>A data string.</returns>
        public static string DataString(int year, int month, int day, int hour, WeatherDay weatherDay, double elevation, double latitude, double longitude, double timeZone)
        {
            if (weatherDay == null)
                return null;

            List<string> values = new List<string>();

            double dryBulbTemperature = weatherDay[WeatherDataType.DryBulbTemperature, hour];
            double wetBulbTemperature = weatherDay[WeatherDataType.WetBulbTemperature, hour];
            double relativeHumidity = weatherDay[WeatherDataType.RelativeHumidity, hour];
            double atmosphericPressure = weatherDay[WeatherDataType.AtmosphericPressure, hour];

            // EPW field 8 is the dew point; SAM keeps the EPW dew point in WetBulbTemperature
            // (see TryGetData). Sources without it (TAS weather) get it derived from DBT and RH,
            // because EnergyPlus uses it for the sky emissivity when the infrared field is missing.
            double dewPointTemperature = wetBulbTemperature;
            if (double.IsNaN(dewPointTemperature))
                dewPointTemperature = EPWDewPointTemperature(dryBulbTemperature, relativeHumidity);

            double globalSolarRadiation = weatherDay[WeatherDataType.GlobalSolarRadiation, hour];
            double directSolarRadiation = weatherDay[WeatherDataType.DirectSolarRadiation, hour];
            double diffuseSolarRadiation = weatherDay[WeatherDataType.DiffuseSolarRadiation, hour];

            // Complete the horizontal triple from global = direct + diffuse; stated values win.
            if (double.IsNaN(directSolarRadiation) && !double.IsNaN(globalSolarRadiation) && !double.IsNaN(diffuseSolarRadiation))
                directSolarRadiation = globalSolarRadiation - diffuseSolarRadiation;

            if (double.IsNaN(diffuseSolarRadiation) && !double.IsNaN(globalSolarRadiation) && !double.IsNaN(directSolarRadiation))
                diffuseSolarRadiation = globalSolarRadiation - directSolarRadiation;

            if (double.IsNaN(globalSolarRadiation) && !double.IsNaN(directSolarRadiation) && !double.IsNaN(diffuseSolarRadiation))
                globalSolarRadiation = directSolarRadiation + diffuseSolarRadiation;

            double extraterrestrialHorizontalRadiation = double.NaN;
            double extraterrestrialDirectNormalRadiation = double.NaN;
            double directNormalRadiation = double.NaN;

            double pressure = double.IsNaN(atmosphericPressure) ? StandardAtmosphericPressure(elevation) : atmosphericPressure;

            DateTime date = new DateTime(year, month, day);
            if (TryGetEPWSolarGeometry(date.DayOfYear, DateTime.IsLeapYear(year) ? 366 : 365, hour, latitude, longitude, timeZone, pressure, out double meanCosZenith, out double extraterrestrialNormalIrradiance, out double limitDirectNormalIrradiance))
            {
                extraterrestrialHorizontalRadiation = extraterrestrialNormalIrradiance * meanCosZenith;
                extraterrestrialDirectNormalRadiation = meanCosZenith > 0 ? extraterrestrialNormalIrradiance : 0;
                directNormalRadiation = EPWDirectNormalRadiation(directSolarRadiation, meanCosZenith, limitDirectNormalIrradiance);
            }

            double windDirection = weatherDay[WeatherDataType.WindDirection, hour];
            double windSpeed = weatherDay[WeatherDataType.WindSpeed, hour];
            double cloudCover = weatherDay[WeatherDataType.CloudCover, hour];

            // EPW sky cover is in tenths (0-10); SAM cloud cover is 0-1 (TryGetData divides by 10).
            double skyCover = double.IsNaN(cloudCover) ? double.NaN : Math.Round(Math.Max(0, Math.Min(1, cloudCover)) * 10.0, 6);

            values.Add(year.ToString()); //Year
            values.Add(month.ToString()); //Month
            values.Add(day.ToString()); //Day
            values.Add((hour + 1).ToString()); // Hour
            values.Add(0.ToString()); //Minute
            values.Add("C9C9C9C9*0?9?9?9?9?9?9?9A7A7A7A7A7A7*0E8*0*0"); //Flags
            values.Add(double.IsNaN(dryBulbTemperature) ? 0.ToString() : dryBulbTemperature.ToString()); //Dry Bulb Temperature
            values.Add(EPWValue(dewPointTemperature, "99.9")); //Dew Point Temperature
            values.Add(double.IsNaN(relativeHumidity) ? 0.ToString() : relativeHumidity.ToString()); //Relative Humidity
            // EnergyPlus rejects station pressure outside [31000, 120000] Pa with a severe
            // ReadEPlusWeatherForDay error — a missing value must never be written as 0.
            // Fall back to the standard-atmosphere pressure at the site elevation (invariant
            // culture: a decimal-comma locale would corrupt the comma-separated row).
            values.Add(double.IsNaN(atmosphericPressure) ? StandardAtmosphericPressure(elevation).ToString(CultureInfo.InvariantCulture) : atmosphericPressure.ToString()); //Atmospheric Pressure
            values.Add(EPWValue(extraterrestrialHorizontalRadiation, "9999")); //Extraterrestrial Horizontal Radiation
            values.Add(EPWValue(extraterrestrialDirectNormalRadiation, "9999")); //Extraterrestrial Direct Normal Radiation
            values.Add("9999"); //Horizontal Infrared Radiation Intensity - not held by SAM; EnergyPlus derives it
            values.Add(EPWRadiationValue(globalSolarRadiation)); //Global Horizontal Radiation
            values.Add(EPWRadiationValue(directNormalRadiation)); //Direct Normal Radiation
            values.Add(EPWRadiationValue(diffuseSolarRadiation)); //Diffuse Horizontal Radiation
            values.Add(string.Join(",", Enumerable.Repeat(0.ToString(), 4))); // Illumination
            values.Add(double.IsNaN(windDirection) ? 0.ToString() : windDirection.ToString()); //Wind Direction
            values.Add(double.IsNaN(windSpeed) ? 0.ToString() : windSpeed.ToString()); //Wind Speed
            values.Add(EPWValue(skyCover, "99")); //Total Sky Cover
            values.Add(EPWValue(skyCover, "99")); //Opaque Sky Cover
            values.Add(string.Join(",", Enumerable.Repeat(0.ToString(), 8))); // Visibility, Ceiling, Present Weather, Precipitable Water, Aerosol, Snow

            return string.Join(",", values);
        }

        /// <summary>
        /// EPW text of a value (invariant culture, at most 2 decimals) or the missing code when NaN.
        /// </summary>
        private static string EPWValue(double value, string missing)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return missing;

            return Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// EPW text of a radiation value [Wh/m2]: negative values (global - diffuse rounding) are 0,
        /// NaN is the missing code 9999.
        /// </summary>
        private static string EPWRadiationValue(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "9999";

            return EPWValue(Math.Max(0, value), "9999");
        }
    }
}
