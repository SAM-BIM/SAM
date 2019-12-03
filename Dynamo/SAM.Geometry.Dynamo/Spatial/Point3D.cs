﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Dynamo
{
    /// <summary>
    /// SAM Point3D
    /// </summary>
    public static class Point3D
    {
        /// <summary>
        /// Creates SAM Point3D by Dynamo coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns name="point3D">SAM point3D</returns>
        /// <search>
        /// Point3D, ByCoordinates
        /// </search>
        public static Spatial.Point3D ByCoordinates(double x = 0, double y = 0, double z = 0)
        {
            return new Spatial.Point3D(x, y, z);
        }

        /// <summary>
        /// Creates SAM Point3D By Dynamo Point 
        /// </summary>
        /// <param name="point">Dynamo point</param>
        /// <returns name="point3D">SAM point3D</returns>
        /// <search>
        /// Point3D, ByPoint
        /// </search>
        public static Spatial.Point3D ByPoint(this Autodesk.DesignScript.Geometry.Point point)
        {
            return new Spatial.Point3D(point.X, point.Y, point.Z);
        }

        /// <summary>
        /// Creates Dynamo Point from SAM Point3D 
        /// </summary>
        /// <param name="point3D">SAM point3D</param>
        /// <returns name="point">Dynamo point</returns>
        /// <search>
        /// Point, ToPoint
        /// </search>
        public static Autodesk.DesignScript.Geometry.Point ToPoint(this Spatial.Point3D point3D)
        {
            return Autodesk.DesignScript.Geometry.Point.ByCoordinates(point3D.X, point3D.Y, point3D.Z);
        }
    }
}
