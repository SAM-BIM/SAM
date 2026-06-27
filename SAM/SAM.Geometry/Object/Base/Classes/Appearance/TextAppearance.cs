// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class TextAppearance : Appearance
    {
        public double Height { get; set; }

        public string FontFamilyName { get; set; }

        public TextAppearance(Color color, double height, string fontFamilyName)
            : base(color)
        {
            Height = height;
            FontFamilyName = fontFamilyName;
        }
        public TextAppearance(JsonObject jsonObject)
            : base(jsonObject)
        {

        }

        public TextAppearance(TextAppearance textAppearance)
            : base(textAppearance)
        {
            if (textAppearance != null)
            {
                Height = textAppearance.Height;
                FontFamilyName = textAppearance.FontFamilyName;
            }
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (!double.IsNaN(Height))
            {
                jsonObject["Height"] = Height;
            }

            if (!string.IsNullOrEmpty(FontFamilyName))
            {
                jsonObject["FontFamilyName"] = FontFamilyName;
            }

            return jsonObject;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Height"))
            {
                Height = jsonObject["Height"]?.GetValue<double>() ?? 0;
            }

            if (jsonObject.ContainsKey("FontFamilyName"))
            {
                FontFamilyName = jsonObject["FontFamilyName"]?.GetValue<string>();
            }

            return true;
        }
    }
}
