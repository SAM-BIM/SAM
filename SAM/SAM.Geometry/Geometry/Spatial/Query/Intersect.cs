// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.Spatial
{
    public static partial class Query
    {
        public static bool Intersect(this Plane plane, BoundingBox3D boundingBox3D, double tolerance = Core.Tolerance.Distance)
        {
            if (plane == null || boundingBox3D == null)
            {
                return false;
            }

            Vector3D normal = plane.Normal;
            Point3D origin = plane.Origin;
            if (normal == null || origin == null)
            {
                return false;
            }

            Point3D center = boundingBox3D.GetCentroid();
            if (center == null)
            {
                return false;
            }

            double dist = (normal.X * (center.X - origin.X)) + (normal.Y * (center.Y - origin.Y)) + (normal.Z * (center.Z - origin.Z));
            double hx = boundingBox3D.Width * 0.5;
            double hy = boundingBox3D.Depth * 0.5;
            double hz = boundingBox3D.Height * 0.5;

            if (double.IsNaN(hx) || double.IsNaN(hy) || double.IsNaN(hz))
            {
                return false;
            }

            double r = (System.Math.Abs(normal.X) * hx) + (System.Math.Abs(normal.Y) * hy) + (System.Math.Abs(normal.Z) * hz);
            return System.Math.Abs(dist) <= (r + tolerance);
        }

        public static bool Intersect(this Face3D face3D, Point3D point3D, Vector3D vector3D, double tolerance = Core.Tolerance.Distance)
        {
            if (face3D == null || point3D == null || vector3D == null)
            {
                return false;
            }

            PlanarIntersectionResult planarIntersectionResult = Create.PlanarIntersectionResult(face3D, point3D, vector3D, tolerance);
            if (planarIntersectionResult == null || !planarIntersectionResult.Intersecting)
            {
                return false;
            }

            Point3D point3D_Intersection = planarIntersectionResult.GetGeometry3Ds<Point3D>()?.FirstOrDefault();

            return point3D_Intersection != null && point3D_Intersection.IsValid();
        }
    }
}
