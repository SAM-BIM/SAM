// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class PanelPanelTypeFilter : EnumFilter<PanelType>
    {

        public PanelPanelTypeFilter(PanelType panelType)
            : base()
        {
            Value = panelType;
        }

        public PanelPanelTypeFilter(PanelPanelTypeFilter panelPanelTypeFilter)
            : base(panelPanelTypeFilter)
        {

        }
        public PanelPanelTypeFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool TryGetEnum(IJSAMObject jSAMObject, out PanelType panelType)
        {
            panelType = PanelType.Undefined;

            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return false;
            }

            panelType = panel.PanelType;
            return true;
        }
    }
}
