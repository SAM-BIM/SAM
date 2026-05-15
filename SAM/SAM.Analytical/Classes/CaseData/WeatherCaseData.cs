// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class WeatherCaseData : BuiltInCaseData
    {
        private string weatherDataName;

        public WeatherCaseData(string weatherDataName)
            : base(nameof(OpeningCaseData))
        {
            this.weatherDataName = weatherDataName;
        }
        public WeatherCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public WeatherCaseData(WeatherCaseData weatherCaseData)
            : base(weatherCaseData)
        {
            if (weatherCaseData != null)
            {
                weatherDataName = weatherCaseData.weatherDataName;
            }
        }

        public string WeatherDataName
        {
            get
            {
                return weatherDataName;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("WeatherDataName"))
            {
                weatherDataName = jsonObject["WeatherDataName"]?.GetValue<string>();
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (weatherDataName is not null)
            {
                result["WeatherDataName"] = weatherDataName;
            }

            return result;
        }
    }
}
