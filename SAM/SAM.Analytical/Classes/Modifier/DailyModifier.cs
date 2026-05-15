// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class DailyModifier : IndexedSimpleModifier
    {
        private List<string> dayNames;
        private Dictionary<string, double[]> values;

        public DailyModifier(ArithmeticOperator arithmeticOperator, IEnumerable<KeyValuePair<string, double[]>> values)
        {
            ArithmeticOperator = arithmeticOperator;
            if (values != null)
            {
                this.values = new Dictionary<string, double[]>();
                dayNames = new List<string>();
                foreach (KeyValuePair<string, double[]> keyValuePair in values)
                {
                    double[] values_Day = new double[24];

                    int count = keyValuePair.Value.Length;
                    for (int i = 0; i < 24; i++)
                    {
                        values_Day[i] = keyValuePair.Value[i % count];
                    }

                    this.values[keyValuePair.Key] = values_Day;
                    dayNames.Add(keyValuePair.Key);
                }
            }
        }

        public DailyModifier(DailyModifier dailyModifier)
            : base(dailyModifier)
        {
            if (dailyModifier != null)
            {
                dayNames = dailyModifier.dayNames == null ? null : new List<string>(dailyModifier.dayNames);
                if (dailyModifier.values != null)
                {
                    values = new Dictionary<string, double[]>();
                    foreach (KeyValuePair<string, double[]> keyValuePair in dailyModifier.values)
                    {
                        double[] values_Day = new double[24];

                        int count = keyValuePair.Value.Length;
                        for (int i = 0; i < 24; i++)
                        {
                            values_Day[i] = keyValuePair.Value[i % count];
                        }

                        values[keyValuePair.Key] = values_Day;
                    }
                }

            }
        }
        public DailyModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public List<string> DayNames
        {
            get
            {
                return dayNames == null ? values == null ? null : values.Keys.ToList() : new List<string>(dayNames);
            }
        }

        public override bool ContainsIndex(int index)
        {
            if (values == null)
            {
                return false;
            }

            if (index < 0)
            {
                return false;
            }

            int count = dayNames == null ? values == null ? 0 : values.Count : dayNames.Count;

            return index >= count * 24;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            if (jsonObject["DayNames"] is JsonArray dayNamesArray)
            {
                dayNames = new List<string>();
                foreach (JsonNode node in dayNamesArray)
                {
                    dayNames.Add(node?.GetValue<string>());
                }
            }

            if (jsonObject["Values"] is JsonArray valuesArray)
            {
                values = new Dictionary<string, double[]>();
                foreach (JsonNode node in valuesArray)
                {
                    if (node is JsonArray entryArray)
                    {
                        string dayName = entryArray[0]?.GetValue<string>();

                        List<double> values_Temp = new List<double>();
                        if (entryArray[1] is JsonArray innerValuesArray)
                        {
                            foreach (JsonNode valueNode in innerValuesArray)
                            {
                                values_Temp.Add(valueNode?.GetValue<double>() ?? double.NaN);
                            }
                        }

                        values[dayName] = values_Temp.ToArray();
                    }
                }
            }

            return result;
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (values == null)
            {
                return value;
            }

            List<string> dayNames_Temp = dayNames == null ? values == null ? null : values.Keys.ToList() : dayNames;
            if (dayNames_Temp == null || dayNames_Temp.Count == 0)
            {
                return value;
            }

            int index_Temp = index % (dayNames_Temp.Count * 24);

            int index_DayName = index_Temp / 24;
            int index_Hour = index_Temp % 24;

            string dayName = dayNames_Temp[index_DayName];

            double value_Temp = values[dayName][index_Hour];

            if (double.IsNaN(value_Temp))
            {
                return double.NaN;
            }

            return Core.Query.Calculate(ArithmeticOperator, value, value_Temp);
        }

        public double GetValue(string dayName, int index)
        {
            if (dayName == null)
            {
                return double.NaN;
            }

            if (values.TryGetValue(dayName, out double[] values_Day))
            {
                return values_Day[index];
            }

            return double.NaN;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (dayNames != null)
            {
                JsonArray dayNamesArray = new JsonArray();
                dayNames.ForEach(x => dayNamesArray.Add(x));

                result["DayNames"] = dayNamesArray;
            }

            if (values != null)
            {
                JsonArray valuesArray = new JsonArray();
                foreach (KeyValuePair<string, double[]> keyValuePair in values)
                {
                    JsonArray entryArray = new JsonArray();
                    entryArray.Add(keyValuePair.Key);

                    JsonArray innerValuesArray = new JsonArray();
                    foreach (double value in keyValuePair.Value)
                    {
                        innerValuesArray.Add(value);
                    }
                    entryArray.Add(innerValuesArray);

                    valuesArray.Add(entryArray);
                }

                result["Values"] = valuesArray;
            }

            return result;
        }
    }
}
