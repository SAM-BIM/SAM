// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Planar;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Planar
{
    public class Polygon2DObject : Polygon2D, IPolygon2DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Polygon2D Polygon2D
        {
            get
            {
                return new Polygon2D(this);
            }
        }

        public Tag Tag { get; set; }

        public Polygon2DObject(Polygon2D polygon2D)
            : base(polygon2D)
        {

        }

        public Polygon2DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Polygon2DObject(Polygon2DObject polygon2DObject)
                : base(polygon2DObject)
        {
            if (polygon2DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(polygon2DObject?.CurveAppearance);
            }

            Tag = polygon2DObject?.Tag;
        }

        public Polygon2DObject(Polygon2D polygon2D, CurveAppearance curveAppearance)
            : base(polygon2D)
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

            Tag = Core.Query.Tag(new JObject(jsonObject));

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

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
