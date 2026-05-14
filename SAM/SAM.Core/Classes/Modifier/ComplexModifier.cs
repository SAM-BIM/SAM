// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public class ComplexModifier : Modifier, IComplexModifier<IModifier>
    {
        public ComplexModifier()
            : base()
        {

        }

        public ComplexModifier(IEnumerable<IModifier> modifiers)
            : base()
        {
            Modifiers = modifiers == null ? null : modifiers.ToList().ConvertAll(x => x?.Clone());
        }

        public ComplexModifier(ComplexModifier complexModifier)
            : base(complexModifier)
        {
            if (complexModifier != null)
            {
                Modifiers = complexModifier.Modifiers.ConvertAll(x => x.Clone());
            }
        }

        public ComplexModifier(JObject jObject)
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
                Modifiers = new List<IModifier>();
                foreach (JsonNode node in modifiersArray)
                {
                    if (node is JsonObject modifierJson)
                    {
                        Modifiers.Add(Query.IJSAMObject<IModifier>(modifierJson));
                    }
                }
            }

            return true;
        }

        public List<IModifier> Modifiers { get; set; }

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
    }
}
