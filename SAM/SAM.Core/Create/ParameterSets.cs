// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Create
    {
        public static List<ParameterSet> ParameterSets(this JsonArray jsonArray)
        {
            if (jsonArray == null)
                return null;

            List<ParameterSet> result = new List<ParameterSet>();

            foreach (JsonNode jsonNode in jsonArray)
                result.Add(new ParameterSet(jsonNode as JsonObject));

            return result;
        }
    }
}
