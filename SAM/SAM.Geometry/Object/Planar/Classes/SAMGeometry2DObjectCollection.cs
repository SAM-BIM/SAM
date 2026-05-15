// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object.Planar
{
    public class SAMGeometry2DObjectCollection : IEnumerable<ISAMGeometry2DObject>, ISAMGeometry2DObject
    {
        private List<ISAMGeometry2DObject> sAMGeometry2DObjects;

        public SAMGeometry2DObjectCollection()
        {
            sAMGeometry2DObjects = new List<ISAMGeometry2DObject>();
        }

        public SAMGeometry2DObjectCollection(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }


        public SAMGeometry2DObjectCollection(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public SAMGeometry2DObjectCollection(SAMGeometry2DObjectCollection sAMGeometryObject2DCollection)
        {
            if (sAMGeometryObject2DCollection != null)
            {
                List<ISAMGeometry2DObject> sAMGeometry2DObjects_Temp = sAMGeometryObject2DCollection.sAMGeometry2DObjects;
                if (sAMGeometry2DObjects_Temp != null)
                {
                    sAMGeometry2DObjects = new List<ISAMGeometry2DObject>();
                    foreach (ISAMGeometry2DObject sAMGeometry2DObject in sAMGeometry2DObjects_Temp)
                    {
                        ISAMGeometry2DObject sAMGeometry2DObject_Temp = Core.Query.Clone(sAMGeometry2DObject);
                        if (sAMGeometry2DObject_Temp != null)
                        {
                            sAMGeometry2DObjects.Add(sAMGeometry2DObject_Temp);
                        }
                    }
                }
            }
        }

        public SAMGeometry2DObjectCollection(IEnumerable<ISAMGeometry2DObject> sAMGeometry2DObjects)
        {
            if (sAMGeometry2DObjects != null)
            {
                this.sAMGeometry2DObjects = new List<ISAMGeometry2DObject>();
                foreach (ISAMGeometry2DObject sAMGeometry2DObject in sAMGeometry2DObjects)
                {
                    ISAMGeometry2DObject sAMGeometry2DObject_Temp = Core.Query.Clone(sAMGeometry2DObject);
                    if (sAMGeometry2DObject_Temp == null)
                    {
                        continue;
                    }

                    this.sAMGeometry2DObjects.Add(sAMGeometry2DObject_Temp);
                }
            }
        }

        public SAMGeometry2DObjectCollection(ISAMGeometry2DObject sAMGeometry2DObject)
        {
            ISAMGeometry2DObject sAMGeometry2DObject_Temp = Core.Query.Clone(sAMGeometry2DObject);

            if (sAMGeometry2DObject_Temp != null)
            {
                sAMGeometry2DObjects = new List<ISAMGeometry2DObject>() { sAMGeometry2DObject_Temp };
            }


        }

        public void Add(ISAMGeometry2DObject sAMGeometry2DObject)
        {
            if (sAMGeometry2DObject == null)
            {
                return;
            }

            ISAMGeometry2DObject sAMGeometry2DObject_Temp = Core.Query.Clone(sAMGeometry2DObject);
            if (sAMGeometry2DObject_Temp == null)
            {
                return;
            }

            if (sAMGeometry2DObjects == null)
            {
                sAMGeometry2DObjects = new List<ISAMGeometry2DObject>();
            }

            sAMGeometry2DObjects.Add(sAMGeometry2DObject_Temp);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Geometry2DObjects"] is JsonArray geometry2DObjectsArray)
            {
                sAMGeometry2DObjects = new List<ISAMGeometry2DObject>();

                foreach (JsonNode node in geometry2DObjectsArray)
                {
                    if (node is JsonObject geometryObjectJson)
                    {
                        ISAMGeometry2DObject sAMGeometryObject = Core.Query.IJSAMObject(geometryObjectJson as JsonObject) as ISAMGeometry2DObject;
                        if (sAMGeometryObject != null)
                        {
                            sAMGeometry2DObjects.Add(sAMGeometryObject);
                        }
                    }
                }
            }

            return true;
        }

        public IEnumerator<ISAMGeometry2DObject> GetEnumerator()
        {
            return sAMGeometry2DObjects?.GetEnumerator();
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (sAMGeometry2DObjects != null)
            {
                JsonArray geometry2DObjectsArray = new JsonArray();
                foreach (ISAMGeometry2DObject sAMGeometry2DObject in sAMGeometry2DObjects)
                {
                    if (sAMGeometry2DObject?.ToJsonObject() is JsonObject geometryObjectJson)
                    {
                        geometry2DObjectsArray.Add(geometryObjectJson.DeepClone());
                    }
                }

                jsonObject["Geometry2DObjects"] = geometry2DObjectsArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sAMGeometry2DObjects?.GetEnumerator();
        }
    }
}
