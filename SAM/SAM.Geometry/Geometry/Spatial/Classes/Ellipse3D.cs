// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Geometry.Planar;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class Ellipse3D : SAMGeometry, IClosedPlanar3D
    {
        private Ellipse2D ellipse2D;
        private Plane plane;

        public Ellipse3D(JObject jObject)
            : base(jObject)
        {

        }

        public Ellipse3D(Ellipse3D ellipse3D)
        {
            ellipse2D = new Ellipse2D(ellipse3D.ellipse2D);
            plane = new Plane(ellipse3D.plane);
        }

        public Ellipse3D(Plane plane, Ellipse2D ellipse2D)
        {
            this.ellipse2D = new Ellipse2D(ellipse2D);
            this.plane = new Plane(plane);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Ellipse2D"] is JsonObject jsonObject_Ellipse2D)
                ellipse2D = new Ellipse2D(new JObject((JsonObject)jsonObject_Ellipse2D.DeepClone()));

            if (jsonObject["Plane"] is JsonObject jsonObject_Plane)
                plane = new Plane(new JObject((JsonObject)jsonObject_Plane.DeepClone()));

            return true;
        }

        public double GetArea()
        {
            return ellipse2D.GetArea();
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            throw new System.NotImplementedException();
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            return new Ellipse3D((Plane)plane.GetMoved(vector3D), ellipse2D);
        }

        public Plane GetPlane()
        {
            if (plane == null)
                return null;

            return new Plane(plane);
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            if (transform3D == null)
            {
                return null;
            }

            return Query.Transform(this, transform3D);
        }

        public void Reverse()
        {
            throw new System.NotImplementedException();
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (ellipse2D?.ToJsonObject() is JsonObject ellipse2DJson)
                jsonObject["Ellipse2D"] = ellipse2DJson.DeepClone();

            if (plane?.ToJsonObject() is JsonObject planeJson)
                jsonObject["Plane"] = planeJson.DeepClone();

            return jsonObject;
        }

        public override ISAMGeometry Clone()
        {
            return new Ellipse3D(plane, (Ellipse2D)ellipse2D?.Clone());
        }

        public Point3D GetCentroid()
        {
            return plane?.Convert(ellipse2D?.Center);
        }

        public Ellipse2D Ellipse2D
        {
            get
            {
                if (ellipse2D == null)
                {
                    return null;
                }

                return new Ellipse2D(ellipse2D);
            }
        }
    }
}
