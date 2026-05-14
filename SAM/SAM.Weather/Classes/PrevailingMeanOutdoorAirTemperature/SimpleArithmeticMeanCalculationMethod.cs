// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Weather
{
    public class SimpleArithmeticMeanCalculationMethod : IPrevailingMeanOutdoorAirTemperatureCalculationMethod
    {
        public int SequentialDays { get; set; } = 15;

        public SimpleArithmeticMeanCalculationMethod()
        {

        }

        public SimpleArithmeticMeanCalculationMethod(int sequentialDays)
        {
            SequentialDays = sequentialDays;
        }

        public SimpleArithmeticMeanCalculationMethod(SimpleArithmeticMeanCalculationMethod simpleArithmeticMeanCalculationMethod)
        {
            if (simpleArithmeticMeanCalculationMethod != null)
            {
                SequentialDays = simpleArithmeticMeanCalculationMethod.SequentialDays;
            }
        }

        public SimpleArithmeticMeanCalculationMethod(JObject jObject)
        {
            FromJObject(jObject);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("SequentialDays"))
            {
                SequentialDays = jsonObject["SequentialDays"]?.GetValue<int>() ?? default;
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this),
                ["SequentialDays"] = SequentialDays
            };

            return jsonObject;
        }
    }
}
