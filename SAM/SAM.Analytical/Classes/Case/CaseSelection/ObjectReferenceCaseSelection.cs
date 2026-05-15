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
        public ObjectReferenceCaseSelection(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        public IEnumerable<ObjectReference> ObjectReferences
        {
            get
            {
                return objectReferences;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
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
                        ObjectReference objectReference = Core.Query.IJSAMObject<ObjectReference>(objectReferenceJson as JsonObject);
                        if (objectReference != null)
                        {
                            objectReferences.Add(objectReference);
                        }
                    }
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
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
                    if (objectReference?.ToJsonObject() is JsonObject objectReferenceJson)
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
