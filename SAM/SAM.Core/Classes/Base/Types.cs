// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Types : IJSAMObject
    {
        private List<object> types;

        public Types()
        {

        }

        public Types(IEnumerable<Type> types)
        {
            if (types != null)
            {
                this.types = new List<object>();
                foreach (Type type in types)
                {
                    this.types.Add(type);
                }
            }
        }

        public Types(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public Types(Types types)
        {
            if (types != null)
            {
                if (types.types != null)
                {
                    this.types = new List<object>();
                    foreach (object @object in types.types)
                    {
                        this.types.Add(@object);
                    }
                }
            }
        }

        public bool Contains(string fullTypeName)
        {
            if (types == null || string.IsNullOrWhiteSpace(fullTypeName) || types.Count == 0)
            {
                return false;
            }

            foreach (object @object in types)
            {
                if (@object is string)
                {
                    if (fullTypeName.Equals((string)@object))
                    {
                        return true;
                    }
                }
                else if (@object is Type)
                {
                    string fullTypeName_Temp = Query.FullTypeName((Type)@object);
                    if (fullTypeName.Equals(fullTypeName_Temp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Contains(Type type)
        {
            if (type == null || types == null || types.Count == 0)
            {
                return false;
            }

            string fullTypeName = Query.FullTypeName(type);

            foreach (object @object in types)
            {
                if (@object == null)
                {
                    continue;
                }

                if (@object is string)
                {
                    if (fullTypeName.Equals((string)@object))
                    {
                        return true;
                    }
                }
                else if (@object is Type)
                {
                    string fullTypeName_Temp = Query.FullTypeName((Type)@object);
                    if (fullTypeName.Equals(fullTypeName_Temp))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Types"] is JsonArray jsonArray)
            {
                types = new List<object>();

                foreach (JsonNode node in jsonArray)
                {
                    string typeName = node?.GetValue<string>();
                    if (string.IsNullOrWhiteSpace(typeName))
                    {
                        continue;
                    }

                    Type type = Query.Type(typeName, true);
                    if (type == null)
                    {
                        types.Add(typeName);
                    }
                    else
                    {
                        types.Add(type);
                    }
                }
            }

            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (types != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (object @object in types)
                {
                    string typeName = null;
                    if (@object is Type)
                    {
                        typeName = Query.FullTypeName((Type)@object);
                    }
                    else if (@object is string)
                    {
                        typeName = (string)@object;
                    }

                    if (string.IsNullOrWhiteSpace(typeName))
                    {
                        continue;
                    }

                    jsonArray.Add(typeName);
                }

                jsonObject["Types"] = jsonArray;
            }

            return jsonObject;
        }
    }
}
