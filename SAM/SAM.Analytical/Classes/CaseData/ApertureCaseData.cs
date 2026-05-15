// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ApertureCaseData : BuiltInCaseData
    {
        private List<double> ratios;

        public ApertureCaseData(IEnumerable<double> ratios)
            : base(nameof(ApertureCaseData))
        {
            this.ratios = ratios == null ? [] : [.. ratios];
        }

        public ApertureCaseData(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public ApertureCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ApertureCaseData(ApertureCaseData apertureCaseData)
            : base(apertureCaseData)
        {
            if (apertureCaseData != null)
            {
                ratios = apertureCaseData.Ratios;
            }
        }

        public List<double> Ratios
        {
            get
            {
                return ratios?.ToList();
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["Ratios"] is JsonArray ratiosArray)
            {
                ratios = [];
                foreach (JsonNode node in ratiosArray)
                {
                    ratios.Add(node?.GetValue<double>() ?? double.NaN);
                }
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (ratios != null)
            {
                JsonArray rationsArray = new JsonArray();
                foreach (double value in ratios)
                {
                    rationsArray.Add(value);
                }
                result["Rations"] = rationsArray;
            }

            return result;
        }
    }
}
