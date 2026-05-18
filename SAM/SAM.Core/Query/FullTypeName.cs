// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static string FullTypeName(this JsonObject jsonObject)
        {
            if (jsonObject == null)
                return null;

            return jsonObject["_type"]?.GetValue<string>();
        }

        public static string FullTypeName(Type type)
        {
            if (type == null)
                return null;

            if (type.IsPrimitive)
                return type.FullName;

            if (type.IsGenericType)
            {
                Type[] types_Generic = type.GetGenericArguments();
                if (types_Generic != null && types_Generic.Length != 0)
                {
                    List<string> typeNames = new List<string>();
                    foreach (Type type_Generic in types_Generic)
                        typeNames.Add(string.Format("[{0}]", FullTypeName(type_Generic)));

                    return string.Format("{0}.{1}[{2}],{3}", type.Namespace, type.UnderlyingSystemType.Name, string.Join(",", typeNames), type.Assembly.GetName().Name);
                }
            }

            return string.Format("{0},{1}", type.FullName, type.Assembly.GetName().Name);
        }

        public static string FullTypeName(IJSAMObject jSAMObject)
        {
            if (jSAMObject == null)
                return null;

            return FullTypeName(jSAMObject.GetType());
        }
    }
}
