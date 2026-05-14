// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Geometry.Spatial;
using System;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class LinkedFace3D : Core.IJSAMObject, IFace3DObject
    {
        private Guid guid;
        private BoundingBox3D boundingBox3D;
        private Face3D face3D;

        public Face3D Face3D
        {
            get
            {
                return face3D;
            }
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public LinkedFace3D(Guid guid, Face3D face3D)
        {
            this.guid = guid;
            this.face3D = new Face3D(face3D);
            boundingBox3D = this.face3D?.GetBoundingBox();
        }

        public LinkedFace3D(LinkedFace3D linkedFace3D)
        {
            if (linkedFace3D == null)
            {
                return;
            }

            guid = linkedFace3D.guid;
            if (linkedFace3D.face3D != null)
            {
                face3D = new Face3D(linkedFace3D.face3D);
                boundingBox3D = face3D?.GetBoundingBox();
            }
        }

        public LinkedFace3D(JObject jObject)
        {
            FromJObject(jObject);
        }

        public BoundingBox3D GetBoundingBox(double offset = 0)
        {
            if (boundingBox3D == null)
            {
                boundingBox3D = face3D?.GetBoundingBox();
            }

            if (boundingBox3D == null)
            {
                return null;
            }

            return new BoundingBox3D(boundingBox3D, offset);
        }

        public void Move(Vector3D vector3D)
        {
            face3D = face3D?.GetMoved(vector3D) as Face3D;
            boundingBox3D = face3D?.GetBoundingBox();
        }

        public void Transform(Transform3D transform3D)
        {
            face3D = face3D?.GetTransformed(transform3D) as Face3D;
            boundingBox3D = face3D?.GetBoundingBox();
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (guid != Guid.Empty)
            {
                jsonObject["Guid"] = guid.ToString();
            }

            if (face3D?.ToJObject()?.Node is JsonObject face3DJson)
            {
                jsonObject["Face3D"] = face3DJson.DeepClone();
            }

            if (boundingBox3D?.ToJObject()?.Node is JsonObject boundingBox3DJson)
            {
                jsonObject["BoundingBox3D"] = boundingBox3DJson.DeepClone();
            }

            return jsonObject;
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

            if (jsonObject.ContainsKey("Guid"))
            {
                guid = Core.Query.Guid(jsonObject, "Guid");
            }

            if (jsonObject["Face3D"] is JsonObject face3DJson)
            {
                face3D = new Face3D(new JObject((JsonObject)face3DJson.DeepClone()));
            }

            if (jsonObject["BoundingBox3D"] is JsonObject boundingBox3DJson)
            {
                boundingBox3D = new BoundingBox3D(new JObject((JsonObject)boundingBox3DJson.DeepClone()));
            }

            return true;
        }
    }
}
