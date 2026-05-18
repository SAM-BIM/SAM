// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class PointAppearance : Appearance
    {
        public double Thickness { get; set; }

        public PointAppearance(Color color, double thickness)
            : base(color)
        {
            Thickness = thickness;
        }
        public PointAppearance(JsonObject jsonObject)
            : base(jsonObject)
        {

        }

        public PointAppearance(PointAppearance pointAppearance)
            : base(pointAppearance)
        {
            if (pointAppearance != null)
            {
                Thickness = pointAppearance.Thickness;
            }
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (!double.IsNaN(Thickness))
            {
                jsonObject["Thickness"] = Thickness;
            }

            return jsonObject;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Thickness"))
            {
                Thickness = jsonObject["Thickness"]?.GetValue<double>() ?? 0;
            }

            return true;
        }
    }
}
