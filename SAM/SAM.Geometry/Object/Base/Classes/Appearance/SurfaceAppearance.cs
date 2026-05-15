// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class SurfaceAppearance : Appearance
    {
        public CurveAppearance CurveAppearance { get; set; }

        public SurfaceAppearance(Color surfaceColor, Color curveColor, double curveThickness)
            : base(surfaceColor)
        {
            CurveAppearance = new CurveAppearance(curveColor, curveThickness);
        }

        public SurfaceAppearance(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }

        public SurfaceAppearance(JsonObject jsonObject)
            : base(jsonObject)
        {

        }

        public SurfaceAppearance(SurfaceAppearance surfaceAppearance)
            : base(surfaceAppearance)
        {
            CurveAppearance curveAppearance = surfaceAppearance?.CurveAppearance;
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (CurveAppearance?.ToJsonObject() is JsonObject curveJson)
            {
                jsonObject["CurveAppearance"] = curveJson.DeepClone();
            }

            return jsonObject;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["CurveAppearance"] is JsonObject jsonObject_CurveAppearance)
            {
                CurveAppearance = new CurveAppearance((JsonObject)jsonObject_CurveAppearance.DeepClone());
            }

            return true;
        }
    }
}
