// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class SimpleModifier : Modifier, ISimpleModifier
    {
        public ArithmeticOperator ArithmeticOperator { get; set; }

        public SimpleModifier()
            : base()
        {

        }

        public SimpleModifier(SimpleModifier simpleModifier)
            : base(simpleModifier)
        {
            if (simpleModifier != null)
            {
                ArithmeticOperator = simpleModifier.ArithmeticOperator;
            }
        }

        public SimpleModifier(JObject jObject)
            : base(jObject)
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
