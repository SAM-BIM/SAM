// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class Function : IJSAMObject, IAnalyticalObject
    {
        private string name;
        private List<double> values;

        public Function(string name, IEnumerable<double> values)
        {
            this.name = name;
            this.values = values == null ? null : [.. values];
        }

        public Function(Function function)
        {
            if (function != null)
            {
                name = function.name;
                values = function.values == null ? null : [.. function.values];
            }
        }

        public Function(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }


        public Function(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public double Count
        {
            get
            {
                return values?.Count ?? 0;
            }
        }

        public double this[int index]
        {
            get
            {
                if (values is null || index >= values.Count)
                {
                    return double.NaN;
                }

                return values[index];
            }

            set
            {
                if (values is null || index >= values.Count)
                {
                    return;
                }

                values[index] = value;
            }
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

            if (jsonObject.ContainsKey("Name"))
            {
                name = jsonObject["Name"]?.GetValue<string>();
            }

            if (jsonObject["Values"] is JsonArray valuesArray)
            {
                values = [];
                foreach (JsonNode node in valuesArray)
                {
                    if (node is JsonValue jsonValue && jsonValue.TryGetValue<double>(out double value))
                    {
                        values.Add(value);
                    }
                }
            }

            return true;
        }

        public FunctionType GetFunctionType()
        {
            string name_Temp = name?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(name_Temp))
            {
                return FunctionType.Undefined;
            }

            foreach (FunctionType functionType in Enum.GetValues(typeof(FunctionType)))
            {
                if (name_Temp.Equals(functionType.ToString()))
                {
                    return functionType;
                }
            }

            return FunctionType.Other;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
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

            if (name != null)
            {
                jsonObject["Name"] = name;
            }

            if (values != null)
            {
                JsonArray valuesArray = new JsonArray();
                foreach (double value in values)
                {
                    valuesArray.Add(value);
                }

                jsonObject["Values"] = valuesArray;
            }

            return jsonObject;
        }

        public override string ToString()
        {
            List<string> strings = [name ?? string.Empty];

            if (values != null)
            {
                foreach (double value in values)
                {
                    strings.Add(value.ToString());
                }
            }

            return string.Join(",", strings);
        }

        public override bool Equals(object obj)
        {

            return base.Equals(obj);
        }
    }
}
