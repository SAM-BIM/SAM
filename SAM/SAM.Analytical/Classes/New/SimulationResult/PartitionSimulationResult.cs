// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class PartitionSimulationResult : Result, IAnalyticalObject
    {
        public PartitionSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public PartitionSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public PartitionSimulationResult(PartitionSimulationResult partitionSimulationResult)
            : base(partitionSimulationResult)
        {

        }

        public PartitionSimulationResult(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public PartitionSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
