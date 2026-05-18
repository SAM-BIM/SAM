// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class Point3DObject : Point3D, IPoint3DObject, ITaggable
    {
        public PointAppearance PointAppearance { get; set; }

        public Tag Tag { get; set; }

        public Point3D Point3D
        {
            get
            {
                return new Point3D(this);
            }
        }
        public Point3DObject(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Point3DObject(Point3DObject point3DObject)
            : base(point3DObject)
        {
            if (point3DObject?.PointAppearance != null)
            {
                PointAppearance = new PointAppearance(point3DObject?.PointAppearance);
            }

            Tag = point3DObject?.Tag;
        }

        public Point3DObject(Point3D point3D)
            : base(point3D)
        {

        }

        public Point3DObject(Point3D point3D, PointAppearance pointAppearance)
            : base(point3D)
        {
            if (pointAppearance != null)
            {
                PointAppearance = new PointAppearance(pointAppearance);
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["PointAppearance"] is JsonObject jsonObject_PointAppearance)
            {
                PointAppearance = new PointAppearance((JsonObject)jsonObject_PointAppearance.DeepClone());
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

            if (PointAppearance?.ToJsonObject() is JsonObject pointJson)
            {
                jsonObject["PointAppearance"] = pointJson.DeepClone();
            }

            Core.Modify.Add(jsonObject, Tag);

            return jsonObject;
        }
    }
}
