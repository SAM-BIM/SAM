// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
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
    }
}
