// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static bool TryGetJToken(this string @string, out JToken jToken)
        {
            jToken = null;

            if (string.IsNullOrWhiteSpace(@string))
                return false;

            try
            {
                jToken = JToken.Wrap(JsonNode.Parse(@string));
                return true;
            }
            catch
            {

            }

            return false;
        }
    }
}
