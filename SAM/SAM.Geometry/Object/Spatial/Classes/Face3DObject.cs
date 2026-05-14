// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Face3DObject : Face3D, IFace3DObject, ITaggable
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Face3D Face3D
        {
            get
            {
                return new Face3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Face3DObject(Face3D face3D)
            : base(face3D)
        {

        }

        public Face3DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Face3DObject(Face3DObject face3DObject)
            : base(face3DObject)
        {
            if (face3DObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(face3DObject?.SurfaceAppearance);
            }

            Tag = face3DObject?.Tag;
        }

        public Face3DObject(Face3D face3D, SurfaceAppearance surfaceAppearance)
            : base(face3D)
        {
            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public Face3DObject(Face3D face3D, System.Drawing.Color surfaceColor, System.Drawing.Color curveColor, double curveThickness)
            : base(face3D)
        {
            SurfaceAppearance = new SurfaceAppearance(surfaceColor, curveColor, curveThickness);
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
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

            if (SurfaceAppearance?.ToJObject()?.Node is JsonObject surfaceJson)
            {
                jsonObject["SurfaceAppearance"] = surfaceJson.DeepClone();
            }

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
