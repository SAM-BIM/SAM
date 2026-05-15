// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class ApertureCase : Case
    {
        private double apertureHeight;
        private ApertureToPanelRatios apertureToPanelRatios;
        private CaseSelection caseSelection;
        private double horizontalSeparation;
        private bool keepSeparationDistance;
        private double offset;
        private double sillHeight;
        private bool subdivide;

        private double? framePercentage = null;
        private double? frameWidth = null;

        public ApertureCase()
            : base()
        {

        }

        public ApertureCase(ApertureToPanelRatios apertureToPanelRatios, bool subdivide, double apertureHeight, double sillHeight, double horizontalSeparation, double offset, bool keepSeparationDistance, CaseSelection caseSelection, double? framePercentage, double? frameWidth)
            : base()
        {
            this.apertureToPanelRatios = apertureToPanelRatios == null ? null : new ApertureToPanelRatios(apertureToPanelRatios);
            this.subdivide = subdivide;
            this.apertureHeight = apertureHeight;
            this.sillHeight = sillHeight;
            this.horizontalSeparation = horizontalSeparation;
            this.offset = offset;
            this.keepSeparationDistance = keepSeparationDistance;
            this.caseSelection = caseSelection;
            this.framePercentage = framePercentage;
            this.frameWidth = frameWidth;
        }
        public ApertureCase(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ApertureCase(ApertureCase apertureCase)
            : base(apertureCase)
        {
            if (apertureCase != null)
            {
                apertureToPanelRatios = apertureCase.apertureToPanelRatios;
                subdivide = apertureCase.subdivide;
                apertureHeight = apertureCase.apertureHeight;
                sillHeight = apertureCase.sillHeight;
                horizontalSeparation = apertureCase.horizontalSeparation;
                offset = apertureCase.offset;
                keepSeparationDistance = apertureCase.keepSeparationDistance;
                caseSelection = apertureCase.caseSelection;
                framePercentage = apertureCase.framePercentage;
                frameWidth = apertureCase.frameWidth;
            }
        }

        public double? FramePercentage
        {
            get
            {
                return framePercentage;
            }

            set
            {
                framePercentage = value;
                OnPropertyChanged(nameof(FramePercentage));
            }
        }

        public double? FrameWidth
        {
            get
            {
                return frameWidth;
            }

            set
            {
                frameWidth = value;
                OnPropertyChanged(nameof(FrameWidth));
            }
        }

        public double ApertureHeight
        {
            get
            {
                return apertureHeight;
            }

            set
            {
                apertureHeight = value;
                OnPropertyChanged(nameof(ApertureHeight));
            }
        }

        public ApertureToPanelRatios ApertureToPanelRatios
        {
            get
            {
                return apertureToPanelRatios;
            }
            set
            {
                apertureToPanelRatios = value;
                OnPropertyChanged(nameof(ApertureToPanelRatios));
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

        public double HorizontalSeparation
        {
            get
            {
                return horizontalSeparation;
            }

            set
            {
                horizontalSeparation = value;
                OnPropertyChanged(nameof(HorizontalSeparation));
            }
        }

        public bool KeepSeparationDistance
        {
            get
            {
                return keepSeparationDistance;
            }

            set
            {
                keepSeparationDistance = value;
                OnPropertyChanged(nameof(KeepSeparationDistance));
            }
        }

        public double Offset
        {
            get
            {
                return offset;
            }

            set
            {
                offset = value;
                OnPropertyChanged(nameof(Offset));
            }
        }

        public double SillHeight
        {
            get
            {
                return sillHeight;
            }

            set
            {
                sillHeight = value;
                OnPropertyChanged(nameof(SillHeight));
            }
        }

        public bool Subdivide
        {
            get
            {
                return subdivide;
            }

            set
            {
                subdivide = value;
                OnPropertyChanged(nameof(Subdivide));
            }
        }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject["ApertureToPanelRatios"] is JsonObject apertureToPanelRatiosJson)
            {
                apertureToPanelRatios = Core.Query.IJSAMObject<ApertureToPanelRatios>(apertureToPanelRatiosJson as JsonObject);
            }

            if (jsonObject.ContainsKey("Subdivide"))
            {
                subdivide = jsonObject["Subdivide"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("ApertureHeight"))
            {
                apertureHeight = jsonObject["ApertureHeight"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SillHeight"))
            {
                sillHeight = jsonObject["SillHeight"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("HorizontalSeparation"))
            {
                horizontalSeparation = jsonObject["HorizontalSeparation"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Offset"))
            {
                offset = jsonObject["Offset"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject["CaseSelection"] is JsonObject caseSelectionJson)
            {
                caseSelection = Core.Query.IJSAMObject<CaseSelection>(caseSelectionJson as JsonObject);
            }

            if (jsonObject.ContainsKey("FramePercentage"))
            {
                framePercentage = jsonObject["FramePercentage"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("FrameWidth"))
            {
                frameWidth = jsonObject["FrameWidth"]?.GetValue<double>() ?? double.NaN;
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

            if (apertureToPanelRatios?.ToJsonObject() is JsonObject apertureToPanelRatiosJson)
            {
                result["ApertureToPanelRatios"] = apertureToPanelRatiosJson.DeepClone();
            }

            result["Subdivide"] = subdivide;

            if (!double.IsNaN(apertureHeight))
            {
                result["ApertureHeight"] = apertureHeight;
            }

            if (!double.IsNaN(sillHeight))
            {
                result["SillHeight"] = sillHeight;
            }

            if (!double.IsNaN(horizontalSeparation))
            {
                result["HorizontalSeparation"] = horizontalSeparation;
            }

            if (!double.IsNaN(offset))
            {
                result["Offset"] = offset;
            }

            result["KeepSeparationDistance"] = keepSeparationDistance;

            if (caseSelection?.ToJsonObject() is JsonObject caseSelectionJson)
            {
                result["CaseSelection"] = caseSelectionJson.DeepClone();
            }

            if(framePercentage is not null && !double.IsNaN(framePercentage.Value))
            {
                result["FramePercentage"] = framePercentage.Value;
            }

            if (frameWidth is not null && !double.IsNaN(frameWidth.Value))
            {
                result["FrameWidth"] = frameWidth.Value;
            }

            return result;
        }
    }
}
