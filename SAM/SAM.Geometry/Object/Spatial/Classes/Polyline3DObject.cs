// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Polyline3DObject : Polyline3D, IPolyline3DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Polyline3D Polyline3D
        {
            get
            {
                return new Polyline3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Polyline3DObject(Polyline3D polyline3D)
            : base(polyline3D)
        {

        }

        public Polyline3DObject(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Polyline3DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Polyline3DObject(Polyline3DObject polyline3DObject)
                : base(polyline3DObject)
        {
            if (polyline3DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(polyline3DObject?.CurveAppearance);
            }

            Tag = polyline3DObject?.Tag;
        }

        public Polyline3DObject(Polyline3D polyline3D, CurveAppearance curveAppearance)
            : base(polyline3D)
        {
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
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

            Tag = Core.Query.Tag(jsonObject);

            return true;
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

            Core.Modify.Add(jsonObject, Tag);

            return jsonObject;
        }
    }
}
