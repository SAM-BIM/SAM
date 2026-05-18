// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Multiple Opening Properties
    /// </summary>
    public class MultipleOpeningProperties : ParameterizedSAMObject, IOpeningProperties
    {
        private List<ISingleOpeningProperties> singleOpeningProperties;

        public MultipleOpeningProperties()
        {

        }
        public MultipleOpeningProperties(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public MultipleOpeningProperties(IEnumerable<ISingleOpeningProperties> singleOpeningProperties)
            : base()
        {
            this.singleOpeningProperties = singleOpeningProperties == null ? null : singleOpeningProperties.ToList().ConvertAll(x => Core.Query.Clone(x));
        }

        public MultipleOpeningProperties(MultipleOpeningProperties multipleOpeningProperties)
            : base(multipleOpeningProperties)
        {
            singleOpeningProperties = multipleOpeningProperties?.singleOpeningProperties?.ConvertAll(x => Core.Query.Clone(x));
        }

        public MultipleOpeningProperties(MultipleOpeningProperties multipleOpeningProperties, IEnumerable<ISingleOpeningProperties> singleOpeningProperties)
            : base(multipleOpeningProperties)
        {
            this.singleOpeningProperties = singleOpeningProperties == null ? null : singleOpeningProperties.ToList().ConvertAll(x => Core.Query.Clone(x));
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject["SingleOpeningProperties"] is JsonArray singleOpeningPropertiesArray)
            {
                singleOpeningProperties = new List<ISingleOpeningProperties>();
                foreach (JsonNode node in singleOpeningPropertiesArray)
                {
                    if (node is JsonObject openingPropertiesJson)
                    {
                        ISingleOpeningProperties openingProperties = Core.Query.IJSAMObject<ISingleOpeningProperties>(openingPropertiesJson as JsonObject);
                        if (openingProperties == null)
                        {
                            continue;
                        }

                        this.singleOpeningProperties.Add(openingProperties);
                    }
                }
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (singleOpeningProperties != null)
            {
                JsonArray singleOpeningPropertiesArray = new JsonArray();
                foreach (ISingleOpeningProperties singleOpeningPropertiesItem in singleOpeningProperties)
                {
                    if (singleOpeningPropertiesItem?.ToJsonObject() is JsonObject singleOpeningPropertiesJson)
                    {
                        singleOpeningPropertiesArray.Add(singleOpeningPropertiesJson.DeepClone());
                    }
                }

                jsonObject["SingleOpeningProperties"] = singleOpeningPropertiesArray;
            }

            return jsonObject;
        }

        public List<ISingleOpeningProperties> SingleOpeningProperties
        {
            get
            {
                return singleOpeningProperties?.ConvertAll(x => Core.Query.Clone(x));
            }
        }

        public double GetDischargeCoefficient()
        {
            ISingleOpeningProperties singleOpeningProperties = this.SingleOpeningProperties();
            if (singleOpeningProperties == null)
            {
                return double.NaN;
            }

            return singleOpeningProperties.GetDischargeCoefficient();
        }

        public double GetFactor()
        {
            ISingleOpeningProperties singleOpeningProperties = this.SingleOpeningProperties();
            if (singleOpeningProperties == null)
            {
                return double.NaN;
            }

            return singleOpeningProperties.GetFactor();
        }
    }
}
