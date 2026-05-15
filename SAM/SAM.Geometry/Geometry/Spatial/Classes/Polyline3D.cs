// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class Polyline3D : SAMGeometry, IBoundable3D, ISegmentable3D, ICurve3D
    {
        private List<Point3D> points;

        public Polyline3D(IEnumerable<Point3D> point3Ds, bool close = false)
        {
            points = Query.Clone(point3Ds);
            if (close && !IsClosed())
                points.Add(points.First());
        }

        public Polyline3D(IEnumerable<Segment3D> segment3Ds, bool close = false)
        {
            points = new List<Point3D>() { segment3Ds.ElementAt(0).GetStart() };
            foreach (Segment3D segment3D in segment3Ds)
                points.Add(segment3D.GetEnd());
        }

        public Polyline3D(Polyline3D polyline3D)
        {
            points = Query.Clone(polyline3D.points);
        }

        public Polyline3D(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public Polyline3D(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public List<Point3D> Points
        {
            get
            {
                return GetPoints();
            }
        }

        public List<Segment3D> GetSegments()
        {
            return Create.Segment3Ds(points, false);
        }

        public List<ICurve3D> GetCurves()
        {
            return GetSegments().ConvertAll(x => (ICurve3D)x);
        }

        public bool IsClosed()
        {
            return points.First().Equals(points.Last());
        }

        public bool IsClosed(double tolerance)
        {
            return points.First().Distance(points.Last()) < tolerance;
        }

        public void Close()
        {
            if (IsClosed())
                return;

            points.Add(new Point3D(points.First()));
        }

        public void Open()
        {
            if (!IsClosed())
                return;

            points.RemoveAt(points.Count - 1);
        }

        public Polygon3D ToPolygon3D()
        {
            return new Polygon3D(points);
        }

        public override ISAMGeometry Clone()
        {
            return new Polyline3D(this);
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            return new BoundingBox3D(points);
        }

        public Point3D GetStart()
        {
            return new Point3D(points.First());
        }

        public Point3D GetEnd()
        {
            return new Point3D(points.Last());
        }

        public void Reverse()
        {
            points.Reverse();
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            return new Polyline3D(points.ConvertAll(x => (Point3D)x.GetMoved(vector3D)));
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            if (transform3D == null)
            {
                return null;
            }

            return Query.Transform(this, transform3D);
        }

        public List<Point3D> GetPoints()
        {
            return points.ConvertAll(x => (Point3D)x.Clone());
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Points"] is JsonArray jsonArray_Points)
            {
                points = new List<Point3D>();
                foreach (JsonNode node in jsonArray_Points)
                {
                    if (node is JsonObject pointJson)
                    {
                        points.Add(new Point3D((JsonObject)pointJson.DeepClone()));
                    }
                }
            }
            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (points != null)
            {
                JsonArray jsonArray_Points = new JsonArray();
                foreach (Point3D point3D in points)
                {
                    if (point3D?.ToJsonObject() is JsonObject pointJson)
                    {
                        jsonArray_Points.Add(pointJson.DeepClone());
                    }
                }
                jsonObject["Points"] = jsonArray_Points;
            }
            return jsonObject;
        }

        public double GetLength()
        {
            List<Segment3D> segments3D = GetSegments();

            if (segments3D == null)
                return double.NaN;

            double length = 0;
            segments3D.ForEach(x => length += x.GetLength());
            return length;
        }

        public bool On(Point3D point3D, double tolerance = Core.Tolerance.Distance)
        {
            return Query.On(this, point3D, tolerance);
        }
    }
}
