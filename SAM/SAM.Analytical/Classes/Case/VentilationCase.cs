// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class VentilationCase : Case, ISelectiveCase
    {
        private double ach;
        private CaseSelection caseSelection;
        private string description;
        private double factor;
        private string function;
        private double m3h;
        private double setback;

        public VentilationCase(string function, double ach, double m3h, double factor, double setback, string description, CaseSelection caseSelection)
            : base()
        {
            this.function = function;
            this.ach = ach;
            this.m3h = m3h;
            this.factor = factor;
            this.setback = setback;
            this.description = description;
            this.caseSelection = caseSelection;
        }

        public VentilationCase(JObject jObject)
            : base(jObject)
        {

        }

        public VentilationCase(VentilationCase ventilationCase)
            : base(ventilationCase)
        {
            if (ventilationCase != null)
            {
                function = ventilationCase.function;
                ach = ventilationCase.ach;
                m3h = ventilationCase.m3h;
                factor = ventilationCase.factor;
                setback = ventilationCase.setback;
                description = ventilationCase.description;
                caseSelection = ventilationCase.caseSelection;
            }
        }

        public double ACH
        {
            get
            {
                return ach;
            }

            set
            {
                ach = value;
                OnPropertyChanged(nameof(ACH));
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

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public double Factor
        {
            get
            {
                return factor;
            }

            set
            {
                factor = value;
                OnPropertyChanged(nameof(Factor));
            }
        }

        public string Function
        {
            get
            {
                return function;
            }

            set
            {
                function = value;
                OnPropertyChanged(nameof(Function));
            }
        }

        public double M3h
        {
            get
            {
                return m3h;
            }

            set
            {
                m3h = value;
                OnPropertyChanged(nameof(M3h));
            }
        }

        public double Setback
        {
            get
            {
                return setback;
            }

            set
            {
                setback = value;
                OnPropertyChanged(nameof(Setback));
            }
        }

        protected override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Function"))
            {
                function = jsonObject["Function"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("ACH"))
            {
                ach = jsonObject["ACH"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("M3h"))
            {
                m3h = jsonObject["M3h"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Factor"))
            {
                factor = jsonObject["Factor"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Setback"))
            {
                setback = jsonObject["Setback"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("Description"))
            {
                description = jsonObject["Description"]?.GetValue<string>();
            }

            if (jsonObject["CaseSelection"] is JsonObject caseSelectionJson)
            {
                caseSelection = Core.Query.IJSAMObject<CaseSelection>(new JObject((JsonObject)caseSelectionJson.DeepClone()));
            }

            return true;
        }

        protected override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (function != null)
            {
                result["Function"] = function;
            }

            if (!double.IsNaN(ach))
            {
                result["ACH"] = ach;
            }

            if (!double.IsNaN(m3h))
            {
                result["M3h"] = m3h;
            }

            if (!double.IsNaN(factor))
            {
                result["Factor"] = factor;
            }

            if (!double.IsNaN(setback))
            {
                result["Setback"] = setback;
            }

            if (description != null)
            {
                result["Description"] = description;
            }

            if (caseSelection?.ToJObject()?.Node is JsonObject caseSelectionJson)
            {
                result["CaseSelection"] = caseSelectionJson.DeepClone();
            }

            return result;
        }
    }
}
