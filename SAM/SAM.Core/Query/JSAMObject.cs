// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Reflection;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public static partial class Query
    {
        public static IJSAMObject IJSAMObject(this JObject jObject)
        {
            return IJSAMObject(jObject?.Node as JsonObject);
        }

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
                return new JSAMObjectWrapper(new JObject(jsonObject));
            }

            Type type = Type(fullTypeName);
            if (type == null)
            {
                return new JSAMObjectWrapper(new JObject(jsonObject));
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

            JObject jObject = new JObject(jsonObject);
            constructorInfo = type.GetConstructor(new Type[] { typeof(JObject) });
            if (constructorInfo == null)
            {
                constructorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(JObject) }, null);
            }

            if (constructorInfo == null)
            {
                IJSAMObject result = Activator.CreateInstance(type) as IJSAMObject;
                if (result != null && result.FromJsonObject(jsonObject))
                {
                    return result;
                }

                return new JSAMObjectWrapper(jObject);
            }

            return constructorInfo.Invoke(new object[] { jObject }) as IJSAMObject;
        }

        public static T IJSAMObject<T>(this JObject jObject) where T : IJSAMObject
        {
            IJSAMObject jSAMObject = IJSAMObject(jObject);
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
