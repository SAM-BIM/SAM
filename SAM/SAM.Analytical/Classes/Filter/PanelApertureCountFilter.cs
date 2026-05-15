// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class PanelApertureCountFilter : NumberFilter
    {
        public PanelApertureCountFilter(NumberComparisonType numberComparisonType, double value)
            : base(numberComparisonType, value)
        {

        }

        public PanelApertureCountFilter(PanelApertureCountFilter panelApertureCountFilter)
            : base(panelApertureCountFilter)
        {

        }

        public PanelApertureCountFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public PanelApertureCountFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool TryGetNumber(IJSAMObject jSAMObject, out double number)
        {
            number = double.NaN;
            Panel panel = jSAMObject as Panel;
            if (panel == null)
            {
                return false;
            }

            List<Aperture> apertures = panel.Apertures;

            number = apertures == null ? 0 : apertures.Count;
            return true;
        }
    }
}
