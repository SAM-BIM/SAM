// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class CurveAppearance : PointAppearance
    {
        public CurveAppearance(Color color, double thickness)
            : base(color, thickness)
        {
        }

        public CurveAppearance(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }

        public CurveAppearance(JsonObject jsonObject)
            : base(jsonObject)
        {

        }

        public CurveAppearance(CurveAppearance curveAppearance)
            : base(curveAppearance)
        {

        }
    }
}
