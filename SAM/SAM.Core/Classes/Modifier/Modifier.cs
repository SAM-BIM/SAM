// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class Modifier : IModifier
    {
        public Modifier()
        {

        }

        public Modifier(Modifier modifier)
        {

        }
        public Modifier(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Query.FullTypeName(this)
            };

            return jsonObject;
        }
    }
}
