// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.Object.Spatial
{
    public static partial class Query
    {
        public static Dictionary<T, Point3D> IntersectionDictionary<T>(this Segment3D segment3D, IEnumerable<T> face3DObjects, bool sort = true, double tolerance = Core.Tolerance.Distance) where T : IFace3DObject
        {
            List<Tuple<T, Point3D>> tuples = IntersectionTuples(segment3D, face3DObjects, sort, tolerance);
            if (tuples == null)
            {
                return null;
            }
            
            Dictionary<T, Point3D> result = new Dictionary<T, Point3D>();
            foreach (Tuple<T, Point3D> tuple in tuples)
            {
                result[tuple.Item1] = tuple.Item2;
            }

            return result;
        }

        public static List<Tuple<T, Point3D>> IntersectionTuples<T>(this Segment3D segment3D, IEnumerable<T> face3DObjects, bool sort = true, double tolerance = Core.Tolerance.Distance) where T : IFace3DObject
        {
            if (segment3D == null || face3DObjects == null)
            {
                return null;
            }

            BoundingBox3D boundingBox3D = segment3D.GetBoundingBox();
            if (boundingBox3D == null)
            {
                return null;
            }

            Point3D point3D = segment3D[0];
            if (point3D == null)
            {
                return null;
            }

            Vector3D vector3D = segment3D.Direction;
            if (vector3D == null)
            {
                return null;
            }

            List<Tuple<T, Point3D>> result = new List<Tuple<T, Point3D>>();
            foreach (T face3DObject in face3DObjects)
            {
                Face3D face3D = face3DObject?.Face3D;
                if (face3D == null)
                {
                    continue;
                }

                BoundingBox3D boundingBox3D_Face3DObject = face3D.GetBoundingBox();
                if (!boundingBox3D.InRange(boundingBox3D_Face3DObject, tolerance))
                {
                    continue;
                }

                PlanarIntersectionResult planarIntersectionResult = Geometry.Spatial.Create.PlanarIntersectionResult(face3D, point3D, vector3D, tolerance);
                if (planarIntersectionResult == null || !planarIntersectionResult.Intersecting)
                {
                    continue;
                }

                Point3D point3D_Intersection = planarIntersectionResult.GetGeometry3Ds<Point3D>()?.FirstOrDefault();
                if (point3D_Intersection == null)
                {
                    continue;
                }

                if (!segment3D.On(point3D_Intersection, tolerance))
                {
                    continue;
                }

                result.Add(new Tuple<T, Point3D>(face3DObject, point3D_Intersection));
            }

            if (sort)
            {
                result.Sort((x, y) => x.Item2.Distance(point3D).CompareTo(y.Item2.Distance(point3D)));
            }

            return result;
        }

        public static Dictionary<T, List<ISAMGeometry3D>> IntersectionDictionary<T>(this T face3DObject, IEnumerable<T> face3DObjects, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance) where T : IFace3DObject
        {
            if (face3DObject == null || face3DObjects == null)
                return null;

            Face3D face3D = face3DObject.Face3D;
            if (face3D == null)
                return null;

            BoundingBox3D boundingBox3D = face3D.GetBoundingBox(tolerance_Distance);

            Dictionary<T, List<ISAMGeometry3D>> result = new Dictionary<T, List<ISAMGeometry3D>>();
            foreach (T face3DObject_Temp in face3DObjects)
            {
                if (face3DObject_Temp.Equals(face3DObject))
                    continue;

                Face3D face3D_Temp = face3DObject_Temp.Face3D;
                if (face3D_Temp == null)
                    continue;

                BoundingBox3D boundingBox3D_Temp = face3D_Temp.GetBoundingBox(tolerance_Distance);
                if (boundingBox3D_Temp == null || !boundingBox3D.InRange(boundingBox3D_Temp, tolerance_Distance))
                    continue;

                PlanarIntersectionResult planarIntersectionResult = Geometry.Spatial.Create.PlanarIntersectionResult(face3D, face3D_Temp, tolerance_Angle, tolerance_Distance);
                if (planarIntersectionResult == null || !planarIntersectionResult.Intersecting)
                    continue;

                List<ISAMGeometry3D> geometry3Ds = planarIntersectionResult.GetGeometry3Ds<ISAMGeometry3D>();
                if (geometry3Ds == null || geometry3Ds.Count == 0)
                    continue;

                result[face3DObject_Temp] = geometry3Ds;
            }

            return result;

        }
    }
}
