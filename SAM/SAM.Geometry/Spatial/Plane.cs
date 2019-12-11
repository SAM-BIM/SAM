﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Spatial
{
    public class Plane : IGeometry3D
    {
        private Vector3D normal;
        private Point3D origin;
        private Vector3D baseX;

        public Plane()
        {
            normal = new Vector3D(0, 0, 1);
            origin = new Point3D(0, 0, 0);
            baseX = GetBaseX();
        }

        public Plane(Vector3D normal, Point3D origin)
        {
            this.normal = normal.Unit;
            this.origin = origin;
            baseX = GetBaseX();
        }

        private Vector3D GetBaseX()
        {
            if (normal == null)
                return null;
            
            if (normal.X == 0 && normal.Y == 0)
                return new Vector3D(1, 0, 0);

            return new Vector3D(normal.Y, -normal.X, 0).Unit;
        }

        public Vector3D Normal
        {
            get
            {
                return new Vector3D(normal);
            }
        }

        public Point3D Origin
        {
            get
            {
                return new Point3D(origin);
            }
        }

        public Vector3D BaseX
        {
            get
            {
                return new Vector3D(baseX);
            }
        }

        public Vector3D BaseY
        {
            get
            {
                return normal.CrossProduct(baseX);
            }
        }


        /// <summary>
        /// Scalar constant relating origin point to normal vector.
        /// </summary>
        public double K
        {
            get
            {
                return normal.DotProduct(origin.ToVector3D());
            }
        }

        public Planar.IGeometry2D Convert(IGeometry3D geometry)
        {
            return Convert(geometry as dynamic);
        }

        public IGeometry3D Convert(Planar.IGeometry2D geometry)
        {
            return Convert(geometry as dynamic);
        }

        public Point3D Convert(Planar.Point2D point2D)
        {
            Vector3D baseY = BaseY;

            Vector3D u = new Vector3D(baseX.X * point2D.X, baseX.Y * point2D.X, baseX.Z * point2D.X);
            Vector3D v = new Vector3D(baseY.X * point2D.Y, baseY.Y * point2D.Y, baseY.Z * point2D.Y);

            return new Point3D(Origin.X + u.X + v.X, Origin.Y + u.Y + v.Y, Origin.Z + u.Z + v.Z);
        }

        public Planar.Point2D Convert(Point3D point3D)
        {
            Vector3D vector3D = new Vector3D(point3D.X - origin.X, point3D.Y - origin.Y, point3D.Z - origin.Z);
            return new Planar.Point2D(baseX.DotProduct(vector3D), BaseY.DotProduct(vector3D));
        }

        public Polygon3D Convert(Planar.Polygon2D polygon2D)
        {
            return new Polygon3D(Convert(polygon2D.Points));
        }

        public Planar.Polygon2D Convert(Polygon3D polygon3D)
        {
            return new Planar.Polygon2D(Convert(polygon3D.Points));
        }

        public Planar.Segment2D Convert(Segment3D segment3D)
        {
            return new Planar.Segment2D(Convert(segment3D.Start), Convert(segment3D.End));
        }

        public Segment3D Convert(Planar.Segment2D segment2D)
        {
            return new Segment3D(Convert(segment2D.Start), Convert(segment2D.End));
        }

        public List<Planar.Point2D> Convert(IEnumerable<Point3D> point3Ds)
        {
            List<Planar.Point2D> point2ds = new List<Planar.Point2D>();
            foreach (Point3D point3D in point3Ds)
                point2ds.Add(Convert(point3D));

            return  point2ds;
        }

        public List<Point3D> Convert(IEnumerable< Planar.Point2D> point2Ds)
        {
            List<Point3D> point3ds = new List<Point3D>();
            foreach (Planar.Point2D point2D in point2Ds)
                point3ds.Add(Convert(point2D));

            return point3ds;
        }

        public double Distance(Point3D point3D)
        {
            return Closest(point3D).Distance(point3D);
        }

        public Point3D Closest(Point3D point3D)
        {
            double factor = point3D.ToVector3D().DotProduct(normal) - K;
            return new Point3D(point3D.X - (normal.X * factor), point3D.Y - (normal.Y * factor), point3D.Z - (normal.Z * factor));
        }
    }
}

