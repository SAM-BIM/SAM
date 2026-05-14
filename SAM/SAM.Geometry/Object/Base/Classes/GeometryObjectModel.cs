// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Object
{
    public class GeometryObjectModel : Core.SAMModel
    {
        private SAMGeometryObjectCollection sAMGeometryObjectCollection;

        public GeometryObjectModel(GeometryObjectModel geometryObjectModel)
            : base(geometryObjectModel)
        {
            if (geometryObjectModel?.sAMGeometryObjectCollection != null)
            {
                sAMGeometryObjectCollection = new SAMGeometryObjectCollection();
                foreach (ISAMGeometryObject sAMGeometryObject in geometryObjectModel.sAMGeometryObjectCollection)
                {
                    sAMGeometryObjectCollection.Add(sAMGeometryObject);
                }
            }
        }

        public GeometryObjectModel(JObject jObject)
            : base(jObject)
        {

        }

        public GeometryObjectModel()
            : base()
        {

        }

        public bool Add(ISAMGeometryObject sAMGeometryObject)
        {
            if (sAMGeometryObject == null)
            {
                return false;
            }

            if (sAMGeometryObjectCollection == null)
            {
                sAMGeometryObjectCollection = new SAMGeometryObjectCollection();
            }

            sAMGeometryObjectCollection.Add(sAMGeometryObject);
            return true;
        }

        public List<T> GetSAMGeometryObjects<T>(Func<T, bool> func = null) where T : ISAMGeometryObject
        {
            if (sAMGeometryObjectCollection == null)
            {
                return null;
            }

            List<T> result = new List<T>();
            foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjectCollection)
            {
                if (!(sAMGeometryObject is T))
                {
                    continue;
                }

                T t = (T)sAMGeometryObject;

                if (func != null)
                {
                    if (!func.Invoke(t))
                    {
                        continue;
                    }
                }

                result.Add(t);
            }

            return result;
        }

        public T GetSAMGeometryObject<T>(Func<T, bool> func, bool recursive = true) where T : ISAMGeometryObject
        {
            if (sAMGeometryObjectCollection == null || func == null)
            {
                return default(T);
            }

            foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjectCollection)
            {
                T t = Query.ISAMGeometryObject(sAMGeometryObject, func, recursive);
                if (t != null)
                {
                    return t;
                }
            }

            return default(T);
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                jsonObject = new JsonObject();
            }

            if (sAMGeometryObjectCollection != null)
            {
                JsonArray jsonArray = new JsonArray();

                foreach (ISAMGeometryObject sAMGeometryObject in sAMGeometryObjectCollection)
                {
                    if (sAMGeometryObject?.ToJObject()?.Node is JsonObject geometryJson)
                    {
                        jsonArray.Add(geometryJson.DeepClone());
                    }
                }

                jsonObject["GeometryObjects"] = jsonArray;
            }
            return jsonObject;
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["GeometryObjects"] is JsonArray jsonArray_GeometryObjects)
            {
                sAMGeometryObjectCollection = new SAMGeometryObjectCollection();
                foreach (JsonNode node in jsonArray_GeometryObjects)
                {
                    if (node is JsonObject geometryJson)
                    {
                        ISAMGeometryObject sAMGeometryObject = Core.Create.IJSAMObject<ISAMGeometryObject>(new JObject((JsonObject)geometryJson.DeepClone()));
                        if (sAMGeometryObject != null)
                        {
                            sAMGeometryObjectCollection.Add(sAMGeometryObject);
                        }
                    }
                }
            }

            return true;
        }
    }
}
