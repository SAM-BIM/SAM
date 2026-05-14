// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Segment3DObject : Segment3D, ISegment3DObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }

        public Segment3D Segment3D
        {
            get
            {
                return new Segment3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Segment3DObject(Segment3D segment3D)
            : base(segment3D)
        {

        }

        public Segment3DObject(Segment3D segment3D, CurveAppearance curveAppearance)
            : base(segment3D)
        {
            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }
        }

        public Segment3DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Segment3DObject(Segment3DObject segment3DObject)
            : base(segment3DObject)
        {
            if (segment3DObject.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(segment3DObject.CurveAppearance);
            }

            Tag = segment3DObject?.Tag;
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
