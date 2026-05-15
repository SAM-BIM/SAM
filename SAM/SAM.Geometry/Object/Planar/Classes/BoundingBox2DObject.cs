// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Planar;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Planar
{
    public class BoundingBox2DObject : BoundingBox2D, IBoundingBox2DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public BoundingBox2D BoundingBox2D
        {
            get
            {
                return new BoundingBox2D(this);
            }
        }

        public Tag Tag { get; set; }

        public BoundingBox2DObject(BoundingBox2D boundingBox2D)
            : base(boundingBox2D)
        {

        }
        public BoundingBox2DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public BoundingBox2DObject(BoundingBox2DObject boundingBox2DObject)
                : base(boundingBox2DObject)
        {
            if (boundingBox2DObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(boundingBox2DObject?.CurveAppearance);
            }

            Tag = boundingBox2DObject?.Tag;
        }

        public BoundingBox2DObject(BoundingBox2D boundingBox2D, CurveAppearance curveAppearance)
            : base(boundingBox2D)
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
