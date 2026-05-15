// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class ExtrusionObject : Extrusion, IExtrusionObject, ITaggable
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Extrusion Extrusion
        {
            get
            {
                return new Extrusion(this);
            }
        }

        public Tag Tag { get; set; }

        public ExtrusionObject(Extrusion extrusion)
            : base(extrusion)
        {

        }

        public ExtrusionObject(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public ExtrusionObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ExtrusionObject(ExtrusionObject extrusionObject)
                : base(extrusionObject)
        {
            if (extrusionObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(extrusionObject?.SurfaceAppearance);
            }

            Tag = extrusionObject?.Tag;
        }

        public ExtrusionObject(Extrusion extrusion, SurfaceAppearance surfaceAppearance)
            : base(extrusion)
        {
            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public ExtrusionObject(Extrusion extrusion, System.Drawing.Color surfaceColor, System.Drawing.Color curveColor, double curveThickness)
            : base(extrusion)
        {
            SurfaceAppearance = new SurfaceAppearance(surfaceColor, curveColor, curveThickness);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["SurfaceAppearance"] is JsonObject jsonObject_SurfaceAppearance)
            {
                SurfaceAppearance = new SurfaceAppearance((JsonObject)jsonObject_SurfaceAppearance.DeepClone());
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

            if (SurfaceAppearance?.ToJsonObject() is JsonObject surfaceJson)
            {
                jsonObject["SurfaceAppearance"] = surfaceJson.DeepClone();
            }

            Core.Modify.Add(jsonObject, Tag);

            return jsonObject;
        }
    }
}
