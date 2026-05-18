// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class SAMColor : IJSAMObject
    {
        private byte alpha;
        private byte blue;
        private byte green;
        private byte red;
        public SAMColor(Color color)
        {
            alpha = color.A;
            red = color.R;
            green = color.G;
            blue = color.B;
        }

        public SAMColor(SAMColor sAMColor)
        {
            alpha = sAMColor.alpha;
            red = sAMColor.red;
            green = sAMColor.Green;
            blue = sAMColor.blue;
        }

        public SAMColor(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public SAMColor(byte alpha, byte red, byte green, byte blue)
        {
            this.alpha = alpha;
            this.red = red;
            this.green = green;
            this.blue = blue;
        }

        public byte Alpha
        {
            get
            {
                return alpha;
            }
        }

        public byte Blue
        {
            get
            {
                return blue;
            }
        }

        public byte Green
        {
            get
            {
                return green;
            }
        }

        public string Name
        {
            get
            {
                Color color = ToColor();
                if (color.IsEmpty)
                    return null;

                if (!color.IsNamedColor)
                    return null;

                return color.Name;
            }
        }
        public byte Red
        {
            get
            {
                return red;
            }
        }
        
        public SAMColor Clone()
        {
            return new SAMColor(this);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            Color color = Color.Empty;
            if (jsonObject.ContainsKey("Name"))
                color = Convert.ToColor(jsonObject["Name"]?.GetValue<string>());

            if (color.Equals(Color.Empty))
            {
                alpha = jsonObject["Alpha"]?.GetValue<byte>() ?? 0;
                red = jsonObject["Red"]?.GetValue<byte>() ?? 0;
                green = jsonObject["Green"]?.GetValue<byte>() ?? 0;
                blue = jsonObject["Blue"]?.GetValue<byte>() ?? 0;
            }

            return true;
        }

        public Color ToColor()
        {
            return Color.FromArgb(alpha, red, green, blue);
        }
        
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            string name = Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                jsonObject["Alpha"] = alpha;
                jsonObject["Red"] = red;
                jsonObject["Green"] = green;
                jsonObject["Blue"] = blue;
            }
            else
            {
                jsonObject["Name"] = name;
            }

            return jsonObject;
        }

        public override string ToString()
        {
            return ToColor().Name;
        }
    }
}
