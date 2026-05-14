// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public class Mesh2D : SAMGeometry, IMesh, ISAMGeometry2D, IBoundable2D
    {
        private List<Tuple<int, int, int>> indexes;
        private List<Point2D> points;
        public Mesh2D(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Mesh2D(Mesh2D mesh2D)
        {
            points = mesh2D?.points?.ConvertAll(x => new Point2D(x));
            indexes = mesh2D?.indexes?.ConvertAll(x => new Tuple<int, int, int>(x.Item1, x.Item2, x.Item3));
        }

        public Mesh2D(IEnumerable<Point2D> points, IEnumerable<Tuple<int, int, int>> indexes)
        {
            this.points = points?.ToList().ConvertAll(x => new Point2D(x));
            this.indexes = indexes?.ToList().ConvertAll(x => new Tuple<int, int, int>(x.Item1, x.Item2, x.Item3));
        }

        public int PointsCount
        {
            get
            {
                if (points == null)
                {
                    return -1;
                }

                return points.Count;
            }
        }

        public int TrianglesCount
        {
            get
            {
                if (indexes == null)
                {
                    return -1;
                }

                return indexes.Count;
            }
        }

        public override ISAMGeometry Clone()
        {
            return new Mesh2D(points, indexes);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Points"] is JsonArray jsonArray_Points)
            {
                points = new List<Point2D>();
                foreach (JsonNode node in jsonArray_Points)
                {
                    if (node is JsonObject pointJson)
                    {
                        points.Add(new Point2D(new JObject((JsonObject)pointJson.DeepClone())));
                    }
                }
            }

            if (jsonObject["Indexes"] is JsonArray jsonArray_Indexes)
            {
                indexes = new List<Tuple<int, int, int>>();

                foreach (JsonNode node in jsonArray_Indexes)
                {
                    if (node is JsonArray jsonArray_Temp && jsonArray_Temp.Count >= 3)
                    {
                        indexes.Add(new Tuple<int, int, int>(
                            jsonArray_Temp[0]?.GetValue<int>() ?? 0,
                            jsonArray_Temp[1]?.GetValue<int>() ?? 0,
                            jsonArray_Temp[2]?.GetValue<int>() ?? 0));
                    }
                }
            }

            return true;
        }

        public BoundingBox2D GetBoundingBox(double offset = 0)
        {
            if (points == null)
            {
                return null;
            }

            return new BoundingBox2D(points, offset);
        }

        public List<ICurve2D> GetCurves()
        {
            return GetSegments()?.ConvertAll(x => x as ICurve2D);
        }

        public ISAMGeometry2D GetMoved(Vector2D vector2D)
        {
            return new Mesh2D(points?.ConvertAll(x => x.GetMoved(vector2D)), indexes);
        }

        public List<Point2D> GetPoints()
        {
            return points?.ConvertAll(x => new Point2D(x));
        }

        public List<Segment2D> GetSegments()
        {
            return GetSegments(false);
        }

        public List<Segment2D> GetSegments(bool includeSimiliar)
        {
            if (points == null || indexes == null)
            {
                return null;
            }

            List<Segment2D> result = new List<Segment2D>();

            if (includeSimiliar)
            {
                List<Triangle2D> triangle2Ds = GetTriangles();
                if (triangle2Ds == null)
                {
                    return null;
                }

                foreach (Triangle2D triangle2D in triangle2Ds)
                {
                    List<Segment2D> segment3Ds_Triangle2D = triangle2D?.GetSegments();
                    if (segment3Ds_Triangle2D != null && segment3Ds_Triangle2D.Count != 0)
                    {
                        result.AddRange(segment3Ds_Triangle2D);
                    }
                }

                return result;
            }
            else
            {
                List<Tuple<int, int>> tuples = new List<Tuple<int, int>>();

                foreach (Tuple<int, int, int> tuple in indexes)
                {
                    List<int> indexes_Triangle = new List<int>() { tuple.Item1, tuple.Item2, tuple.Item3 };
                    indexes_Triangle.Sort();

                    int index = -1;

                    index = tuples.FindIndex(x => x.Item1 == indexes_Triangle[0] && x.Item2 == indexes_Triangle[1]);
                    if (index == -1)
                    {
                        tuples.Add(new Tuple<int, int>(indexes_Triangle[0], indexes_Triangle[1]));
                    }

                    index = tuples.FindIndex(x => x.Item1 == indexes_Triangle[0] && x.Item2 == indexes_Triangle[2]);
                    if (index == -1)
                    {
                        tuples.Add(new Tuple<int, int>(indexes_Triangle[0], indexes_Triangle[2]));
                    }

                    index = tuples.FindIndex(x => x.Item1 == indexes_Triangle[1] && x.Item2 == indexes_Triangle[2]);
                    if (index == -1)
                    {
                        tuples.Add(new Tuple<int, int>(indexes_Triangle[1], indexes_Triangle[2]));
                    }
                }

                foreach (Tuple<int, int> tuple in tuples)
                {
                    if (tuple.Item1 < 0 || tuple.Item1 >= points.Count)
                    {
                        continue;
                    }

                    if (tuple.Item2 < 0 || tuple.Item2 >= points.Count)
                    {
                        continue;
                    }

                    result.Add(new Segment2D(points[tuple.Item1], points[tuple.Item2]));
                }
            }

            return result;
        }

        public Triangle2D GetTriangle(int index)
        {
            if (points == null || indexes == null)
            {
                return null;
            }

            if (index < 0 || index >= indexes.Count)
            {
                return null;
            }

            int index_1 = indexes[index].Item1;
            if (index_1 < 0 || index_1 >= points.Count)
            {
                return null;
            }

            int index_2 = indexes[index].Item2;
            if (index_2 < 0 || index_2 >= points.Count)
            {
                return null;
            }

            int index_3 = indexes[index].Item3;
            if (index_3 < 0 || index_3 >= points.Count)
            {
                return null;
            }

            return new Triangle2D(points[index_1], points[index_2], points[index_3]);
        }

        public List<Triangle2D> GetTriangles()
        {
            if (points == null || indexes == null)
            {
                return null;
            }

            int count = TrianglesCount;
            if (count == -1)
            {
                return null;
            }

            List<Triangle2D> result = new List<Triangle2D>();
            if (count == 0)
            {
                return result;
            }

            for (int i = 0; i < TrianglesCount; i++)
            {
                result.Add(GetTriangle(i));
            }

            return result;
        }

        public int IndexOf(Point2D point2D)
        {
            if (point2D == null || points == null)
            {
                return -1;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (point2D.Equals(points[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public int IndexOf(Point2D point2D, double tolerance)
        {
            if (point2D == null || points == null)
            {
                return -1;
            }

            for (int i = 0; i < points.Count; i++)
            {
                if (point2D.AlmostEquals(points[i], tolerance))
                {
                    return i;
                }
            }

            return -1;
        }

        public List<Tuple<int, int, int>> GetIndexes()
        {
            if (indexes == null)
            {
                return null;
            }

            List<Tuple<int, int, int>> result = new List<Tuple<int, int, int>>();
            for (int i = 0; i < indexes.Count; i++)
            {
                result.Add(new Tuple<int, int, int>(indexes[i].Item1, indexes[i].Item2, indexes[i].Item3));
            }

            return result;
        }

        public bool On(Point2D point2D, double tolerance = Core.Tolerance.Distance)
        {
            if (points == null || indexes == null)
            {
                return false;
            }

            if (!GetBoundingBox().InRange(point2D, tolerance))
            {
                return false;
            }

            for (int i = 0; i < indexes.Count; i++)
            {
                Triangle2D triangle2D = GetTriangle(i);
                if (triangle2D == null)
                {
                    continue;
                }

                if (!triangle2D.GetBoundingBox().InRange(point2D, tolerance))
                {
                    continue;
                }

                double distance = new Face2D(triangle2D).Distance(point2D, tolerance);
                if (distance < tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        public bool OnEdge(Point2D point2D, double tolerance = Core.Tolerance.Distance)
        {
            if (points == null)
            {
                return false;
            }

            if (!GetBoundingBox().InRange(point2D, tolerance))
            {
                return false;
            }

            List<Segment2D> segment2Ds = GetSegments(false);
            if (segment2Ds == null || segment2Ds.Count == 0)
            {
                return false;
            }

            return segment2Ds.Find(x => x.On(point2D, tolerance)) != null;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (points != null)
            {
                JsonArray jsonArray_Points = new JsonArray();
                foreach (Point2D point2D in points)
                {
                    if (point2D?.ToJsonObject() is JsonObject pointJson)
                    {
                        jsonArray_Points.Add(pointJson.DeepClone());
                    }
                }
                jsonObject["Points"] = jsonArray_Points;
            }

            if (indexes != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (Tuple<int, int, int> tuple in indexes)
                {
                    JsonArray jsonArray_Temp = new JsonArray
                    {
                        tuple.Item1,
                        tuple.Item2,
                        tuple.Item3
                    };
                    jsonArray.Add(jsonArray_Temp);
                }
                jsonObject["Indexes"] = jsonArray;
            }

            return jsonObject;
        }

        public ISAMGeometry2D GetTransformed(ITransform2D transform2D)
        {
            return new Mesh2D(Query.Transform(points, transform2D), indexes);
        }

        public bool Transform(ITransform2D transform2D)
        {
            if (transform2D == null)
            {
                return false;
            }

            points = Query.Transform(points, transform2D);
            return true;
        }
    }
}
