// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class FinShadeCase : Case, ISelectiveCase
    {
        private bool glassPartOnly;
        private double leftFinDepth;
        private double leftFinFrontOffset;
        private double leftFinOffset;
        private double overhangDepth;
        private double overhangFrontOffset;
        private double overhangVerticalOffset;
        private double rightFinDepth;
        private double rightFinFrontOffset;
        private double rightFinOffset;
        private CaseSelection caseSelection;

        public FinShadeCase()
        {

        }

        public FinShadeCase(bool glassPartOnly, double overhangDepth, double overhangVerticalOffset, double overhangFrontOffset, double leftFinDepth, double leftFinOffset, double leftFinFrontOffset, double rightFinDepth, double rightFinOffset, double rightFinFrontOffset, CaseSelection caseSelection)
            : base()
        {
            this.glassPartOnly = glassPartOnly;
            this.overhangDepth = overhangDepth;
            this.overhangVerticalOffset = overhangVerticalOffset;
            this.overhangFrontOffset = overhangFrontOffset;
            this.leftFinDepth = leftFinDepth;
            this.leftFinOffset = leftFinOffset;
            this.leftFinFrontOffset = leftFinFrontOffset;
            this.rightFinDepth = rightFinDepth;
            this.rightFinOffset = rightFinOffset;
            this.rightFinFrontOffset = rightFinFrontOffset;
            this.caseSelection = caseSelection;
        }
        public FinShadeCase(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public FinShadeCase(FinShadeCase shadeCase)
            : base(shadeCase)
        {
            if (shadeCase != null)
            {
                glassPartOnly = shadeCase.glassPartOnly;
                overhangDepth = shadeCase.overhangDepth;
                overhangVerticalOffset = shadeCase.overhangVerticalOffset;
                overhangFrontOffset = shadeCase.overhangFrontOffset;
                leftFinDepth = shadeCase.leftFinDepth;
                leftFinOffset = shadeCase.leftFinOffset;
                leftFinFrontOffset = shadeCase.leftFinFrontOffset;
                rightFinDepth = shadeCase.rightFinDepth;
                rightFinOffset = shadeCase.rightFinOffset;
                rightFinFrontOffset = shadeCase.rightFinFrontOffset;
                caseSelection = shadeCase.caseSelection;
            }
        }

        public bool GlassPartOnly
        {
            get
            {
                return glassPartOnly;
            }

            set
            {
                glassPartOnly = value;
                OnPropertyChanged(nameof(GlassPartOnly));
            }
        }

        public double LeftFinDepth
        {
            get
            {
                return leftFinDepth;
            }

            set
            {
                leftFinDepth = value;
                OnPropertyChanged(nameof(LeftFinDepth));
            }
        }

        public double LeftFinFrontOffset
        {
            get
            {
                return leftFinFrontOffset;
            }

            set
            {
                leftFinFrontOffset = value;
                OnPropertyChanged(nameof(LeftFinFrontOffset));
            }
        }

        public double LeftFinOffset
        {
            get
            {
                return leftFinOffset;
            }

            set
            {
                leftFinOffset = value;
                OnPropertyChanged(nameof(LeftFinOffset));
            }
        }

        public double OverhangDepth
        {
            get
            {
                return overhangDepth;
            }

            set
            {
                overhangDepth = value;
                OnPropertyChanged(nameof(OverhangDepth));
            }
        }

        public double OverhangFrontOffset
        {
            get
            {
                return overhangFrontOffset;
            }

            set
            {
                overhangFrontOffset = value;
                OnPropertyChanged(nameof(OverhangFrontOffset));
            }
        }

        public double OverhangVerticalOffset
        {
            get
            {
                return overhangVerticalOffset;
            }

            set
            {
                overhangVerticalOffset = value;
                OnPropertyChanged(nameof(OverhangVerticalOffset));
            }
        }

        public double RightFinDepth
        {
            get
            {
                return rightFinDepth;
            }

            set
            {
                rightFinDepth = value;
                OnPropertyChanged(nameof(RightFinDepth));
            }
        }

        public double RightFinFrontOffset
        {
            get
            {
                return rightFinFrontOffset;
            }

            set
            {
                rightFinFrontOffset = value;
                OnPropertyChanged(nameof(RightFinFrontOffset));
            }
        }

        public double RightFinOffset
        {
            get
            {
                return rightFinOffset;
            }

            set
            {
                rightFinOffset = value;
                OnPropertyChanged(nameof(RightFinOffset));
            }
        }

        public CaseSelection CaseSelection
        {
            get
            {
                return caseSelection;
            }

            set
            {
                caseSelection = value;
                OnPropertyChanged(nameof(CaseSelection));
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("GlassPartOnly"))
            {
                glassPartOnly = jsonObject["GlassPartOnly"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("OverhangDepth"))
            {
                overhangDepth = jsonObject["OverhangDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("OverhangVerticalOffset"))
            {
                overhangVerticalOffset = jsonObject["OverhangVerticalOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("OverhangFrontOffset"))
            {
                overhangFrontOffset = jsonObject["OverhangFrontOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinDepth"))
            {
                leftFinDepth = jsonObject["LeftFinDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinOffset"))
            {
                leftFinOffset = jsonObject["LeftFinOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("LeftFinFrontOffset"))
            {
                leftFinFrontOffset = jsonObject["LeftFinFrontOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("RightFinDepth"))
            {
                rightFinDepth = jsonObject["RightFinDepth"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("RightFinOffset"))
            {
                rightFinOffset = jsonObject["RightFinOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("RightFinFrontOffset"))
            {
                rightFinFrontOffset = jsonObject["RightFinFrontOffset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject["CaseSelection"] is JsonObject caseSelectionJson)
            {
                caseSelection = Core.Query.IJSAMObject<CaseSelection>(caseSelectionJson as JsonObject);
            }

            return true;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            result["GlassPartOnly"] = glassPartOnly;

            if (!double.IsNaN(overhangDepth))
            {
                result["OverhangDepth"] = overhangDepth;
            }

            if (!double.IsNaN(overhangVerticalOffset))
            {
                result["OverhangVerticalOffset"] = overhangVerticalOffset;
            }

            if (!double.IsNaN(overhangFrontOffset))
            {
                result["OverhangFrontOffset"] = overhangFrontOffset;
            }

            if (!double.IsNaN(leftFinDepth))
            {
                result["LeftFinDepth"] = leftFinDepth;
            }

            if (!double.IsNaN(leftFinOffset))
            {
                result["LeftFinOffset"] = leftFinOffset;
            }

            if (!double.IsNaN(leftFinFrontOffset))
            {
                result["LeftFinFrontOffset"] = leftFinFrontOffset;
            }

            if (!double.IsNaN(rightFinDepth))
            {
                result["RightFinDepth"] = rightFinDepth;
            }

            if (!double.IsNaN(rightFinOffset))
            {
                result["RightFinOffset"] = rightFinOffset;
            }

            if (!double.IsNaN(rightFinFrontOffset))
            {
                result["RightFinFrontOffset"] = rightFinFrontOffset;
            }

            if (caseSelection?.ToJsonObject() is JsonObject caseSelectionJson)
            {
                result["CaseSelection"] = caseSelectionJson.DeepClone();
            }

            return result;
        }
    }
}
