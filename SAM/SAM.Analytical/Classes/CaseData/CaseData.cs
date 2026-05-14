// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class CaseData : IJSAMObject, IAnalyticalObject
    {
        private string name;

        public CaseData(string name)
        {
            this.name = name;
        }

        public CaseData(JObject jObject)
        {
            FromJObject(jObject);
        }

        public CaseData(CaseData caseData)
        {
            if (caseData != null)
            {
                name = caseData.name;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Name"))
            {
                name = jsonObject["Name"]?.GetValue<string>();
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (name != null)
            {
                jsonObject["Name"] = name;
            }

            return jsonObject;
        }
    }
}
