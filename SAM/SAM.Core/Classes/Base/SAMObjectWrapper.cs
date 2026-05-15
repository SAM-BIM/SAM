// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class JSAMObjectWrapper : ISAMObject
    {
        // Stores the underlying BCL JsonObject directly so the wrapper no
        // longer depends on the JObject shim for its internal state. JObject
        // is reconstructed only at the public boundary.
        private JsonObject jsonObject;

        public JSAMObjectWrapper(JSAMObjectWrapper jSAMObjectWrapper)
        {
            jsonObject = jSAMObjectWrapper.jsonObject?.DeepClone() as JsonObject;
        }

        public JSAMObjectWrapper(JObject jObject)
        {
            jsonObject = jObject?.Node as JsonObject;
        }

        public JSAMObjectWrapper(JsonObject jsonObject)
        {
            this.jsonObject = jsonObject;
        }

        public Guid Guid
        {
            get
            {
                if (jsonObject == null)
                    return Guid.Empty;

                return Query.Guid(jsonObject);
            }
        }

        public string Name
        {
            get
            {
                if (jsonObject == null)
                    return null;

                return Query.Name(jsonObject);
            }
        }

        public JSAMObjectWrapper Clone()
        {
            return new JSAMObjectWrapper(this);
        }
        public bool FromJsonObject(JsonObject? jsonObject)
        {
            if (jsonObject == null)
                return false;

            this.jsonObject = jsonObject;
            return true;
        }

        public string GetAssemblyName()
        {
            string fullTypeName = Query.FullTypeName(jsonObject);
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            if (!Query.TryGetTypeNameAndAssemblyName(fullTypeName, out string typeName, out string assemblyName))
            {
                return null;
            }

            return assemblyName;
        }

        public string GetTypeName()
        {
            string fullTypeName = Query.FullTypeName(jsonObject);
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return null;
            }

            if (!Query.TryGetTypeNameAndAssemblyName(fullTypeName, out string typeName, out string assemblyName))
            {
                return null;
            }

            return typeName;
        }

        public IJSAMObject ToIJSAMObject()
        {
            return jsonObject == null ? null : Query.IJSAMObject(new JObject(jsonObject));
        }
        public JsonObject? ToJsonObject()
        {
            return jsonObject?.DeepClone() as JsonObject;
        }
    }
}
