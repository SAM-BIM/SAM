// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class SAMJsonCollection<T> : Collection<T> where T : IJSAMObject
    {
        public SAMJsonCollection()
        {

        }

        public SAMJsonCollection(string path)
        {
            FromJson(path);
        }

        public SAMJsonCollection(T t)
        {
            Add(t);
        }

        public SAMJsonCollection(IEnumerable<T> ts)
        {
            if (ts == null)
                return;

            foreach (T t in ts)
                Add(t);
        }

        public bool FromJson(string path)
        {
            JsonNode jsonNode = JsonNode.Parse(File.ReadAllText(path));
            JsonArray jsonArray = jsonNode as JsonArray;
            if (jsonArray == null && jsonNode is JsonObject jsonObject)
                jsonArray = new JsonArray(jsonObject);

            return FromJsonArray(jsonArray);
        }

        public bool ToJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            File.WriteAllText(path, ToJsonArray().ToJsonString());
            return true;
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jsonArray = new JsonArray();
            foreach (IJSAMObject jSAMObject in this)
            {
                if (jSAMObject?.ToJsonObject() is JsonObject jsonObject)
                    jsonArray.Add(jsonObject.DeepClone());
            }

            return jsonArray;
        }

        public bool FromJsonArray(JsonArray jsonArray)
        {
            if (jsonArray == null)
                return false;

            foreach (JsonNode jsonNode in jsonArray)
            {
                if (jsonNode is JsonObject jsonObject)
                    Add(Create.IJSAMObject<T>(jsonObject));
            }

            return true;
        }
    }
}
