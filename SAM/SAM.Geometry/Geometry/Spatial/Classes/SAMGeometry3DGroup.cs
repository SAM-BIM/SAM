// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Spatial
{
    public class SAMGeometry3DGroup : ISAMGeometry3D, IEnumerable<ISAMGeometry3D>
    {
        private CoordinateSystem3D coordinateSystem3D;
        private List<ISAMGeometry3D> sAMGeometry3Ds;

        public SAMGeometry3DGroup()
        {
            coordinateSystem3D = CoordinateSystem3D.World;
        }

        public SAMGeometry3DGroup(SAMGeometry3DGroup sAMGeometry3DGroup)
        {
            if (sAMGeometry3DGroup != null)
            {
                coordinateSystem3D = sAMGeometry3DGroup.coordinateSystem3D == null ? null : new CoordinateSystem3D(sAMGeometry3DGroup.coordinateSystem3D);
                sAMGeometry3Ds = sAMGeometry3DGroup.sAMGeometry3Ds?.ConvertAll(x => x?.Clone() as ISAMGeometry3D);
            }
        }

        public SAMGeometry3DGroup(CoordinateSystem3D coordinateSystem3D)
        {
            this.coordinateSystem3D = coordinateSystem3D == null ? null : new CoordinateSystem3D(coordinateSystem3D);
        }

        private SAMGeometry3DGroup(CoordinateSystem3D coordinateSystem3D, IEnumerable<ISAMGeometry3D> sAMGeometry3Ds)
        {
            this.coordinateSystem3D = coordinateSystem3D == null ? null : new CoordinateSystem3D(coordinateSystem3D);
            this.sAMGeometry3Ds = sAMGeometry3Ds == null ? null : sAMGeometry3Ds.ToList().ConvertAll(x => x.Clone() as ISAMGeometry3D);
        }

        public SAMGeometry3DGroup(IEnumerable<ISAMGeometry3D> sAMGeometry3Ds)
        {
            coordinateSystem3D = CoordinateSystem3D.World;
            this.sAMGeometry3Ds = sAMGeometry3Ds == null ? null : sAMGeometry3Ds.ToList().ConvertAll(x => x.Clone() as ISAMGeometry3D);
        }

        public SAMGeometry3DGroup(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }


        public SAMGeometry3DGroup(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public bool Add(ISAMGeometry3D sAMGeometry3D)
        {
            if (sAMGeometry3D == null || coordinateSystem3D == null)
            {
                return false;
            }

            Transform3D transform3D = Transform3D.GetCoordinateSystem3DToCoordinateSystem3D(CoordinateSystem3D.World, coordinateSystem3D);
            if (transform3D == null)
            {
                return false;
            }

            if (sAMGeometry3Ds == null)
            {
                sAMGeometry3Ds = new List<ISAMGeometry3D>();
            }

            sAMGeometry3Ds.Add(sAMGeometry3D.GetTransformed(transform3D));
            return true;
        }

        public int Count
        {
            get
            {
                if (sAMGeometry3Ds == null)
                {
                    return -1;
                }

                return sAMGeometry3Ds.Count;
            }
        }

        public ISAMGeometry3D this[int index]
        {
            get
            {
                Transform3D transform3D = Transform3D.GetCoordinateSystem3DToCoordinateSystem3D(coordinateSystem3D, CoordinateSystem3D.World);

                return sAMGeometry3Ds[index].GetTransformed(transform3D);
            }

            set
            {
                if (value == null)
                {
                    sAMGeometry3Ds[index] = null;
                    return;
                }

                Transform3D transform3D = Transform3D.GetCoordinateSystem3DToCoordinateSystem3D(CoordinateSystem3D.World, coordinateSystem3D);
                sAMGeometry3Ds[index] = value.GetTransformed(transform3D);
            }
        }

        public ISAMGeometry Clone()
        {
            return new SAMGeometry3DGroup(this);
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

            if (jsonObject["CoordinateSystem3D"] is JsonObject jsonObject_CoordinateSystem3D)
            {
                coordinateSystem3D = new CoordinateSystem3D((JsonObject)jsonObject_CoordinateSystem3D.DeepClone());
            }

            if (jsonObject["SAMGeometry3Ds"] is JsonArray jsonArray_SAMGeometry3Ds)
            {
                sAMGeometry3Ds = new List<ISAMGeometry3D>();
                foreach (JsonNode node in jsonArray_SAMGeometry3Ds)
                {
                    if (node is JsonObject childJson)
                    {
                        sAMGeometry3Ds.Add(Core.Query.IJSAMObject<ISAMGeometry3D>(childJson));
                    }
                }
            }

            return true;
        }

        public IEnumerator<ISAMGeometry3D> GetEnumerator()
        {
            List<ISAMGeometry3D> sAMGeometry3Ds_Temp = new List<ISAMGeometry3D>();
            if (sAMGeometry3Ds != null)
            {
                Transform3D transform3D = Transform3D.GetCoordinateSystem3DToCoordinateSystem3D(coordinateSystem3D, CoordinateSystem3D.World);
                foreach (ISAMGeometry3D sAMGeometry3D in sAMGeometry3Ds)
                {
                    sAMGeometry3Ds_Temp.Add(sAMGeometry3D.GetTransformed(transform3D));
                }
            }

            return sAMGeometry3Ds_Temp.GetEnumerator();
        }

        public ISAMGeometry3D GetMoved(Vector3D vector3D)
        {
            if (vector3D == null)
            {
                return null;
            }

            Transform3D transform3D = Transform3D.GetTranslation(vector3D);

            return GetTransformed(transform3D);
        }

        public ISAMGeometry3D GetTransformed(Transform3D transform3D)
        {
            if (transform3D == null)
            {
                return null;
            }

            CoordinateSystem3D coordinateSystem3D_New = coordinateSystem3D.Transform(transform3D);

            return new SAMGeometry3DGroup(coordinateSystem3D_New, sAMGeometry3Ds);
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

            if (coordinateSystem3D?.ToJsonObject() is JsonObject coordinateJson)
            {
                jsonObject["CoordinateSystem3D"] = coordinateJson.DeepClone();
            }

            if (sAMGeometry3Ds != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (ISAMGeometry3D sAMGeometry3D in sAMGeometry3Ds)
                {
                    if (sAMGeometry3D?.ToJsonObject() is JsonObject childJson)
                    {
                        jsonArray.Add(childJson.DeepClone());
                    }
                }

                jsonObject["SAMGeometry3Ds"] = jsonArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
