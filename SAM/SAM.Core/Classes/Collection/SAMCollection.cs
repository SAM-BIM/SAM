// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class SAMCollection<T> : Collection<T>, ISAMObject where T : IJSAMObject
    {
        private string name;
        private Guid guid;
        private List<ParameterSet> parameterSets;

        public SAMCollection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public SAMCollection(SAMCollection<T> sAMCollection)
        {
            if (sAMCollection != null)
            {
                name = sAMCollection.name;
                guid = sAMCollection.guid;
                parameterSets = sAMCollection.parameterSets?.Clone();

                foreach (T t in sAMCollection)
                {
                    Add(t);
                }
            }
        }

        public SAMCollection()
        {
            guid = Guid.NewGuid();
        }

        public SAMCollection(T t)
        {
            guid = Guid.NewGuid();
            Add(t);
        }

        public SAMCollection(IEnumerable<T> ts)
        {
            guid = Guid.NewGuid();

            if (ts == null)
                return;

            foreach (T t in ts)
                Add(t);
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public Guid Guid
        {
            get
            {
                return guid;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
                return false;

            name = Query.Name(jsonObject);
            guid = Query.Guid(jsonObject);

            if (jsonObject["ParameterSets"] is JsonArray parameterSetsArray)
            {
                parameterSets = new List<ParameterSet>();
                foreach (JsonNode node in parameterSetsArray)
                {
                    if (node is JsonObject parameterSetJson)
                    {
                        parameterSets.Add(new ParameterSet(new JObject((JsonObject)parameterSetJson.DeepClone())));
                    }
                }
            }

            if (jsonObject["Collection"] is JsonArray collectionArray)
            {
                foreach (JsonNode node in collectionArray)
                {
                    if (node is JsonObject itemJson)
                    {
                        Add(Create.IJSAMObject<T>(itemJson));
                    }
                }
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JsonObject jsonObject = ToJsonObject();
            return jsonObject == null ? null : new JObject(jsonObject);
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            if (name != null)
                jsonObject["Name"] = name;

            jsonObject["Guid"] = guid.ToString();

            if (parameterSets != null)
            {
                JsonArray parameterSetsArray = new JsonArray();
                foreach (ParameterSet parameterSet in parameterSets)
                {
                    if (parameterSet?.ToJsonObject() is JsonObject parameterSetJson)
                    {
                        parameterSetsArray.Add(parameterSetJson.DeepClone());
                    }
                }
                jsonObject["ParameterSets"] = parameterSetsArray;
            }

            JsonArray collectionArray = new JsonArray();
            foreach (T t in this)
            {
                if (t?.ToJsonObject() is JsonObject itemJson)
                {
                    collectionArray.Add(itemJson.DeepClone());
                }
            }
            jsonObject["Collection"] = collectionArray;

            return jsonObject;
        }

        public new List<T> Items
        {
            get
            {
                IList<T> items = base.Items;
                if (items == null)
                {
                    return null;
                }

                List<T> result = new List<T>();
                foreach (T t in items)
                {
                    T t_temp = t == null ? default : Query.Clone(t);
                    result.Add(t_temp);
                }

                return result;
            }
        }
    }
}
