// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class Line3D : SAMGeometry, ISAMGeometry3D
    {
        private Point3D origin;
        private Vector3D vector;

        public Line3D(Line3D line3D)
        {
            origin = new Point3D(line3D.origin);
            vector = new Vector3D(line3D.vector);
        }

        public Line3D(Point3D origin, Vector3D vector3D)
        {
            this.origin = new Point3D(origin);
            vector = vector3D.Unit;
        }

        public Line3D(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public Line3D(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public Vector3D Direction
        {
            get
            {
                return new Vector3D(vector);
            }
        }

        public Point3D Origin
        {
            get
            {
                return new Point3D(origin);
            }
        }

        public override ISAMGeometry Clone()
        {
            return new Line3D(this);
        }

        public void Reverse()
        {
            vector.Negate();
        }

        public Point3D Intersection(Line3D line3D, double tolerance = Core.Tolerance.Distance)
        {
            if (line3D == null)
                return null;

            return Query.Intersection(origin, vector, line3D.origin, line3D.vector, tolerance);
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            return new Segment3D((Point3D)origin.GetMoved(vector3D), (Vector3D)vector.Clone());
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            throw new System.NotImplementedException();
        }

        public Point3D Project(Point3D point3D)
        {
            double t = (point3D - origin).DotProduct(vector);
            return origin.GetMoved(vector * t) as Point3D;
        }

        public Segment3D Bound(Point3D start, Point3D end)
        {
            return new Segment3D(Project(start), Project(end));
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject["Origin"] is JsonObject jsonObject_Origin)
                origin = new Point3D((JsonObject)jsonObject_Origin.DeepClone());

            if (jsonObject["Vector"] is JsonObject jsonObject_Vector)
                vector = new Vector3D((JsonObject)jsonObject_Vector.DeepClone());

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return null;

            if (origin?.ToJsonObject() is JsonObject originJson)
                jsonObject["Origin"] = originJson.DeepClone();

            if (vector?.ToJsonObject() is JsonObject vectorJson)
                jsonObject["Vector"] = vectorJson.DeepClone();

            return jsonObject;
        }

        /// <summary>
        /// Changes line2D to string formula. Source: https://math.stackexchange.com/questions/404440/what-is-the-equation-for-a-3d-line
        /// </summary>
        /// <param name="lineFormulaForm">LineFormulaForm</param>
        /// <returns>string</returns>
        public string ToString(LineFormulaForm lineFormulaForm)
        {
            if (lineFormulaForm == LineFormulaForm.Undefined)
                return null;

            Vector3D direction = vector.Unit;

            switch (lineFormulaForm)
            {
                case LineFormulaForm.Parameteric:

                    return string.Format("x={0}{1}\ny={2}{3}\nz={4}{5}", origin.X, Core.Convert.ToString(direction.X, "t"), origin.Y, Core.Convert.ToString(direction.Y, "t"), origin.Z, Core.Convert.ToString(direction.Z, "t"));

                case LineFormulaForm.Vector:
                    return string.Format("(x;y;z)=({0};{1};{2}})+t({3};{4};{5})", origin.X, origin.Y, origin.Z, direction.X, direction.Y, direction.Z);

                case LineFormulaForm.Symmetric:
                    return string.Format("(x{0})/{1}=(y{2})/{3}=(z{4})/{5}", -origin.X, direction.X, -origin.Y, direction.X, -origin.Z, direction.Z);
            }

            return null;
        }
    }
}
