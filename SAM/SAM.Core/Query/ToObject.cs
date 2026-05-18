// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
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
    }
}
