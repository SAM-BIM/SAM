// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class NCMNameCollection : IEnumerable<NCMName>, IJSAMObject
    {
        private List<NCMName> nCMNames = new List<NCMName>();

        public NCMNameCollection()
        {

        }

        public NCMNameCollection(IEnumerable<NCMName> nCMNames)
        {
            if (nCMNames != null)
            {
                this.nCMNames = new List<NCMName>(nCMNames);
            }
        }

        public NCMNameCollection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public bool Add(NCMName nCMName)
        {
            if (nCMName == null)
            {
                return false;
            }

            nCMNames.Add(nCMName);
            return true;
        }

        public bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            nCMNames = new List<NCMName>();

            if (jsonObject["NCMNames"] is JsonArray nCMNamesArray)
            {
                foreach (JsonNode node in nCMNamesArray)
                {
                    if (node is JsonObject ncmNameJson)
                    {
                        nCMNames.Add(new NCMName(new JObject((JsonObject)ncmNameJson.DeepClone())));
                    }
                }
            }

            return true;
        }

        public IEnumerator<NCMName> GetEnumerator()
        {
            return nCMNames.GetEnumerator();
        }

        public JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (nCMNames != null)
            {
                JsonArray nCMNamesArray = new JsonArray();
                foreach (NCMName nCMName in nCMNames)
                {
                    if (nCMName?.ToJsonObject() is JsonObject ncmNameJson)
                    {
                        nCMNamesArray.Add(ncmNameJson.DeepClone());
                    }
                }

                jsonObject["NCMNames"] = nCMNamesArray;
            }

            return jsonObject;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
