// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class SurfaceSimulationResult : Result
    {
        public SurfaceSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public SurfaceSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public SurfaceSimulationResult(SurfaceSimulationResult surfaceSimulationResult)
            : base(surfaceSimulationResult)
        {

        }

        public SurfaceSimulationResult(Guid guid, SurfaceSimulationResult surfaceSimulationResult)
            : base(guid, surfaceSimulationResult)
        {

        }
        public SurfaceSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
