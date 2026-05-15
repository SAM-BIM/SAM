// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class PanelPanelGroupFilter : EnumFilter<PanelGroup>
    {

        public PanelPanelGroupFilter(PanelGroup panelGroup)
            : base()
        {
            Value = panelGroup;
        }

        public PanelPanelGroupFilter(PanelPanelGroupFilter panelPanelGroupFilter)
            : base(panelPanelGroupFilter)
        {

        }

        public PanelPanelGroupFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public PanelPanelGroupFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool TryGetEnum(IJSAMObject jSAMObject, out PanelGroup panelGroup)
        {
            panelGroup = PanelGroup.Undefined;

            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return false;
            }

            panelGroup = panel.PanelGroup;
            return true;
        }
    }
}
