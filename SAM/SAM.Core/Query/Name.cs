// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Attributes;
using System;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static string Name(this JsonObject jsonObject)
        {
            return jsonObject?["Name"]?.GetValue<string>();
        }

        public static string Name(this Assembly assembly)
        {
            string name = null;

            object[] customAttributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (customAttributes != null && customAttributes.Length > 0)
                name = ((AssemblyTitleAttribute)customAttributes.First()).Title;

            if (name == null)
                name = assembly.ManifestModule.Name;

            return name;
        }

        public static string Name(this Enum @enum)
        {
            string result = null;

            ParameterProperties parameterProperties = ParameterProperties.Get(@enum);
            if (parameterProperties != null)
            {
                result = parameterProperties.Name;
            }

            if (result == null)
                result = @enum.ToString();

            return result;
        }
    }
}
