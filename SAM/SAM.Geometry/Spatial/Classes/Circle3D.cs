﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Spatial
{
    public class Circle3D: IClosed3D
    {
        private Plane plane;
        private double radious;

        public Circle3D(Plane plane, double radious)
        {
            this.plane = new Plane(plane);
            this.radious = radious;
        }

        public Point3D Center
        {
            get
            {
                return new Point3D(plane.Origin);
            }
            set
            {
                plane.Origin = value;
            }
        }

        public double Radious
        {
            get
            {
                return radious;
            }
            set
            {
                radious = value;
            }
        }

        public double GetArea()
        {
            return Math.PI * radious * radious;
        }

        public double Diameter
        {
            get
            {
                return radious * 2;
            }
            set
            {
                radious = value / 2;
            }
        }

        public Point3D GetCentroid()
        {
            return new Point3D(plane.Origin);
        }

        public Circle3D GetMoved(Vector3D vector3D)
        {
            return new Circle3D(plane.GetMoved(vector3D), radious);
        }

        public void Move(Vector3D vector3D)
        {
            plane = plane.GetMoved(vector3D);
        }

        public double GetPerimeter()
        {
            return 2 * Math.PI * radious;
        }
    }
}
