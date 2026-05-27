// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Weather
{
    /// <summary>
    /// Represents a single hour's weather data.
    /// </summary>
    public class WeatherHour : IWeatherObject
    {
        private Dictionary<string, double> dictionary;

        /// <summary>
        /// Constructor for WeatherDay class, initializing a new Dictionary.
        /// </summary>
        /// <returns>
        /// A new instance of WeatherDay.
        /// </returns>
        public WeatherHour()
        {
            dictionary = new Dictionary<string, double>();
        }

        /// <summary>
        /// Constructor for WeatherHour class that takes a WeatherHour object as a parameter.
        /// </summary>
        /// <param name="weatherHour">WeatherHour object to be used for initialization.</param>
        /// <returns>A new WeatherHour object.</returns>
        public WeatherHour(WeatherHour weatherHour)
        {
            dictionary = new Dictionary<string, double>();
            if (weatherHour != null)
            {
                foreach (KeyValuePair<string, double> keyValuePair in weatherHour.dictionary)
                {
                    dictionary[keyValuePair.Key] = keyValuePair.Value;
                }
            }
        }

        public WeatherHour(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>
        /// Gets or sets the value for the given name.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <returns>The value for the given name.</returns>
        public double this[string name]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(name) || dictionary == null)
                {
                    return double.NaN;
                }

                if (!dictionary.TryGetValue(name, out double value))
                {
                    return double.NaN;
                }

                return value;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                if (dictionary == null)
                {
                    dictionary = new Dictionary<string, double>();
                }

                dictionary[name] = value;
            }
        }

        /// <summary>
        /// Gets or sets the weather data type value.
        /// </summary>
        /// <param name="weatherDataType">The weather data type.</param>
        /// <returns>The weather data type value.</returns>
        public double this[WeatherDataType weatherDataType]
        {
            get
            {
                return this[weatherDataType.ToString()];
            }
            set
            {
                this[weatherDataType.ToString()] = value;
            }
        }

        /// <summary>
        /// Checks if the WeatherDataType is contained in the collection.
        /// </summary>
        /// <param name="weatherDataType">The WeatherDataType to check for.</param>
        /// <returns>True if the WeatherDataType is contained in the collection, false otherwise.</returns>
        public bool Contains(WeatherDataType weatherDataType)
        {
            return Contains(weatherDataType.ToString());
        }

        /// <summary>
        /// Checks if the dictionary contains the specified name.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the dictionary contains the specified name, false otherwise.</returns>
        public bool Contains(string name)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return false;
            }

            return dictionary.ContainsKey(name);
        }

        /// <summary>
        /// Removes an item from the dictionary.
        /// </summary>
        /// <param name="name">The name of the item to remove.</param>
        /// <returns>True if the item was removed, false otherwise.</returns>
        public bool Remove(string name)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            if (!dictionary.ContainsKey(name))
            {
                return false;
            }

            return dictionary.Remove(name);
        }

        /// <summary>
        /// Removes the specified WeatherDataType from the collection.
        /// </summary>
        /// <param name="weatherDataType">The WeatherDataType to remove.</param>
        /// <returns>True if the WeatherDataType was successfully removed, false otherwise.</returns>
        public bool Remove(WeatherDataType weatherDataType)
        {
            return Remove(weatherDataType.ToString());
        }

        /// <summary>
        /// Gets the keys of the dictionary.
        /// </summary>
        /// <returns>The keys of the dictionary.</returns>
        public IEnumerable<string> Keys
        {
            get
            {
                return dictionary == null ? null : dictionary.Keys;
            }
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Data"] is JsonArray dataArray)
            {
                dictionary = new Dictionary<string, double>();
                foreach (JsonNode dataNode in dataArray)
                {
                    if (!(dataNode is JsonObject dataObject) || !dataObject.ContainsKey("Name"))
                        continue;

                    string name = dataObject["Name"]?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    double value = double.NaN;
                    if (dataObject.ContainsKey("Value"))
                        value = dataObject["Value"]?.GetValue<double>() ?? default;

                    dictionary[name] = value;
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

            if (dictionary != null)
            {
                JsonArray dataArray = new JsonArray();
                foreach (KeyValuePair<string, double> keyValuePair in dictionary)
                {
                    if (double.IsNaN(keyValuePair.Value) || double.IsInfinity(keyValuePair.Value))
                        continue;

                    JsonObject dataObject = new JsonObject
                    {
                        ["Name"] = keyValuePair.Key,
                        ["Value"] = keyValuePair.Value
                    };
                    dataArray.Add(dataObject);
                }
                jsonObject["Data"] = dataArray;
            }

            return jsonObject;
        }

        /// <summary>
        /// Calculates the ground temperature based on the dry bulb temperature and global radiation.
        /// </summary>
        /// <returns>The calculated ground temperature.</returns>
        public double CalculatedGroundTemperature()
        {
            double dryBulbTemperature = this[WeatherDataType.DryBulbTemperature];
            if (double.IsNaN(dryBulbTemperature))
            {
                return double.NaN;
            }

            double globalRadiation = CalculatedGlobalSolarRadiation();
            if (double.IsNaN(globalRadiation))
            {
                return double.NaN;
            }

            return (0.02 * globalRadiation) + dryBulbTemperature;
        }

        /// <summary>
        /// Calculates the global solar radiation from direct and diffuse solar radiation.
        /// </summary>
        /// <returns>
        /// The global solar radiation.
        /// </returns>
        public double CalculatedGlobalSolarRadiation()
        {
            double result = this[WeatherDataType.GlobalSolarRadiation];
            if (!double.IsNaN(result))
            {
                return result;
            }

            double directSolarRadiation = this[WeatherDataType.DirectSolarRadiation];
            if (double.IsNaN(directSolarRadiation))
            {
                return double.NaN;
            }

            double diffuseSolarRadiation = this[WeatherDataType.DiffuseSolarRadiation];
            if (double.IsNaN(diffuseSolarRadiation))
            {
                return double.NaN;
            }

            return directSolarRadiation + diffuseSolarRadiation;
        }

        public double CalculatedDiffuseSolarRadiation()
        {
            double result = this[WeatherDataType.DiffuseSolarRadiation];
            if (!double.IsNaN(result))
            {
                return result;
            }

            double globalSolarRadiation = this[WeatherDataType.GlobalSolarRadiation];
            if (double.IsNaN(globalSolarRadiation))
            {
                return double.NaN;
            }

            double directSolarRadiation = this[WeatherDataType.DirectSolarRadiation];
            if (double.IsNaN(directSolarRadiation))
            {
                return double.NaN;
            }

            return globalSolarRadiation - directSolarRadiation;
        }

        public double CalculatedDirectSolarRadiation()
        {
            double result = this[WeatherDataType.DirectSolarRadiation];
            if (!double.IsNaN(result))
            {
                return result;
            }

            double globalSolarRadiation = this[WeatherDataType.GlobalSolarRadiation];
            if (double.IsNaN(globalSolarRadiation))
            {
                return double.NaN;
            }

            double diffuseSolarRadiation = this[WeatherDataType.DiffuseSolarRadiation];
            if (double.IsNaN(diffuseSolarRadiation))
            {
                return double.NaN;
            }

            return globalSolarRadiation - diffuseSolarRadiation;
        }

        public double GlobalSolarRadiation
        {
            get
            {
                return this[WeatherDataType.GlobalSolarRadiation];
            }
            set
            {
                this[WeatherDataType.GlobalSolarRadiation] = value;
            }
        }

        public double DiffuseSolarRadiation
        {
            get
            {
                return this[WeatherDataType.DiffuseSolarRadiation];
            }
            set
            {
                this[WeatherDataType.DiffuseSolarRadiation] = value;
            }
        }

        public double DirectSolarRadiation
        {
            get
            {
                return this[WeatherDataType.DirectSolarRadiation];
            }
            set
            {
                this[WeatherDataType.DirectSolarRadiation] = value;
            }
        }

        public double CloudCover
        {
            get
            {
                return this[WeatherDataType.CloudCover];
            }
            set
            {
                this[WeatherDataType.CloudCover] = value;
            }
        }

        public double DryBulbTemperature
        {
            get
            {
                return this[WeatherDataType.DryBulbTemperature];
            }
            set
            {
                this[WeatherDataType.DryBulbTemperature] = value;
            }
        }

        public double WetBulbTemperature
        {
            get
            {
                return this[WeatherDataType.WetBulbTemperature];
            }
            set
            {
                this[WeatherDataType.WetBulbTemperature] = value;
            }
        }

        public double RelativeHumidity
        {
            get
            {
                return this[WeatherDataType.RelativeHumidity];
            }
            set
            {
                this[WeatherDataType.RelativeHumidity] = value;
            }
        }

        public double WindSpeed
        {
            get
            {
                return this[WeatherDataType.WindSpeed];
            }
            set
            {
                this[WeatherDataType.WindSpeed] = value;
            }
        }

        public double WindDirection
        {
            get
            {
                return this[WeatherDataType.WindDirection];
            }
            set
            {
                this[WeatherDataType.WindDirection] = value;
            }
        }

        public double AtmosphericPressure
        {
            get
            {
                return this[WeatherDataType.AtmosphericPressure];
            }
            set
            {
                this[WeatherDataType.AtmosphericPressure] = value;
            }
        }
    }
}
