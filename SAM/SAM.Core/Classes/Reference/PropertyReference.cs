// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    /// <summary>
    /// Property reference in format {TypeName}::[{Reference}]::"{PropertyName}"
    /// 
    /// example:
    /// Space::"InternalCondition"
    /// Space::[0]::"Name"
    /// </summary>
    public class PropertyReference : ObjectReference
    {
        private string propertyName;

        public PropertyReference(string propertyName)
            : base(null as string)
        {
            this.propertyName = propertyName;
        }

        public PropertyReference(string typeName, string propertyName)
            : base(typeName)
        {
            this.propertyName = propertyName;
        }

        public PropertyReference(ObjectReference objectReference, string propertyName)
            : base(objectReference)
        {
            this.propertyName = propertyName;
        }

        public PropertyReference(string typeName, Reference? reference, string propertyName)
            : base(typeName, reference)
        {
            this.propertyName = propertyName;
        }

        public PropertyReference(System.Type type, string propertyName)
            : base(type)
        {
            this.propertyName = propertyName;
        }

        public PropertyReference(SAMObject sAMObject, string propertyName)
            : base(sAMObject)
        {
            this.propertyName = propertyName;
        }
        public PropertyReference(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public PropertyReference(PropertyReference propertyReference)
            : base(propertyReference)
        {
            propertyName = propertyReference?.propertyName;
        }

        public string PropertyName
        {
            get
            {
                return propertyName;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("PropertyName"))
            {
                propertyName = jsonObject["PropertyName"]?.GetValue<string>();
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (propertyName != null)
            {
                result["PropertyName"] = propertyName;
            }

            return result;
        }

        public override bool IsValid()
        {
            return !string.IsNullOrEmpty(propertyName);
        }

        public override string ToString()
        {
            List<string> values = new List<string>();

            string value = base.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                values.Add(string.Format(@"""{0}""", propertyName));
            }

            return string.Join("::", values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            bool result = base.Equals(obj);
            if (!result)
            {
                return result;
            }

            PropertyReference propertyReference = obj as PropertyReference;
            if (propertyReference == null)
            {
                result = false;
                return result;
            }

            return propertyReference.PropertyName == propertyName;

        }

        public static bool operator ==(PropertyReference propertyReference_1, PropertyReference propertyReference_2)
        {
            if (ReferenceEquals(propertyReference_1, null) && ReferenceEquals(propertyReference_2, null))
            {
                return true;
            }

            if (ReferenceEquals(propertyReference_1, null) || ReferenceEquals(propertyReference_2, null))
            {
                return false;
            }

            return propertyReference_1.Equals(propertyReference_2);
        }

        public static bool operator !=(PropertyReference propertyReference_1, PropertyReference propertyReference_2)
        {
            return !(propertyReference_1 == propertyReference_2);
        }
    }
}
