// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Triangle3DObject : Triangle3D, ITriangle3DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Triangle3D Triangle3D
        {
            get
            {
                return new Triangle3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Triangle3DObject(Triangle3D triangle3D)
            : base(triangle3D)
        {

        }

        public Triangle3DObject(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public Triangle3DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Triangle3DObject(Triangle3DObject triangle3DObject)
                : base(triangle3DObject)
        {
            if (triangle3DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(triangle3DObject?.CurveAppearance);
            }

            Tag = triangle3DObject?.Tag;
        }

        public Triangle3DObject(Triangle3D triangle3D, CurveAppearance curveAppearance)
            : base(triangle3D)
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
