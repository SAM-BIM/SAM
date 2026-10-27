// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.Planar
{
    public static partial class Convert
    {
        public static Polygon2D ToSAM(this LinearRing linearRing, double tolerance = Core.Tolerance.Distance)
        {
            if (linearRing == null || linearRing.IsEmpty)
                return null;

            List<Point2D> point2Ds = linearRing.Coordinates.ToSAM(tolerance);
            if (point2Ds == null || point2Ds.Count < 3)
                return null;

            if (tolerance != 0)
            {
                List<Point2D> point2Ds_Temp = new List<Point2D>();
                point2Ds_Temp.Add(point2Ds[0]);
                for (int i = 1; i < point2Ds.Count; i++)
                {
                    if (!point2Ds[i].AlmostEquals(point2Ds_Temp.Last()))
                    {
                        point2Ds_Temp.Add(point2Ds[i]);
                    }
                }


                if (point2Ds_Temp.Count < 3)
                    return null;

                point2Ds = point2Ds_Temp;
            }

            // Polygon2D drops an exact closing point, so count the points it keeps: a ring the conversion above collapsed
            // to fewer than 3 distinct points (A, B, A) would otherwise become an invalid 2-point loop.
            int count = point2Ds.Count;
            if (point2Ds[count - 1].Equals(point2Ds[0]))
                count--;

            if (count < 3)
                return null;

            Polygon2D result = new Polygon2D(point2Ds);
            return result;
        }
    }
}
