// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;

namespace SAM.Analytical
{
    public class PanelHasAperturesFilter : BooleanFilter, IBooleanFilter
    {
        public PanelHasAperturesFilter(bool value)
            : base(value)
        {

        }

        public PanelHasAperturesFilter(PanelHasAperturesFilter panelHasApertureFilter)
            : base(panelHasApertureFilter)
        {

        }
        public PanelHasAperturesFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool TryGetBoolean(IJSAMObject jSAMObject, out bool boolean)
        {
            boolean = false;

            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return false;
            }

            boolean = panel.HasApertures;

            return true;
        }
    }
}
