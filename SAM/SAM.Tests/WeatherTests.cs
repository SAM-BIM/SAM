// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Tests.Helpers;
using SAM.Weather;
using System;
using System.Linq;
using Xunit;

namespace SAM.Tests
{
    public class WeatherTests
    {
        [Fact]
        public void RoundTrip_WeatherDay_PreservesHourlyValues()
        {
            WeatherDay expected = new WeatherDay();
            expected[WeatherDataType.DryBulbTemperature] = Enumerable.Range(0, 24).Select(x => 21.5 + x).ToArray();
            expected[WeatherDataType.RelativeHumidity] = Enumerable.Range(0, 24).Select(x => 55.0 + x).ToArray();

            WeatherDay result = RoundTrip.Once(expected);

            Assert.Equal(21.5, result[WeatherDataType.DryBulbTemperature, 0]);
            Assert.Equal(78.0, result[WeatherDataType.RelativeHumidity, 23]);
        }

        [Fact]
        public void RoundTrip_WeatherData_PreservesNestedWeatherYear()
        {
            WeatherYear weatherYear = new WeatherYear(2025);
            WeatherDay weatherDay = new WeatherDay();
            weatherDay[WeatherDataType.DryBulbTemperature] = Enumerable.Range(0, 24).Select(x => 12.25 + x).ToArray();
            weatherYear[0] = weatherDay;

            WeatherData expected = new WeatherData("Station", "Description", 51.5, -0.1, 42.0, weatherYear);

            WeatherData result = RoundTrip.Once(expected);

            Assert.Equal("Description", result.Description);
            Assert.Contains(2025, result.Years);
            Assert.Equal(12.25, result.GetWeatherHour(new DateTime(2025, 1, 1, 0, 0, 0))[WeatherDataType.DryBulbTemperature]);
        }
    }
}
