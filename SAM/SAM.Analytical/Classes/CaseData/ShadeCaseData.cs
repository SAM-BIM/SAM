// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ShadeCaseData : BuiltInCaseData
    {
        private double overhangDepth = double.NaN;
        private double leftFinDepth = double.NaN;
        private double rightFinDepth = double.NaN;

        public ShadeCaseData()
            : base(nameof(ShadeCaseData))
        {

        }

        public ShadeCaseData(double overhangDepth, double leftFinDepth, double rightFinDepth)
            : base(nameof(ShadeCaseData))
        {
            this.overhangDepth = overhangDepth;
            this.leftFinDepth = leftFinDepth;
            this.rightFinDepth = rightFinDepth;
        }

        public ShadeCaseData(JObject jObject)
            : base(jObject)
        {

        }

        public ShadeCaseData(ShadeCaseData shadeCaseData)
            : base(shadeCaseData)
        {
            if (shadeCaseData != null)
            {
                overhangDepth = shadeCaseData.overhangDepth;
                leftFinDepth = shadeCaseData.leftFinDepth;
                rightFinDepth = shadeCaseData.rightFinDepth;
            }
        }

        public double OverhangDepth
        {
            get
            {
                return overhangDepth;
            }
        }

        public double LeftFinDepth
        {
            get
            {
                return leftFinDepth;
            }
        }

        public double RightFinDepth
        {
            get
            {
                return rightFinDepth;
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("OverhangDepth"))
            {
                overhangDepth = jsonObject["OverhangDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinDepth"))
            {
                leftFinDepth = jsonObject["LeftFinDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("RightFinDepth"))
            {
                rightFinDepth = jsonObject["RightFinDepth"]?.GetValue<double>() ?? double.NaN;
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

            if (!double.IsNaN(overhangDepth))
            {
                result["OverhangDepth"] = overhangDepth;
            }

            if (!double.IsNaN(leftFinDepth))
            {
                result["LeftFinDepth"] = leftFinDepth;
            }

            if (!double.IsNaN(rightFinDepth))
            {
                result["RightFinDepth"] = rightFinDepth;
            }

            return result;
        }
    }
}
