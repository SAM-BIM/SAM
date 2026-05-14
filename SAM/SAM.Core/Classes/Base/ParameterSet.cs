// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ParameterSet : ISAMObject
    {
        private string name;
        private Guid guid;
        private Dictionary<string, object> dictionary;

        public ParameterSet(ParameterSet parameterSet)
        {
            name = parameterSet.name;
            guid = parameterSet.guid;

            if (parameterSet.dictionary != null)
            {
                dictionary = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> keyValuePair in parameterSet.dictionary)
                {
                    object @object = keyValuePair.Value;
                    if (@object is IJSAMObject)
                    {
                        object @object_Temp = ((IJSAMObject)@object).Clone();
                        if (object_Temp != null)
                        {
                            @object = @object_Temp;
                        }
                    }

                    dictionary[keyValuePair.Key] = @object;
                }
            }
        }

        public ParameterSet(string name)
        {
            this.name = name;
            dictionary = new Dictionary<string, object>();
        }

        public ParameterSet(Guid guid, string name)
        {
            this.name = name;
            this.guid = guid;
            dictionary = new Dictionary<string, object>();
        }

        public ParameterSet(Assembly assembly)
        {
            name = Query.Name(assembly);
            guid = Query.Guid(assembly);
            dictionary = new Dictionary<string, object>();
        }

        public ParameterSet(JObject jObject)
        {
            FromJObject(jObject);
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public bool Add(string name, string value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, double value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, int value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, bool value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, IJSAMObject value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, JObject value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, DateTime value)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Add(string name, System.Drawing.Color color)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = new SAMColor(color);
            return true;
        }

        public bool Add(string name, SAMColor sAMColor)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = sAMColor;
            return true;
        }

        public bool Add(string name, JArray jArray)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = jArray;
            return true;
        }

        public bool Add(string name, Guid guid)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = guid;
            return true;
        }

        public bool Add(string name)
        {
            if (dictionary == null || name == null)
                return false;

            dictionary[name] = null;
            return true;
        }

        public bool Copy(ParameterSet parameterSet)
        {
            if (parameterSet == null)
                return false;

            if (dictionary == null)
                dictionary = new Dictionary<string, object>();

            foreach (KeyValuePair<string, object> keyValuePair in parameterSet.dictionary)
                dictionary[keyValuePair.Key] = keyValuePair.Value;

            return true;
        }

        public void Clear()
        {
            if (dictionary != null)
                dictionary.Clear();
        }

        public bool Remove(string name)
        {
            if (dictionary == null || name == null)
                return false;

            return dictionary.Remove(name);
        }

        public bool Modify(string name, string value)
        {
            if (dictionary == null || name == null || !dictionary.ContainsKey(name))
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Modify(string name, double value)
        {
            if (dictionary == null || name == null || !dictionary.ContainsKey(name))
                return false;

            dictionary[name] = value;
            return true;
        }

        public bool Contains(string name)
        {
            if (dictionary == null || name == null)
                return false;

            return dictionary.ContainsKey(name);
        }

        public string ToString(string name)
        {
            string result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return null;

            return result;
        }

        public double ToDouble(string name)
        {
            double result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return double.NaN;

            return result;
        }

        public int ToInt(string name)
        {
            int result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return int.MinValue;

            return result;
        }

        public bool ToBool(string name)
        {
            bool result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return false;

            return result;
        }

        public DateTime ToDateTime(string name)
        {
            DateTime result;
            if (Query.TryGetValue(dictionary, name, out result))
                return result;

            // JSON has no native date type, so a DateTime parameter round-trips
            // through the wire as an ISO string and lands back in the dictionary
            // as a string. Parse on demand so the typed accessor still works,
            // and strings that merely *look* like dates can keep their type.
            string text;
            if (!Query.TryGetValue(dictionary, name, out text))
                return DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(text)
                && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed))
            {
                return parsed;
            }

            return DateTime.MinValue;
        }

        public JObject ToJObject(string name)
        {
            JObject result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return null;

            return result;
        }

        public JArray ToJArray(string name)
        {
            JArray result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return null;

            return result;
        }

        public T ToSAMObject<T>(string name) where T : IJSAMObject
        {
            IJSAMObject result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return default(T);

            if (!(result is T))
                return default(T);

            return (T)(object)result;
        }

        public System.Drawing.Color ToColor(string name)
        {
            SAMColor sAMColor = ToSAMObject<SAMColor>(name);

            if (sAMColor == null)
                return System.Drawing.Color.Empty;

            return sAMColor.ToColor();
        }

        public object ToObject(string name)
        {
            object result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return null;

            return result;
        }

        public Type GetType(string name)
        {
            object result;
            if (!Query.TryGetValue(dictionary, name, out result))
                return null;

            if (result == null)
                return null;

            return result.GetType();
        }

        public IEnumerable<string> Names
        {
            get
            {
                if (dictionary == null)
                    return null;

                return dictionary.Keys;
            }
        }

        public ParameterSet Clone()
        {
            return new ParameterSet(this);
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            dictionary = new Dictionary<string, object>();

            name = Query.Name(jsonObject);
            guid = Query.Guid(jsonObject);

            if (!(jsonObject["Parameters"] is JsonArray parametersArray))
                return true;

            foreach (JsonNode parameterNode in parametersArray)
            {
                if (!(parameterNode is JsonObject parameterObject))
                    continue;

                string parameterName = parameterObject["Name"]?.GetValue<string>();
                if (parameterName == null)
                    continue;

                JsonNode valueNode = parameterObject["Value"];
                if (valueNode == null)
                    continue;

                dictionary[parameterName] = ReadParameterValue(valueNode);
            }

            return true;
        }

        private JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = GetType().FullName
            };

            if (name != null)
                jsonObject["Name"] = name;

            jsonObject["Guid"] = guid.ToString();

            JsonArray parametersArray = new JsonArray();
            foreach (KeyValuePair<string, object> keyValuePair in dictionary)
            {
                JsonObject parameterObject = new JsonObject
                {
                    ["Name"] = keyValuePair.Key
                };

                if (keyValuePair.Value != null)
                {
                    JsonNode valueNode = WriteParameterValue(keyValuePair.Value);
                    if (valueNode != null)
                        parameterObject["Value"] = valueNode;
                }

                parametersArray.Add(parameterObject);
            }

            if (parametersArray.Count != 0)
                jsonObject["Parameters"] = parametersArray;

            return jsonObject;
        }

        private static object ReadParameterValue(JsonNode valueNode)
        {
            switch (valueNode.GetValueKind())
            {
                case JsonValueKind.String:
                    // Stay as string. DateTime parameters were serialized as ISO
                    // text, but JSON has no native date type — promoting every
                    // ISO-shaped string to DateTime here would silently change
                    // the runtime type of any string parameter that happens to
                    // look like a date. ParameterSet.ToDateTime parses on demand
                    // instead, which preserves typed access without bleeding.
                    return valueNode.GetValue<string>();

                case JsonValueKind.Number:
                    if (IsIntegerNumber(valueNode))
                        return valueNode.GetValue<int>();
                    return valueNode.GetValue<double>();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return valueNode.GetValue<bool>();

                case JsonValueKind.Object:
                    JObject wrappedObject = new JObject((JsonObject)valueNode.DeepClone());
                    JSAMObjectWrapper wrapper = new JSAMObjectWrapper(wrappedObject);
                    IJSAMObject inner = wrapper.ToIJSAMObject();
                    return inner ?? (object)wrapper.ToJObject();

                case JsonValueKind.Array:
                    return new JArray((JsonArray)valueNode.DeepClone());

                default:
                    return null;
            }
        }

        private static JsonNode WriteParameterValue(object value)
        {
            switch (value)
            {
                case IJSAMObject jSAMObject:
                    JsonObject innerJson = jSAMObject.ToJObject()?.Node as JsonObject;
                    return innerJson == null ? null : (JsonNode)innerJson.DeepClone();

                case JArray shimArray:
                    return shimArray.Node?.DeepClone();

                case JObject shimObject:
                    return shimObject.Node?.DeepClone();

                default:
                    // Delegate primitive handling (string, bool, int, long,
                    // double, float, decimal, DateTime, Guid, enum, ...) to
                    // the shim's ToNode helper, which already emits doubles
                    // with explicit decimals so round-trip preserves Float.
                    return JToken.ToNode(value);
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
