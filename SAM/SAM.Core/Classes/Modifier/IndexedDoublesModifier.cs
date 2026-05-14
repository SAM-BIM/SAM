// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class IndexedDoublesModifier : IndexedSimpleModifier
    {
        public IndexedDoubles IndexedDoubles { get; set; }

        public IndexedDoublesModifier(ArithmeticOperator arithmeticOperator, IndexedDoubles indexedDoubles)
        {
            ArithmeticOperator = arithmeticOperator;
            IndexedDoubles = indexedDoubles == null ? null : new IndexedDoubles(indexedDoubles);
        }

        public IndexedDoublesModifier(IndexedDoublesModifier indexedModifier)
            : base(indexedModifier)
        {
            if (indexedModifier != null)
            {
                IndexedDoubles = indexedModifier?.IndexedDoubles == null ? null : new IndexedDoubles(indexedModifier.IndexedDoubles);
            }
        }

        public IndexedDoublesModifier(JObject jObject)
            : base(jObject)
        {

        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["IndexedDoubles"] is JsonObject indexedDoublesJson)
            {
                IndexedDoubles = Query.IJSAMObject<IndexedDoubles>(new JObject((JsonObject)indexedDoublesJson.DeepClone()));
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (IndexedDoubles != null && IndexedDoubles.ToJObject()?.Node is JsonObject indexedDoublesJson)
            {
                result["IndexedDoubles"] = indexedDoublesJson.DeepClone();
            }

            return result;
        }

        public override bool ContainsIndex(int index)
        {
            return IndexedDoubles != null && IndexedDoubles.ContainsIndex(index);
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (IndexedDoubles == null)
            {
                return value;
            }

            if (IndexedDoubles.ContainsIndex(index))
            {
                return Query.Calculate(ArithmeticOperator, value, IndexedDoubles[index]);
            }

            return value;
        }
    }
}
