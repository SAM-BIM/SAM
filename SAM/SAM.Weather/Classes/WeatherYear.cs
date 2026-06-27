// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Weather
{
    /// <summary>
    /// Represents weather data for an entire year.
    /// </summary>
    public class WeatherYear : IWeatherObject
    {
        private int year;
        private WeatherDay[] weatherDays = new WeatherDay[365];

        /// <summary>
        /// Initializes a new instance of the WeatherYear class with the specified year.
        /// </summary>
        /// <param name="year">The year for which the weather data is stored.</param>
        public WeatherYear(int year)
        {
            this.year = year;
        }

        /// <summary>
        /// Initializes a new instance of the WeatherYear class by copying data from another WeatherYear instance.
        /// </summary>
        /// <param name="weatherYear">The WeatherYear instance to copy data from.</param>
        public WeatherYear(WeatherYear weatherYear)
        {
            year = weatherYear.year;

            if (weatherYear.weatherDays != null)
            {
                weatherDays = new WeatherDay[weatherYear.weatherDays.Length];
                for (int i = 0; i < weatherYear.weatherDays.Length; i++)
                    weatherDays[i] = weatherYear.weatherDays[i] == null ? null : new WeatherDay(weatherYear.weatherDays[i]);
            }
        }

        public WeatherYear(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>
        /// Gets or sets the WeatherDay at the specified index.
        /// </summary>
        /// <param name="i">The index of the WeatherDay to get or set.</param>
        /// <returns>The WeatherDay at the specified index.</returns>
        public WeatherDay this[int i]
        {
            get
            {
                if (i < 0 || i >= 365)
                    return null;

                if (weatherDays == null)
                    return null;

                return weatherDays[i];
            }
            set
            {
                if (i < 0 || i >= 365)
                    return;

                weatherDays[i] = value;
            }
        }

        /// <summary>
        /// Adds weather data for a specific day and hour.
        /// </summary>
        /// <param name="day">The day of the year (0-364).</param>
        /// <param name="hour">The hour of the day (0-23).</param>
        /// <param name="values">A dictionary containing weather data values.</param>
        /// <returns>True if the data is added successfully, otherwise false.</returns>
        public bool Add(int day, int hour, Dictionary<string, double> values)
        {
            if (day < 0 || day >= 365)
            {
                return false;
            }

            if (hour < 0 || hour >= 24)
            {
                return false;
            }

            if (weatherDays == null)
            {
                weatherDays = new WeatherDay[365];
            }

            WeatherDay weatherDay = weatherDays[day];
            if (weatherDay == null)
            {
                weatherDay = new WeatherDay();
                weatherDays[day] = weatherDay;
            }

            foreach (KeyValuePair<string, double> keyValuePair in values)
            {
                weatherDay[keyValuePair.Key, hour] = keyValuePair.Value;
            }

            return true;
        }

        public bool Add(int day, int hour, WeatherHour weatherHour)
        {
            IEnumerable<string> keys = weatherHour?.Keys;
            if (keys == null || keys.Count() == 0)
            {
                return false;
            }


            Dictionary<string, double> dictionary = new Dictionary<string, double>();
            foreach (string key in keys)
            {
                dictionary[key] = weatherHour[key];
            }

            return Add(day, hour, dictionary);
        }

        public bool Add(int hour, WeatherHour weatherHour)
        {
            if (weatherHour == null)
            {
                return false;
            }

            int day = Query.DayIndex(hour, out int dayHour);

            return Add(day, dayHour, weatherHour);
        }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        public int Year
        {
            get
            {
                return year;
            }
            set
            {
                year = value;
            }
        }

        /// <summary>
        /// Gets a list of WeatherDay objects representing daily weather data for the year.
        /// </summary>
        /// <summary>
        /// Gets a list of WeatherDay objects.
        /// </summary>
        /// <returns>A list of WeatherDay objects.</returns>
        public List<WeatherDay> WeatherDays
        {
            get
            {
                if (weatherDays == null)
                    return null;

                return weatherDays.ToList();
            }
        }

        public List<WeatherHour> GetWeatherHours()
        {
            if (weatherDays == null)
            {
                return null;
            }

            List<WeatherHour> result = new List<WeatherHour>();
            foreach (WeatherDay weatherDay in weatherDays)
            {
                List<WeatherHour> weatherHours = weatherDay?.GetWeatherHours();
                if (weatherHours == null)
                {
                    continue;
                }

                result.AddRange(weatherHours);
            }

            return result;
        }

        public List<WeatherHour> GetWeatherHours(IEnumerable<int> hours)
        {
            if (weatherDays == null || hours == null)
            {
                return null;
            }

            List<WeatherHour> result = new List<WeatherHour>();
            foreach (int hour in hours)
            {
                result.Add(GetWeatherHour(hour));
            }

            return result;
        }

        /// <summary>
        /// Gets a list of weather data values for a specific parameter by name.
        /// </summary>
        /// <param name="name">The name of the weather data parameter.</param>
        /// <returns>A list of weather data values for the specified parameter.</returns>
        public List<double> GetValues(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            List<double> result = new List<double>();
            for (int i = 0; i < weatherDays.Length; i++)
            {
                WeatherDay weatherDay = weatherDays[i];
                if (weatherDay == null)
                {
                    result.AddRange(Enumerable.Repeat(double.NaN, 24));
                    continue;
                }

                double[] values = weatherDay[name];
                if (values == null || values.Length != 24)
                {
                    result.AddRange(Enumerable.Repeat(double.NaN, 24));
                }
                result.AddRange(values);
            }

            return result;
        }

        public IndexedDoubles GetIndexedDoubles(string name)
        {
            List<double> values = GetValues(name);
            if (values == null)
            {
                return null;
            }

            return new IndexedDoubles(values);
        }

        public IndexedDoubles GetIndexedDoubles(WeatherDataType weatherDataType)
        {
            return GetIndexedDoubles(weatherDataType.ToString());
        }

        /// <summary>
        /// Gets a list of weather data values for a specific parameter by WeatherDataType.
        /// </summary>
        /// <param name="weatherDataType">The WeatherDataType of the weather data parameter.</param>
        /// <returns>A list of weather data values for the specified parameter.</returns>
        public List<double> GetValues(WeatherDataType weatherDataType)
        {
            return GetValues(weatherDataType.ToString());
        }

        public WeatherHour GetWeatherHour(int hour)
        {
            int day = Query.DayIndex(hour, out int dayHour);

            return GetWeatherHour(day, dayHour);
        }

        public WeatherHour GetWeatherHour(int day, int hour)
        {
            if (day >= weatherDays.Length || hour < 0 || hour > 23)
            {
                return null;
            }

            return weatherDays[day]?.GetWeatherHour(hour);
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject.ContainsKey("Year"))
                year = jsonObject["Year"]?.GetValue<int>() ?? default;

            if (jsonObject["WeatherDays"] is JsonArray weatherDaysArray && weatherDaysArray.Count == 365)
            {
                weatherDays = new WeatherDay[365];
                for (int i = 0; i < 365; i++)
                {
                    if (!(weatherDaysArray[i] is JsonObject weatherDayObject))
                        continue;

                    weatherDays[i] = new WeatherDay((JsonObject)weatherDayObject.DeepClone());
                }
            }

            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (year != int.MinValue)
                jsonObject["Year"] = year;

            if (weatherDays != null)
            {
                JsonArray weatherDaysArray = new JsonArray();
                foreach (WeatherDay weatherDay in weatherDays)
                    weatherDaysArray.Add(weatherDay?.ToJsonObject());

                jsonObject["WeatherDays"] = weatherDaysArray;
            }

            return jsonObject;
        }
    }
}
