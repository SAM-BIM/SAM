// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class SphereObject : Sphere, ISphereObject, ITaggable
    {
        public SurfaceAppearance SurfaceAppearance { get; set; }

        public Sphere Sphere
        {
            get
            {
                return new Sphere(this);
            }
        }

        public Tag Tag { get; set; }

        public SphereObject(Sphere sphere)
            : base(sphere)
        {

        }
        public SphereObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public SphereObject(SphereObject sphereObject)
            : base(sphereObject)
        {
            if (sphereObject?.SurfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(sphereObject?.SurfaceAppearance);
            }

            Tag = sphereObject?.Tag;
        }

        public SphereObject(Sphere sphere, SurfaceAppearance surfaceAppearance)
            : base(sphere)
        {
            if (surfaceAppearance != null)
            {
                SurfaceAppearance = new SurfaceAppearance(surfaceAppearance);
            }
        }

        public SphereObject(Sphere sphere, System.Drawing.Color surfaceColor, System.Drawing.Color curveColor, double curveThickness)
            : base(sphere)
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
