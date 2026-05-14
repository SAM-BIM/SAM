// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class BooleanFilter : Filter, IBooleanFilter
    {
        public BooleanFilter(JObject jObject)
            : base(jObject)
        {
        }

        public BooleanFilter(BooleanFilter booleanFilter)
            : base(booleanFilter)
        {
            if (booleanFilter != null)
            {
                Value = booleanFilter.Value;
            }
        }

        public BooleanFilter(bool value)
        {
            Value = value;
        }

        public bool Value { get; set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Value"))
            {
                Value = jsonObject["Value"]?.GetValue<bool>() ?? false;
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (!TryGetBoolean(jSAMObject, out bool boolean))
            {
                return false;
            }

            bool result = boolean == Value;
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

            result["Value"] = Value;

            return result;
        }

        public abstract bool TryGetBoolean(IJSAMObject jSAMObject, out bool boolean);
    }
}
