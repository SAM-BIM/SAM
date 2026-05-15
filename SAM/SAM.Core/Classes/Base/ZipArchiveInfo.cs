// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ZipArchiveInfo : IJSAMObject
    {
        public static string EntryName { get; } = string.Format("_{0}", typeof(ZipArchiveInfo).Name);

        private HashSet<Guid> guids;

        internal ZipArchiveInfo()
        {
        }

        internal ZipArchiveInfo(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        internal ZipArchiveInfo(ZipArchiveInfo zipArchiveInfo)
        {
            guids = new HashSet<Guid>();
            foreach (Guid guid in zipArchiveInfo.guids)
                guids.Add(guid);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Guids"] is JsonArray jsonArray)
            {
                guids = new HashSet<Guid>();
                foreach (JsonNode node in jsonArray)
                {
                    Guid guid;
                    if (!Guid.TryParse(node?.GetValue<string>(), out guid))
                        continue;

                    guids.Add(guid);
                }
            }

            return true;
        }

        public Guid NewGuid()
        {
            Guid guid = Guid.NewGuid();

            if (guids == null)
                guids = new HashSet<Guid>();

            while (guids.Contains(guid))
                guid = Guid.NewGuid();

            guids.Add(guid);
            return guid;
        }

        public List<IJSAMObject> OrderedIJSAMObjects(ZipArchive zipArchive)
        {
            if (zipArchive == null || guids == null)
                return null;

            List<IJSAMObject> result = new List<IJSAMObject>();
            foreach (Guid guid in guids)
                result.Add(Create.IJSAMObject(zipArchive.GetEntry(guid.ToString())));

            return result;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (guids != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (Guid guid in guids)
                {
                    jsonArray.Add(guid.ToString());
                }

                jsonObject["Guids"] = jsonArray;
            }

            return jsonObject;
        }
    }
}
