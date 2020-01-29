﻿using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Convert
    {
        public static Spatial.IGeometry3D ToSAM(this GH_Curve curve, bool simplify = true)
        {
            if (curve.Value is LineCurve)
                return ((LineCurve)curve.Value).Line.ToSAM();
            else
                return ToSAM(curve.Value, simplify);
        }

        public static Spatial.IGeometry3D ToSAM(this Curve curve, bool simplify = true)
        { 
            if(curve is PolylineCurve)
                return ((PolylineCurve)curve).ToSAM();

            if(curve is PolyCurve)
                return ((PolyCurve)curve).ToSAM();

            if (curve is LineCurve)
                return ((LineCurve)curve).ToSAM();

            if (simplify)
            {
                PolylineCurve polylineCurve_Temp = curve.ToPolyline(Tolerance.MicroDistance, Tolerance.Angle, 0.4, 1);
                return polylineCurve_Temp.ToSAM();
            }
            else
            {
                PolylineCurve polylineCurve = ToRhino_PolylineCurve(curve);
                return polylineCurve.ToSAM();
            }

            return null;
        }

        public static Spatial.IGeometry3D ToSAM(this PolylineCurve polylineCurve, bool simplify = true, double tolerance = Tolerance.MicroDistance)
        {
            int count = polylineCurve.PointCount;
            if (count == 2)
                return new Spatial.Segment3D(polylineCurve.Point(0).ToSAM(), polylineCurve.Point(1).ToSAM());

            List<Spatial.Point3D> point3Ds = new List<Spatial.Point3D>();
            if (polylineCurve.IsClosed)
                count--;

            for (int i = 0; i < count; i++)
                point3Ds.Add(polylineCurve.Point(i).ToSAM());

            if (simplify)
                point3Ds = Spatial.Point3D.SimplifyByAngle(point3Ds, polylineCurve.IsClosed, Tolerance.Angle);
          
            if (polylineCurve.IsClosed && polylineCurve.IsPlanar(tolerance))
                return new Spatial.Polygon3D(point3Ds);

            return new Spatial.Polyline3D(point3Ds);
        }
    }
}
