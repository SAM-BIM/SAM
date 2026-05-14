// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public abstract class MultiRelationAdjacencyClusterFilter<T> : MultiRelationFilter<T>, IAdjacencyClusterFilter where T : IJSAMObject
    {
        public AdjacencyCluster AdjacencyCluster { get; set; }

        public MultiRelationAdjacencyClusterFilter(IFilter filter)
        {
            Filter = filter;
        }

        public MultiRelationAdjacencyClusterFilter(JObject jObject)
            : base(jObject)
        {

        }

        public MultiRelationAdjacencyClusterFilter(MultiRelationAdjacencyClusterFilter<T> multiRelationAdjacencyClusterFilter)
            : base(multiRelationAdjacencyClusterFilter)
        {
            AdjacencyCluster = multiRelationAdjacencyClusterFilter?.AdjacencyCluster;
        }
    }
}
