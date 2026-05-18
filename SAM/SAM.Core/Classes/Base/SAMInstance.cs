// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors


using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class SAMInstance<T> : SAMObject, ISAMInstance where T : SAMType
    {
        private T type;

        public SAMInstance(SAMInstance<T> instance)
            : base(instance)
        {
            type = instance.Type;
        }

        public SAMInstance(SAMInstance<T> instance, T type)
            : base(type?.Name, instance)
        {
            this.type = type;
        }

        public SAMInstance(Guid guid, SAMInstance<T> instance)
            : base(guid, instance)
        {
            type = instance?.Type;
        }

        public SAMInstance(Guid guid, T type)
            : base(guid, type?.Name)
        {
            this.type = type;
        }

        public SAMInstance(T type)
            : base(type?.Name)
        {
            this.type = type;
        }

        public SAMInstance(string? prefix, T type)
            : base(GetName(prefix, type?.Name))
        {
            this.type = type;
        }

        private static string? GetName(string prefix, string name)
        {
            if (prefix is null && name is null)
            {
                return null;
            }

            if (prefix is null)
            {
                return name;
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                return name ?? prefix;
            }

            if (name is null)
            {
                return prefix;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return prefix ?? name;
            }

            List<string> values = [];
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                values.Add(prefix);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                values.Add(name);
            }

            return string.Join(" ", values);
        }

        public SAMInstance(Guid guid, IEnumerable<ParameterSet> parameterSets, T type)
            : base(guid, type?.Name, parameterSets)
        {
            this.type = type;
        }
        public SAMInstance(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public T Type
        {
            get
            {
                return type?.Clone();
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                name = value.Name;
                type = value;
            }
        }

        public Guid TypeGuid
        {
            get
            {
                if (type == null)
                    return Guid.Empty;

                return type.Guid;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Type"] is JsonObject typeObject)
            {
                type = Create.IJSAMObject<T>(typeObject as JsonObject);
            }
            else
            {
                //TODO: Remove in the future. This is for backward compability only
                if (jsonObject["SAMType"] is JsonObject samTypeObject)
                {
                    type = Create.IJSAMObject<T>(samTypeObject as JsonObject);
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
                return jsonObject;

            if (type != null)
            {
                if (type.ToJsonObject() is JsonObject typeObject)
                    jsonObject["Type"] = typeObject.DeepClone();
            }

            return jsonObject;
        }
    }
}
