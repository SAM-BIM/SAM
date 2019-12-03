﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Dynamo
{
    /// <summary>
    /// SAM Point2D
    /// </summary>
    public static class Point2D
    {
        /// <summary>
        /// Creates Point2D by coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns name="point2D">point2D</returns>
        /// <search>
        /// Point2D, ByCoordinates
        /// </search>
        public static Planar.Point2D ByCoordinates(double x = 0, double y = 0)
        {
            return new Planar.Point2D(x, y);
        }

        /// <summary>
        /// Creates Point2D by Dynamo Point
        /// </summary>
        /// <param name="point">Dynamo Point</param>
        /// <returns name="point2D">point2D</returns>
        /// <search>
        /// Point2D, ByPoint
        /// </search>
        public static Planar.Point2D ByPoint(this Autodesk.DesignScript.Geometry.Point point)
        {
            return new Planar.Point2D(point.X, point.Y);
        }

        /// <summary>
        /// Creates Dynamo Point at Z=0 from SAM Point2D 
        /// </summary>
        /// <param name="point2D">SAM point2D</param>
        /// <returns name="point">Dynamo point</returns>
        /// <search>
        /// Point, ToPoint
        /// </search>
        public static Autodesk.DesignScript.Geometry.Point ToPoint(this Planar.Point2D point2D)
        {
            return Autodesk.DesignScript.Geometry.Point.ByCoordinates(point2D.X, point2D.Y, 0);
        }
    }

}
