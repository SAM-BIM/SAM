// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public interface IJSAMObject
    {
        bool FromJsonObject(JsonObject? jsonObject);

        JsonObject? ToJsonObject();
    }
}
