// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class NCMName : IJSAMObject
    {
        private string name;
        private string version;
        private string description;
        private string group;

        public NCMName(string name)
        {
            this.name = name;
            version = null;
            description = null;
            group = null;
        }

        public NCMName(string name, string version, string description, string group)
        {
            this.name = name;
            this.version = version;
            this.description = description;
            this.group = group;
        }
        public NCMName(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public string Group
        {
            get
            {
                return group;
            }
        }

        public string FullName
        {
            get
            {
                List<string> values = new List<string>();
                if (!string.IsNullOrWhiteSpace(group))
                {
                    values.Add(group);
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    values.Add(name);
                }

                return string.Join("_", values);
            }
        }

        public string UniqueId
        {
            get
            {
                List<string> values = new List<string>();
                if (!string.IsNullOrWhiteSpace(group))
                {
                    values.Add(group);
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    values.Add(name);
                }

                if (!string.IsNullOrWhiteSpace(version))
                {
                    values.Add(version);
                }


                return string.Join("_", values);
            }
        }
        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Name"))
            {
                name = jsonObject["Name"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Version"))
            {
                version = jsonObject["Version"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Description"))
            {
                description = jsonObject["Description"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("Group"))
            {
                group = jsonObject["Group"]?.GetValue<string>();
            }

            return true;
        }
        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (name != null)
            {
                jsonObject["Name"] = name;
            }

            if (version != null)
            {
                jsonObject["Version"] = version;
            }

            if (description != null)
            {
                jsonObject["Description"] = description;
            }

            if (group != null)
            {
                jsonObject["Group"] = group;
            }

            return jsonObject;
        }

        public override string ToString()
        {
            return FullName?.ToString();
        }


        public static implicit operator string(NCMName nCMName)
        {
            return nCMName?.FullName;
        }


        public static implicit operator NCMName(string value)
        {
            if (value == null)
            {
                return null;
            }

            int index = value.IndexOf('_');
            if (index == -1 || index == value.Length - 1)
            {
                return new NCMName(value);
            }

            string group = value.Substring(0, index);
            string name = value.Substring(index + 1);

            return new NCMName(name, null, null, group);
        }
    }
}
