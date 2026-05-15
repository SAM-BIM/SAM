// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Reflection;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class Setting : SAMObject
    {
        private DateTime created;
        private DateTime updated;

        public Setting()
            : base()
        {
            created = DateTime.Now;
            updated = DateTime.Now;
        }

        public Setting(Assembly assembly)
            : base(Query.Guid(assembly), Query.Name(assembly))
        {
            created = DateTime.Now;
            updated = DateTime.Now;
        }
        public Setting(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public DateTime Created
        {
            get
            {
                return created;
            }
        }

        public DateTime Updated
        {
            get
            {
                return updated;
            }
        }

        public void Clear()
        {
        }

        public bool Add(string name, IJSAMObject value)
        {
            return Add(name, (object)value);
        }

        public bool Add(string name, string value)
        {
            return Add(name, (object)value);
        }

        public bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            ParameterSet parameterSet = GetParameterSet(Assembly.GetAssembly(GetType()));
            if (parameterSet == null)
                return false;

            return parameterSet.Contains(name);
        }

        public bool TryGetValue<T>(string name, out T value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            Assembly assembly = Assembly.GetAssembly(GetType());

            ParameterSet parameterSet = GetParameterSet(assembly);
            if (parameterSet == null)
                return false;

            if (!parameterSet.Contains(name))
                return false;

            object @object = parameterSet.ToObject(name);

            if (@object == null && value == null)
                return true;

            if (@object is T)
            {
                value = (T)@object;
                return true;
            }

            return false;
        }

        public T GetValue<T>(string name)
        {
            T result;

            if (!TryGetValue(name, out result))
                return default;

            return result;
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
                return false;

            created = jsonObject["Created"]?.GetValue<DateTime>() ?? default;
            updated = jsonObject["Updated"]?.GetValue<DateTime>() ?? default;
            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jObject = base.ToJsonObject();
            if (jObject == null)
                return null;

            jObject["Created"] = created;
            jObject["Updated"] = DateTime.Now;
            return jObject;
        }

        private bool Add(string name, object @object)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            Assembly assembly = Assembly.GetAssembly(GetType());

            ParameterSet parameterSet = GetParameterSet(assembly);

            if (parameterSet == null)
            {
                parameterSet = new ParameterSet(assembly);
                Add(parameterSet);
            }

            return parameterSet.Add(name, @object as dynamic);
        }
    }
}
