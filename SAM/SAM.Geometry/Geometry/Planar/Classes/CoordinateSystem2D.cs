// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public class CoordinateSystem2D : IJSAMObject
    {
        private Point2D origin;
        private Vector2D axisX;
        private Vector2D axisY;

        public CoordinateSystem2D(Point2D origin, Vector2D axisX, Vector2D axisY)
        {
            this.origin = origin == null ? null : new Point2D(origin);
            this.axisX = axisX == null ? null : new Vector2D(axisX);
            this.axisY = axisY == null ? null : new Vector2D(axisY);
        }

        public CoordinateSystem2D(Point2D origin)
        {
            this.origin = origin == null ? null : new Point2D(origin);
            axisX = Vector2D.WorldX;
            axisY = Vector2D.WorldY;
        }

        public CoordinateSystem2D()
        {
            origin = Point2D.Zero;
            axisX = Vector2D.WorldX;
            axisY = Vector2D.WorldY;
        }

        public CoordinateSystem2D(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }

        public CoordinateSystem2D(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public CoordinateSystem2D(CoordinateSystem2D coordinateSystem2D)
        {
            if (coordinateSystem2D != null)
            {
                origin = coordinateSystem2D.origin;
                axisX = coordinateSystem2D.axisX;
                axisY = coordinateSystem2D.axisY;
            }
        }

        public Vector2D AxisX
        {
            get
            {
                return axisX == null ? null : new Vector2D(axisX);
            }
        }

        public Vector2D AxisY
        {
            get
            {
                return axisY == null ? null : new Vector2D(axisY);
            }
        }

        public Point2D Origin
        {
            get
            {
                return origin == null ? null : new Point2D(origin);
            }
        }

        public CoordinateSystem2D GetMoved(Vector2D vector2D)
        {
            return new CoordinateSystem2D(origin.GetMoved(vector2D), axisX, axisY);
        }

        public bool Move(Vector2D vector2D)
        {
            if (origin == null || vector2D == null)
            {
                return false;
            }

            origin.Move(vector2D);

            return true;
        }

        public bool IsValid()
        {
            return origin != null && axisX != null && axisY != null && axisX.IsValid() && axisY.IsValid() && origin.IsValid();
        }
        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["AxisX"] is JsonObject axisXJson)
            {
                axisX = new Vector2D((JsonObject)axisXJson.DeepClone());
            }

            if (jsonObject["AxisY"] is JsonObject axisYJson)
            {
                axisY = new Vector2D((JsonObject)axisYJson.DeepClone());
            }

            if (jsonObject["Origin"] is JsonObject originJson)
            {
                origin = new Point2D((JsonObject)originJson.DeepClone());
            }

            return true;
        }
        public JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (axisX?.ToJsonObject() is JsonObject axisXJson)
            {
                result["AxisX"] = axisXJson.DeepClone();
            }

            if (axisY?.ToJsonObject() is JsonObject axisYJson)
            {
                result["AxisY"] = axisYJson.DeepClone();
            }

            if (origin?.ToJsonObject() is JsonObject originJson)
            {
                result["Origin"] = originJson.DeepClone();
            }

            return result;
        }

        public CoordinateSystem2D GetTransformed(ITransform2D transform2D)
        {
            if (transform2D == null)
            {
                return null;
            }

            Point2D origin_New = Query.Transform(origin, transform2D);
            Vector2D axisX_New = Query.Transform(axisX, transform2D);
            Vector2D axisY_New = Query.Transform(axisY, transform2D);

            return new CoordinateSystem2D(origin_New, axisX_New, axisY_New);
        }

        public CoordinateSystem2D Clone()
        {
            return new CoordinateSystem2D(origin, axisX, axisY);
        }

        public static CoordinateSystem2D World
        {
            get
            {
                return new CoordinateSystem2D();
            }
        }

    }
}
