// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Weather;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class WeatherDataCase : Case
    {
        private WeatherData weatherData;

        public WeatherDataCase(WeatherData weatherData)
            : base()
        {
            this.weatherData = weatherData;
        }

        public WeatherDataCase(WeatherDataCase weatherDataCase)
            : base(weatherDataCase)
        {
            if (weatherDataCase != null)
            {
                weatherData = weatherDataCase.weatherData;
            }
        }

        public WeatherDataCase(JObject jObject)
            : base(jObject)
        {

        }

        public WeatherData WeatherData
        {
            get
            {
                return weatherData;
            }

            set
            {
                weatherData = value;
                OnPropertyChanged(nameof(WeatherData));
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["WeatherData"] is JsonObject weatherDataJson)
            {
                weatherData = Core.Query.IJSAMObject<WeatherData>(new JObject((JsonObject)weatherDataJson.DeepClone()));
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (weatherData?.ToJObject()?.Node is JsonObject weatherDataJson)
            {
                result["WeatherData"] = weatherDataJson.DeepClone();
            }

            return result;
        }
    }
}
