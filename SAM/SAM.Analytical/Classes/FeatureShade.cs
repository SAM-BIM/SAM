// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class FeatureShade : SAMObject, IAnalyticalObject
    {
        private string description = null;
        private double surfaceHeight = double.NaN;
        private double surfaceWidth = double.NaN;
        private double leftFinDepth = double.NaN;
        private double leftFinOffset = double.NaN;
        private double leftFinTransmittance = double.NaN;
        private double rightFinDepth = double.NaN;
        private double rightFinOffset = double.NaN;
        private double rightFinTransmittance = double.NaN;
        private double overhangDepth = double.NaN;
        private double overhangOffset = double.NaN;
        private double overhangTransmittance = double.NaN;

        public FeatureShade(string name)
            : base(name)
        {

        }

        public FeatureShade(string name, string description, double surfaceHeight, double surfaceWidth, double leftFinDepth, double leftFinOffset, double leftFinTransmittance, double rightFinDepth, double rightFinOffset, double rightFinTransmittance, double overhangDepth, double overhangOffset, double overhangTransmittance)
            : base(name)
        {
            this.description = description;
            this.surfaceHeight = surfaceHeight;
            this.surfaceWidth = surfaceWidth;
            this.leftFinDepth = leftFinDepth;
            this.leftFinOffset = leftFinOffset;
            this.leftFinTransmittance = leftFinTransmittance;
            this.rightFinDepth = rightFinDepth;
            this.rightFinOffset = rightFinOffset;
            this.rightFinTransmittance = rightFinTransmittance;
            this.overhangDepth = overhangDepth;
            this.overhangOffset = overhangOffset;
            this.overhangTransmittance = overhangTransmittance;
        }

        public FeatureShade(FeatureShade featureShade)
            : base(featureShade)
        {
            if (featureShade is not null)
            {
                description = featureShade.description;
                surfaceHeight = featureShade.surfaceHeight;
                surfaceWidth = featureShade.surfaceWidth;
                leftFinDepth = featureShade.leftFinDepth;
                leftFinOffset = featureShade.leftFinOffset;
                leftFinTransmittance = featureShade.leftFinTransmittance;
                rightFinDepth = featureShade.rightFinDepth;
                rightFinOffset = featureShade.rightFinOffset;
                rightFinTransmittance = featureShade.rightFinTransmittance;
                overhangDepth = featureShade.overhangDepth;
                overhangOffset = featureShade.overhangOffset;
                overhangTransmittance = featureShade.overhangTransmittance;
            }
        }

        public FeatureShade(JObject jObject)
            : base(jObject)
        {

        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            if (!base.FromJsonObject(jsonObject))
            {
                return false;
            }

            if (jsonObject.ContainsKey("Description"))
            {
                description = jsonObject["Description"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("SurfaceHeight"))
            {
                surfaceHeight = jsonObject["SurfaceHeight"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SurfaceWidth"))
            {
                surfaceWidth = jsonObject["SurfaceWidth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinDepth"))
            {
                leftFinDepth = jsonObject["LeftFinDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinDepth"))
            {
                leftFinDepth = jsonObject["LeftFinDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinOffset"))
            {
                leftFinOffset = jsonObject["LeftFinOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("RightFinTransmittance"))
            {
                rightFinTransmittance = jsonObject["RightFinTransmittance"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("OverhangDepth"))
            {
                overhangDepth = jsonObject["OverhangDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("OverhangOffset"))
            {
                overhangOffset = jsonObject["OverhangOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("OverhangTransmittance"))
            {
                overhangTransmittance = jsonObject["OverhangTransmittance"]?.GetValue<double>() ?? double.NaN;
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

            if (description is not null)
            {
                jsonObject["Description"] = description;
            }

            if (!double.IsNaN(surfaceHeight))
            {
                jsonObject["SurfaceHeight"] = surfaceHeight;
            }

            if (!double.IsNaN(surfaceWidth))
            {
                jsonObject["SurfaceWidth"] = surfaceWidth;
            }

            if (!double.IsNaN(leftFinDepth))
            {
                jsonObject["LeftFinDepth"] = leftFinDepth;
            }

            if (!double.IsNaN(leftFinOffset))
            {
                jsonObject["LeftFinOffset"] = leftFinOffset;
            }

            if (!double.IsNaN(leftFinTransmittance))
            {
                jsonObject["LeftFinTransmittance"] = leftFinTransmittance;
            }

            if (!double.IsNaN(rightFinDepth))
            {
                jsonObject["RightFinDepth"] = rightFinDepth;
            }

            if (!double.IsNaN(rightFinOffset))
            {
                jsonObject["RightFinOffset"] = rightFinOffset;
            }

            if (!double.IsNaN(rightFinTransmittance))
            {
                jsonObject["RightFinTransmittance"] = rightFinTransmittance;
            }

            if (!double.IsNaN(overhangDepth))
            {
                jsonObject["OverhangDepth"] = overhangDepth;
            }

            if (!double.IsNaN(overhangOffset))
            {
                jsonObject["OverhangOffset"] = overhangOffset;
            }

            if (!double.IsNaN(overhangTransmittance))
            {
                jsonObject["OverhangTransmittance"] = overhangTransmittance;
            }

            return jsonObject;
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        public double SurfaceHeight
        {
            get
            {
                return surfaceHeight;
            }
        }

        public double SurfaceWidth
        {
            get
            {
                return surfaceWidth;
            }
        }

        public double LeftFinDepth
        {
            get
            {
                return leftFinDepth;
            }
        }

        public double LeftFinOffset
        {
            get
            {
                return leftFinOffset;
            }
        }

        public double LeftFinTransmittance
        {
            get
            {
                return leftFinTransmittance;
            }
        }

        public double RightFinDepth
        {
            get
            {
                return rightFinDepth;
            }
        }

        public double RightFinOffset
        {
            get
            {
                return rightFinOffset;
            }
        }

        public double RightFinTransmittance
        {
            get
            {
                return rightFinTransmittance;
            }
        }

        public double OverhangDepth
        {
            get
            {
                return overhangDepth;
            }
        }

        public double OverhangOffset
        {
            get
            {
                return overhangOffset;
            }
        }

        public double OverhangTransmittance
        {
            get
            {
                return overhangTransmittance;
            }
        }
    }
}
