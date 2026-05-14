// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public abstract class Appearance : IAppearance
    {
        public Color Color { get; set; }

        public double Opacity { get; set; } = 1;

        public bool Visible { get; set; } = true;

        public Appearance(Color color)
        {
            Color = color;
        }

        public Appearance(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Appearance(Appearance appearance)
        {
            if (appearance != null)
            {
                Color = appearance.Color;
                Opacity = appearance.Opacity;
                Visible = appearance.Visible;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Color"] is JsonObject jsonObject_Color)
            {
                Core.SAMColor sAMColor = new Core.SAMColor(new JObject((JsonObject)jsonObject_Color.DeepClone()));
                if (sAMColor != null)
                {
                    Color = Color.FromArgb(sAMColor.Alpha, sAMColor.Red, sAMColor.Green, sAMColor.Blue);
                }
            }

            if (jsonObject.ContainsKey("Opacity"))
            {
                Opacity = jsonObject["Opacity"]?.GetValue<double>() ?? 0;
            }

            if (jsonObject.ContainsKey("Visible"))
            {
                Visible = jsonObject["Visible"]?.GetValue<bool>() ?? false;
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (new Core.SAMColor(Color.A, Color.R, Color.G, Color.B).ToJObject()?.Node is JsonObject colorJson)
                jsonObject["Color"] = colorJson.DeepClone();

            if (!double.IsNaN(Opacity))
            {
                jsonObject["Opacity"] = Opacity;
            }

            jsonObject["Visible"] = Visible;

            return jsonObject;
        }
    }
}
