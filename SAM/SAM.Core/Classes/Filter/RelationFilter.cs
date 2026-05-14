// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class RelationFilter<T> : Filter, IRelationFilter where T : IJSAMObject
    {
        public RelationFilter(JObject jObject)
            : base(jObject)
        {
        }

        public RelationFilter(IFilter filter)
        {
            Filter = filter;
        }

        public RelationFilter(RelationFilter<T> relationFilter)
            : base(relationFilter)
        {
            if (relationFilter != null)
            {
                Filter = relationFilter.Filter;
            }
        }

        public IFilter Filter { get; set; }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Filter"] is JsonObject filterObject)
            {
                Filter = Query.IJSAMObject(new JObject((JsonObject)filterObject.DeepClone())) as IFilter;
            }

            return true;
        }

        public abstract T GetRelative(IJSAMObject jSAMObject);

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (jSAMObject == null)
            {
                return false;
            }

            if (Filter == null)
            {
                return true;
            }

            bool result = Filter.IsValid(GetRelative(jSAMObject));
            if (Inverted)
            {
                result = !result;
            }

            return result;
        }
        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            if (Filter != null)
            {
                if (Filter.ToJObject()?.Node is JsonObject filterObject)
                    result["Filter"] = filterObject.DeepClone();
            }

            return result;
        }
    }
}
