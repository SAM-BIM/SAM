// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public class Ellipse2D : SAMGeometry, IClosed2D, IBoundable2D
    {
        private Point2D center;
        private double height;
        private Vector2D heightDirection;
        private double width;

        public Ellipse2D(double width, double height)
        {
            center = Point2D.Zero;
            this.width = width;
            this.height = height;
            heightDirection = Vector2D.WorldY;
        }

        public Ellipse2D(Point2D center, double width, double height, Vector2D heightDirection)
        {
            this.center = new Point2D(center);
            this.width = width;
            this.height = height;
            this.heightDirection = new Vector2D(heightDirection);
        }

        public Ellipse2D(Ellipse2D ellipse2D)
        {
            center = new Point2D(ellipse2D.center);
            width = ellipse2D.width;
            height = ellipse2D.height;
            heightDirection = ellipse2D.heightDirection;
        }
        public Ellipse2D(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public Point2D Center
        {
            get
            {
                return new Point2D(center);
            }
            set
            {
                center = value;
            }
        }

        public double Height
        {
            get
            {
                return height;
            }
            set
            {
                height = value;
            }
        }

        public Vector2D HeightDirection
        {
            get
            {
                return heightDirection;
            }
        }

        public double Width
        {
            get
            {
                return width;
            }
            set
            {
                width = value;
            }
        }

        public Vector2D WidthDirection
        {
            get
            {
                return heightDirection?.GetPerpendicular(Orientation.Clockwise);
            }
        }

        public override ISAMGeometry Clone()
        {
            return new Ellipse2D(this);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Center"] is JsonObject jsonObject_Center)
                center = new Point2D((JsonObject)jsonObject_Center.DeepClone());

            width = jsonObject["Width"]?.GetValue<double>() ?? 0;
            height = jsonObject["Height"]?.GetValue<double>() ?? 0;

            if (jsonObject["HeightDirection"] is JsonObject jsonObject_HeightDirection)
                heightDirection = new Vector2D((JsonObject)jsonObject_HeightDirection.DeepClone());

            return true;
        }

        public double GetArea()
        {
            return System.Math.PI * ((width / 2) * (height / 2));
        }

        public BoundingBox2D GetBoundingBox(double offset = 0)
        {
            throw new NotImplementedException();
        }

        public Point2D GetCentroid()
        {
            return new Point2D(center);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(center, width, height, heightDirection).GetHashCode();
        }

        public Point2D GetInternalPoint2D(double tolerance = Core.Tolerance.Distance)
        {
            return new Point2D(center);
        }

        public Ellipse2D GetMoved(Vector2D vector2D)
        {
            return new Ellipse2D(center.GetMoved(vector2D), width, height, heightDirection);
        }

        public double GetPerimeter()
        {
            return 2 * System.Math.PI * (3 * (width * height) - System.Math.Sqrt(((3 * width) + height) * (width + (3 * height))));
        }

        public bool Inside(Point2D point2D, double tolerance = Core.Tolerance.Distance)
        {
            throw new NotImplementedException();
        }

        public bool Inside(IClosed2D closed2D, double tolerance = Core.Tolerance.Distance)
        {
            throw new NotImplementedException();
        }

        public void Move(Vector2D vector2D)
        {
            center.Move(vector2D);
        }

        public bool On(Point2D point2D, double tolerance = Core.Tolerance.Distance)
        {
            throw new NotImplementedException();
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (center?.ToJsonObject() is JsonObject centerJson)
                jsonObject["Center"] = centerJson.DeepClone();

            jsonObject["Width"] = width;
            jsonObject["Height"] = height;

            if (heightDirection?.ToJsonObject() is JsonObject heightDirectionJson)
                jsonObject["HeightDirection"] = heightDirectionJson.DeepClone();

            return jsonObject;
        }

        public ISAMGeometry2D GetTransformed(ITransform2D transform2D)
        {
            Point2D center_New = Query.Transform(center, transform2D);

            Vector2D vector2D_Height = heightDirection * height;
            Vector2D vector2D_Width = WidthDirection * width;

            vector2D_Height = Query.Transform(vector2D_Height, transform2D);

            return new Ellipse2D(center_New, vector2D_Width.Length, vector2D_Height.Length, vector2D_Height.Unit);
        }

        public bool Transform(ITransform2D transform2D)
        {
            if (transform2D == null)
            {
                return false;
            }

            Ellipse2D ellipse2D = GetTransformed(transform2D) as Ellipse2D;
            if (ellipse2D == null)
            {
                return false;
            }

            center = ellipse2D.center;
            height = ellipse2D.height;
            heightDirection = ellipse2D.heightDirection;
            width = ellipse2D.width;
            return true;
        }
    }
}
