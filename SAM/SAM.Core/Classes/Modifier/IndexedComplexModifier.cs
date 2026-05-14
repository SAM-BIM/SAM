// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class IndexedComplexModifier : IndexedModifier, IIndexedComplexModifier
    {
        public IndexedComplexModifier()
            : base()
        {

        }

        public IndexedComplexModifier(IEnumerable<IIndexedModifier> indexedModifiers)
            : base()
        {
            Modifiers = indexedModifiers == null ? null : indexedModifiers.ToList().ConvertAll(x => x?.Clone());
        }

        public IndexedComplexModifier(IndexedComplexModifier complexModifier)
            : base(complexModifier)
        {
            if (complexModifier != null)
            {
                Modifiers = complexModifier.Modifiers.ConvertAll(x => x.Clone());
            }
        }

        public IndexedComplexModifier(JObject jObject)
            : base(jObject)
        {

        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["Modifiers"] is JsonArray modifiersArray)
            {
                Modifiers = new List<IIndexedModifier>();
                foreach (JsonNode node in modifiersArray)
                {
                    if (node is JsonObject modifierJson)
                    {
                        Modifiers.Add(Query.IJSAMObject<IIndexedModifier>(modifierJson));
                    }
                }
            }

            return true;
        }

        public List<IIndexedModifier> Modifiers { get; set; }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result == null)
            {
                return null;
            }

            if (Modifiers != null)
            {
                JsonArray modifiersArray = new JsonArray();
                foreach (IModifier modifier in Modifiers)
                {
                    if (modifier?.ToJsonObject() is JsonObject modifierJson)
                    {
                        modifiersArray.Add(modifierJson.DeepClone());
                    }
                }

                result["Modifiers"] = modifiersArray;
            }

            return result;
        }

        public override bool ContainsIndex(int index)
        {
            if (Modifiers == null)
            {
                return false;
            }

            foreach (IIndexedModifier indexedModifier in Modifiers)
            {
                if (indexedModifier.ContainsIndex(index))
                {
                    return true;
                }
            }

            return false;
        }

        public override double GetCalculatedValue(int index, double value)
        {
            if (Modifiers == null)
            {
                return value;
            }

            foreach (IIndexedModifier indexedModifier in Modifiers)
            {
                if (indexedModifier.ContainsIndex(index))
                {
                    return indexedModifier.GetCalculatedValue(index, value);
                }
            }

            return value;
        }
    }
}
