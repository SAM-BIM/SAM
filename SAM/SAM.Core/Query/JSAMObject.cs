// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Reflection;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static IJSAMObject IJSAMObject(this JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return null;
            }

            jsonObject = jsonObject.DeepClone() as JsonObject;

            string fullTypeName = FullTypeName(jsonObject);
            if (string.IsNullOrWhiteSpace(fullTypeName))
            {
                return new JSAMObjectWrapper(jsonObject);
            }

            Type type = Type(fullTypeName);
            if (type == null)
            {
                return new JSAMObjectWrapper(jsonObject);
            }

            ConstructorInfo constructorInfo = type.GetConstructor(new Type[] { typeof(JsonObject) });
            if (constructorInfo == null)
            {
                constructorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(JsonObject) }, null);
            }

            if (constructorInfo != null)
            {
                return constructorInfo.Invoke(new object[] { jsonObject }) as IJSAMObject;
            }

            IJSAMObject result = Activator.CreateInstance(type) as IJSAMObject;
            if (result != null && result.FromJsonObject(jsonObject))
            {
                return result;
            }

            return new JSAMObjectWrapper(jsonObject);
        }

        public static T IJSAMObject<T>(this JsonObject jsonObject) where T : IJSAMObject
        {
            IJSAMObject jSAMObject = IJSAMObject(jsonObject);
            if (jSAMObject == null)
            {
                return default;
            }

            if (!(jSAMObject is T))
            {
                return default;
            }

            return (T)jSAMObject;
        }
    }
}
