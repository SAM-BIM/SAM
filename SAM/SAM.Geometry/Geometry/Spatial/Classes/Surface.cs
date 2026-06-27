// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class Surface : SAMGeometry, IClosed3D
    {
        private IClosed3D externalEdge3D;

        public Surface(IClosed3D externalEdge3D)
        {
            this.externalEdge3D = externalEdge3D?.Clone() as IClosed3D;
        }

        public Surface(Surface surface)
        {
            externalEdge3D = surface.externalEdge3D.Clone() as IClosed3D;
        }
        public Surface(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Face3D ToFace(double tolerance = Core.Tolerance.Distance)
        {
            if (externalEdge3D is IClosedPlanar3D)
                return new Face3D(externalEdge3D as IClosedPlanar3D);

            List<ICurve3D> curve3Ds = null;
            if (externalEdge3D is Polycurve3D)
                curve3Ds = ((Polycurve3D)externalEdge3D).Explode();
            else if (externalEdge3D is ICurvable3D)
                curve3Ds = ((ICurvable3D)externalEdge3D).GetCurves();

            IEnumerable<Segment3D> segment3Ds = curve3Ds.FindAll(x => x is Segment3D).Cast<Segment3D>();
            if (segment3Ds.Count() == curve3Ds.Count)
            {
                List<Point3D> point3Ds = Create.Point3Ds(segment3Ds, false);
                Plane plane = Create.Plane(point3Ds, tolerance);
                if (plane != null)
                    return new Face3D(new Polygon3D(point3Ds));
            }

            throw new NotImplementedException();
        }

        public override ISAMGeometry Clone()
        {
            return new Surface(this);
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            return externalEdge3D.GetBoundingBox(offset);
        }

        public IClosed3D ExternalEdge3D
        {
            get
            {
                return externalEdge3D.Clone() as IClosed3D;
            }
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            return new Surface((IClosed3D)externalEdge3D.GetMoved(vector3D));
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            externalEdge3D = Core.Create.IJSAMObject<IClosed3D>(jsonObject as JsonObject);
            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (externalEdge3D?.ToJsonObject() is JsonObject boundaryJson)
                jsonObject["Boundary"] = boundaryJson.DeepClone();

            return jsonObject;
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            throw new NotImplementedException();
        }
    }
}
