// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Planar;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Planar
{
    public class Segment2DObject : Segment2D, ISegment2DObject, ITaggable, IBoundable2DObject
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Segment2D Segment2D
        {
            get
            {
                return new Segment2D(this);
            }
        }

        public Tag Tag { get; set; }

        public Segment2DObject(Segment2D segment2D)
            : base(segment2D)
        {

        }

        public Segment2DObject(Segment2D segment2D, CurveAppearance curveAppearance)
            : base(segment2D)
        {
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
        }

        public Segment2DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Segment2DObject(Segment2DObject segment2DObject)
            : base(segment2DObject)
        {
            if (segment2DObject.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(segment2DObject.CurveAppearance);
            }

            Tag = segment2DObject?.Tag;
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
