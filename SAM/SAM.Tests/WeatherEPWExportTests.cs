// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Tests
{
    /// <summary>
    /// EPW export of SAM weather (Query.DataString / Convert.ToEPW): radiation field mapping, the
    /// horizontal-beam to direct-normal conversion, sky fields and missing values.
    /// <para>
    /// References: five unmodified TMYx EPWs (Resources/Weather, climate.onebuilding.org) whose
    /// direct-normal field is an independent dataset value. SAM imports only global and diffuse
    /// horizontal from them, so direct normal has to be reconstructed by the writer and can be
    /// compared with the reference.
    /// </para>
    /// <para>
    /// The geometry check uses <see cref="AlmanacSun"/>, the Michalsky (1988) Astronomical Almanac
    /// algorithm (sidereal-time route), deliberately not the NOAA/Spencer equation-of-time route the
    /// writer uses, so an agreement is not one implementation agreeing with itself.
    /// </para>
    /// </summary>
    public class WeatherEPWExportTests
    {
        private readonly ITestOutputHelper output;

        public WeatherEPWExportTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public static IEnumerable<object[]> References()
        {
            yield return new object[] { "HUN_BU_Budapest.Met.Center.128400_TMYx.epw.gz" };
            yield return new object[] { "USA_AK_Fairbanks.Intl.AP.702610_TMYx.2004-2018.epw.gz" };
            yield return new object[] { "AUS_NSW_Sydney.Intl.AP.947670_TMYx.2004-2018.epw.gz" };
            yield return new object[] { "SGP_SG_Changi.Intl.AP.486980_TMYx.2004-2018.epw.gz" };
            yield return new object[] { "USA_AZ_Phoenix-Sky.Harbor.Intl.AP.722780_TMYx.2004-2018.epw.gz" };
        }

        #region Reference EPWs

        [Fact]
        public void ReferenceFixtures_MatchRecordedHashes()
        {
            string directory = WeatherDirectory();
            Dictionary<string, string> hashes = File.ReadAllLines(Path.Combine(directory, "SHA256SUMS.txt"))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(new[] { ' ' }, 2))
                .ToDictionary(x => x[1].TrimStart('*').Trim(), x => x[0]);

            foreach (object[] reference in References())
            {
                string name = (string)reference[0];
                using SHA256 sHA256 = SHA256.Create();
                string hash = string.Concat(sHA256.ComputeHash(Decompress(Path.Combine(directory, name))).Select(x => x.ToString("x2")));

                // The hash is of the original, uncompressed EPW.
                Assert.Equal(hashes[name.Substring(0, name.Length - ".gz".Length)], hash);
            }
        }

        [Theory]
        [MemberData(nameof(References))]
        public void ReferenceEpw_Export_PreservesHorizontalAndSkyFields(string name)
        {
            List<string> lines = ReadReference(name).Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            List<EpwRow> reference = Rows(lines);
            List<EpwRow> exported = Rows(Weather.Convert.ToEPW(Weather.Convert.ToSAM(lines)).Split('\n'));

            Assert.Equal(8760, reference.Count);
            Assert.Equal(8760, exported.Count);

            for (int i = 0; i < reference.Count; i++)
            {
                EpwRow r = reference[i];
                EpwRow e = exported[i];

                Assert.Equal((r.Month, r.Day, r.Hour), (e.Month, e.Day, e.Hour));
                Assert.Equal(32, e.FieldCount);

                // Stated values are written through unchanged.
                Assert.Equal(r.DryBulb, e.DryBulb, 6);
                Assert.Equal(r.DewPoint, e.DewPoint, 6);
                Assert.Equal(r.GlobalHorizontal, e.GlobalHorizontal, 6);
                Assert.Equal(r.DiffuseHorizontal, e.DiffuseHorizontal, 6);
                Assert.Equal(r.TotalSkyCover, e.TotalSkyCover, 6);

                // SAM holds one cloud amount: it is written to opaque sky cover too.
                Assert.Equal(r.TotalSkyCover, e.OpaqueSkyCover, 6);

                // Not held by SAM: missing, so EnergyPlus derives it (never a 0 W/m2 sky).
                Assert.Equal(9999, e.HorizontalInfrared);

                Assert.InRange(e.DirectNormal, 0, 1500);
                Assert.InRange(e.ExtraterrestrialHorizontal, 0, 1420);
            }
        }

        /// <summary>
        /// References whose own direct normal closes with their own global - diffuse on the EPW
        /// time base (hour ending, local standard time): reconstructing direct normal from the
        /// horizontal components must reproduce the dataset value - annually, hourly with the sun up,
        /// and as beam on the horizontal and on vertical facades facing N, E, S and W. The statistics
        /// are written to the test output (they are the evidence quoted in the PR record).
        /// </summary>
        [Theory]
        [InlineData("USA_AK_Fairbanks.Intl.AP.702610_TMYx.2004-2018.epw.gz")]
        [InlineData("AUS_NSW_Sydney.Intl.AP.947670_TMYx.2004-2018.epw.gz")]
        [InlineData("SGP_SG_Changi.Intl.AP.486980_TMYx.2004-2018.epw.gz")]
        public void ConsistentReferenceEpw_Export_ReproducesDirectNormalAndFacadeBeam(string name)
        {
            Comparison comparison = Compare(name);

            Assert.InRange(comparison.ReferenceClosureError, 0, 0.02);

            Assert.InRange(comparison.ExportedBeamOnHorizontal / comparison.HorizontalBeam, 0.995, 1.005);
            Assert.InRange(comparison.ExportedDirectNormal / comparison.ReferenceDirectNormal, 0.99, 1.01);
            Assert.InRange(System.Math.Abs(comparison.MedianRelativeError), 0, 0.005);
            Assert.InRange(comparison.Percentile90AbsoluteRelativeError, 0, 0.02);

            for (int k = 0; k < 4; k++)
            {
                // Facades that receive almost no beam (south in Sydney) are compared absolutely.
                Assert.True(System.Math.Abs(comparison.ExportedFacadeBeam[k] - comparison.ReferenceFacadeBeam[k]) <= System.Math.Max(0.015 * comparison.ReferenceFacadeBeam[k], 1000), $"facade {AlmanacSun.FacadeNames[k]}");
            }
        }

        /// <summary>
        /// References whose own direct normal does NOT close with their own global - diffuse on the
        /// EPW time base: the closure is best with the geometry about 30 minutes earlier, i.e. the
        /// dataset's direct normal and its horizontal components are on time bases half an hour apart
        /// (and Budapest's own extraterrestrial column is 30 minutes later still). No conversion that
        /// follows the EPW convention can reproduce such a direct normal from global - diffuse. What
        /// is required is that the export keeps the stated horizontal beam (energy closure) and that
        /// the inconsistency is the dataset's, which this test measures rather than assumes.
        /// </summary>
        [Theory]
        [InlineData("HUN_BU_Budapest.Met.Center.128400_TMYx.epw.gz")]
        [InlineData("USA_AZ_Phoenix-Sky.Harbor.Intl.AP.722780_TMYx.2004-2018.epw.gz")]
        public void InconsistentReferenceEpw_Export_KeepsHorizontalBeam(string name)
        {
            Comparison comparison = Compare(name);

            Assert.True(comparison.ReferenceClosureError > 0.05, "the reference is internally inconsistent on the EPW time base");
            Assert.True(comparison.ReferenceClosureErrorShifted < comparison.ReferenceClosureError / 2, "and consistent with the geometry 30 minutes earlier");

            // All of the stated horizontal beam is placed except what exceeds the Rayleigh bound at grazing sun.
            Assert.InRange(comparison.ExportedBeamOnHorizontal / comparison.HorizontalBeam, 0.99, 1.0005);

            // Where the sun is high the half-hour inconsistency is small.
            Assert.InRange(System.Math.Abs(comparison.MedianRelativeError), 0, 0.03);
        }

        /// <summary>
        /// The F1 incident hours (SAM_UI PR #239): the embedded-weather export wrote global horizontal 0
        /// and the horizontal beam (global - diffuse) into the direct-normal field.
        /// </summary>
        [Theory]
        [InlineData(6, 16, 8, 403, 569, 120)]
        [InlineData(6, 16, 10, 623, 600, 174)]
        public void BudapestIncidentHours_ExportGlobalAndDirectNormal(int month, int day, int hour, double globalHorizontal, double directNormal, double diffuseHorizontal)
        {
            List<string> lines = ReadReference("HUN_BU_Budapest.Met.Center.128400_TMYx.epw.gz").Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            EpwRow e = Rows(Weather.Convert.ToEPW(Weather.Convert.ToSAM(lines)).Split('\n')).Single(x => x.Month == month && x.Day == day && x.Hour == hour);

            Assert.Equal(globalHorizontal, e.GlobalHorizontal, 6);
            Assert.Equal(diffuseHorizontal, e.DiffuseHorizontal, 6);

            // The direct normal puts exactly the stated beam on the horizontal...
            double meanCosZenith = AlmanacSun.Hour(2018, month, day, hour, 47.517, 19.033, 1).MeanCosZenith;
            Assert.InRange(e.DirectNormal * meanCosZenith, (globalHorizontal - diffuseHorizontal) * 0.995, (globalHorizontal - diffuseHorizontal) * 1.005);

            // ...is not the horizontal beam any more, and is near the dataset's own value, which sits
            // on a time base half an hour earlier (see InconsistentReferenceEpw_Export_KeepsHorizontalBeam).
            Assert.True(e.DirectNormal - (globalHorizontal - diffuseHorizontal) > 100);
            Assert.InRange(e.DirectNormal, directNormal * 0.8, directNormal * 1.05);
        }

        [Theory]
        [MemberData(nameof(References))]
        public void ReferenceEpw_ExportReimportExport_IsStable(string name)
        {
            List<string> lines = ReadReference(name).Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            string first = Weather.Convert.ToEPW(Weather.Convert.ToSAM(lines));
            string second = Weather.Convert.ToEPW(Weather.Convert.ToSAM(first.Split('\n').ToList()));

            Assert.Equal(first, second);
        }

        [Fact]
        public void ReferenceEpw_Import_IsUnchanged()
        {
            // The explicit-EPW route reads the file itself; the SAM reader keeps its mapping.
            List<string> lines = ReadReference("HUN_BU_Budapest.Met.Center.128400_TMYx.epw.gz").Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            WeatherData weatherData = Weather.Convert.ToSAM(lines);
            WeatherHour weatherHour = weatherData.GetWeatherHour(new DateTime(2018, 6, 16, 9, 0, 0));

            Assert.Equal(623, weatherHour[WeatherDataType.GlobalSolarRadiation]);
            Assert.Equal(174, weatherHour[WeatherDataType.DiffuseSolarRadiation]);
            Assert.True(double.IsNaN(weatherHour[WeatherDataType.DirectSolarRadiation]));
            Assert.Equal(17.3, weatherHour[WeatherDataType.DryBulbTemperature]);
            Assert.Equal(5.9, weatherHour[WeatherDataType.WetBulbTemperature]);
            Assert.Equal(0.2, weatherHour[WeatherDataType.CloudCover], 12);
            Assert.Equal("UTC+01:00", weatherData.GetValue<string>(WeatherDataParameter.TimeZone));
        }

        #endregion

        #region Synthetic hours

        /// <summary>
        /// A known direct normal at solar noon, low sun, winter and summer, both hemispheres and
        /// longitudes far from the zone meridian: the beam put on the horizontal with the independent
        /// geometry comes back as that direct normal. Within 1 % with the sun up; within 3 % in hours
        /// where the sun stays below ~5 degrees, where the two solar-position algorithms' few
        /// arc-minutes of difference are a percent of the altitude.
        /// </summary>
        [Theory]
        [InlineData(0.0, 0.0, 0.0, 3, 20, 11, 900.0, 0.01)] // equator, equinox, hour ending 12:00 (solar noon at 12:07)
        [InlineData(51.5, -0.1, 0.0, 6, 21, 12, 850.0, 0.01)] // London, summer, hour 12-13
        [InlineData(51.5, -0.1, 0.0, 12, 21, 11, 600.0, 0.01)] // London, winter noon, altitude ~15 deg
        [InlineData(51.5, -0.1, 0.0, 12, 21, 8, 150.0, 0.03)] // London, winter, first hour after sunrise
        [InlineData(-33.9, 151.2, 10.0, 12, 21, 12, 900.0, 0.01)] // Sydney, southern summer
        [InlineData(-33.9, 151.2, 10.0, 6, 21, 7, 200.0, 0.03)] // Sydney, southern winter, sunrise hour
        [InlineData(64.8, -147.9, -9.0, 12, 21, 13, 300.0, 0.03)] // Fairbanks, ~2 deg sun, 13 deg west of the zone meridian
        [InlineData(1.35, 104.0, 8.0, 9, 1, 13, 800.0, 0.01)] // Singapore, 14 deg east of the zone meridian
        [InlineData(28.6, 77.1, 5.5, 4, 15, 10, 850.0, 0.01)] // New Delhi, half-hour time zone
        public void KnownDirectNormal_PutOnHorizontal_IsRecovered(double latitude, double longitude, double timeZone, int month, int day, int hour, double directNormal, double tolerance)
        {
            double meanCosZenith = AlmanacSun.MeanCosZenith(2025, month, day, hour + 1, latitude, longitude, timeZone);
            Assert.True(meanCosZenith > 0.01, "the case must have the sun up");

            double diffuse = 80;
            EpwRow e = SingleHour(latitude, longitude, timeZone, month, day, hour, global: directNormal * meanCosZenith + diffuse, diffuse: diffuse);

            output.WriteLine($"mean cos zenith {meanCosZenith:F4}, exported DNI {e.DirectNormal}");
            Assert.InRange(e.DirectNormal, directNormal * (1 - tolerance), directNormal * (1 + tolerance));
            Assert.Equal(System.Math.Round(directNormal * meanCosZenith + diffuse, 2), e.GlobalHorizontal, 6);
            Assert.Equal(diffuse, e.DiffuseHorizontal, 6);

            // Extraterrestrial horizontal is the solar constant (eccentricity corrected) on the same geometry.
            Assert.InRange(e.ExtraterrestrialHorizontal / (e.ExtraterrestrialDirectNormal * meanCosZenith), 1 - tolerance, 1 + tolerance);
            Assert.InRange(e.ExtraterrestrialDirectNormal, 1320, 1415);
        }

        [Fact]
        public void SolarNoon_Equinox_Equator_GivesNearOverheadGeometry()
        {
            // Hours ending 12:00 and 13:00 straddle solar noon (12:07 at lon 0 on 20 March).
            EpwRow before = SingleHour(0, 0, 0, 3, 20, 11, global: 900, diffuse: 100);
            EpwRow after = SingleHour(0, 0, 0, 3, 20, 12, global: 900, diffuse: 100);

            // Sun within ~16 deg of the zenith all the time: DNI only slightly above the horizontal beam.
            Assert.InRange(before.DirectNormal, 800, 830);
            Assert.InRange(after.DirectNormal, 800, 830);
            Assert.InRange(before.ExtraterrestrialHorizontal, 1340, 1370);
        }

        [Fact]
        public void Night_WritesZeroRadiation_AndNoExtraterrestrial()
        {
            EpwRow e = SingleHour(51.5, -0.1, 0, 1, 15, 1, global: 0, diffuse: 0);

            Assert.Equal(0, e.GlobalHorizontal);
            Assert.Equal(0, e.DirectNormal);
            Assert.Equal(0, e.DiffuseHorizontal);
            Assert.Equal(0, e.ExtraterrestrialHorizontal);
            Assert.Equal(0, e.ExtraterrestrialDirectNormal);
        }

        [Fact]
        public void BeamWithSunBelowHorizon_IsNotInventedAsDirectNormal_AndGlobalIsKept()
        {
            // 02:00-03:00 in January in London: a horizontal beam here is a timing or data error;
            // it cannot be a direct-normal value. The stated global and diffuse stay as stated.
            EpwRow e = SingleHour(51.5, -0.1, 0, 1, 15, 2, global: 30, diffuse: 10);

            Assert.Equal(0, e.DirectNormal);
            Assert.Equal(30, e.GlobalHorizontal);
            Assert.Equal(10, e.DiffuseHorizontal);
        }

        [Fact]
        public void SunriseHour_DirectNormalIsLimitedToRayleighAtmosphere()
        {
            // London, 21 December, 08:00-09:00: the sun rises at ~08:04 and stays below ~4 deg.
            // 180 W/m2 of horizontal beam would need ~4500 W/m2 direct normal - impossible. The
            // writer keeps the stated global and diffuse and bounds the direct normal by what a pure
            // Rayleigh atmosphere transmits at that air mass (here roughly half the extraterrestrial).
            double meanCosZenith = AlmanacSun.MeanCosZenith(2025, 12, 21, 9, 51.5, -0.1, 0);
            Assert.InRange(meanCosZenith, 0.02, 0.06);

            EpwRow e = SingleHour(51.5, -0.1, 0, 12, 21, 8, global: 200, diffuse: 20);

            Assert.InRange(e.DirectNormal, 0.3 * e.ExtraterrestrialDirectNormal, 0.7 * e.ExtraterrestrialDirectNormal);
            Assert.True(e.DirectNormal * meanCosZenith < 180);
            Assert.Equal(200, e.GlobalHorizontal);
            Assert.Equal(20, e.DiffuseHorizontal);

            // A physically possible beam at the same hour is not touched by the bound.
            EpwRow possible = SingleHour(51.5, -0.1, 0, 12, 21, 8, global: 20 + 100 * meanCosZenith, diffuse: 20);
            Assert.InRange(possible.DirectNormal, 97, 103);
        }

        [Fact]
        public void HighSun_RayleighBoundIsNotReached_ByARealClearSky()
        {
            // Phoenix-like clear noon, 1050 W/m2 direct normal (above typical sea-level clear-sky maxima).
            double meanCosZenith = AlmanacSun.MeanCosZenith(2025, 6, 21, 13, 33.4, -112.0, -7);
            EpwRow e = SingleHour(33.4, -112.0, -7, 6, 21, 12, global: 1050 * meanCosZenith + 90, diffuse: 90);

            Assert.InRange(e.DirectNormal, 1040, 1060);
        }

        [Fact]
        public void MissingLocation_WritesDirectNormalMissing_NotTheHorizontalBeam()
        {
            WeatherDay weatherDay = Day(global: 500, diffuse: 200);
            WeatherYear weatherYear = new WeatherYear(2025);
            weatherYear[170] = weatherDay;

            // No time zone stated: the solar geometry is undefined.
            WeatherData weatherData = new WeatherData("Station", "Description", 51.5, -0.1, 10, weatherYear);
            EpwRow e = Rows(weatherData.DataString().Split('\n'))[12];

            Assert.Equal(9999, e.DirectNormal);
            Assert.Equal(9999, e.ExtraterrestrialHorizontal);
            Assert.Equal(500, e.GlobalHorizontal);
            Assert.Equal(200, e.DiffuseHorizontal);

            // The location-less public overloads behave the same.
            EpwRow overload = Rows(Query.DataString(2025, 6, 20, 12, weatherDay).Split('\n'))[0];
            Assert.Equal(9999, overload.DirectNormal);
            Assert.Equal(500, overload.GlobalHorizontal);
        }

        [Fact]
        public void MissingRadiationAndSky_AreWrittenAsEpwMissingValues_NeverZero()
        {
            WeatherDay weatherDay = new WeatherDay();
            weatherDay[WeatherDataType.DryBulbTemperature] = Enumerable.Repeat(10.0, 24).ToArray();

            EpwRow e = SingleHour(weatherDay, 51.5, -0.1, 0, 6, 20, 12);

            Assert.Equal(9999, e.GlobalHorizontal);
            Assert.Equal(9999, e.DirectNormal);
            Assert.Equal(9999, e.DiffuseHorizontal);
            Assert.Equal(9999, e.HorizontalInfrared);
            Assert.Equal(99, e.TotalSkyCover);
            Assert.Equal(99, e.OpaqueSkyCover);
            Assert.Equal(99.9, e.DewPoint);
        }

        [Fact]
        public void PartialRadiation_IsCompletedFromGlobalEqualsDirectPlusDiffuse()
        {
            // Direct (horizontal) and diffuse stated, global not: global is their sum.
            WeatherDay weatherDay = new WeatherDay();
            weatherDay[WeatherDataType.DirectSolarRadiation] = Enumerable.Repeat(300.0, 24).ToArray();
            weatherDay[WeatherDataType.DiffuseSolarRadiation] = Enumerable.Repeat(100.0, 24).ToArray();

            EpwRow e = SingleHour(weatherDay, 51.5, -0.1, 0, 6, 21, 12);
            double meanCosZenith = AlmanacSun.MeanCosZenith(2025, 6, 21, 13, 51.5, -0.1, 0);

            Assert.Equal(400, e.GlobalHorizontal);
            Assert.Equal(100, e.DiffuseHorizontal);
            Assert.InRange(e.DirectNormal, 300 / meanCosZenith * 0.99, 300 / meanCosZenith * 1.01);
        }

        [Fact]
        public void TasStyleWeather_DerivesDewPoint_AndWritesCloudInTenths()
        {
            // TAS weather has no dew point and no pressure; cloud cover is 0-1.
            WeatherDay weatherDay = new WeatherDay();
            weatherDay[WeatherDataType.DryBulbTemperature] = Enumerable.Repeat(20.0, 24).ToArray();
            weatherDay[WeatherDataType.RelativeHumidity] = Enumerable.Repeat(50.0, 24).ToArray();
            weatherDay[WeatherDataType.CloudCover] = Enumerable.Repeat(0.7, 24).ToArray();

            EpwRow e = SingleHour(weatherDay, 51.5, -0.1, 0, 6, 21, 12);

            // 20 C, 50 %: dew point 9.26 C (psychrometric tables 9.3 C).
            Assert.InRange(e.DewPoint, 9.2, 9.35);
            Assert.Equal(7, e.TotalSkyCover);
            Assert.Equal(7, e.OpaqueSkyCover);
        }

        [Fact]
        public void LeapYear_WritesOneRecordPerHourOf365Days_WithGeometryOfTheWrittenDate()
        {
            WeatherYear leap = new WeatherYear(2024);
            WeatherYear common = new WeatherYear(2023);
            for (int i = 0; i < 365; i++)
            {
                leap[i] = Day(global: 500, diffuse: 200);
                common[i] = Day(global: 500, diffuse: 200);
            }

            List<EpwRow> leapRows = Rows(Located(leap).DataString().Split('\n'));
            List<EpwRow> commonRows = Rows(Located(common).DataString().Split('\n'));

            Assert.Equal(8760, leapRows.Count);
            Assert.Equal(8760, commonRows.Count);

            // SAM WeatherYear has 365 days: in a leap year day index 59 is written as 29 February
            // (existing labelling, unchanged here). The geometry follows the written date.
            Assert.Contains(leapRows, x => x.Month == 2 && x.Day == 29);
            EpwRow leapJune = leapRows.Single(x => x.Month == 6 && x.Day == 21 && x.Hour == 13);
            EpwRow commonJune = commonRows.Single(x => x.Month == 6 && x.Day == 21 && x.Hour == 13);
            Assert.InRange(leapJune.DirectNormal / commonJune.DirectNormal, 0.995, 1.005);
        }

        #endregion

        #region Helpers

        private static WeatherDay Day(double global, double diffuse)
        {
            WeatherDay weatherDay = new WeatherDay();
            weatherDay[WeatherDataType.GlobalSolarRadiation] = Enumerable.Repeat(global, 24).ToArray();
            weatherDay[WeatherDataType.DiffuseSolarRadiation] = Enumerable.Repeat(diffuse, 24).ToArray();
            weatherDay[WeatherDataType.DryBulbTemperature] = Enumerable.Repeat(15.0, 24).ToArray();
            return weatherDay;
        }

        private static WeatherData Located(WeatherYear weatherYear)
        {
            WeatherData weatherData = new WeatherData("Station", "Description", 51.5, -0.1, 10, weatherYear);
            weatherData.SetValue(WeatherDataParameter.TimeZone, Core.Query.Description(Core.Query.UTC(0.0)));
            return weatherData;
        }

        private static EpwRow SingleHour(double latitude, double longitude, double timeZone, int month, int day, int hour, double global, double diffuse)
        {
            return SingleHour(Day(global, diffuse), latitude, longitude, timeZone, month, day, hour);
        }

        /// <summary>
        /// Exports one hour through the public WeatherData route (the route Convert.ToEPW uses).
        /// </summary>
        private static EpwRow SingleHour(WeatherDay weatherDay, double latitude, double longitude, double timeZone, int month, int day, int hour)
        {
            WeatherYear weatherYear = new WeatherYear(2025);
            int index = new DateTime(2025, month, day).DayOfYear - 1;
            weatherYear[index] = weatherDay;

            WeatherData weatherData = new WeatherData("Station", "Description", latitude, longitude, 10, weatherYear);
            weatherData.SetValue(WeatherDataParameter.TimeZone, Core.Query.Description(Core.Query.UTC(timeZone)));

            return Rows(weatherData.DataString().Split('\n')).Single(x => x.Month == month && x.Day == day && x.Hour == hour + 1);
        }

        private static string WeatherDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "Resources", "Weather");
        }

        private static byte[] Decompress(string path)
        {
            using FileStream fileStream = File.OpenRead(path);
            using GZipStream gZipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using MemoryStream memoryStream = new MemoryStream();
            gZipStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        private static string ReadReference(string name)
        {
            return System.Text.Encoding.UTF8.GetString(Decompress(Path.Combine(WeatherDirectory(), name)));
        }

        private static List<EpwRow> Rows(IEnumerable<string> lines)
        {
            List<EpwRow> result = new List<EpwRow>();
            foreach (string line in lines)
            {
                string[] values = line.Trim().Split(',');
                if (values.Length < 24 || !int.TryParse(values[0], out _) || !int.TryParse(values[1], out _))
                    continue;

                double Value(int index) => double.Parse(values[index], CultureInfo.InvariantCulture);

                result.Add(new EpwRow
                {
                    FieldCount = values.Length,
                    Month = int.Parse(values[1]),
                    Day = int.Parse(values[2]),
                    Hour = int.Parse(values[3]),
                    DryBulb = Value(6),
                    DewPoint = Value(7),
                    ExtraterrestrialHorizontal = Value(10),
                    ExtraterrestrialDirectNormal = Value(11),
                    HorizontalInfrared = Value(12),
                    GlobalHorizontal = Value(13),
                    DirectNormal = Value(14),
                    DiffuseHorizontal = Value(15),
                    TotalSkyCover = Value(22),
                    OpaqueSkyCover = Value(23),
                });
            }

            return result;
        }

        private sealed class Comparison
        {
            public double ReferenceDirectNormal;
            public double ExportedDirectNormal;
            public double HorizontalBeam;
            public double ExportedBeamOnHorizontal;
            public double ReferenceClosureError;
            public double ReferenceClosureErrorShifted;
            public double MedianRelativeError;
            public double Percentile90AbsoluteRelativeError;
            public double[] ReferenceFacadeBeam = new double[4];
            public double[] ExportedFacadeBeam = new double[4];
        }

        /// <summary>
        /// Imports a reference EPW into SAM, exports it again and compares the exported direct normal
        /// with the reference's own, using the independent geometry. Closure error: sum over hours of
        /// |DNI_ref * meanCosZenith - (GHI - DHI)| / sum (GHI - DHI), on the EPW time base and with the
        /// geometry 30 minutes earlier.
        /// </summary>
        private Comparison Compare(string name)
        {
            List<string> lines = ReadReference(name).Split('\n').Select(x => x.TrimEnd('\r')).ToList();
            List<EpwRow> reference = Rows(lines);
            WeatherData weatherData = Weather.Convert.ToSAM(lines);
            List<EpwRow> exported = Rows(Weather.Convert.ToEPW(weatherData).Split('\n'));
            Assert.Equal(reference.Count, exported.Count);

            double timeZone = Core.Query.Double(Core.Query.UTC(weatherData.GetValue<string>(WeatherDataParameter.TimeZone)));

            Comparison result = new Comparison();
            double closure = 0;
            double closureShifted = 0;
            List<double> relativeErrors = new List<double>();
            for (int i = 0; i < reference.Count; i++)
            {
                EpwRow r = reference[i];
                EpwRow e = exported[i];

                AlmanacSun.HourGeometry geometry = AlmanacSun.Hour(2018, r.Month, r.Day, r.Hour, weatherData.Latitude, weatherData.Longitude, timeZone, 0, 120);
                AlmanacSun.HourGeometry shifted = AlmanacSun.Hour(2018, r.Month, r.Day, r.Hour, weatherData.Latitude, weatherData.Longitude, timeZone, -30, 60);
                double beam = System.Math.Max(0, r.GlobalHorizontal - r.DiffuseHorizontal);

                result.ReferenceDirectNormal += r.DirectNormal;
                result.ExportedDirectNormal += e.DirectNormal;
                result.HorizontalBeam += beam;
                result.ExportedBeamOnHorizontal += e.DirectNormal * geometry.MeanCosZenith;
                closure += System.Math.Abs(r.DirectNormal * geometry.MeanCosZenith - beam);
                closureShifted += System.Math.Abs(r.DirectNormal * shifted.MeanCosZenith - beam);

                for (int k = 0; k < 4; k++)
                {
                    result.ReferenceFacadeBeam[k] += r.DirectNormal * geometry.MeanCosIncidence[k];
                    result.ExportedFacadeBeam[k] += e.DirectNormal * geometry.MeanCosIncidence[k];
                }

                // Sun above ~15 degrees for the whole hour and a material beam: the conversion is well conditioned.
                if (geometry.MeanCosZenith > 0.26 && r.DirectNormal > 100)
                    relativeErrors.Add((e.DirectNormal - r.DirectNormal) / r.DirectNormal);
            }

            relativeErrors.Sort();
            result.ReferenceClosureError = closure / result.HorizontalBeam;
            result.ReferenceClosureErrorShifted = closureShifted / result.HorizontalBeam;
            result.MedianRelativeError = relativeErrors[relativeErrors.Count / 2];
            result.Percentile90AbsoluteRelativeError = relativeErrors.Select(System.Math.Abs).OrderBy(x => x).ElementAt((int)(relativeErrors.Count * 0.9));

            output.WriteLine($"{name}: lat {weatherData.Latitude}, lon {weatherData.Longitude}, tz {timeZone}");
            output.WriteLine($"  reference closure |DNI*cosZ - (GHI-DHI)|: {result.ReferenceClosureError * 100:F2} % (geometry 30 min earlier: {result.ReferenceClosureErrorShifted * 100:F2} %)");
            output.WriteLine($"  annual DNI: reference {result.ReferenceDirectNormal / 1000:F1}, exported {result.ExportedDirectNormal / 1000:F1} kWh/m2 ({(result.ExportedDirectNormal / result.ReferenceDirectNormal - 1) * 100:+0.00;-0.00} %)");
            output.WriteLine($"  annual horizontal beam: GHI-DHI {result.HorizontalBeam / 1000:F1}, exported DNI*cosZ {result.ExportedBeamOnHorizontal / 1000:F1} kWh/m2 ({(result.ExportedBeamOnHorizontal / result.HorizontalBeam - 1) * 100:+0.00;-0.00} %)");
            output.WriteLine($"  sun-up hours n={relativeErrors.Count}: median DNI error {result.MedianRelativeError * 100:+0.00;-0.00} %, 90th percentile |error| {result.Percentile90AbsoluteRelativeError * 100:F2} %");
            for (int k = 0; k < 4; k++)
                output.WriteLine($"  facade {AlmanacSun.FacadeNames[k]} annual beam: reference {result.ReferenceFacadeBeam[k] / 1000:F1}, exported {result.ExportedFacadeBeam[k] / 1000:F1} kWh/m2");

            return result;
        }

        private sealed class EpwRow
        {
            public int FieldCount;
            public int Month;
            public int Day;
            public int Hour;
            public double DryBulb;
            public double DewPoint;
            public double ExtraterrestrialHorizontal;
            public double ExtraterrestrialDirectNormal;
            public double HorizontalInfrared;
            public double GlobalHorizontal;
            public double DirectNormal;
            public double DiffuseHorizontal;
            public double TotalSkyCover;
            public double OpaqueSkyCover;
        }

        /// <summary>
        /// Independent solar position: Michalsky (1988), "The Astronomical Almanac's algorithm for
        /// approximate solar position (1950-2050)", Solar Energy 40(3) - mean longitude, anomaly,
        /// ecliptic longitude, right ascension and Greenwich mean sidereal time. No equation of time.
        /// </summary>
        private static class AlmanacSun
        {
            public static readonly string[] FacadeNames = { "N", "E", "S", "W" };

            public sealed class HourGeometry
            {
                /// <summary>Hour mean of max(0, cos zenith).</summary>
                public double MeanCosZenith;

                /// <summary>Hour mean of max(0, cos incidence) on vertical facades facing N, E, S, W, with the sun up.</summary>
                public double[] MeanCosIncidence = new double[4];
            }

            public static double MeanCosZenith(int year, int month, int day, int epwHour, double latitude, double longitude, double timeZone)
            {
                return Hour(year, month, day, epwHour, latitude, longitude, timeZone).MeanCosZenith;
            }

            /// <summary>
            /// Geometry averaged over the EPW hour ending at <paramref name="epwHour"/>:00 local
            /// standard time (shifted by <paramref name="shiftMinutes"/>), from <paramref name="steps"/> sub-steps.
            /// </summary>
            public static HourGeometry Hour(int year, int month, int day, int epwHour, double latitude, double longitude, double timeZone, double shiftMinutes = 0, int steps = 600)
            {
                DateTime start = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(epwHour - 1 - timeZone).AddMinutes(shiftMinutes);
                HourGeometry result = new HourGeometry();
                for (int i = 0; i < steps; i++)
                {
                    (double cosZenith, double east, double north) = Sun(start.AddHours((i + 0.5) / steps), latitude, longitude);
                    if (cosZenith <= 0)
                        continue;

                    result.MeanCosZenith += cosZenith / steps;

                    // Outward normals of vertical facades: N (0,1), E (1,0), S (0,-1), W (-1,0).
                    result.MeanCosIncidence[0] += System.Math.Max(0, north) / steps;
                    result.MeanCosIncidence[1] += System.Math.Max(0, east) / steps;
                    result.MeanCosIncidence[2] += System.Math.Max(0, -north) / steps;
                    result.MeanCosIncidence[3] += System.Math.Max(0, -east) / steps;
                }

                return result;
            }

            /// <summary>
            /// Unit sun vector (up, east, north) at the given instant.
            /// </summary>
            private static (double, double, double) Sun(DateTime utc, double latitude, double longitude)
            {
                double n = (utc - new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)).TotalDays;
                double hours = utc.TimeOfDay.TotalHours;

                double meanLongitude = Normalise(280.460 + 0.9856474 * n);
                double meanAnomaly = Radians(Normalise(357.528 + 0.9856003 * n));
                double eclipticLongitude = Radians(Normalise(meanLongitude + 1.915 * System.Math.Sin(meanAnomaly) + 0.020 * System.Math.Sin(2 * meanAnomaly)));
                double obliquity = Radians(23.439 - 0.0000004 * n);

                double rightAscension = System.Math.Atan2(System.Math.Cos(obliquity) * System.Math.Sin(eclipticLongitude), System.Math.Cos(eclipticLongitude));
                double declination = System.Math.Asin(System.Math.Sin(obliquity) * System.Math.Sin(eclipticLongitude));

                // n includes the fraction of the day, which supplies the sidereal excess of the UT hours.
                double greenwichMeanSiderealTime = (6.697375 + 0.0657098242 * n + hours) % 24.0;
                double localMeanSiderealTime = greenwichMeanSiderealTime + longitude / 15.0;
                double hourAngle = Radians(localMeanSiderealTime * 15.0) - rightAscension;

                double latitudeRadians = Radians(latitude);
                double up = System.Math.Sin(latitudeRadians) * System.Math.Sin(declination) + System.Math.Cos(latitudeRadians) * System.Math.Cos(declination) * System.Math.Cos(hourAngle);
                double east = -System.Math.Cos(declination) * System.Math.Sin(hourAngle);
                double north = System.Math.Cos(latitudeRadians) * System.Math.Sin(declination) - System.Math.Sin(latitudeRadians) * System.Math.Cos(declination) * System.Math.Cos(hourAngle);
                return (up, east, north);
            }

            private static double Normalise(double degrees)
            {
                double result = degrees % 360.0;
                return result < 0 ? result + 360.0 : result;
            }

            private static double Radians(double degrees)
            {
                return degrees * System.Math.PI / 180.0;
            }
        }

        #endregion
    }
}
