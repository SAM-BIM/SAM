// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// Opening Properties
    /// </summary>
    public class OpeningProperties : ParameterizedSAMObject, ISingleOpeningProperties
    {
        public double Factor { get; set; } = 1;

        private double dischargeCoefficient { get; set; }

        public OpeningProperties()
        {

        }

        public OpeningProperties(double dischargeCoefficient)
        {
            this.dischargeCoefficient = dischargeCoefficient;
        }

        public OpeningProperties(JObject jObject)
            : base(jObject)
        {
        }

        public OpeningProperties(OpeningProperties openingProperties)
            : base(openingProperties)
        {
            if (openingProperties != null)
            {
                Factor = openingProperties.Factor;
                dischargeCoefficient = openingProperties.dischargeCoefficient;
            }
        }

        public OpeningProperties(IOpeningProperties openingProperties, double dischargeCoefficient)
            : base(openingProperties as ParameterizedSAMObject)
        {
            this.dischargeCoefficient = dischargeCoefficient;
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("DischargeCoefficient"))
            {
                dischargeCoefficient = jsonObject["DischargeCoefficient"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Factor"))
            {
                Factor = jsonObject["Factor"]?.GetValue<double>() ?? double.NaN;
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject jsonObject = base.ToJsonObject();
            if (jsonObject == null)
            {
                return null;
            }

            if (!double.IsNaN(dischargeCoefficient))
            {
                jsonObject["DischargeCoefficient"] = dischargeCoefficient;
            }

            if (!double.IsNaN(Factor))
            {
                jsonObject["Factor"] = Factor;
            }

            return jsonObject;
        }

        public double GetDischargeCoefficient()
        {
            return dischargeCoefficient;
        }

        public double GetFactor()
        {
            return Factor;
        }

        public ISingleOpeningProperties SingleOpeningProperties
        {
            get
            {
                return this.Clone();
            }
        }
    }
}
