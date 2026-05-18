// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Core
{
    public abstract class Filter : IFilter
    {
        public bool Inverted { get; set; } = false;

        public Filter()
        {
        }

        public Filter(bool inverted)
        {
            Inverted = inverted;
        }

        public Filter(Filter filter)
        {
            if (filter != null)
            {
                Inverted = filter.Inverted;
            }
        }
        public Filter(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        public abstract bool IsValid(IJSAMObject jSAMObject);

        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Inverted"))
            {
                Inverted = jsonObject["Inverted"]?.GetValue<bool>() ?? false;
            }
            return true;
        }

        public virtual JsonObject ToJsonObject()
        {
            return new JsonObject
            {
                ["_type"] = Query.FullTypeName(this),
                ["Inverted"] = Inverted
            };
        }
    }
}
