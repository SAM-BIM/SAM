// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class SAMGeometryObjectCollection : IEnumerable<ISAMGeometryObject>, ISAMGeometryObject
    {
        private List<ISAMGeometryObject> sAMGeometryObjects;

        public SAMGeometryObjectCollection()
        {
            sAMGeometryObjects = new List<ISAMGeometryObject>();
        }
        public SAMGeometryObjectCollection(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public SAMGeometryObjectCollection(SAMGeometryObjectCollection sAMGeometryObjectCollection)
        {
            if (sAMGeometryObjectCollection != null)
            {
                List<ISAMGeometryObject> sAMGeometryObjects_Temp = sAMGeometryObjectCollection.sAMGeometryObjects;
                if (sAMGeometryObjects_Temp != null)
                {
                    sAMGeometryObjects = new List<ISAMGeometryObject>();
                    foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects_Temp)
                    {
                        ISAMGeometryObject sAMGeometryObject_Temp = Core.Query.Clone(sAMGeometryObject);
                        if (sAMGeometryObject_Temp != null)
                        {
                            sAMGeometryObjects.Add(sAMGeometryObject_Temp);
                        }
                    }
                }
            }
        }

        public SAMGeometryObjectCollection(IEnumerable<ISAMGeometryObject> sAMGeometryObjects)
        {
            if (sAMGeometryObjects != null)
            {
                this.sAMGeometryObjects = new List<ISAMGeometryObject>();
                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    ISAMGeometryObject sAMGeometryObject_Temp = Core.Query.Clone(sAMGeometryObject);
                    if (sAMGeometryObject_Temp != null)
                    {
                        this.sAMGeometryObjects.Add(sAMGeometryObject_Temp);
                    }
                }
            }
        }

        public SAMGeometryObjectCollection(ISAMGeometryObject sAMGeometryObject)
        {
            ISAMGeometryObject sAMGeometryObject_Temp = Core.Query.Clone(sAMGeometryObject);
            if (sAMGeometryObject_Temp != null)
            {
                sAMGeometryObjects = new List<ISAMGeometryObject>() { sAMGeometryObject_Temp };
            }
        }

        public void Add(ISAMGeometryObject sAMGeometryObject)
        {
            ISAMGeometryObject sAMGeometryObject_Temp = Core.Query.Clone(sAMGeometryObject);
            if (sAMGeometryObject_Temp == null)
            {
                return;
            }

            if (sAMGeometryObjects == null)
            {
                sAMGeometryObjects = new List<ISAMGeometryObject>();
            }

            sAMGeometryObjects.Add(sAMGeometryObject_Temp);
        }
        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["GeometryObjects"] is JsonArray geometryObjectsArray)
            {
                sAMGeometryObjects = new List<ISAMGeometryObject>();

                foreach (JsonNode node in geometryObjectsArray)
                {
                    if (node is JsonObject geometryObjectJson)
                    {
                        ISAMGeometryObject sAMGeometryObject = Core.Query.IJSAMObject(geometryObjectJson as JsonObject) as ISAMGeometryObject;
                        if (sAMGeometryObject != null)
                        {
                            sAMGeometryObjects.Add(sAMGeometryObject);
                        }
                    }
                }
            }

            return true;
        }

        public IEnumerator<ISAMGeometryObject> GetEnumerator()
        {
            return sAMGeometryObjects?.GetEnumerator();
        }
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (sAMGeometryObjects != null)
            {
                JsonArray geometryObjectsArray = new JsonArray();
                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjects)
                {
                    if (sAMGeometryObject?.ToJsonObject() is JsonObject geometryObjectJson)
                    {
                        geometryObjectsArray.Add(geometryObjectJson.DeepClone());
                    }
                }

                jsonObject["GeometryObjects"] = geometryObjectsArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return sAMGeometryObjects?.GetEnumerator();
        }
    }
}
