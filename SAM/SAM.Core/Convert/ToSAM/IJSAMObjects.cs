// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Convert
    {
        public static List<T> ToSAM<T>(string pathOrJson) where T : IJSAMObject
        {
            if (string.IsNullOrWhiteSpace(pathOrJson))
                return null;

            if (File.Exists(pathOrJson))
            {
                List<IJSAMObject> jSAMObjects = ToSAM(pathOrJson, Query.SAMFileType(pathOrJson));
                return jSAMObjects?.ConvertAll(x => x is T ? (T)x : default);
            }

            JsonArray jsonArray = JsonArray(pathOrJson);
            if (jsonArray == null)
                return null;

            return Create.IJSAMObjects<T>(jsonArray);
        }

        public static List<IJSAMObject> ToSAM(string pathOrJson)
        {
            return ToSAM<IJSAMObject>(pathOrJson);
        }

        public static List<IJSAMObject> ToSAM(string path, SAMFileType sAMFileType)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            switch (sAMFileType)
            {
                case SAMFileType.Json:
                    return ToSAM_FromJsonFile(path);
                case SAMFileType.SAM:
                    return ToSAM_FromSAMFile(path);
                default:
                    return ToSAM_FromJsonFile(path);
            }
        }

        private static List<IJSAMObject> ToSAM_FromJsonFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            JsonArray jsonArray = JsonArray(json);
            if (jsonArray == null)
                return null;

            return Create.IJSAMObjects<IJSAMObject>(jsonArray);
        }

        private static List<IJSAMObject> ToSAM_FromSAMFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            List<IJSAMObject> result = new List<IJSAMObject>();
            using (ZipArchive zipArchieve = ZipFile.OpenRead(path))
            {
                ZipArchiveInfo zipArchiveInfo = Create.ZipArchiveInfo(zipArchieve);
                if (zipArchiveInfo != null)
                    return zipArchiveInfo.OrderedIJSAMObjects(zipArchieve);

                foreach (ZipArchiveEntry zipArchiveEntry in zipArchieve.Entries)
                    result.Add(Create.IJSAMObject(zipArchiveEntry));
            }

            return result;

        }

        private static JsonArray JsonArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            JsonNode jsonNode;
            try { jsonNode = JsonNode.Parse(json); }
            catch { return null; }

            if (jsonNode is JsonArray jsonArray)
                return jsonArray;

            if (jsonNode is JsonObject jsonObject)
                return new JsonArray(jsonObject);

            return null;
        }
    }
}
