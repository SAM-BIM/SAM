// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Spatial
{
    public class SAMGeometry3DObjectCollection : IEnumerable<ISAMGeometry3DObject>, ISAMGeometry3DObject
    {
        private List<ISAMGeometry3DObject> sAMGeometry3DObjects;

        public SAMGeometry3DObjectCollection()
        {
            sAMGeometry3DObjects = new List<ISAMGeometry3DObject>();
        }
        public SAMGeometry3DObjectCollection(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public SAMGeometry3DObjectCollection(SAMGeometry3DObjectCollection sAMGeometryObject3DCollection)
        {
            if (sAMGeometryObject3DCollection != null)
            {
                List<ISAMGeometry3DObject> sAMGeometry3DObjects_Temp = sAMGeometryObject3DCollection.sAMGeometry3DObjects;
                if (sAMGeometry3DObjects_Temp != null)
                {
                    sAMGeometry3DObjects = new List<ISAMGeometry3DObject>();
                    foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjects_Temp)
                    {
                        ISAMGeometry3DObject sAMGeometry3DObject_Temp = Core.Query.Clone(sAMGeometry3DObject);
                        if (sAMGeometry3DObject_Temp != null)
                        {
                            sAMGeometry3DObjects.Add(sAMGeometry3DObject_Temp);
                        }
                    }
                }
            }
        }

        public SAMGeometry3DObjectCollection(IEnumerable<ISAMGeometry3DObject> sAMGeometry3DObjects)
        {
            if (sAMGeometry3DObjects != null)
            {
                this.sAMGeometry3DObjects = new List<ISAMGeometry3DObject>();
                foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjects)
                {
                    ISAMGeometry3DObject sAMGeometry3DObject_Temp = Core.Query.Clone(sAMGeometry3DObject);
                    if (sAMGeometry3DObject_Temp == null)
                    {
                        continue;
                    }

                    this.sAMGeometry3DObjects.Add(sAMGeometry3DObject_Temp);
                }
            }
        }

        public SAMGeometry3DObjectCollection(ISAMGeometry3DObject sAMGeometry3DObject)
        {
            ISAMGeometry3DObject sAMGeometry3DObject_Temp = Core.Query.Clone(sAMGeometry3DObject);

            if (sAMGeometry3DObject_Temp != null)
            {
                sAMGeometry3DObjects = new List<ISAMGeometry3DObject>() { sAMGeometry3DObject_Temp };
            }


        }

        public void Add(ISAMGeometry3DObject sAMGeometry3DObject)
        {
            if (sAMGeometry3DObject == null)
            {
                return;
            }

            ISAMGeometry3DObject sAMGeometry3DObject_Temp = Core.Query.Clone(sAMGeometry3DObject);
            if (sAMGeometry3DObject_Temp == null)
            {
                return;
            }

            if (sAMGeometry3DObjects == null)
            {
                sAMGeometry3DObjects = new List<ISAMGeometry3DObject>();
            }

            sAMGeometry3DObjects.Add(sAMGeometry3DObject_Temp);
        }
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Geometry3DObjects"] is JsonArray jsonArray_Geometry3DObjects)
            {
                sAMGeometry3DObjects = new List<ISAMGeometry3DObject>();

                foreach (JsonNode node in jsonArray_Geometry3DObjects)
                {
                    if (node is JsonObject geometryJson)
                    {
                        ISAMGeometry3DObject sAMGeometryObject = Core.Query.IJSAMObject(geometryJson as JsonObject) as ISAMGeometry3DObject;
                        if (sAMGeometryObject != null)
                        {
                            sAMGeometry3DObjects.Add(sAMGeometryObject);
                        }
                    }
                }
            }

            return true;
        }

        public IEnumerator<ISAMGeometry3DObject> GetEnumerator()
        {
            return sAMGeometry3DObjects?.GetEnumerator();
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (sAMGeometry3DObjects != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (ISAMGeometry3DObject sAMGeometry3DObject in sAMGeometry3DObjects)
                {
                    if (sAMGeometry3DObject?.ToJsonObject() is JsonObject geometryJson)
                    {
                        jsonArray.Add(geometryJson.DeepClone());
                    }
                }

                jsonObject["Geometry3DObjects"] = jsonArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sAMGeometry3DObjects?.GetEnumerator();
        }
    }
}
