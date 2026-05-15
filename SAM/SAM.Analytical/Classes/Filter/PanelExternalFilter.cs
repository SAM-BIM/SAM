// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical
{
    public class PanelExternalFilter : BooleanFilter, IAdjacencyClusterFilter
    {
        public PanelExternalFilter(bool value)
            : base(value)
        {

        }

        public PanelExternalFilter(PanelExternalFilter panelExternalFilter)
            : base(panelExternalFilter)
        {

        }
        public PanelExternalFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public AdjacencyCluster AdjacencyCluster { get; set; }

        public override bool TryGetBoolean(IJSAMObject jSAMObject, out bool boolean)
        {
            boolean = false;
            if(AdjacencyCluster is null)
            {
                return false;
            }


            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return false;
            }

            boolean = AdjacencyCluster.External(panel);

            return true;
        }
    }
}
