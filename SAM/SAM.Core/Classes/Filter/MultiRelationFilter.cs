// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class MultiRelationFilter<T> : Filter, IMultiRelationFilter where T : IJSAMObject
    {
        public MultiRelationFilter(JObject jObject)
            : base(jObject)
        {
        }

        public MultiRelationFilter()
            : base()
        {
        }

        public MultiRelationFilter(MultiRelationFilter<T> multiRelationFilter)
            : base(multiRelationFilter)
        {
            if (multiRelationFilter != null)
            {
                FilterLogicalOperator = multiRelationFilter.FilterLogicalOperator;
                Filter = multiRelationFilter.Filter?.Clone();
            }
        }

        public IFilter Filter { get; set; }

        public FilterLogicalOperator FilterLogicalOperator { get; set; } = FilterLogicalOperator.Or;

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("FilterLogicalOperator"))
            {
                FilterLogicalOperator = Query.Enum<FilterLogicalOperator>(jsonObject["FilterLogicalOperator"]?.GetValue<string>());
            }

            if (jsonObject["Filter"] is JsonObject filterObject)
            {
                Filter = Query.IJSAMObject(filterObject as JsonObject) as IFilter;
            }

            return true;
        }

        public abstract List<T> GetRelatives(IJSAMObject jSAMObject);

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (jSAMObject == null || FilterLogicalOperator == FilterLogicalOperator.Undefined)
            {
                return false;
            }

            if (Filter == null)
            {
                return true;
            }

            List<T> relatives = GetRelatives(jSAMObject);
            if (relatives == null || relatives.Count == 0)
            {
                return Filter.Inverted ? true : false;
            }

            bool result = false;
            if (FilterLogicalOperator == FilterLogicalOperator.And)
            {
                result = relatives.TrueForAll(x => Filter.IsValid(x));
            }
            else if (FilterLogicalOperator == FilterLogicalOperator.Or)
            {
                result = relatives.Find(x => Filter.IsValid(x)) != null;
            }

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

            if (Filter != null)
            {
                if (Filter.ToJsonObject() is JsonObject filterObject)
                    result["Filter"] = filterObject.DeepClone();
            }

            result["FilterLogicalOperator"] = FilterLogicalOperator.ToString();

            return result;
        }
    }
}
