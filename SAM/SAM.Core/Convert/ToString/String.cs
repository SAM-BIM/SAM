// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace SAM.Core
{
    public static partial class Convert
    {
        public static string ToString(this IJSAMObject jSAMObject)
        {
            return ToString(jSAMObject, Formatting.Indented);
        }

        public static string ToString(this IJSAMObject jSAMObject, Formatting formatting)
        {
            if (jSAMObject == null)
                return null;

            JsonObject jsonObject = jSAMObject.ToJsonObject();
            if (jsonObject == null)
                return null;

            return jsonObject.ToJsonString(SerializerOptions(formatting));
        }

        public static string ToString<T>(this IEnumerable<T> jSAMObjects) where T : IJSAMObject
        {
            return ToString(jSAMObjects, Formatting.Indented);
        }

        public static string ToString<T>(this IEnumerable<T> jSAMObjects, Formatting formatting) where T : IJSAMObject
        {
            if (jSAMObjects == null)
                return null;

            JsonArray jsonArray = new JsonArray();
            foreach (T jSAMObject in jSAMObjects)
            {
                if (jSAMObject == null)
                {
                    continue;
                }

                JsonObject jsonObject = jSAMObject.ToJsonObject();
                if (jsonObject == null)
                {
                    continue;
                }

                // DeepClone detaches the node from the freshly-built JObject
                // wrapper, so adding it to jsonArray doesn't violate the
                // single-parent invariant on JsonNode.
                jsonArray.Add(jsonObject.DeepClone());
            }

            return jsonArray.ToJsonString(SerializerOptions(formatting));
        }

        private static readonly IJsonTypeInfoResolver typeInfoResolver = new DefaultJsonTypeInfoResolver();

        public static JsonSerializerOptions SerializerOptions(Formatting formatting)
        {
            return new JsonSerializerOptions
            {
                WriteIndented = formatting == Formatting.Indented,
                TypeInfoResolver = typeInfoResolver
            };
        }

        public static string ToString<T>(IEnumerable<T> sAMObjects, string path) where T : IJSAMObject
        {
            if (sAMObjects == null)
                return null;

            string @string = ToString(sAMObjects.Cast<IJSAMObject>());

            File.WriteAllText(path, @string);

            return @string;
        }

        public static string ToString(Color color)
        {
            return color.Name;
        }

        public static string ToString(double value, string prefix)
        {
            if (value < 0)
                return string.Format("-{0}{1}", prefix, System.Math.Abs(value));

            return "+" + prefix + value.ToString();
        }
    }
}
