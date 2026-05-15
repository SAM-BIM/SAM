// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class IndexedSimpleModifier : IndexedModifier, ISimpleModifier
    {
        public ArithmeticOperator ArithmeticOperator { get; set; }

        public IndexedSimpleModifier()
            : base()
        {

        }

        public IndexedSimpleModifier(IndexedSimpleModifier indexedSimpleModifier)
            : base(indexedSimpleModifier)
        {
            if (indexedSimpleModifier != null)
            {
                ArithmeticOperator = indexedSimpleModifier.ArithmeticOperator;
            }
        }

        public IndexedSimpleModifier(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public IndexedSimpleModifier(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("ArithmeticOperator"))
            {
                ArithmeticOperator = Query.Enum<ArithmeticOperator>(jsonObject["ArithmeticOperator"]?.GetValue<string>());
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            result["ArithmeticOperator"] = ArithmeticOperator.ToString();

            return result;
        }
    }
}
