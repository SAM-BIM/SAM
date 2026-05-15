// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class SpaceSimulationResult : Result, IAnalyticalObject
    {
        public SpaceSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public SpaceSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public SpaceSimulationResult(SpaceSimulationResult spaceSimulationResult)
            : base(spaceSimulationResult)
        {

        }

        public SpaceSimulationResult(Guid guid, SpaceSimulationResult spaceSimulationResult)
            : base(guid, spaceSimulationResult)
        {

        }

        public SpaceSimulationResult(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public SpaceSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
