// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class NumberFilter : Filter, INumberFilter
    {
        public NumberFilter(System.Text.Json.Nodes.JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public NumberFilter(NumberFilter numberFilter)
            : base(numberFilter)
        {
            if (numberFilter != null)
            {
                NumberComparisonType = numberFilter.NumberComparisonType;
                Value = numberFilter.Value;
            }
        }

        public NumberFilter(NumberComparisonType numberComparisonType, double value)
        {
            NumberComparisonType = numberComparisonType;
            Value = value;
        }

        public NumberComparisonType NumberComparisonType { get; set; } = NumberComparisonType.Equals;

        public double Value { get; set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
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

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (!TryGetNumber(jSAMObject, out double number))
            {
                return false;
            }

            bool result = Query.Compare(number, Value, NumberComparisonType);
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

            result["NumberComparisonType"] = NumberComparisonType.ToString();

            if (!double.IsNaN(Value))
            {
                result["Value"] = JToken.ToNode(Value);
            }

            return result;
        }

        public abstract bool TryGetNumber(IJSAMObject jSAMObject, out double number);
    }
}
