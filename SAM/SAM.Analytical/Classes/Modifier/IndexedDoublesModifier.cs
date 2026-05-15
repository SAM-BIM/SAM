// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class IndexedDoublesModifier : IndexedSimpleModifier
    {
        public IndexedDoublesModifier(ArithmeticOperator arithmeticOperator, IndexedDoubles values)
        {
            ArithmeticOperator = arithmeticOperator;
            Values = values == null ? null : new IndexedDoubles(values);
        }

        public IndexedDoublesModifier(IndexedDoublesModifier indexedDoublesModifier)
            : base(indexedDoublesModifier)
        {
            if (indexedDoublesModifier != null)
            {
                Values = indexedDoublesModifier?.Values == null ? null : new IndexedDoubles(indexedDoublesModifier.Values);
            }
        }
        public IndexedDoublesModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public IndexedDoubles Values { get; set; }

        public override bool ContainsIndex(int index)
        {
            if (Values == null)
            {
                return false;
            }

            return Values.ContainsIndex(index);
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            if (jsonObject["Values"] is JsonObject valuesJson)
            {
                Values = Core.Query.IJSAMObject<IndexedDoubles>(valuesJson as JsonObject);
            }

            return result;
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (Values == null)
            {
                return value;
            }

            if (!Values.TryGetValue(index, out double value_Temp))
            {
                return value;
            }

            if (double.IsNaN(value_Temp))
            {
                return double.NaN;
            }

            return Core.Query.Calculate(ArithmeticOperator, value, value_Temp);
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (Values?.ToJsonObject() is JsonObject valuesJson)
            {
                result["Values"] = valuesJson.DeepClone();
            }

            return result;
        }
    }
}
