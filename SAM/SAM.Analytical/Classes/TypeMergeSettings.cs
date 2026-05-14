// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class TypeMergeSettings : IJSAMObject
    {
        private HashSet<string> excludedParameterNames;
        private string typeName;
        public TypeMergeSettings(string typeName, IEnumerable<string> excludedParameterNames)
        {
            this.typeName = typeName;
            this.excludedParameterNames = excludedParameterNames == null ? null : new HashSet<string>(excludedParameterNames);
        }

        public TypeMergeSettings(TypeMergeSettings mergeSettings)
            : this(mergeSettings?.typeName, mergeSettings?.excludedParameterNames)
        {

        }

        public TypeMergeSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public TypeMergeSettings(string typeName)
        {
            this.typeName = typeName;
        }

        public HashSet<string> ExcludedParameterNames
        {
            get
            {
                return excludedParameterNames == null ? null : new HashSet<string>(excludedParameterNames);
            }
        }

        public string TypeName
        {
            get
            {
                return typeName;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("TypeName"))
            {
                typeName = jsonObject["TypeName"]?.GetValue<string>();
            }

            if (jsonObject["ExcludedParameterNames"] is JsonArray excludedParameterNamesArray)
            {
                excludedParameterNames = new HashSet<string>();
                foreach (JsonNode node in excludedParameterNamesArray)
                {
                    excludedParameterNames.Add(node?.GetValue<string>());
                }
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (typeName != null)
            {
                jsonObject["TypeName"] = typeName;
            }

            if (excludedParameterNames != null)
            {
                JsonArray excludedParameterNamesArray = new JsonArray();
                foreach (string parameterName in excludedParameterNames)
                {
                    excludedParameterNamesArray.Add(parameterName);
                }

                jsonObject["ExcludedParameterNames"] = excludedParameterNamesArray;
            }

            return jsonObject;
        }
    }
}
