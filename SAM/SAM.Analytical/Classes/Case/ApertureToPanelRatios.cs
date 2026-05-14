// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class ApertureToPanelRatios : IJSAMObject
    {
        private List<ApertureToPanelRatio> apertureToPanelRatios;

        public ApertureToPanelRatios(IEnumerable<ApertureToPanelRatio> apertureToPanelRatios)
        {
            this.apertureToPanelRatios = apertureToPanelRatios == null ? [] : [.. apertureToPanelRatios];
        }

        public ApertureToPanelRatios(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ApertureToPanelRatios(ApertureToPanelRatios apertureToPanelRatios)
        {
            if (apertureToPanelRatios is not null)
            {
                this.apertureToPanelRatios = [];
                foreach (var item in apertureToPanelRatios.apertureToPanelRatios)
                {
                    this.apertureToPanelRatios.Add(new ApertureToPanelRatio(item));
                }
            }
        }

        public ApertureToPanelRatio this[int index]
        {
            get
            {
                return apertureToPanelRatios[index];
            }

            set
            {
                apertureToPanelRatios[index] = value;

            }
        }

        public int Count
        {
            get
            {
                return apertureToPanelRatios?.Count ?? 0;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            return FromJsonObject(jObject?.Node as JsonObject);
        }

        protected virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject["ApertureToPanelRatios"] is JsonArray apertureToPanelRatiosArray)
            {
                apertureToPanelRatios = [];
                foreach (JsonNode node in apertureToPanelRatiosArray)
                {
                    if (node is JsonObject apertureToPanelRatioJson)
                    {
                        ApertureToPanelRatio apertureToPanelRatio = Core.Query.IJSAMObject<ApertureToPanelRatio>(new JObject((JsonObject)apertureToPanelRatioJson.DeepClone()));
                        if (apertureToPanelRatio is not null)
                        {
                            apertureToPanelRatios.Add(apertureToPanelRatio);
                        }
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

        protected virtual JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (apertureToPanelRatios != null)
            {
                JsonArray apertureToPanelRatiosArray = new JsonArray();
                foreach (ApertureToPanelRatio apertureToPanelRatio in apertureToPanelRatios)
                {
                    if (apertureToPanelRatio?.ToJObject()?.Node is JsonObject apertureToPanelRatioJson)
                    {
                        apertureToPanelRatiosArray.Add(apertureToPanelRatioJson.DeepClone());
                    }
                }

                result["ApertureToPanelRatios"] = apertureToPanelRatiosArray;
            }

            return result;
        }
    }
}
