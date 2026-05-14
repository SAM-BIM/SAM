// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class CoordinateSystem3D : IJSAMObject
    {
        private Point3D origin;
        private Vector3D axisX;
        private Vector3D axisY;
        private Vector3D axisZ;

        public CoordinateSystem3D(Point3D origin, Vector3D axisX, Vector3D axisY, Vector3D axisZ)
        {
            this.origin = origin;
            this.axisX = axisX;
            this.axisY = axisY;
            this.axisZ = axisZ;
        }

        public CoordinateSystem3D(Plane plane)
        {
            if (plane != null)
            {
                origin = plane.Origin;
                axisX = plane.AxisX;
                axisY = plane.AxisY;
                axisZ = plane.AxisZ;
            }
        }

        public CoordinateSystem3D(CoordinateSystem3D coordinateSystem3D)
        {
            if (coordinateSystem3D != null)
            {
                origin = coordinateSystem3D.Origin;
                axisX = coordinateSystem3D.AxisX;
                axisY = coordinateSystem3D.AxisX;
                axisZ = coordinateSystem3D.AxisZ;
            }
        }

        public CoordinateSystem3D()
        {
            origin = Point3D.Zero;
            axisX = Vector3D.WorldX;
            axisY = Vector3D.WorldY;
            axisZ = Vector3D.WorldZ;
        }

        public CoordinateSystem3D(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Vector3D AxisX
        {
            get
            {
                return axisX == null ? null : new Vector3D(axisX);
            }
        }

        public Vector3D AxisY
        {
            get
            {
                return axisY == null ? null : new Vector3D(axisY);
            }
        }

        public Vector3D AxisZ
        {
            get
            {
                return axisZ == null ? null : new Vector3D(axisZ);
            }
        }

        public Point3D Origin
        {
            get
            {
                return origin == null ? null : new Point3D(origin);
            }
        }

        public bool IsValid()
        {
            return origin != null && axisX != null && axisY != null && axisZ != null && axisX.IsValid() && axisY.IsValid() && axisZ.IsValid() && origin.IsValid();
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        private bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["AxisX"] is JsonObject axisXJson)
            {
                axisX = new Vector3D(new JObject((JsonObject)axisXJson.DeepClone()));
            }

            if (jsonObject["AxisY"] is JsonObject axisYJson)
            {
                axisY = new Vector3D(new JObject((JsonObject)axisYJson.DeepClone()));
            }

            if (jsonObject["AxisZ"] is JsonObject axisZJson)
            {
                axisZ = new Vector3D(new JObject((JsonObject)axisZJson.DeepClone()));
            }

            if (jsonObject["Origin"] is JsonObject originJson)
            {
                origin = new Point3D(new JObject((JsonObject)originJson.DeepClone()));
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (axisX?.ToJObject()?.Node is JsonObject axisXJson)
            {
                result["AxisX"] = axisXJson.DeepClone();
            }

            if (axisY?.ToJObject()?.Node is JsonObject axisYJson)
            {
                result["AxisY"] = axisYJson.DeepClone();
            }

            if (axisZ?.ToJObject()?.Node is JsonObject axisZJson)
            {
                result["AxisZ"] = axisZJson.DeepClone();
            }

            if (origin?.ToJObject()?.Node is JsonObject originJson)
            {
                result["Origin"] = originJson.DeepClone();
            }

            return result;
        }

        public static CoordinateSystem3D World
        {
            get
            {
                return new CoordinateSystem3D();
            }
        }

    }
}
