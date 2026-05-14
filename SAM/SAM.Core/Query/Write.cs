// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core
{
    public static partial class Query
    {
        public static bool Write(this IJSAMObject jSAMObject, string path)
        {
            string json = jSAMObject?.ToJsonObject()?.ToJsonString();
            if (json == null)
                return false;

            System.IO.File.WriteAllText(path, json);
            return true;
        }
    }
}
