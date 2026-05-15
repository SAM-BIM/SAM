// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ComplexReferenceTextFilter : ComplexReferenceFilter, ITextFilter
    {
        public FilterLogicalOperator FilterLogicalOperator { get; set; } = FilterLogicalOperator.Or;

        public TextComparisonType TextComparisonType { get; set; } = TextComparisonType.Equals;

        public string Value { get; set; } = null;

        public bool CaseSensitive { get; set; } = true;

        public ComplexReferenceTextFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public ComplexReferenceTextFilter(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ComplexReferenceTextFilter()
            : base()
        {
        }

        public ComplexReferenceTextFilter(ComplexReferenceTextFilter complexReferenceTextFilter)
            : base(complexReferenceTextFilter)
        {
            if (complexReferenceTextFilter != null)
            {
                FilterLogicalOperator = complexReferenceTextFilter.FilterLogicalOperator;
                TextComparisonType = complexReferenceTextFilter.TextComparisonType;
                Value = complexReferenceTextFilter.Value;
                CaseSensitive = complexReferenceTextFilter.CaseSensitive;
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

            if (jsonObject.ContainsKey("TextComparisonType"))
            {
                TextComparisonType = Query.Enum<TextComparisonType>(jsonObject["TextComparisonType"]?.GetValue<string>());
            }

            if (jsonObject.ContainsKey("Value"))
            {
                Value = jsonObject["Value"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("CaseSensitive"))
            {
                CaseSensitive = jsonObject["CaseSensitive"]?.GetValue<bool>() ?? true;
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
                if (!Query.TryConvert(value, out string text))
                {
                    if (FilterLogicalOperator == FilterLogicalOperator.And)
                    {
                        return false;
                    }

                    continue;
                }

                if (!Query.Compare(text, Value, TextComparisonType, CaseSensitive))
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

            result["TextComparisonType"] = TextComparisonType.ToString();

            result["CaseSensitive"] = CaseSensitive;

            if (Value != null)
            {
                result["Value"] = Value;
            }

            return result;
        }
    }
}
