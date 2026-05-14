// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class DailyWeightedExceedance : IAnalyticalObject
    {
        private int dayIndex;
        private IndexedDoubles temperatureDifferences;

        public DailyWeightedExceedance(DailyWeightedExceedance dailyWeightedExceedance)
        {
            if (dailyWeightedExceedance != null)
            {
                dayIndex = dailyWeightedExceedance.dayIndex;
                temperatureDifferences = dailyWeightedExceedance.temperatureDifferences == null ? null : new IndexedDoubles(dailyWeightedExceedance.temperatureDifferences);
            }
        }

        public DailyWeightedExceedance(JObject jObject)
        {
            FromJObject(jObject);
        }

        public DailyWeightedExceedance(int dayIndex, IndexedDoubles temperatureDifferences)
        {
            this.dayIndex = dayIndex;
            this.temperatureDifferences = temperatureDifferences == null ? null : new IndexedDoubles(temperatureDifferences);
        }

        public List<double> UniqueTemperatureDifferences
        {
            get
            {
                return GetCountDictionary()?.Keys?.ToList();
            }
        }

        public List<int> UniqueCounts
        {
            get
            {
                return GetCountDictionary()?.Values?.ToList();
            }
        }

        public int DayIndex
        {
            get
            {
                return dayIndex;
            }
        }

        private Dictionary<double, int> GetCountDictionary()
        {
            IEnumerable<int> keys = temperatureDifferences?.Keys;
            if (keys == null)
            {
                return null;
            }

            Dictionary<double, int> result = new Dictionary<double, int>();
            foreach (int key in keys)
            {
                double temperatureDifference = temperatureDifferences[key];
                if (!result.ContainsKey(temperatureDifference))
                {
                    result[temperatureDifference] = 0;
                }

                result[temperatureDifference]++;
            }

            return result;
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("DayIndex"))
            {
                dayIndex = jsonObject["DayIndex"]?.GetValue<int>() ?? 0;
            }

            if (jsonObject["TemperatureDifferences"] is JsonObject temperatureDifferencesJson)
            {
                temperatureDifferences = new IndexedDoubles(new JObject((JsonObject)temperatureDifferencesJson.DeepClone()));
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (dayIndex != -1)
            {
                jsonObject["DayIndex"] = dayIndex;
            }

            if (temperatureDifferences?.ToJsonObject() is JsonObject temperatureDifferencesJson)
            {
                jsonObject["TemperatureDifferences"] = temperatureDifferencesJson.DeepClone();
            }

            return jsonObject;
        }

        public IndexedDoubles TemperatureDifferences
        {
            get
            {
                return temperatureDifferences == null ? null : new IndexedDoubles(temperatureDifferences);
            }
        }

        public double WeightedExceedance
        {
            get
            {
                Dictionary<double, int> dictionary = GetCountDictionary();
                if (dictionary == null || dictionary.Count == 0)
                {
                    return double.NaN;
                }

                double result = 0;
                foreach (KeyValuePair<double, int> keyValuePair in dictionary)
                {
                    result += keyValuePair.Key * System.Convert.ToDouble(keyValuePair.Value);
                }

                return result;
            }
        }

        public bool Exceed
        {
            get
            {
                double weightedExceedance = WeightedExceedance;
                if (double.IsNaN(weightedExceedance))
                {
                    return false;
                }

                return weightedExceedance > 6;
            }
        }

        public int StartHourIndex
        {
            get
            {
                return dayIndex * 24;
            }
        }

        public int EndHourIndex
        {
            get
            {
                return (dayIndex * 24) + 23;
            }
        }
    }
}
