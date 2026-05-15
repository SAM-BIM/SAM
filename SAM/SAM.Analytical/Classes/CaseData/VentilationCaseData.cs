// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class VentilationCaseData : BuiltInCaseData
    {
        private double aCH;

        public VentilationCaseData(double aCH)
            : base(nameof(OpeningCaseData))
        {
            this.aCH = aCH;
        }

        public VentilationCaseData(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public VentilationCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public VentilationCaseData(VentilationCaseData ventilationCaseData)
            : base(ventilationCaseData)
        {
            if (ventilationCaseData != null)
            {
                aCH = ventilationCaseData.aCH;
            }
        }

        public double ACH
        {
            get
            {
                return aCH;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("ACH"))
            {
                aCH = jsonObject["ACH"]?.GetValue<double>() ?? double.NaN;
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

            if (!double.IsNaN(aCH))
            {
                result["ACH"] = aCH;
            }

            return result;
        }
    }
}
