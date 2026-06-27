// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.IO;
using System.Net;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static string LatestVersion()
        {
            string url = @"https://api.github.com/repos/HoareLea/SAM_Deploy/releases/latest";

            HttpWebRequest httpWebRequest = WebRequest.CreateHttp(url);
            if (httpWebRequest == null)
                return null;

            string userAgent = "SAM";

            string currentVersion = CurrentVersion();

            if (!string.IsNullOrWhiteSpace(currentVersion))
                userAgent = string.Format("{0}({1})", userAgent, currentVersion);

            httpWebRequest.Method = "GET";
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.UserAgent = userAgent;

            string json = null;

            using (WebResponse webResponse = httpWebRequest.GetResponse())
            {
                Stream stream = webResponse.GetResponseStream();
                if (stream != null)
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        json = streamReader.ReadToEnd();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(json))
                return null;

            JsonObject jsonObject = JsonNode.Parse(json) as JsonObject;
            if (jsonObject == null)
                return null;

            return jsonObject["tag_name"]?.GetValue<string>();
        }
    }
}
