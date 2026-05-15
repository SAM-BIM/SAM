// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class BoundingBox3DObject : BoundingBox3D, IBoundingBox3DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public BoundingBox3D BoundingBox3D
        {
            get
            {
                return new BoundingBox3D(this);
            }
        }

        public Tag Tag { get; set; }

        public BoundingBox3DObject(BoundingBox3D boundingBox3D)
            : base(boundingBox3D)
        {

        }
        public BoundingBox3DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public BoundingBox3DObject(BoundingBox3DObject boundingBox3DObject)
                : base(boundingBox3DObject)
        {
            if (boundingBox3DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(boundingBox3DObject?.CurveAppearance);
            }

            Tag = boundingBox3DObject?.Tag;
        }

        public BoundingBox3DObject(BoundingBox3D boundingBox3D, CurveAppearance curveAppearance)
            : base(boundingBox3D)
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
