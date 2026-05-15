// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class WindowSizeCase : Case, ISelectiveCase
    {
        private double apertureScaleFactor = 0.8;
        private CaseSelection caseSelection;

        public WindowSizeCase()
        {

        }

        public WindowSizeCase(double apertureScaleFactor)
        {
            this.apertureScaleFactor = apertureScaleFactor;
        }

        public WindowSizeCase(double apertureScaleFactor, CaseSelection caseSelection)
            : base()
        {
            this.apertureScaleFactor = apertureScaleFactor;
            this.caseSelection = caseSelection;
        }

        public WindowSizeCase(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public WindowSizeCase(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public WindowSizeCase(WindowSizeCase windowSizeCase)
            : base(windowSizeCase)
        {
            if (windowSizeCase is not null)
            {
                apertureScaleFactor = windowSizeCase.apertureScaleFactor;
                caseSelection = windowSizeCase.caseSelection;
            }
        }

        public double ApertureScaleFactor
        {
            get
            {
                return apertureScaleFactor;
            }

            set
            {
                apertureScaleFactor = value;
                OnPropertyChanged(nameof(ApertureScaleFactor));
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

            if (jsonObject.ContainsKey("ApertureScaleFactor"))
            {
                apertureScaleFactor = jsonObject["ApertureScaleFactor"]?.GetValue<double>() ?? double.NaN;
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

            if (!double.IsNaN(apertureScaleFactor))
            {
                result["ApertureScaleFactor"] = apertureScaleFactor;
            }

            if (caseSelection?.ToJsonObject() is JsonObject caseSelectionJson)
            {
                result["CaseSelection"] = caseSelectionJson.DeepClone();
            }

            return result;
        }
    }
}
