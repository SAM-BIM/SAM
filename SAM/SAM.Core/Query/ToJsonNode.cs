// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static bool IsIsoDateTime(string text)
        {
            if (text == null || text.Length < 19)
                return false;

            if (text[4] != '-' || text[7] != '-' || text[10] != 'T' || text[13] != ':' || text[16] != ':')
                return false;

            DateTime dateTime;
            return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dateTime);
        }

        public static JsonNode ToJsonNode(object value)
        {
            if (value == null)
                return null;

            if (value is JsonNode jsonNode)
                return jsonNode.DeepClone();

            if (value is double doubleValue)
                return JsonNode.Parse(FormatFloatingPoint(doubleValue));

            if (value is float floatValue)
                return JsonNode.Parse(FormatFloatingPoint(floatValue));

            if (value is decimal decimalValue)
                return JsonNode.Parse(FormatDecimal(decimalValue));

            return JsonSerializer.SerializeToNode(value);
        }

        public static object ToObject(this JsonNode jsonNode)
        {
            if (jsonNode == null)
                return null;

            if (jsonNode is JsonObject || jsonNode is JsonArray)
                return jsonNode.DeepClone();

            JsonValue jsonValue = jsonNode as JsonValue;
            if (jsonValue == null)
                return jsonNode.ToJsonString();

            bool boolValue;
            if (jsonValue.TryGetValue(out boolValue))
                return boolValue;

            int intValue;
            if (jsonValue.TryGetValue(out intValue))
                return intValue;

            long longValue;
            if (jsonValue.TryGetValue(out longValue))
                return longValue;

            double doubleValue;
            if (jsonValue.TryGetValue(out doubleValue))
                return doubleValue;

            string stringValue;
            if (jsonValue.TryGetValue(out stringValue))
                return stringValue;

            Guid guidValue;
            if (jsonValue.TryGetValue(out guidValue))
                return guidValue;

            DateTime dateTimeValue;
            if (jsonValue.TryGetValue(out dateTimeValue))
                return dateTimeValue;

            return jsonNode.ToJsonString();
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
    }
}
