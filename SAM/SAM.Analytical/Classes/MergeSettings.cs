// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class MergeSettings : IJSAMObject
    {
        private Dictionary<string, TypeMergeSettings> dictionary;

        public MergeSettings(IEnumerable<TypeMergeSettings> typeMergeSettings)
        {
            if (typeMergeSettings != null)
            {
                dictionary = new Dictionary<string, TypeMergeSettings>();

                foreach (TypeMergeSettings typeMergeSettings_Temp in typeMergeSettings)
                {
                    if (typeMergeSettings_Temp?.TypeName == null)
                    {
                        continue;
                    }

                    dictionary[typeMergeSettings_Temp.TypeName] = typeMergeSettings_Temp.Clone();
                }
            }
        }

        public MergeSettings(MergeSettings mergeSettings)
            : this(mergeSettings?.dictionary?.Values)
        {

        }

        public MergeSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public TypeMergeSettings this[string name]
        {
            get
            {
                if (dictionary == null || !dictionary.TryGetValue(name, out TypeMergeSettings result))
                {
                    return null;
                }

                return result;
            }
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

            if (jsonObject["TypeMergeSettings"] is JsonArray typeMergeSettingsArray)
            {
                dictionary = new Dictionary<string, TypeMergeSettings>();
                foreach (JsonNode node in typeMergeSettingsArray)
                {
                    if (node is JsonObject typeMergeSettingsJson)
                    {
                        TypeMergeSettings typeMergeSettings = Core.Query.IJSAMObject<TypeMergeSettings>(typeMergeSettingsJson as JsonObject);
                        if (typeMergeSettings?.TypeName == null)
                        {
                            continue;
                        }

                        dictionary[typeMergeSettings.TypeName] = typeMergeSettings;
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

        public JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (dictionary != null)
            {
                JsonArray typeMergeSettingsArray = new JsonArray();
                foreach (TypeMergeSettings typeMergeSettings in dictionary.Values)
                {
                    if (typeMergeSettings?.ToJsonObject() is JsonObject typeMergeSettingsJson)
                    {
                        typeMergeSettingsArray.Add(typeMergeSettingsJson.DeepClone());
                    }
                }

                jsonObject["TypeMergeSettings"] = typeMergeSettingsArray;
            }

            return jsonObject;
        }
    }
}
