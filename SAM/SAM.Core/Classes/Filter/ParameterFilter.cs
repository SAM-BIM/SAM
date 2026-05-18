// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ParameterFilter : Filter
    {
        private Enum @enum;
        public ParameterFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ParameterFilter(string name, string value, TextComparisonType textComparisonType)
        {
            Name = name;
            Value = value;
            @enum = textComparisonType;
        }

        public ParameterFilter(string name, double value, NumberComparisonType numberComparisonType)
        {
            Name = name;
            Value = value;
            @enum = numberComparisonType;
        }

        public string Name { get; set; }

        public NumberComparisonType? NumberComparisonType
        {
            get
            {
                if (@enum is NumberComparisonType)
                {
                    return (NumberComparisonType)@enum;
                }

                return null;
            }

            set
            {
                if (value == null || !value.HasValue)
                {
                    return;
                }

                @enum = value.Value;
            }
        }

        public TextComparisonType? TextComparisonType
        {
            get
            {
                if (@enum is TextComparisonType)
                {
                    return (TextComparisonType)@enum;
                }

                return null;
            }

            set
            {
                if (value == null || !value.HasValue)
                {
                    return;
                }

                @enum = value.Value;
            }
        }

        public object Value { get; set; }
        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Name"))
            {
                Name = jsonObject["Name"]?.GetValue<string>();
            }

            JsonNode valueNode = jsonObject["Value"];
            if (valueNode != null)
            {
                Value = ReadValue(valueNode);
            }

            if (jsonObject.ContainsKey("Enum"))
            {
                string text = jsonObject["Enum"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Enum enum_Text = null;
                    if (Query.TryGetEnum(text, out TextComparisonType textComparisonType))
                    {
                        enum_Text = textComparisonType;
                    }

                    Enum enum_Number = null;
                    if (Query.TryGetEnum(text, out NumberComparisonType numberComparisonType))
                    {
                        enum_Number = numberComparisonType;
                    }

                    if (enum_Text != null || enum_Number != null)
                    {
                        if (enum_Text != null && enum_Number != null)
                        {
                            if (Query.IsNumeric(Value))
                            {
                                @enum = enum_Number;
                            }
                            else
                            {
                                @enum = enum_Text;
                            }
                        }
                        else if (enum_Text != null)
                        {
                            @enum = enum_Text;
                        }
                        else
                        {
                            @enum = enum_Number;
                        }
                    }
                }
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (!Query.TryGetValue(jSAMObject, Name, out object value))
            {
                return false;
            }

            bool result = false;

            TextComparisonType? textComparisonType = TextComparisonType;
            if (textComparisonType == null || !textComparisonType.HasValue)
            {
                NumberComparisonType? numberComparisonType = NumberComparisonType;
                if (numberComparisonType == null || !numberComparisonType.HasValue)
                {
                    return false;
                }

                if (!Query.TryConvert(value, out double double_1))
                {
                    return false;
                }

                if (!Query.TryConvert(Value, out double double_2))
                {
                    return false;
                }

                result = Query.Compare(double_1, double_2, numberComparisonType.Value);
            }
            else
            {
                if (!Query.TryConvert(Value, out string string_1))
                {
                    return false;
                }

                if (!Query.TryConvert(value, out string string_2))
                {
                    return false;
                }

                result = Query.Compare(string_1, string_2, textComparisonType.Value);
            }

            if (Inverted)
            {
                result = !result;
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            if (Name != null)
            {
                result["Name"] = Name;
            }

            if (Value != null)
            {
                JsonNode valueNode = WriteValue(Value);
                if (valueNode != null)
                    result["Value"] = valueNode;
            }

            if (@enum != null)
            {
                result["Enum"] = @enum.ToString();
            }

            return result;
        }

        private static object ReadValue(JsonNode valueNode)
        {
            switch (valueNode.GetValueKind())
            {
                case JsonValueKind.String:
                    string stringValue = valueNode.GetValue<string>();
                    if (Query.IsIsoDateTime(stringValue))
                        return DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    return stringValue;

                case JsonValueKind.Number:
                    if (IsIntegerNumber(valueNode))
                        return valueNode.GetValue<int>();
                    return valueNode.GetValue<double>();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return valueNode.GetValue<bool>();

                case JsonValueKind.Object:
                    JSAMObjectWrapper wrapper = new JSAMObjectWrapper((JsonObject)valueNode.DeepClone());
                    IJSAMObject inner = wrapper.ToIJSAMObject();
                    return inner ?? (object)wrapper.ToJsonObject();

                case JsonValueKind.Array:
                    return valueNode.DeepClone();

                default:
                    return null;
            }
        }

        private static JsonNode WriteValue(object value)
        {
            switch (value)
            {
                case IJSAMObject jSAMObject:
                    JsonObject innerJson = jSAMObject.ToJsonObject();
                    return innerJson == null ? null : (JsonNode)innerJson.DeepClone();

                case JsonArray jsonArray:
                    return jsonArray.DeepClone();

                case JsonObject jsonObject:
                    return jsonObject.DeepClone();

                default:
                    return Query.ToJsonNode(value);
            }
        }

        private static bool IsIntegerNumber(JsonNode node)
        {
            if (!(node is JsonValue jsonValue) || !jsonValue.TryGetValue(out JsonElement element))
                return false;

            string raw = element.GetRawText();
            return raw.IndexOf('.') < 0 && raw.IndexOf('e') < 0 && raw.IndexOf('E') < 0;
        }
    }
}
