// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class SpaceZonesFilter : MultiRelationAdjacencyClusterFilter<Zone>
    {

        public SpaceZonesFilter(IFilter filter)
        : base(filter)
        {

        }

        public SpaceZonesFilter(SpaceZonesFilter spaceZonesFilter)
            : base(spaceZonesFilter)
        {

        }

        public SpaceZonesFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public SpaceZonesFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override List<Zone> GetRelatives(IJSAMObject jSAMObject)
        {
            Space space = (jSAMObject as Space);
            if (space == null)
            {
                return null;
            }

            return AdjacencyCluster.GetZones(space);
        }
    }
}
