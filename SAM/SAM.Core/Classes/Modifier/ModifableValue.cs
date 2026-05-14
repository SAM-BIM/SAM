// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ModifiableValue : IModifiableValue
    {
        public ModifiableValue(IModifier modifier, double value)
        {
            Value = value;
            Modifier = modifier;
        }

        public ModifiableValue(double value)
        {
            Value = value;
        }

        public ModifiableValue(ModifiableValue modifiableValue)
        {
            if (modifiableValue != null)
            {
                Value = modifiableValue.Value;
                Modifier = modifiableValue.Modifier;
            }
        }

        public ModifiableValue(JObject jObject)
        {
            FromJObject(jObject);
        }

        public IModifier Modifier { get; set; }

        public double Value { get; set; }

        public static implicit operator ModifiableValue(double value)
        {
            return new ModifiableValue(value);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Value"))
            {
                Value = jsonObject["Value"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject["Modifier"] is JsonObject modifierJson)
            {
                Modifier = Query.IJSAMObject<IModifier>(new JObject((JsonObject)modifierJson.DeepClone()));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (!double.IsNaN(Value))
            {
                jsonObject["Value"] = Value;
            }

            if (Modifier != null && Modifier.ToJObject()?.Node is JsonObject modifierJson)
            {
                jsonObject["Modifier"] = modifierJson.DeepClone();
            }

            return jsonObject;
        }
    }
}
