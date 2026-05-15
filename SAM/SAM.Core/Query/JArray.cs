// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static JsonArray JsonArray<T>(this T[,] values)
        {
            if (values == null)
            {
                return null;
            }

            JsonArray result = new JsonArray();
            for (int i = 0; i < values.GetLength(0); i++)
            {
                JsonArray jsonArray = new JsonArray();
                for (int j = 0; j < values.GetLength(1); j++)
                {
                    jsonArray.Add(Query.ToJsonNode(values[i, j]));
                }

                result.Add(jsonArray);
            }

            return result;
        }
    }
}
