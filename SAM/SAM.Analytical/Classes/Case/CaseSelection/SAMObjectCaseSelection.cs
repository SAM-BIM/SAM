// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public abstract class SAMObjectCaseSelection<TJSAMObject> : CaseSelection where TJSAMObject : IJSAMObject
    {
        private List<TJSAMObject> objects;

        public SAMObjectCaseSelection()
        {
            objects = [];
        }

        public SAMObjectCaseSelection(IEnumerable<TJSAMObject> objects)
        {
            this.objects = objects == null ? [] : [.. objects];
        }

        public SAMObjectCaseSelection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public List<TJSAMObject> Objects
        {
            get
            {
                return objects;
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["Objects"] is JsonArray objectsArray)
            {
                objects = [];
                foreach (JsonNode node in objectsArray)
                {
                    if (node is JsonObject objectJson)
                    {
                        TJSAMObject @object = Core.Query.IJSAMObject<TJSAMObject>(new JObject((JsonObject)objectJson.DeepClone()));
                        if (@object != null)
                        {
                            objects.Add(@object);
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

            if (objects != null)
            {
                JsonArray objectsArray = new JsonArray();

                foreach (TJSAMObject @object in objects)
                {
                    if (@object?.ToJObject()?.Node is JsonObject objectJson)
                    {
                        objectsArray.Add(objectJson.DeepClone());
                    }
                }

                result["Objects"] = objectsArray;
            }

            return result;
        }
    }

    public class SAMObjectCaseSelection : SAMObjectCaseSelection<IJSAMObject>
    {
        public SAMObjectCaseSelection()
            : base()
        {
        }

        public SAMObjectCaseSelection(IEnumerable<IJSAMObject> objects)
            : base(objects)
        {
        }

        public SAMObjectCaseSelection(JObject jObject)
            : base(jObject)
        {
        }
    }
}
