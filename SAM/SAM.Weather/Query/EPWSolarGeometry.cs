// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Weather
{
    public static partial class Query
    {
        /// <summary>
        /// Solar constant [W/m2] used for the extraterrestrial irradiance of the EPW record
        /// (the value the EnergyPlus weather converter and the TMY2/TMY3 EPW sources use).
        /// </summary>
        private const double EPWSolarConstant = 1367.0;

        /// <summary>
        /// Number of equal sub-intervals used to average the solar geometry over one EPW hour.
        /// </summary>
        private const int EPWSolarSubIntervals = 60;

        /// <summary>
        /// Hour-averaged solar geometry of one EPW record, used to convert SAM horizontal beam
        /// radiation into EPW direct-normal radiation.
        /// <para>
        /// An EPW hour <c>h</c> (1-24) holds the radiation received during the hour that ends at
        /// <c>h:00</c> local standard time (EnergyPlus Auxiliary Programs, EPW data dictionary:
        /// "during the number of minutes preceding the time indicated"); SAM stores it at hour index
        /// <c>h - 1</c> (see <see cref="TryGetData"/>). The interval is therefore
        /// <c>[hour, hour + 1)</c> local standard time for SAM <paramref name="hour"/>.
        /// </para>
        /// <para>
        /// <paramref name="meanCosZenith"/> is the mean over that interval of max(0, cos zenith), so a
        /// constant direct-normal irradiance <c>DNI</c> over the hour puts <c>DNI * meanCosZenith</c>
        /// on the horizontal - the energy relation the conversion inverts. Sun position: NOAA
        /// general solar position equations (Spencer 1971 Fourier series for declination and
        /// equation of time), no refraction; accuracy is a few arc-minutes, well below the
        /// sub-hour averaging.
        /// </para>
        /// <para>
        /// <paramref name="limitDirectNormalIrradiance"/> is the largest direct-normal irradiance
        /// that can reach the ground in the hour: the beam of a pure Rayleigh (aerosol-, water- and
        /// ozone-free) atmosphere, <c>I0 exp(-dR(m') m')</c>, with the Kasten and Young (1989)
        /// relative air mass <c>m</c>, <c>m' = m p / 101325</c> and the Kasten (1996) integral Rayleigh
        /// optical thickness <c>dR</c>, averaged over the hour with cos-zenith weights (the constant
        /// direct normal that would give that beam on the horizontal). A real atmosphere only
        /// attenuates more, so the value is a physical bound, not a clear-sky model.
        /// </para>
        /// </summary>
        /// <param name="dayOfYear">Day of year, 1-based.</param>
        /// <param name="daysInYear">Days in the calendar year of the record (365 or 366).</param>
        /// <param name="hour">SAM hour index 0-23 (EPW hour minus one).</param>
        /// <param name="latitude">Latitude [degrees], north positive.</param>
        /// <param name="longitude">Longitude [degrees], east positive.</param>
        /// <param name="timeZone">Standard time zone [hours from UTC], east positive (EPW LOCATION convention).</param>
        /// <param name="pressure">Station pressure [Pa] for the air-mass correction; NaN uses 101325 Pa.</param>
        /// <param name="meanCosZenith">Hour mean of max(0, cos zenith) [-].</param>
        /// <param name="extraterrestrialNormalIrradiance">Extraterrestrial direct-normal irradiance for the day [W/m2].</param>
        /// <param name="limitDirectNormalIrradiance">Rayleigh-atmosphere bound on the hour's direct normal [W/m2]; 0 when the sun is down all hour.</param>
        /// <returns>False when the location or time zone is not stated (NaN) or out of range.</returns>
        private static bool TryGetEPWSolarGeometry(int dayOfYear, int daysInYear, int hour, double latitude, double longitude, double timeZone, double pressure, out double meanCosZenith, out double extraterrestrialNormalIrradiance, out double limitDirectNormalIrradiance)
        {
            meanCosZenith = double.NaN;
            extraterrestrialNormalIrradiance = double.NaN;
            limitDirectNormalIrradiance = double.NaN;

            if (double.IsNaN(latitude) || double.IsNaN(longitude) || double.IsNaN(timeZone))
                return false;

            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180 || timeZone < -14 || timeZone > 14)
                return false;

            if (hour < 0 || hour > 23 || dayOfYear < 1 || dayOfYear > daysInYear)
                return false;

            double pressureRatio = double.IsNaN(pressure) || pressure <= 0 ? 1.0 : pressure / 101325.0;

            double dayAngle = 2.0 * Math.PI * (dayOfYear - 1) / daysInYear;
            extraterrestrialNormalIrradiance = EPWSolarConstant * (1.000110 + 0.034221 * Math.Cos(dayAngle) + 0.001280 * Math.Sin(dayAngle) + 0.000719 * Math.Cos(2 * dayAngle) + 0.000077 * Math.Sin(2 * dayAngle));

            double latitudeRadians = latitude * Math.PI / 180.0;
            double sumCosZenith = 0;
            double sumLimitBeam = 0;
            for (int i = 0; i < EPWSolarSubIntervals; i++)
            {
                // Mid-point of the sub-interval, in hours of local standard time.
                double time = hour + (i + 0.5) / EPWSolarSubIntervals;

                double gamma = 2.0 * Math.PI / daysInYear * (dayOfYear - 1 + (time - 12.0) / 24.0);

                double equationOfTime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma) - 0.014615 * Math.Cos(2 * gamma) - 0.040849 * Math.Sin(2 * gamma));

                double declination = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma) - 0.006758 * Math.Cos(2 * gamma) + 0.000907 * Math.Sin(2 * gamma) - 0.002697 * Math.Cos(3 * gamma) + 0.00148 * Math.Sin(3 * gamma);

                // True solar time [min]: standard meridian is 15 degrees per hour of time zone.
                double trueSolarTime = time * 60.0 + equationOfTime + 4.0 * longitude - 60.0 * timeZone;
                double hourAngle = (trueSolarTime / 4.0 - 180.0) * Math.PI / 180.0;

                double cosZenith = Math.Sin(latitudeRadians) * Math.Sin(declination) + Math.Cos(latitudeRadians) * Math.Cos(declination) * Math.Cos(hourAngle);
                if (cosZenith <= 0)
                    continue;

                sumCosZenith += cosZenith;

                double airMass = RelativeAirMass(cosZenith) * pressureRatio;
                sumLimitBeam += extraterrestrialNormalIrradiance * Math.Exp(-RayleighOpticalThickness(airMass) * airMass) * cosZenith;
            }

            meanCosZenith = sumCosZenith / EPWSolarSubIntervals;
            limitDirectNormalIrradiance = sumCosZenith > 0 ? sumLimitBeam / sumCosZenith : 0;
            return true;
        }

        /// <summary>
        /// Relative optical air mass, Kasten and Young (1989), from the cosine of the solar zenith
        /// (sun above the horizon).
        /// </summary>
        private static double RelativeAirMass(double cosZenith)
        {
            double zenith = Math.Acos(Math.Min(1.0, cosZenith)) * 180.0 / Math.PI;
            return 1.0 / (cosZenith + 0.50572 * Math.Pow(96.07995 - zenith, -1.6364));
        }

        /// <summary>
        /// Integral Rayleigh optical thickness, Kasten (1996), for the pressure-corrected air mass.
        /// </summary>
        private static double RayleighOpticalThickness(double airMass)
        {
            if (airMass <= 20)
                return 1.0 / (6.6296 + 1.7513 * airMass - 0.1202 * Math.Pow(airMass, 2) + 0.0065 * Math.Pow(airMass, 3) - 0.00013 * Math.Pow(airMass, 4));

            return 1.0 / (10.4 + 0.718 * airMass);
        }

        /// <summary>
        /// Direct-normal radiation [Wh/m2] for one EPW hour from the horizontal beam (global minus
        /// diffuse) and the hour-averaged solar geometry: <c>DNI = beam / meanCosZenith</c>, limited
        /// to the Rayleigh-atmosphere bound of the hour. Horizontal beam in an hour where the sun is
        /// below the horizon for the whole interval cannot be represented as a direct-normal value
        /// and gives 0. NaN when the horizontal beam or the geometry is not known (written as the EPW
        /// missing value).
        /// </summary>
        private static double EPWDirectNormalRadiation(double horizontalBeamRadiation, double meanCosZenith, double limitDirectNormalIrradiance)
        {
            if (double.IsNaN(horizontalBeamRadiation) || double.IsNaN(meanCosZenith))
                return double.NaN;

            if (horizontalBeamRadiation <= 0 || meanCosZenith <= 0)
                return 0;

            double result = horizontalBeamRadiation / meanCosZenith;
            if (!double.IsNaN(limitDirectNormalIrradiance) && result > limitDirectNormalIrradiance)
                result = limitDirectNormalIrradiance;

            return result;
        }

        /// <summary>
        /// Dew-point temperature [C] from dry-bulb temperature [C] and relative humidity [%]
        /// (Magnus form over water, Alduchov and Eskridge 1996 coefficients; within about 0.35 K
        /// from -40 C to 50 C). NaN when either input is missing or the humidity is not positive.
        /// </summary>
        private static double EPWDewPointTemperature(double dryBulbTemperature, double relativeHumidity)
        {
            if (double.IsNaN(dryBulbTemperature) || double.IsNaN(relativeHumidity) || relativeHumidity <= 0)
                return double.NaN;

            const double a = 17.625;
            const double b = 243.04;

            double value = Math.Log(Math.Min(relativeHumidity, 100.0) / 100.0) + a * dryBulbTemperature / (b + dryBulbTemperature);
            return b * value / (a - value);
        }

        /// <summary>
        /// Standard time zone [hours from UTC] stated on the weather data, or NaN when it is not stated.
        /// </summary>
        private static double EPWTimeZone(this WeatherData weatherData)
        {
            if (weatherData == null)
                return double.NaN;

            if (!weatherData.TryGetValue(WeatherDataParameter.TimeZone, out string timeZone) || string.IsNullOrWhiteSpace(timeZone))
                return double.NaN;

            return Core.Query.Double(Core.Query.UTC(timeZone));
        }
    }
}
