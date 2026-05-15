// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Create
    {
        public static MaterialLibrary MaterialLibrary(this string path)
        {
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);

            JsonObject jsonObject = JsonNode.Parse(json) as JsonObject;
            if (jsonObject == null)
                return null;

            return new MaterialLibrary(jsonObject);
        }

        public static MaterialLibrary MaterialLibrary(string name, IEnumerable<IMaterial> materials)
        {
            MaterialLibrary result = new MaterialLibrary(name);

            if (materials == null)
                return result;

            foreach (IMaterial material in materials)
                result.Add(material);

            return result;
        }
    }
}
