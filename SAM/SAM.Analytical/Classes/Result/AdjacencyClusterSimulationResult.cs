// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class AdjacencyClusterSimulationResult : Result
    {
        public AdjacencyClusterSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public AdjacencyClusterSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public AdjacencyClusterSimulationResult(AdjacencyClusterSimulationResult adjacencyClusterSimulationResult)
            : base(adjacencyClusterSimulationResult)
        {

        }

        public AdjacencyClusterSimulationResult(Guid guid, AdjacencyClusterSimulationResult adjacencyClusterSimulationResult)
            : base(guid, adjacencyClusterSimulationResult)
        {

        }
        public AdjacencyClusterSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
