// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class EnumFilter<T> : Filter, IEnumFilter where T : Enum
    {
        public EnumFilter(EnumFilter<T> enumFilter)
            : base(enumFilter)
        {
            if (enumFilter != null)
            {
                Value = enumFilter.Value;
            }
        }

        public EnumFilter(JObject jObject)
            : base(jObject)
        {

        }

        public EnumFilter()
            : base()
        {

        }

        public Enum Enum
        {
            get
            {
                return Value;
            }

            set
            {
                if (value is T)
                {
                    Value = (T)value;
                }
            }
        }

        public T Value { get; set; }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Enum"))
            {
                string text = jsonObject["Enum"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Value = Query.Enum<T>(text);
                }
            }

            return true;
        }

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (jSAMObject == null)
            {
                return false;
            }

            if (!TryGetEnum(jSAMObject, out T @enum))
            {
                return false;
            }

            if (@enum == null)
            {
                return false;
            }

            if (Value == null && @enum == null)
            {
                return true;
            }

            if (Value == null || @enum == null)
            {
                return false;
            }

            bool result = Value.Equals(@enum);

            if (Inverted)
            {
                result = !result;
            }

            return result;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return result;
            }

            if (Value != null)
            {
                result["Enum"] = Value.ToString();
            }

            return result;
        }

        public abstract bool TryGetEnum(IJSAMObject jSAMObject, out T @enum);
    }
}
