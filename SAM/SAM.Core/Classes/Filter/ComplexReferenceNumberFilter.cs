// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ComplexReferenceNumberFilter : ComplexReferenceFilter, INumberFilter
    {
        public FilterLogicalOperator FilterLogicalOperator { get; set; } = FilterLogicalOperator.Or;

        public NumberComparisonType NumberComparisonType { get; set; } = NumberComparisonType.Equals;

        public double Value { get; set; }

        public ComplexReferenceNumberFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public ComplexReferenceNumberFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ComplexReferenceNumberFilter()
            : base()
        {
        }

        public ComplexReferenceNumberFilter(ComplexReferenceNumberFilter complexReferenceNumberFilter)
            : base(complexReferenceNumberFilter)
        {
            if (complexReferenceNumberFilter != null)
            {
                FilterLogicalOperator = complexReferenceNumberFilter.FilterLogicalOperator;
                NumberComparisonType = complexReferenceNumberFilter.NumberComparisonType;
                Value = complexReferenceNumberFilter.Value;
            }
        }

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

            if (jsonObject.ContainsKey("NumberComparisonType"))
            {
                NumberComparisonType = Query.Enum<NumberComparisonType>(jsonObject["NumberComparisonType"]?.GetValue<string>());
            }

            if (jsonObject.ContainsKey("Value"))
            {
                Value = jsonObject["Value"]?.GetValue<double>() ?? double.NaN;
            }

            return true;
        }

        protected override bool IsValid(IEnumerable<object> values)
        {
            if (values == null || values.Count() == 0)
            {
                return false;
            }

            foreach (object value in values)
            {
                if (!Query.TryConvert(value, out double number))
                {
                    if (FilterLogicalOperator == FilterLogicalOperator.And)
                    {
                        return false;
                    }

                    continue;
                }

                if (!Query.Compare(number, Value, NumberComparisonType))
                {
                    if (FilterLogicalOperator == FilterLogicalOperator.And)
                    {
                        return false;
                    }

                    continue;
                }

                if (FilterLogicalOperator == FilterLogicalOperator.Or)
                {
                    return true;
                }
            }

            return FilterLogicalOperator == FilterLogicalOperator.And;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            result["FilterLogicalOperator"] = FilterLogicalOperator.ToString();

            result["NumberComparisonType"] = NumberComparisonType.ToString();

            if (!double.IsNaN(Value))
            {
                result["Value"] = JToken.ToNode(Value);
            }

            return result;
        }
    }
}
