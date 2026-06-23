// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Drawing;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Tag : IJSAMObject
    {
        private object value;

        public Tag(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public Tag(Tag tag)
        {
            value = tag?.value;
        }

        public Tag(double value)
        {
            this.value = value;
        }

        public Tag(Guid value)
        {
            this.value = value;
        }

        public Tag(bool value)
        {
            this.value = value;
        }

        public Tag(DateTime value)
        {
            this.value = value;
        }

        public Tag(IJSAMObject value)
        {
            this.value = value;
        }

        public Tag(SAMObject value)
        {
            this.value = value;
        }

        public Tag(string value)
        {
            this.value = value;
        }

        public Tag(Color value)
        {
            this.value = value;
        }

        public object Value
        {
            get
            {
                return value;
            }
        }

        public ValueType ValueType
        {
            get
            {
                return Query.ValueType(Value);
            }
        }

        public static implicit operator Tag(double value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(double? value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(Guid value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(Guid? value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(bool value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(bool? value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(DateTime value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(DateTime? value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(SAMObject value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(string value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(Color value)
        {
            return new Tag(value);
        }

        public static implicit operator Tag(Color? value)
        {
            return new Tag(value);
        }

        public static bool operator !=(Tag tag_1, Tag tag_2)
        {
            return !(tag_1 == tag_2);
        }

        public static bool operator ==(Tag tag_1, Tag tag_2)
        {
            if ((object)tag_1 == null)
                return (object)tag_2 == null;

            return tag_1.Equals(tag_2);
        }

        public override bool Equals(object @object)
        {
            if (@object == null)
            {
                return false;
            }

            if (@object is Tag && this == (Tag)@object)
            {
                return true;
            }

            if (value == @object)
            {
                return true;
            }

            return false;
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            JsonNode valueNode = jsonObject["Value"];
            if (valueNode == null && !jsonObject.ContainsKey("ValueType"))
            {
                return false;
            }

            if (!jsonObject.ContainsKey("ValueType"))
            {
                value = valueNode.ToObject();
                return true;
            }

            ValueType valueType = Query.Enum<ValueType>(jsonObject["ValueType"]?.GetValue<string>());
            if (valueType == ValueType.Undefined)
            {
                value = null;
                return true;
            }

            if (valueNode == null)
            {
                return false;
            }

            switch (valueType)
            {
                case ValueType.Boolean:
                    value = valueNode.GetValue<bool>();
                    return true;

                case ValueType.Color:
                    if (!(valueNode is JsonObject colorObject))
                    {
                        return false;
                    }

                    value = new SAMColor((JsonObject)colorObject.DeepClone()).ToColor();
                    return true;

                case ValueType.DateTime:
                    value = valueNode.GetValue<DateTime>();
                    return true;

                case ValueType.Double:
                    value = valueNode.GetValue<double>();
                    return true;

                case ValueType.Guid:
                    if (!Guid.TryParse(valueNode.GetValue<string>(), out Guid guid))
                    {
                        return false;
                    }
                    value = guid;
                    return true;

                case ValueType.IJSAMObject:
                    if (!(valueNode is JsonObject objectNode))
                    {
                        return false;
                    }

                    value = new JSAMObjectWrapper((JsonObject)objectNode.DeepClone()).ToIJSAMObject();
                    return true;

                case ValueType.Integer:
                    value = valueNode.GetValue<int>();
                    return true;

                case ValueType.String:
                    value = valueNode.GetValue<string>();
                    return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (value == null)
                {
                    return -1;
                }

                return value.GetHashCode();
            }
        }

        public T GetValue<T>()
        {
            if (value is T)
            {
                return (T)value;
            }

            if (!Query.TryConvert(value, out T result))
            {
                return default(T);
            }

            return result;
        }

        public void SetValue(double value)
        {
            this.value = value;
        }

        public void SetValue(Guid value)
        {
            this.value = value;
        }

        public void SetValue(bool value)
        {
            this.value = value;
        }

        public void SetValue(DateTime value)
        {
            this.value = value;
        }

        public void SetValue(IJSAMObject value)
        {
            this.value = value;
        }

        public void SetValue(string value)
        {
            this.value = value;
        }

        public void SetValue(Color value)
        {
            this.value = value;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            ValueType valueType = ValueType;
            jsonObject["ValueType"] = valueType.ToString();

            if (valueType != ValueType.Undefined)
            {
                JsonNode valueNode = null;
                switch (valueType)
                {
                    case ValueType.Boolean:
                        if (Query.TryConvert(Value, out bool @bool))
                        {
                            valueNode = Query.ToJsonNode(@bool);
                        }
                        break;

                    case ValueType.Color:
                        if (Query.TryConvert(Value, out Color color))
                        {
                            valueNode = new SAMColor(color).ToJsonObject();
                        }
                        break;

                    case ValueType.DateTime:
                        if (Query.TryConvert(Value, out DateTime dateTime))
                        {
                            valueNode = Query.ToJsonNode(dateTime);
                        }
                        break;

                    case ValueType.Double:
                        if (Query.TryConvert(Value, out double @double))
                        {
                            valueNode = Query.ToJsonNode(@double);
                        }
                        break;

                    case ValueType.Guid:
                        if (Query.TryConvert(Value, out Guid @guid))
                        {
                            valueNode = Query.ToJsonNode(@guid);
                        }
                        break;

                    case ValueType.IJSAMObject:
                        // ToJsonObject() returns a fresh, parent-less node - assign it directly (no redundant clone).
                        valueNode = ((IJSAMObject)Value).ToJsonObject();
                        break;

                    case ValueType.Integer:
                        if (Query.TryConvert(Value, out int @int))
                        {
                            valueNode = Query.ToJsonNode(@int);
                        }
                        break;

                    case ValueType.String:
                        if (Query.TryConvert(Value, out string @string))
                        {
                            valueNode = Query.ToJsonNode(@string);
                        }
                        break;
                }

                if (valueNode != null)
                {
                    jsonObject["Value"] = valueNode;
                }
            }

            return jsonObject;
        }
    }
}
