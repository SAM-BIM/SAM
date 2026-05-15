// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class Zone : Group, IAnalyticalObject
    {
        public Zone(string name)
            : base(name)
        {

        }

        public Zone(Guid guid, string name)
            : base(guid, name)
        {

        }

        public Zone(Zone zone, string name)
            : base(zone, name)
        {

        }

        public Zone(Zone zone)
            : base(zone)
        {

        }

        public Zone(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public Zone(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
