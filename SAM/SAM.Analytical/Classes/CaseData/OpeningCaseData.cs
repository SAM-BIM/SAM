// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class OpeningCaseData : BuiltInCaseData
    {
        private double openingAngle;

        public OpeningCaseData(double openingAngle)
            : base(nameof(OpeningCaseData))
        {
            this.openingAngle = openingAngle;
        }

        public OpeningCaseData(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public OpeningCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public OpeningCaseData(OpeningCaseData openingCaseData)
            : base(openingCaseData)
        {
            if (openingCaseData != null)
            {
                openingAngle = openingCaseData.openingAngle;
            }
        }

        public double OpeningAngle
        {
            get
            {
                return openingAngle;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("OpeningAngle"))
            {
                openingAngle = jsonObject["OpeningAngle"]?.GetValue<double>() ?? double.NaN;
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

            if (!double.IsNaN(openingAngle))
            {
                result["OpeningAngle"] = openingAngle;
            }

            return result;
        }
    }
}
