// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Geometry
{
    public class PointGraphEdge<X, T> : QuickGraph.IEdge<X>, IJSAMObject where T : IJSAMObject where X : IPoint
    {
        private T jSAMObject;
        private X source;
        private X target;

        public PointGraphEdge(T jSAMObject, X source, X target)
        {
            this.jSAMObject = jSAMObject;
            this.source = source;
            this.target = target;
        }

        public PointGraphEdge(JObject jObject)
        {
            FromJObject(jObject);
        }

        public PointGraphEdge(PointGraphEdge<X, T> pointGraphEdge)
        {
            if (pointGraphEdge != null)
            {
                jSAMObject = pointGraphEdge.jSAMObject;
                source = pointGraphEdge.source == null ? default : Core.Query.Clone(pointGraphEdge.source);
                target = pointGraphEdge.target == null ? default : Core.Query.Clone(pointGraphEdge.target);
            }
        }

        public X Source
        {
            get
            {
                return source == null ? default : Core.Query.Clone(source);
            }
        }

        public X Target
        {
            get
            {
                return target == null ? default : Core.Query.Clone(target);
            }
        }

        public T JSAMObject
        {
            get
            {
                return jSAMObject;
            }
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

            if (jsonObject["JSAMObject"] is JsonObject jSAMObjectJson)
            {
                jSAMObject = Core.Query.IJSAMObject<T>(new JObject((JsonObject)jSAMObjectJson.DeepClone()));
            }

            if (jsonObject["Source"] is JsonObject sourceJson)
            {
                source = Core.Query.IJSAMObject<X>(new JObject((JsonObject)sourceJson.DeepClone()));
            }

            if (jsonObject["Target"] is JsonObject targetJson)
            {
                target = Core.Query.IJSAMObject<X>(new JObject((JsonObject)targetJson.DeepClone()));
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

            if (jSAMObject?.ToJObject()?.Node is JsonObject jSAMObjectJson)
            {
                result["JSAMObject"] = jSAMObjectJson.DeepClone();
            }

            if (source?.ToJObject()?.Node is JsonObject sourceJson)
            {
                result["Source"] = sourceJson.DeepClone();
            }

            if (target?.ToJObject()?.Node is JsonObject targetJson)
            {
                result["Target"] = targetJson.DeepClone();
            }

            return result;
        }
    }
}
