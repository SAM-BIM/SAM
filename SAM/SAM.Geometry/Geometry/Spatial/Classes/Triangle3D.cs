// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class Triangle3D : SAMGeometry, IClosedPlanar3D, ISegmentable3D, IBoundable3D
    {
        private Point3D[] points = new Point3D[3];

        public Triangle3D(Point3D point3D_1, Point3D point3D_2, Point3D point3D_3)
        {
            points[0] = point3D_1;
            points[1] = point3D_2;
            points[2] = point3D_3;
        }

        public Triangle3D(Triangle3D triangle3D)
        {
            points = Query.Clone(triangle3D?.points)?.ToArray();
        }

        public Triangle3D(JObject jObject)
            : base(jObject)
        {
        }

        public List<Point3D> GetPoints()
        {
            return new List<Point3D>() { new Point3D(points[0]), new Point3D(points[1]), new Point3D(points[2]) };
        }

        public Vector3D GetNormal()
        {
            if (points.Length < 3)
                return null;

            return Query.Normal(points[0], points[1], points[2]);
        }

        public Plane GetPlane(double tolerance)
        {
            return Create.Plane(points, tolerance);
        }

        public Plane GetPlane()
        {
            return Create.Plane(points, Core.Tolerance.Distance);
        }

        public double Distance(Point3D point3D)
        {
            if (point3D == null || points == null)
                return double.NaN;

            Plane plane = new Plane(points[0], points[1], points[2]);
            if (plane == null || !plane.IsValid())
            {
                return double.NaN;
            }

            Point3D point3D_Project = plane.Project(point3D);

            Planar.Point2D point2D = plane.Convert(point3D_Project);
            if (point2D == null)
                return double.NaN;

            List<Planar.Segment2D> segment2Ds = plane.Convert(this)?.GetSegments();
            if (segment2Ds == null || segment2Ds.Count == 0)
            {
                return double.NaN;
            }

            double a = point3D_Project.Distance(point3D);
            double b = segment2Ds.ConvertAll(x => x.Distance(point2D)).Min();

            return System.Math.Sqrt((a * a) + (b * b));
        }

        public override ISAMGeometry Clone()
        {
            return new Triangle3D(this);
        }

        public List<Segment3D> GetSegments()
        {
            return new List<Segment3D>() { new Segment3D(points[0], points[1]), new Segment3D(points[1], points[2]), new Segment3D(points[2], points[0]) };
        }

        public List<ICurve3D> GetCurves()
        {
            return new List<ICurve3D>() { new Segment3D(points[0], points[1]), new Segment3D(points[1], points[2]), new Segment3D(points[2], points[0]) };
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            return new BoundingBox3D(points, offset);
        }

        public IClosed3D GetExternalEdge()
        {
            return new Triangle3D(this);
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            return new Triangle3D((Point3D)points[0].GetMoved(vector3D), (Point3D)points[1].GetMoved(vector3D), (Point3D)points[2].GetMoved(vector3D));
        }

        public double GetArea()
        {
            if (points == null)
            {
                return double.NaN;
            }

            double a = points[0].Distance(points[1]);
            double b = points[1].Distance(points[2]);
            double c = points[2].Distance(points[0]);

            double s = (a + b + c) / 2;
            return System.Math.Sqrt(s * (s - a) * (s - b) * (s - c));

            //Plane plane = GetPlane();
            //return Planar.Query.Area(points.ToList().ConvertAll(x => plane.Convert(x)));
        }

        public Point3D GetCentroid()
        {
            return Query.Centroid(points);
        }

        public void Reverse()
        {
            points.Reverse();
        }

        public Point3D this[int index]
        {
            get
            {
                return new Point3D(points[index]);
            }
        }


        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Points"] is JsonArray jsonArray_Points)
            {
                List<Point3D> point3Ds = new List<Point3D>();
                foreach (JsonNode node in jsonArray_Points)
                {
                    if (node is JsonObject pointJson)
                    {
                        point3Ds.Add(new Point3D((JsonObject)pointJson.DeepClone()));
                    }
                }
                points = point3Ds.ToArray();
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (points != null && points.Length == 3)
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

        public bool On(Point3D point3D, double tolerance = Core.Tolerance.Distance)
        {
            return Query.On(this, point3D, tolerance);
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            if (transform3D == null)
            {
                return null;
            }

            return Query.Transform(this, transform3D);
        }

        public double GetLength()
        {
            List<Segment3D> segment3Ds = GetSegments();
            if (segment3Ds == null)
            {
                return double.NaN;
            }

            return segment3Ds.ConvertAll(x => x.GetLength()).Sum();

        }

        public override bool Equals(object obj)
        {
            Triangle3D triangle3D = obj as Triangle3D;
            if (triangle3D == null)
            {
                return false;
            }

            return triangle3D.points[0] == points[0] && triangle3D.points[1] == points[1] && triangle3D.points[2] == points[2];
        }

        public override int GetHashCode()
        {
            if (points == null || points.Length != 3)
            {
                return -1;
            }

            return new Tuple<Point3D, Point3D, Point3D>(points[0], points[1], points[2]).GetHashCode();
        }

        public static bool operator ==(Triangle3D triangle3D_1, Triangle3D triangle3D_2)
        {
            if (ReferenceEquals(triangle3D_1, null) && ReferenceEquals(triangle3D_2, null))
                return true;

            if (ReferenceEquals(triangle3D_1, null))
                return false;

            if (ReferenceEquals(triangle3D_2, null))
                return false;

            if (triangle3D_1.points == null && triangle3D_2.points == null)
                return true;

            if (triangle3D_1.points == null || triangle3D_2.points == null)
                return false;

            if (triangle3D_1.points.Length != triangle3D_2.points.Length)
                return false;

            for (int i = 0; i < triangle3D_1.points.Length; i++)
                if (triangle3D_1.points[i] != triangle3D_2.points[i])
                    return false;

            return true;
        }

        public static bool operator !=(Triangle3D triangle3D_1, Triangle3D triangle3D_2)
        {
            if (ReferenceEquals(triangle3D_1, null) && ReferenceEquals(triangle3D_2, null))
                return false;

            if (ReferenceEquals(triangle3D_1, null))
                return true;

            if (ReferenceEquals(triangle3D_2, null))
                return true;

            if (triangle3D_1.points == null && triangle3D_2.points == null)
                return false;

            if (triangle3D_1.points == null || triangle3D_2.points == null)
                return true;

            if (triangle3D_1.points.Length != triangle3D_2.points.Length)
                return true;

            for (int i = 0; i < triangle3D_1.points.Length; i++)
                if (triangle3D_1.points[i] != triangle3D_2.points[i])
                    return true;

            return false;
        }
    }
}
