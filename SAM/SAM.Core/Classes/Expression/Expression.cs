// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Expression : IJSAMObject
    {
        private string text;

        public Expression(string text)
        {
            this.text = text;
        }

        public Expression(Expression expression)
        {
            text = expression?.text;
        }
        public Expression(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public string Text
        {
            get
            {
                return text;
            }
        }

        public List<ExpressionVariable> GetExpressionVariables(char openSymbol = '[', char closeSymbol = ']')
        {
            if (text == null)
                return null;

            List<ExpressionVariable> result = new List<ExpressionVariable>();
            List<string> texts = Query.Texts(text, true, openSymbol, closeSymbol);
            if (texts != null && texts.Count > 0)
                result = texts.ConvertAll(x => new ExpressionVariable(x));

            return result;
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            if (jsonObject.ContainsKey("Text"))
                text = jsonObject["Text"]?.GetValue<string>();

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (text != null)
                jsonObject["Text"] = text;

            return jsonObject;
        }

        public override bool Equals(object obj)
        {
            Expression expression = obj as Expression;
            if (expression == null)
            {
                return false;
            }

            return text == expression.text;
        }

        public override int GetHashCode()
        {
            if (text == null)
            {
                return -1;
            }

            return text.GetHashCode();
        }

        public static bool operator ==(Expression expression, object @object)
        {
            if (ReferenceEquals(expression, null) || ReferenceEquals(expression.text, null))
                return ReferenceEquals(@object, null) ? true : false;

            Expression expression_Temp = @object as Expression;
            if (expression_Temp == null)
                return false;

            return expression.text.Equals(expression_Temp.text);
        }

        public static bool operator !=(Expression expression, object @object)
        {
            return !(expression == @object);
        }

        public static implicit operator string(Expression expression) => expression?.text;

        public static implicit operator Expression(string @string) => new Expression(@string);
    }
}
