// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class ZoneSimulationResult : Result, IAnalyticalObject
    {
        public ZoneSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public ZoneSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public ZoneSimulationResult(SpaceSimulationResult spaceSimulationResult)
            : base(spaceSimulationResult)
        {

        }
        public ZoneSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
