// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class TypeFilter : Filter
    {
        public TypeFilter(JObject jObject)
            : base(jObject)
        {
        }

        public TypeFilter()
        {
        }

        public TypeFilter(TypeFilter typeFilter)
            : base(typeFilter)
        {
            Type = typeFilter?.Type;
        }

        public System.Type Type { get; set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Type"))
            {
                string fullTypeName = jsonObject["Type"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(fullTypeName))
                {
                    Type = Query.Type(fullTypeName);
                }
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (jSAMObject == null)
            {
                return false;
            }

            if (Type == null)
            {
                return true;
            }

            bool result = Type.IsAssignableFrom(jSAMObject.GetType());
            if (Inverted)
            {
                result = !result;
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            if (Type != null)
            {
                result["Type"] = Query.FullTypeName(Type);
            }

            return result;
        }
    }
}
