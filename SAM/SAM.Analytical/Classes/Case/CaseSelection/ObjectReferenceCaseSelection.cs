// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ObjectReferenceCaseSelection : CaseSelection
    {
        private List<ObjectReference> objectReferences;

        public ObjectReferenceCaseSelection()
        {
            objectReferences = [];
        }

        public ObjectReferenceCaseSelection(IEnumerable<ObjectReference> objectReferences)
        {
            this.objectReferences = objectReferences == null ? [] : [.. objectReferences];
        }

        public ObjectReferenceCaseSelection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public IEnumerable<ObjectReference> ObjectReferences
        {
            get
            {
                return objectReferences;
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["ObjectReferences"] is JsonArray objectReferencesArray)
            {
                objectReferences = [];
                foreach (JsonNode node in objectReferencesArray)
                {
                    if (node is JsonObject objectReferenceJson)
                    {
                        ObjectReference objectReference = Core.Query.IJSAMObject<ObjectReference>(new JObject((JsonObject)objectReferenceJson.DeepClone()));
                        if (objectReference != null)
                        {
                            objectReferences.Add(objectReference);
                        }
                    }
                }
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

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
    }
}
