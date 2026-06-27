// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static bool TryConvert(this object? @object, out object? result, Type type)
        {
            result = default;

            if (type == typeof(object))
            {
                result = @object;
                return true;
            }

            Type? type_Object = @object?.GetType();
            if (type_Object == type || type == null)
            {
                result = @object;
                return true;
            }

            Type type_Temp = Nullable.GetUnderlyingType(type);
            if (type_Temp == null)
            {
                type_Temp = type;
            }

            if (@object is JsonNode jsonNode && TryConvertJsonNode(jsonNode, out result, type_Temp))
            {
                return true;
            }

            if (type_Temp == typeof(string))
            {
                if (@object != null)
                {
                    if (@object is IEnumerable && @object is not string)
                    {
                        JsonArray jsonArray = new JsonArray();
                        foreach (object @object_Temp in (IEnumerable)@object)
                        {
                            if (TryConvert(@object_Temp, out string? value) && value != null)
                                jsonArray.Add(value);
                            else
                                jsonArray.Add(string.Empty);
                        }

                        result = jsonArray.ToJsonString();
                    }
                    else if (@object is IJSAMObject jSAMObject)
                    {
                        result = jSAMObject.ToJsonObject()?.ToJsonString();
                    }

                    if (result == default)
                        result = @object?.ToString();
                }

                return true;
            }
            else if (type_Temp == typeof(bool))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    bool @bool;
                    if (bool.TryParse((string)@object, out @bool))
                    {
                        result = @bool;
                        return true;
                    }

                    string @string = ((string)@object).Trim().ToUpper();
                    result = (@string.Equals("1") || @string.Equals("YES") || @string.Equals("TRUE"));
                    return true;
                }
                else if (IsNumeric(@object))
                {
                    result = (System.Convert.ToInt64(@object) == 1);
                    return true;
                }
            }
            else if (type_Temp == typeof(int))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    int @int;
                    if (int.TryParse((string)@object, out @int))
                    {
                        result = @int;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    result = System.Convert.ToInt32(@object);
                    return true;
                }
                else if (@object is Enum)
                {
                    result = (int)@object;
                    return true;
                }
            }
            else if (type_Temp == typeof(double))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    double @double;
                    //if (double.TryParse((string)@object, out @double))
                    //{
                    //    result = @double;
                    //    return true;
                    //}
                    if (TryParseDouble((string)@object, out @double))
                    {
                        result = @double;
                        return true;
                    }
                }
                else if (IsNumeric(@object) && !(@object is Type))
                {
                    result = System.Convert.ToDouble(@object);
                    return true;
                }
                else if (@object is bool)
                {
                    double @double = 0;
                    if ((bool)@object)
                        @double = 1;

                    result = @double;
                    return true;
                }
            }
            else if (type_Temp == typeof(uint))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    uint @uint;
                    if (uint.TryParse((string)@object, out @uint))
                    {
                        result = @uint;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    result = System.Convert.ToUInt32(@object);
                    return true;
                }
                else if (@object is SAMColor)
                {
                    result = Convert.ToUint(((SAMColor)@object).ToColor());
                    return true;
                }
            }
            else if (type_Temp == typeof(short))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    short @short;
                    if (short.TryParse((string)@object, out @short))
                    {
                        result = @short;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    result = System.Convert.ToInt16(@object);
                    return true;
                }
            }
            else if (type_Temp == typeof(byte))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    if (byte.TryParse((string)@object, out byte @byte))
                    {
                        result = @byte;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    result = System.Convert.ToByte(@object);
                    return true;
                }
            }
            else if (type_Temp == typeof(long))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    long @long;
                    if (long.TryParse((string)@object, out @long))
                    {
                        result = @long;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    result = System.Convert.ToInt64(@object);
                    return true;
                }
            }
            else if (type_Temp == typeof(Guid))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    if (System.Guid.TryParse((string)@object, out Guid guid))
                    {
                        result = guid;
                        return true;
                    }
                }
            }
            else if (type_Temp == typeof(DateTime))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    DateTime dateTime;
                    if (DateTime.TryParse((string)@object, out dateTime))
                    {
                        result = dateTime;
                        return true;
                    }
                }
                else if (IsNumeric(@object))
                {
                    if (@object is double)
                        result = DateTime.FromOADate((double)@object);
                    else
                        result = new DateTime(System.Convert.ToInt64(@object));

                    return true;
                }
            }
            else if (type_Temp == typeof(System.Drawing.Color))
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is Type)
                {
                    return false;
                }

                if (@object is string)
                {
                    string @string = (string)@object;
                    if (@string.StartsWith("##"))
                    {
                        result = Convert.ToColor(@string);
                        if (!result.Equals(System.Drawing.Color.Empty))
                            return true;
                    }

                    int @int;
                    if (int.TryParse(@string, out @int))
                    {
                        result = Convert.ToColor(@int);
                        return true;
                    }

                    uint @uint;
                    if (uint.TryParse(@string, out @uint))
                    {
                        result = Convert.ToColor(@uint);
                        return true;
                    }

                    result = Convert.ToColor(@string);
                    if (!result.Equals(System.Drawing.Color.Empty))
                        return true;

                }
                else if (@object is SAMColor)
                {
                    result = ((SAMColor)@object).ToColor();
                    return true;
                }
                else if (@object is int)
                {
                    result = Convert.ToColor((int)@object);
                    return true;
                }
                else if (@object is uint)
                {
                    result = Convert.ToColor((uint)@object);
                    return true;
                }
            }
            else if (typeof(IJSAMObject).IsAssignableFrom(type_Temp))
            {
                if (@object is string)
                {
                    List<IJSAMObject> sAMObjects = Convert.ToSAM((string)@object);
                    if (sAMObjects != null && sAMObjects.Count != 0)
                    {
                        IJSAMObject jSAMObject = sAMObjects.Find(x => x != null && type_Temp.IsAssignableFrom(x.GetType()));
                        if (jSAMObject != null)
                        {
                            result = jSAMObject;
                            return true;
                        }
                    }
                    else if (typeof(SAMColor).IsAssignableFrom(type_Temp))
                    {
                        if (int.TryParse((string)@object, out int int_color))
                        {
                            result = new SAMColor(Convert.ToColor(int_color));
                            return true;
                        }
                        else
                        {
                            string value = (string)@object;
                            if (!string.IsNullOrWhiteSpace(value) && value.Contains(","))
                            {
                                string[] values = value.Split(',');
                                if (values.Length == 3)
                                {
                                    if (int.TryParse(values[0], out int r) && int.TryParse(values[1], out int g) && int.TryParse(values[2], out int b))
                                    {
                                        result = new SAMColor(System.Drawing.Color.FromArgb(r, g, b));
                                        return true;
                                    }
                                }
                                else if (values.Length == 4)
                                {
                                    if (int.TryParse(values[0], out int a) && int.TryParse(values[1], out int r) && int.TryParse(values[2], out int g) && int.TryParse(values[3], out int b))
                                    {
                                        result = new SAMColor(System.Drawing.Color.FromArgb(a, r, g, b));
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                if (type_Object == typeof(SAMColor))
                {
                    System.Drawing.Color color = System.Drawing.Color.Empty;
                    if (TryConvert(@object, out color))
                    {
                        if (color == System.Drawing.Color.Empty)
                        {
                            result = default;
                            return true;
                        }

                        result = new SAMColor(color);
                        return true;
                    }
                }

                if (type_Object == typeof(System.Drawing.Color))
                {
                    result = new SAMColor((System.Drawing.Color)@object);
                    return true;
                }
            }
            else if (typeof(JsonObject).IsAssignableFrom(type_Temp))
            {
                if (@object is string)
                {
                    result = JsonNode.Parse((string)@object) as JsonObject;
                    return true;
                }
            }
            else if (type_Temp.IsEnum)
            {
                if (@object == null)
                {
                    return false;
                }

                if (@object is string)
                {
                    string @string = ((string)@object).Replace(" ", string.Empty).ToUpper();
                    if (string.IsNullOrEmpty(@string))
                    {
                        return false;
                    }

                    foreach (Enum @enum in System.Enum.GetValues(type_Temp))
                    {
                        string name = @enum.ToString().ToUpper();
                        if (@string.Equals(name))
                        {
                            result = @enum;
                            return true;
                        }

                        string description = Description(@enum)?.Replace(" ", string.Empty)?.ToUpper();
                        if (@string.Equals(description))
                        {
                            result = @enum;
                            return true;
                        }
                    }
                }
            }

            result = default;
            return false;
        }

        public static bool TryConvert<T>(this object? @object, out T? result)
        {
            result = default;

            object result_Object;
            if (!TryConvert(@object, out result_Object, typeof(T)))
                return false;

            result = (T)result_Object;
            return true;
        }

        private static bool TryConvertJsonNode(JsonNode jsonNode, out object? result, Type type)
        {
            result = default;

            JsonValueKind jsonValueKind = jsonNode.GetValueKind();
            if (jsonValueKind == JsonValueKind.Null)
            {
                return type == typeof(string);
            }

            if (type == typeof(JsonNode) || type.IsAssignableFrom(jsonNode.GetType()))
            {
                result = jsonNode;
                return true;
            }

            if (type == typeof(string))
            {
                result = jsonValueKind == JsonValueKind.String ? jsonNode.GetValue<string>() : jsonNode.ToJsonString();
                return true;
            }

            if (jsonValueKind == JsonValueKind.Object || jsonValueKind == JsonValueKind.Array)
            {
                // Allow IJSAMObject deserialization from a stored JsonObject so that
                // GetValue<T> round-trips correctly for parameters typed as IJSAMObject
                // (e.g. AnalyticalModelParameter.SolarModel).
                if (typeof(IJSAMObject).IsAssignableFrom(type) && jsonNode is JsonObject jsonObjectForISAM)
                {
                    return TryConvert((object?)jsonObjectForISAM.ToJsonString(), out result, type);
                }

                if (jsonNode is JsonArray jsonArray && TryConvertJsonArray(jsonArray, out result, type))
                {
                    return true;
                }

                return false;
            }

            if (jsonValueKind == JsonValueKind.String)
            {
                return TryConvert((object?)jsonNode.GetValue<string>(), out result, type);
            }

            if (jsonValueKind == JsonValueKind.True || jsonValueKind == JsonValueKind.False)
            {
                return TryConvert((object)jsonNode.GetValue<bool>(), out result, type);
            }

            if (jsonValueKind == JsonValueKind.Number)
            {
                return TryConvertJsonNumber(jsonNode.ToJsonString(), out result, type);
            }

            return false;
        }

        private static bool TryConvertJsonArray(JsonArray jsonArray, out object? result, Type type)
        {
            result = default;

            Type? elementType = GetEnumerableElementType(type);
            if (elementType == null || !typeof(IJSAMObject).IsAssignableFrom(elementType))
            {
                return false;
            }

            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
            foreach (JsonNode? itemNode in jsonArray)
            {
                // Mirror the object branch: each element is round-tripped through the
                // string -> IJSAMObject path rather than re-parsing the JsonObject inline.
                if (!(itemNode is JsonObject itemObject))
                {
                    return false;
                }

                if (!TryConvert((object?)itemObject.ToJsonString(), out object? item, elementType) || item == null)
                {
                    return false;
                }

                list.Add(item);
            }

            if (type.IsArray)
            {
                System.Array array = System.Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                result = array;
                return true;
            }

            if (type.IsAssignableFrom(list.GetType()))
            {
                result = list;
                return true;
            }

            try
            {
                result = Activator.CreateInstance(type, list);
                return result != null;
            }
            catch (MissingMethodException)
            {
                result = default;
                return false;
            }
        }

        private static Type? GetEnumerableElementType(Type type)
        {
            if (type == typeof(string))
            {
                return null;
            }

            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static bool TryConvertJsonNumber(string value, out object? result, Type type)
        {
            result = default;

            try
            {
                if (type == typeof(bool))
                {
                    if (TryParseDouble(value, out double @double))
                    {
                        result = System.Convert.ToInt64(@double) == 1;
                        return true;
                    }
                }
                else if (type == typeof(int))
                {
                    if (int.TryParse(value, out int @int))
                    {
                        result = @int;
                        return true;
                    }

                    if (TryParseDouble(value, out double @double))
                    {
                        result = System.Convert.ToInt32(@double);
                        return true;
                    }
                }
                else if (type == typeof(double))
                {
                    if (TryParseDouble(value, out double @double))
                    {
                        result = @double;
                        return true;
                    }
                }
                else if (type == typeof(uint))
                {
                    if (uint.TryParse(value, out uint @uint))
                    {
                        result = @uint;
                        return true;
                    }
                }
                else if (type == typeof(short))
                {
                    if (short.TryParse(value, out short @short))
                    {
                        result = @short;
                        return true;
                    }
                }
                else if (type == typeof(byte))
                {
                    if (byte.TryParse(value, out byte @byte))
                    {
                        result = @byte;
                        return true;
                    }
                }
                else if (type == typeof(long))
                {
                    if (long.TryParse(value, out long @long))
                    {
                        result = @long;
                        return true;
                    }
                }
                else if (type == typeof(DateTime))
                {
                    if (long.TryParse(value, out long @long))
                    {
                        result = new DateTime(@long);
                        return true;
                    }

                    if (TryParseDouble(value, out double @double))
                    {
                        result = DateTime.FromOADate(@double);
                        return true;
                    }
                }
            }
            catch (OverflowException)
            {
                result = default;
            }

            return false;
        }
    }
}
