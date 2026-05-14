// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Polygon3DObject : Polygon3D, IPolygon3DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Polygon3D Polygon3D
        {
            get
            {
                return new Polygon3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Polygon3DObject(Polygon3D polygon3D)
            : base(polygon3D)
        {

        }

        public Polygon3DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Polygon3DObject(Polygon3DObject polygon3DObject)
                : base(polygon3DObject)
        {
            if (polygon3DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(polygon3DObject?.CurveAppearance);
            }

            Tag = polygon3DObject?.Tag;
        }

        public Polygon3DObject(Polygon3D polygon3D, CurveAppearance curveAppearance)
            : base(polygon3D)
        {
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["CurveAppearance"] is JsonObject jsonObject_CurveAppearance)
            {
                CurveAppearance = new CurveAppearance(new JObject((JsonObject)jsonObject_CurveAppearance.DeepClone()));
            }

            Tag = Core.Query.Tag(new JObject(jsonObject));

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (CurveAppearance?.ToJObject()?.Node is JsonObject curveJson)
            {
                jsonObject["CurveAppearance"] = curveJson.DeepClone();
            }

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
