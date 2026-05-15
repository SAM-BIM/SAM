// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class TextFilter : Filter, ITextFilter
    {
        public TextFilter(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }

        public TextFilter(System.Text.Json.Nodes.JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        public TextFilter(TextFilter textFilter)
            : base(textFilter)
        {
            if (textFilter != null)
            {
                TextComparisonType = textFilter.TextComparisonType;
                Value = textFilter.Value;
                CaseSensitive = textFilter.CaseSensitive;
            }
        }

        public TextFilter(TextComparisonType textComparisonType, string value)
        {
            TextComparisonType = textComparisonType;
            Value = value;
        }

        public bool CaseSensitive { get; set; } = true;

        public TextComparisonType TextComparisonType { get; set; } = TextComparisonType.Equals;

        public string Value { get; set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
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

        public override bool IsValid(IJSAMObject jSAMObject)
        {
            if (!TryGetText(jSAMObject, out string text))
            {
                return false;
            }

            bool result = Query.Compare(text, Value, TextComparisonType, CaseSensitive);
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

            result["TextComparisonType"] = TextComparisonType.ToString();

            if (Value != null)
            {
                result["Value"] = Value;
            }

            result["CaseSensitive"] = CaseSensitive;

            return result;
        }

        public abstract bool TryGetText(IJSAMObject jSAMObject, out string text);
    }
}
