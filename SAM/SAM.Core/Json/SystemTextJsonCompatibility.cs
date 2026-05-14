// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

#nullable disable

namespace SAM.Core.Json
{
    public enum Formatting
    {
        None,
        Indented
    }

    public enum JTokenType
    {
        None,
        Object,
        Array,
        Constructor,
        Property,
        Comment,
        Integer,
        Float,
        String,
        Boolean,
        Null,
        Undefined,
        Date,
        Raw,
        Bytes,
        Guid,
        Uri,
        TimeSpan
    }

    public abstract class JToken : IEnumerable<JToken>
    {
        // Exposed so packages outside SAM.Core (SAM.Geometry, SAM.Analytical,
        // SAM.Architectural) can reach the underlying BCL node during their
        // migration to the JsonObject API. Setter stays private-protected.
        public JsonNode Node { get; private protected set; }

        internal JToken(JsonNode node)
        {
            Node = node;
        }

        public JTokenType Type
        {
            get
            {
                if (Node == null)
                    return JTokenType.Null;

                if (Node is JsonObject)
                    return JTokenType.Object;

                if (Node is JsonArray)
                    return JTokenType.Array;

                object value = ToValue();
                if (value == null)
                    return JTokenType.Null;

                if (value is bool)
                    return JTokenType.Boolean;

                if (value is Guid)
                    return JTokenType.Guid;

                if (value is DateTime)
                    return JTokenType.Date;

                if (value is string text)
                {
                    // Strict ISO 8601 date-time shape only. Without this gate,
                    // any string that DateTime.TryParse accepts (e.g. a name,
                    // a Guid, a version label) would be misclassified as Date
                    // and stored as DateTime downstream in ParameterSet.
                    if (IsIsoDateTime(text))
                        return JTokenType.Date;

                    return JTokenType.String;
                }

                if (IsInteger(value))
                    return JTokenType.Integer;

                if (IsNumber(value))
                    return JTokenType.Float;

                return JTokenType.String;
            }
        }

        internal static bool IsIsoDateTime(string text)
        {
            if (text == null || text.Length < 19)
                return false;

            if (text[4] != '-' || text[7] != '-' || text[10] != 'T' || text[13] != ':' || text[16] != ':')
                return false;

            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _);
        }

        public static JToken Parse(string json)
        {
            return Wrap(JsonNode.Parse(json));
        }

        public T Value<T>()
        {
            object value = ToValue();
            if (value == null)
                return default;

            Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (typeof(T).IsAssignableFrom(GetType()))
                return (T)(object)this;

            if (type == typeof(JToken))
                return (T)(object)this;

            if (type == typeof(JObject))
                return (T)(object)(this as JObject);

            if (type == typeof(JArray))
                return (T)(object)(this as JArray);

            if (type == typeof(object))
                return (T)value;

            if (type.IsEnum)
            {
                if (value is string enumText)
                    return (T)Enum.Parse(type, enumText, true);

                return (T)Enum.ToObject(type, System.Convert.ToInt32(value, CultureInfo.InvariantCulture));
            }

            if (type == typeof(Guid))
                return (T)(object)(value is Guid guid ? guid : Guid.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture)));

            if (type == typeof(DateTime))
                return (T)(object)(value is DateTime dateTime ? dateTime : DateTime.Parse(System.Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

            if (type == typeof(string))
                return (T)(object)System.Convert.ToString(value, CultureInfo.InvariantCulture);

            return (T)System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        public override string ToString()
        {
            return ToString(Formatting.None);
        }

        public string ToString(Formatting formatting)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = formatting == Formatting.Indented
            };

            return Node?.ToJsonString(options) ?? "null";
        }

        public JToken DeepClone()
        {
            return Wrap(Node?.DeepClone());
        }

        public static explicit operator double(JToken token)
        {
            return token.Value<double>();
        }

        public static explicit operator int(JToken token)
        {
            return token.Value<int>();
        }

        public static explicit operator string(JToken token)
        {
            return token.Value<string>();
        }

        public static explicit operator bool(JToken token)
        {
            return token.Value<bool>();
        }

        public IEnumerator<JToken> GetEnumerator()
        {
            if (Node is JsonArray jsonArray)
            {
                foreach (JsonNode node in jsonArray)
                    yield return Wrap(node);
            }
            else if (Node is JsonObject jsonObject)
            {
                foreach (KeyValuePair<string, JsonNode> keyValuePair in jsonObject)
                    yield return Wrap(keyValuePair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal static JToken Wrap(JsonNode node)
        {
            if (node is JsonObject jsonObject)
                return new JObject(jsonObject);

            if (node is JsonArray jsonArray)
                return new JArray(jsonArray);

            return new JValue(node);
        }

        internal static JsonNode ToNode(object value)
        {
            if (value == null)
                return null;

            if (value is JToken token)
                return token.Node?.DeepClone();

            if (value is JsonNode jsonNode)
                return jsonNode.DeepClone();

            // System.Text.Json's default writer strips trailing zeros from
            // doubles, so 5.0 round-trips as "5" and reads back as Integer,
            // silently changing the value's type. Emit with an explicit
            // decimal so the read side classifies it as Float.
            if (value is double doubleValue)
                return JsonNode.Parse(FormatFloatingPoint(doubleValue));

            if (value is float floatValue)
                return JsonNode.Parse(FormatFloatingPoint(floatValue));

            if (value is decimal decimalValue)
                return JsonNode.Parse(FormatDecimal(decimalValue));

            return JsonSerializer.SerializeToNode(value);
        }

        private static string FormatFloatingPoint(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "null";

            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (text.IndexOf('.') < 0 && text.IndexOf('e') < 0 && text.IndexOf('E') < 0)
                text += ".0";

            return text;
        }

        private static string FormatDecimal(decimal value)
        {
            string text = value.ToString(CultureInfo.InvariantCulture);
            if (text.IndexOf('.') < 0)
                text += ".0";

            return text;
        }

        internal object ToValue()
        {
            if (Node == null)
                return null;

            if (Node is JsonObject || Node is JsonArray)
                return this;

            if (Node is JsonValue jsonValue)
            {
                if (jsonValue.TryGetValue<bool>(out bool boolValue))
                    return boolValue;

                if (jsonValue.TryGetValue<int>(out int intValue))
                    return intValue;

                if (jsonValue.TryGetValue<long>(out long longValue))
                    return longValue;

                if (jsonValue.TryGetValue<double>(out double doubleValue))
                    return doubleValue;

                // <string> must be tried before <Guid>/<DateTime> so that JSON
                // strings are reported as strings; otherwise STJ's permissive
                // string-to-Guid/DateTime conversion would surface them as
                // typed values and Type would classify them as Guid/Date.
                if (jsonValue.TryGetValue<string>(out string stringValue))
                    return stringValue;

                if (jsonValue.TryGetValue<Guid>(out Guid guidValue))
                    return guidValue;

                if (jsonValue.TryGetValue<DateTime>(out DateTime dateTimeValue))
                    return dateTimeValue;
            }

            return Node.ToJsonString();
        }

        private static bool IsInteger(object value)
        {
            return value is sbyte || value is byte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong;
        }

        private static bool IsNumber(object value)
        {
            return IsInteger(value) || value is float || value is double || value is decimal;
        }
    }

    public sealed class JObject : JToken, IEnumerable<KeyValuePair<string, JToken>>
    {
        private JsonObject JsonObject => (JsonObject)Node;

        public JObject()
            : base(new JsonObject())
        {
        }

        // Public so cross-assembly migration code can wrap a JsonObject in the
        // legacy shim at IJSAMObject boundaries.
        public JObject(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public JToken this[string name]
        {
            get
            {
                if (name == null || !JsonObject.TryGetPropertyValue(name, out JsonNode node))
                    return null;

                return Wrap(node);
            }
            set
            {
                JsonObject[name] = ToNode(value);
            }
        }

        public static new JObject Parse(string json)
        {
            return Wrap(JsonNode.Parse(json)) as JObject;
        }

        public void Add(string name, object value)
        {
            JsonObject[name] = ToNode(value);
        }

        public bool ContainsKey(string name)
        {
            return JsonObject.ContainsKey(name);
        }

        public JToken GetValue(string name)
        {
            return this[name];
        }

        public T Value<T>(string name)
        {
            JToken token = this[name];
            return token == null ? default : token.Value<T>();
        }

        public IEnumerable<JProperty> Properties()
        {
            foreach (KeyValuePair<string, JsonNode> keyValuePair in JsonObject)
                yield return new JProperty(keyValuePair.Key, Wrap(keyValuePair.Value));
        }

        public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
        {
            foreach (KeyValuePair<string, JsonNode> keyValuePair in JsonObject)
                yield return new KeyValuePair<string, JToken>(keyValuePair.Key, Wrap(keyValuePair.Value));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public sealed class JArray : JToken, IEnumerable<JToken>
    {
        private JsonArray JsonArray => (JsonArray)Node;

        public JArray()
            : base(new JsonArray())
        {
        }

        public JArray(object content)
            : this()
        {
            AddContent(content);
        }

        internal JArray(JsonArray jsonArray)
            : base(jsonArray)
        {
        }

        public int Count => JsonArray.Count;

        public JToken this[int index]
        {
            get
            {
                JsonNode node = JsonArray[index];
                return Wrap(node);
            }
            set
            {
                JsonArray[index] = ToNode(value);
            }
        }

        public static new JArray Parse(string json)
        {
            return Wrap(JsonNode.Parse(json)) as JArray;
        }

        public void Add(object value)
        {
            JsonArray.Add(ToNode(value));
        }

        public IEnumerator<JToken> GetEnumerator()
        {
            foreach (JsonNode node in JsonArray)
                yield return Wrap(node);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void AddContent(object content)
        {
            if (content == null)
            {
                Add(null);
                return;
            }

            if (content is string || content is JToken)
            {
                Add(content);
                return;
            }

            if (content is IEnumerable enumerable)
            {
                foreach (object value in enumerable)
                    Add(value);

                return;
            }

            Add(content);
        }
    }

    public sealed class JValue : JToken
    {
        public JValue(object value)
            : base(ToNode(value))
        {
        }

        internal JValue(JsonNode node)
            : base(node)
        {
        }

        public object Value => ToValue();
    }

    public sealed class JProperty
    {
        public JProperty(string name, JToken value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public JToken Value { get; }
    }
}
