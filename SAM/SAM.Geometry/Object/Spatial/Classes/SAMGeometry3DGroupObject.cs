// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class SAMGeometry3DGroupObject : SAMGeometry3DGroup, ISAMGeometry3DGroupObject, ITaggable
    {
        public CurveAppearance CurveAppearance { get; set; }
        public SurfaceAppearance SurfaceAppearance { get; set; }
        public PointAppearance PointAppearance { get; set; }

        public SAMGeometry3DGroup SAMGeometry3DGroup
        {
            get
            {
                return new SAMGeometry3DGroup(this);
            }
        }

        public Tag Tag { get; set; }

        public SAMGeometry3DGroupObject(SAMGeometry3DGroup sAMGeometry3DGroup)
            : base(sAMGeometry3DGroup)
        {

        }

        public SAMGeometry3DGroupObject(JObject jObject)
            : base(jObject)
        {

        }

        public SAMGeometry3DGroupObject(SAMGeometry3DGroupObject sAMGeometry3DGroupObject)
                : base(sAMGeometry3DGroupObject)
        {
            if (sAMGeometry3DGroupObject?.CurveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(sAMGeometry3DGroupObject?.CurveAppearance);
            }

            if (sAMGeometry3DGroupObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(sAMGeometry3DGroupObject?.SurfaceAppearance);
            }

            if (sAMGeometry3DGroupObject?.PointAppearance != null)
            {
                PointAppearance = new PointAppearance(sAMGeometry3DGroupObject?.PointAppearance);
            }

            Tag = sAMGeometry3DGroupObject?.Tag;
        }

        public SAMGeometry3DGroupObject(SAMGeometry3DGroup sAMGeometry3DGroup, PointAppearance pointAppearance, CurveAppearance curveAppearance, SurfaceAppearance surfaceAppearance)
            : base(sAMGeometry3DGroup)
        {
            if (pointAppearance != null)
            {
                PointAppearance = new PointAppearance(pointAppearance);
            }

            if (curveAppearance != null)
            {
                CurveAppearance = new CurveAppearance(curveAppearance);
            }

            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
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

            if (jsonObject["PointAppearance"] is JsonObject jsonObject_PointAppearance)
            {
                PointAppearance = new PointAppearance(new JObject((JsonObject)jsonObject_PointAppearance.DeepClone()));
            }

            if (jsonObject["SurfaceAppearance"] is JsonObject jsonObject_SurfaceAppearance)
            {
                SurfaceAppearance = new SurfaceAppearance(new JObject((JsonObject)jsonObject_SurfaceAppearance.DeepClone()));
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

            if (SurfaceAppearance?.ToJObject()?.Node is JsonObject surfaceJson)
            {
                jsonObject["SurfaceAppearance"] = surfaceJson.DeepClone();
            }

            if (PointAppearance?.ToJObject()?.Node is JsonObject pointJson)
            {
                jsonObject["PointAppearance"] = pointJson.DeepClone();
            }

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
