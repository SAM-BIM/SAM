// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public class TransformGroup2D : ITransform2D, IEnumerable<ITransform2D>
    {
        private List<ITransform2D> transform2Ds;
        public TransformGroup2D(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public TransformGroup2D(IEnumerable<ITransform2D> transform2Ds)
        {
            if (transform2Ds != null)
            {
                this.transform2Ds = new List<ITransform2D>();
                foreach (ITransform2D transform2D in transform2Ds)
                {
                    if (transform2D == null)
                    {
                        continue;
                    }

                    this.transform2Ds.Add(transform2D.Clone());
                }
            }
        }
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Transform2Ds"] is JsonArray transform2DsArray)
            {
                transform2Ds = new List<ITransform2D>();
                foreach (JsonNode node in transform2DsArray)
                {
                    if (node is JsonObject transform2DJson)
                    {
                        transform2Ds.Add(Core.Query.IJSAMObject<ITransform2D>(transform2DJson));
                    }
                }
            }

            return true;
        }

        public IEnumerator<ITransform2D> GetEnumerator()
        {
            return transform2Ds?.ConvertAll(x => x.Clone()).GetEnumerator();
        }

        public void Inverse()
        {
            if (transform2Ds == null)
            {
                return;
            }

            transform2Ds.Reverse();

            foreach (ITransform2D transform2D in transform2Ds)
            {
                transform2D.Inverse();
            }
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (transform2Ds != null)
            {
                JsonArray transform2DsArray = new JsonArray();
                foreach (Transform2D transform2D in transform2Ds)
                {
                    if (transform2D?.ToJsonObject() is JsonObject transform2DJson)
                    {
                        transform2DsArray.Add(transform2DJson.DeepClone());
                    }
                }

                jsonObject["Transform2Ds"] = transform2DsArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
