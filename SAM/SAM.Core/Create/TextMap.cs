// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Create
    {
        public static TextMap TextMap(string name)
        {
            return new TextMap(name);
        }

        public static TextMap TextMap(TextMap textMap)
        {
            if (textMap == null)
            {
                return null;
            }

            return new TextMap(textMap);
        }

        public static TextMap TextMap(string name, TextMap textMap)
        {
            if (textMap == null)
            {
                return null;
            }

            return new TextMap(name, textMap);
        }

        public static TextMap TextMap(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return null;
            }

            return new TextMap(jsonObject);
        }
    }
}
