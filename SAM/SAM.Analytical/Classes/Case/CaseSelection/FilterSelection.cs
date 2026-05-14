// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class FilterSelection : CaseSelection
    {
        private IFilter filter;

        public FilterSelection(IFilter filter)
        {
            this.filter = filter;
        }

        public FilterSelection()
        {

        }

        public FilterSelection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public IFilter Filter
        {
            get
            {
                return filter;
            }

            set
            {
                filter = value;
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["Filter"] is JsonObject filterJson)
            {
                filter = Core.Query.IJSAMObject<IFilter>(new JObject((JsonObject)filterJson.DeepClone()));
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (filter?.ToJObject()?.Node is JsonObject filterJson)
            {
                result["Filter"] = filterJson.DeepClone();
            }

            return result;
        }
    }
}
