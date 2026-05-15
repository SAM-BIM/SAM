// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Text3DObject : IText3DObject, ITaggable
    {
        public Plane Plane { get; private set; }
        public string Text { get; private set; }

        public TextAppearance TextAppearance { get; set; }

        public Tag Tag { get; set; }

        public Text3DObject(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Text3DObject(Text3DObject text3DObject)
        {
            if (text3DObject != null)
            {
                if (text3DObject.TextAppearance != null)
                {
                    TextAppearance = new TextAppearance(text3DObject.TextAppearance);
                }

                Tag = text3DObject.Tag;

                Plane = text3DObject.Plane;

                Text = text3DObject.Text;
            }
        }

        public Text3DObject(string text)
        {
            Text = text;
            Plane = Plane.WorldXY;
        }

        public Text3DObject(string text, Plane plane, TextAppearance textAppearance)
        {
            Text = text;

            if (textAppearance != null)
            {
                TextAppearance = new TextAppearance(textAppearance);
            }

            if (plane != null)
            {
                Plane = new Plane(plane);
            }
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Text"))
            {
                Text = jsonObject["Text"]?.GetValue<string>();
            }

            if (jsonObject["TextAppearance"] is JsonObject textAppearanceJson)
            {
                TextAppearance = new TextAppearance((JsonObject)textAppearanceJson.DeepClone());
            }

            if (jsonObject["Plane"] is JsonObject planeJson)
            {
                Plane = new Plane((JsonObject)planeJson.DeepClone());
            }

            // Core.Query.Tag still takes JObject; the wrapper has no copy cost.
            Tag = Core.Query.Tag(jsonObject);

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (TextAppearance?.ToJsonObject() is JsonObject textAppearanceJson)
            {
                jsonObject["TextAppearance"] = textAppearanceJson.DeepClone();
            }

            if (Plane?.ToJsonObject() is JsonObject planeJson)
            {
                jsonObject["Plane"] = planeJson.DeepClone();
            }

            if (Text != null)
            {
                jsonObject["Text"] = Text;
            }

            // Core.Modify.Add takes JObject; the wrapper shares the same node
            // so mutations land directly on jsonObject.
            Core.Modify.Add(jsonObject, Tag);

            return jsonObject;
        }
    }
}
