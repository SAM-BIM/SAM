// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static Tag Tag(this JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return null;
            }

            if (!(jsonObject["Tag"] is JsonObject jsonObject_Tag))
            {
                return null;
            }

            return new Tag(jsonObject_Tag);
        }
    }
}
