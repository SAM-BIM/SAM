// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json; // Required for working with JSON
using System.Collections.Generic; // Required for using List<>
using System.Linq; // Required for using Enumerable.Repeat() and Enumerable.ToList()
using System.Text.Json.Nodes;
using System.Threading.Tasks; // Required for using Parallel.For()

namespace SAM.Core
{
    public static partial class Create
    {
        // This method converts a JArray into a List of IJSAMObjects of type T
        public static List<T> IJSAMObjects<T>(this JArray jArray) where T : IJSAMObject
        {
            return IJSAMObjects<T>(jArray?.Node as JsonArray);
        }

        public static List<T> IJSAMObjects<T>(this JsonArray jsonArray) where T : IJSAMObject
        {
            if (jsonArray == null)
            {
                return null;
            }

            // Initialize a List of type T with default values
            List<T> result = Enumerable.Repeat<T>(default, jsonArray.Count).ToList();

            // Process each element of the JsonArray in parallel
            Parallel.For(0, jsonArray.Count, (int i) =>
            {
                JsonObject jsonObject = jsonArray[i]?.DeepClone() as JsonObject;
                if (jsonObject == null)
                {
                    return;
                }

                // Convert the current element to an IJSAMObject of type T and store it in the result List
                result[i] = IJSAMObject<T>(jsonObject);

            });

            // Return the List of IJSAMObjects of type T
            return result;
        }

        // This method converts a JSON string into a List of IJSAMObjects of type T
        public static List<T> IJSAMObjects<T>(this string json) where T : IJSAMObject
        {
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            JsonNode jsonNode = JsonNode.Parse(json);

            JsonArray jsonArray = jsonNode as JsonArray;
            if (jsonArray == null)
            {
                return null;
            }

            return IJSAMObjects<T>(jsonArray);
        }
    }
}
