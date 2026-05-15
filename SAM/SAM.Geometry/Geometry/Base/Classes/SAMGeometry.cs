// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Geometry
{
    public abstract class SAMGeometry : ISAMGeometry
    {
        public SAMGeometry()
        {
        }

        public SAMGeometry(JObject jObject)
        {
            FromJsonObject(jObject?.Node as System.Text.Json.Nodes.JsonObject);
        }

        public SAMGeometry(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public abstract ISAMGeometry Clone();

        // Bridge: most subclasses still override the public JObject variant
        // directly. The protected FromJsonObject hook lets newly-migrated
        // subclasses do their work against the BCL JsonObject API without
        // touching the shim. As subclasses migrate, they replace the public
        // override with a protected override of FromJsonObject.
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            return jsonObject != null;
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject jsonObject = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };
            return jsonObject;
        }
    }
}
