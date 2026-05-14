// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class LogicalFilter : Filter
    {
        public LogicalFilter(JObject jObject)
            : base(jObject)
        {

        }

        public LogicalFilter(LogicalFilter logicalFilter)
            : base(logicalFilter)
        {
            if (logicalFilter != null)
            {
                FilterLogicalOperator = logicalFilter.FilterLogicalOperator;
                Filters = logicalFilter.Filters?.ConvertAll(x => Query.Clone(x));
            }
        }

        public LogicalFilter(FilterLogicalOperator filterLogicalOperator, IEnumerable<IFilter> filters)
        {
            FilterLogicalOperator = filterLogicalOperator;

            Filters = filters == null ? null : new List<IFilter>(filters);
        }

        public LogicalFilter(FilterLogicalOperator filterLogicalOperator, params Filter[] filters)
        {
            FilterLogicalOperator = filterLogicalOperator;

            Filters = filters == null ? null : new List<IFilter>(filters);
        }

        public FilterLogicalOperator FilterLogicalOperator { get; set; }

        public List<IFilter> Filters { get; set; }

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

            if (jsonObject["Filters"] is JsonArray jsonArray)
            {
                Filters = new List<IFilter>();
                foreach (JsonNode node in jsonArray)
                {
                    if (node is JsonObject filterObject)
                    {
                        IFilter filter = Query.IJSAMObject(filterObject as JsonObject) as IFilter;
                        if (filter != null)
                        {
                            Filters.Add(filter);
                        }
                    }
                }
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (FilterLogicalOperator == FilterLogicalOperator.Undefined)
            {
                return false;
            }

            if (Filters == null || Filters.Count == 0)
            {
                return true;
            }

            bool result = false;
            switch (FilterLogicalOperator)
            {
                case FilterLogicalOperator.Or:
                    result = Filters.Find(x => x.IsValid(jSAMObject)) != null;
                    break;

                case FilterLogicalOperator.And:
                    result = Filters.TrueForAll(x => x.IsValid(jSAMObject));
                    break;
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
                return null;
            }

            if (Filters != null)
            {
                JsonArray jsonArray = new JsonArray();
                foreach (IFilter filter in Filters)
                {
                    if (filter == null)
                    {
                        continue;
                    }

                    if (filter.ToJsonObject() is JsonObject filterObject)
                        jsonArray.Add(filterObject.DeepClone());
                }

                result["Filters"] = jsonArray;
            }

            result["FilterLogicalOperator"] = FilterLogicalOperator.ToString();

            return result;
        }
    }
}
