// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Text.Json.Nodes;

namespace SAM.Architectural
{
    public class MaterialLayer : IJSAMObject, IArchitecturalObject
    {
        private string name;
        private double thickness;

        public MaterialLayer(string name, double thickness)
        {
            this.thickness = thickness;
            this.name = name;
        }

        public MaterialLayer(MaterialLayer materialLayer)
        {
            thickness = materialLayer.thickness;
            name = materialLayer.name;
        }

        public MaterialLayer(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public double Thickness
        {
            get
            {
                return thickness;
            }
        }
        public static bool operator !=(MaterialLayer materialLayer_1, MaterialLayer materialLayer_2)
        {
            return materialLayer_1?.name != materialLayer_2?.name || materialLayer_1?.thickness != materialLayer_2?.thickness;
        }

        public static bool operator ==(MaterialLayer materialLayer_1, MaterialLayer materialLayer_2)
        {
            return materialLayer_1?.name == materialLayer_2?.name && materialLayer_1?.thickness == materialLayer_2?.thickness;
        }

        public override bool Equals(object obj)
        {
            MaterialLayer materialLayer = obj as MaterialLayer;
            if (materialLayer == null)
            {
                return false;
            }

            return materialLayer.name == name && materialLayer.thickness == thickness;
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject.ContainsKey("Thickness"))
                thickness = jsonObject["Thickness"]?.GetValue<double>() ?? double.NaN;

            if (jsonObject.ContainsKey("Name"))
                name = jsonObject["Name"]?.GetValue<string>();

            return true;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(name, thickness).GetHashCode();
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (name != null)
                jsonObject["Name"] = name;

            if (!double.IsNaN(thickness))
                jsonObject["Thickness"] = thickness;

            return jsonObject;
        }
    }
}
