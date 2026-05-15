// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class PanelConstructionFilter : RelationFilter<Construction>
    {
        public PanelConstructionFilter(IFilter filter)
            : base(filter)
        {

        }

        public PanelConstructionFilter(PanelConstructionFilter panelConstructionFilter)
            : base(panelConstructionFilter)
        {

        }
        public PanelConstructionFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override Construction GetRelative(IJSAMObject jSAMObject)
        {
            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return null;
            }

            return panel.Construction;
        }
    }
}
