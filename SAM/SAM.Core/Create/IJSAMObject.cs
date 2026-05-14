// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Create
    {
        public static IJSAMObject IJSAMObject(this JObject jObject)
        {
            if (jObject == null)
            {
                return null;
            }

            JSAMObjectWrapper jSAMObjectWrapper = new JSAMObjectWrapper(jObject);
            IJSAMObject jSAMObject = jSAMObjectWrapper.ToIJSAMObject();
            if (jSAMObject == null)
            {
                return jSAMObjectWrapper;
            }

            return jSAMObject;
        }

        public static T IJSAMObject<T>(this JObject jObject) where T : IJSAMObject
        {
            IJSAMObject jSAMObject = IJSAMObject(jObject);
            if (jSAMObject == null)
            {
                return default;
            }

            if (jSAMObject is T)
            {
                return (T)jSAMObject;
            }

            return default;
        }

        public static T IJSAMObject<T>(this string json) where T : IJSAMObject
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            JsonNode jsonNode = JsonNode.Parse(json);

            JsonObject jsonObject = jsonNode as JsonObject;
            if (jsonObject == null)
            {
                if (jsonNode is JsonArray jsonArray && jsonArray.Count > 0)
                {
                    // DeepClone detaches the first element from its parent
                    // array so it can be wrapped into a fresh JObject without
                    // tripping JsonNode's single-parent invariant.
                    jsonObject = jsonArray[0]?.DeepClone() as JsonObject;
                }
            }

            if (jsonObject == null)
            {
                return default;
            }

            return IJSAMObject<T>(new JObject(jsonObject));
        }

        public static IJSAMObject IJSAMObject(this string json)
        {
            return IJSAMObject<IJSAMObject>(json);
        }

        public static IJSAMObject IJSAMObject(this ZipArchiveEntry zipArchiveEntry)
        {
            if (zipArchiveEntry == null)
            {
                return null;
            }

            IJSAMObject result = null;
            using (StreamReader streamReader = new StreamReader(zipArchiveEntry.Open()))
            {
                result = IJSAMObject(streamReader.ReadToEnd());
            }

            return result;
        }

        public static T IJSAMObject<T>(this ZipArchiveEntry zipArchiveEntry) where T : IJSAMObject
        {
            IJSAMObject jSAMObject = IJSAMObject(zipArchiveEntry);
            if (jSAMObject is T)
            {
                return (T)jSAMObject;
            }

            return default;
        }
    }
}
