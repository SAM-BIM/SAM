// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NetTopologySuite.Geometries;
using System.Collections.Generic;

namespace SAM.Geometry.Planar
{
    public static partial class Convert
    {
        public static List<Polygon2D> ToSAM_Polygon2Ds(this Polygon polygon, double tolerance = Core.Tolerance.Distance)
        {
            NetTopologySuite.Geometries.Geometry geometry = polygon?.Boundary;
            if (geometry == null)
                return null;

            // A ring that converts to null (fewer than 3 distinct points) bounds no loop and is left out.
            if (geometry is LinearRing)
            {
                Polygon2D polygon2D = ((LinearRing)geometry).ToSAM(tolerance);
                return polygon2D == null ? new List<Polygon2D>() : new List<Polygon2D>() { polygon2D };
            }
            else if (geometry is MultiLineString)
            {
                MultiLineString multiLineString = (MultiLineString)geometry;
                IEnumerable<NetTopologySuite.Geometries.Geometry> geometries = multiLineString?.Geometries;
                if (geometries == null)
                    return null;

                List<Polygon2D> polygon2Ds = new List<Polygon2D>();
                foreach (NetTopologySuite.Geometries.Geometry geometry_Temp in geometries)
                {
                    LinearRing linearRing = geometry_Temp as LinearRing;
                    if (linearRing == null)
                        continue;

                    Polygon2D polygon2D = linearRing.ToSAM(tolerance);
                    if (polygon2D == null)
                        continue;

                    polygon2Ds.Add(polygon2D);
                }
                return polygon2Ds;
            }

            return null;
        }
    }
}
