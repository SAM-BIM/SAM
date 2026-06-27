// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using SAM.Core;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class BuildingElement<T> : SAMInstance<T>, IAnalyticalObject, IFace3DObject where T : BuildingElementType
    {
        private Face3D face3D;

        public BuildingElement(BuildingElement<T> buildingElement)
            : base(buildingElement)
        {
            face3D = buildingElement?.Face3D?.Clone() as Face3D;
        }
        public BuildingElement(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public BuildingElement(T buildingElementType, Face3D face3D)
            : base(buildingElementType)
        {
            this.face3D = face3D;
        }

        public BuildingElement(System.Guid guid, T buildingElementType, Face3D face3D)
            : base(guid, buildingElementType)
        {
            this.face3D = face3D;
        }

        public BuildingElement(System.Guid guid, BuildingElement<T> buildingElement, Face3D face3D)
            : base(guid, buildingElement)
        {
            if (face3D != null)
            {
                this.face3D = new Face3D(face3D);
            }
        }

        public Face3D Face3D
        {
            get
            {
                if (face3D == null)
                    return null;

                return new Face3D(face3D);
            }
        }

        public virtual double GetArea()
        {
            if (face3D == null)
            {
                return double.NaN;
            }

            return face3D.GetArea();
        }

        public virtual void Transform(Transform3D transform3D)
        {
            face3D = Geometry.Spatial.Query.Transform(face3D, transform3D);
        }

        public virtual void Move(Vector3D vector3D)
        {
            face3D = face3D?.GetMoved(vector3D) as Face3D;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Face3D"] is JsonObject face3DJson)
            {
                face3D = Geometry.Create.ISAMGeometry<Face3D>((JsonObject)face3DJson.DeepClone());
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();

            if (jsonObject == null)
            {
                return jsonObject;
            }

            if (face3D?.ToJsonObject() is JsonObject face3DJson)
            {
                jsonObject["Face3D"] = face3DJson.DeepClone();
            }

            return jsonObject;
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            return face3D?.GetBoundingBox(offset);
        }

        public Vector3D Normal
        {
            get
            {
                return face3D?.GetPlane()?.Normal;
            }
        }

    }
}
