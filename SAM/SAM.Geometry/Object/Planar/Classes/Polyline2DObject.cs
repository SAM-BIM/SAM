// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Planar;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Planar
{
    public class Polyline2DObject : Polyline2D, IPolyline2DObject, ITaggable, IBoundable2DObject
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Polyline2D Polyline2D
        {
            get
            {
                return new Polyline2D(this);
            }
        }

        public Tag Tag { get; set; }

        public Polyline2DObject(Polyline2D polyline2D)
            : base(polyline2D)
        {

        }

        public Polyline2DObject(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Polyline2DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Polyline2DObject(Polyline2DObject polyline2DObject)
                : base(polyline2DObject)
        {
            if (polyline2DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(polyline2DObject?.CurveAppearance);
            }

            Tag = polyline2DObject?.Tag;
        }

        public Polyline2DObject(Polyline2D polyline2D, CurveAppearance curveAppearance)
            : base(polyline2D)
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
