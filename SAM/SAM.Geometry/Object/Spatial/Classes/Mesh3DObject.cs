// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Mesh3DObject : Mesh3D, IMesh3DObject, ITaggable
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Mesh3D Mesh3D
        {
            get
            {
                return new Mesh3D(this);
            }
        }

        public Tag Tag { get; set; }

        public Mesh3DObject(Mesh3D mesh3D)
            : base(mesh3D)
        {

        }

        public Mesh3DObject(JObject jObject)
            : base(jObject)
        {

        }

        public Mesh3DObject(Mesh3DObject mesh3DObject)
            : base(mesh3DObject)
        {
            if (mesh3DObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(mesh3DObject?.SurfaceAppearance);
            }

            Tag = mesh3DObject?.Tag;
        }

        public Mesh3DObject(Mesh3D mesh3D, SurfaceAppearance surfaceAppearance)
            : base(mesh3D)
        {
            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public Mesh3DObject(Mesh3D mesh3D, System.Drawing.Color surfaceColor, System.Drawing.Color curveColor, double curveThickness)
            : base(mesh3D)
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

            if (SurfaceAppearance?.ToJsonObject() is JsonObject surfaceJson)
            {
                jsonObject["SurfaceAppearance"] = surfaceJson.DeepClone();
            }

            Core.Modify.Add(new JObject(jsonObject), Tag);

            return jsonObject;
        }
    }
}
