// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    /// <summary>
    /// Parh reference in format {ObjectReference_1}->{ObjectRefernce_2}->{ObjectReference_3}
    /// 
    /// example:
    /// Space::"InternalCondition"->InternalCondition::"Name"
    /// Space->"InternalCondition"->"Name"
    /// Space->Zone::"Name"
    /// </summary>
    public class PathReference : IComplexReference, IEnumerable<ObjectReference>
    {
        private List<ObjectReference> objectReferences;

        public PathReference(IEnumerable<ObjectReference> objectReferences)
        {
            if (objectReferences != null)
            {
                this.objectReferences = objectReferences == null ? null : new List<ObjectReference>(objectReferences);
            }
        }

        public PathReference(IEnumerable<ObjectReference> objectReferences, ObjectReference objectReference)
        {
            if (objectReferences != null)
            {
                this.objectReferences = objectReferences == null ? null : new List<ObjectReference>(objectReferences);
            }

            if (objectReference != null)
            {
                if (this.objectReferences == null)
                {
                    this.objectReferences = new List<ObjectReference>();
                }

                this.objectReferences.Add(objectReference);
            }
        }

        public PathReference(JObject jObject)
        {
            FromJObject(jObject);
        }

        public override string ToString()
        {
            List<string> values = objectReferences?.ConvertAll(x => x?.ToString()).ConvertAll(x => x == null ? string.Empty : x);
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("->", values);
        }

        public bool IsValid()
        {
            return objectReferences != null && objectReferences.Count != 0 && objectReferences.TrueForAll(x => x.IsValid());
        }

        public IEnumerator<ObjectReference> GetEnumerator()
        {
            return objectReferences == null ? null : objectReferences.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        private bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["ObjectReferences"] is JsonArray objectReferencesArray)
            {
                objectReferences = new List<ObjectReference>();
                foreach (JsonNode node in objectReferencesArray)
                {
                    if (node is JsonObject objectReferenceJson)
                    {
                        ObjectReference objectReference = Query.IJSAMObject<ObjectReference>(new JObject((JsonObject)objectReferenceJson.DeepClone()));
                        if (objectReference != null)
                        {
                            objectReferences.Add(objectReference);
                        }
                    }
                }
            }

            return true;
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        private JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (objectReferences != null)
            {
                JsonArray objectReferencesArray = new JsonArray();

                foreach (ObjectReference objectReference in objectReferences)
                {
                    if (objectReference?.ToJObject()?.Node is JsonObject objectReferenceJson)
                    {
                        objectReferencesArray.Add(objectReferenceJson.DeepClone());
                    }
                }

                result["ObjectReferences"] = objectReferencesArray;
            }

            return result;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            PathReference pathReference = obj as PathReference;
            if (pathReference == null)
            {
                return false;
            }

            List<ObjectReference> objectReferences_Temp = pathReference.objectReferences;
            if ((objectReferences == null || objectReferences.Count == 0) && (objectReferences_Temp == null || objectReferences_Temp.Count == 0))
            {
                return true;
            }

            if (objectReferences == null || objectReferences.Count == 0 || objectReferences_Temp == null || objectReferences_Temp.Count == 0)
            {
                return false;
            }

            if (objectReferences.Count != objectReferences_Temp.Count)
            {
                return false;
            }

            for (int i = 0; i < objectReferences.Count; i++)
            {
                if (objectReferences[i] != objectReferences_Temp[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(PathReference pathReference_1, PathReference pathReference_2)
        {
            if (ReferenceEquals(pathReference_1, null) && ReferenceEquals(pathReference_2, null))
            {
                return true;
            }

            if (ReferenceEquals(pathReference_1, null) || ReferenceEquals(pathReference_2, null))
            {
                return false;
            }

            return pathReference_1.Equals(pathReference_2);
        }

        public static bool operator !=(PathReference pathReference_1, PathReference pathReference_2)
        {
            return !(pathReference_1 == pathReference_2);
        }
    }
}
