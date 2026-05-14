// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Classes
{
    public class ApertureConstructionCase : Case, ISelectiveCase
    {
        private ApertureConstruction apertureConstruction;
        private CaseSelection caseSelection;

        public ApertureConstructionCase(ApertureConstruction apertureConstruction, CaseSelection caseSelection)
            : base()
        {
            this.apertureConstruction = apertureConstruction;
            this.caseSelection = caseSelection;
        }

        public ApertureConstructionCase(ApertureConstructionCase apertureConstructionCase)
            : base(apertureConstructionCase)
        {
            if (apertureConstructionCase != null)
            {
                apertureConstruction = apertureConstructionCase.apertureConstruction;
                caseSelection = apertureConstructionCase.caseSelection;
            }
        }

        public ApertureConstructionCase(JObject jObject)
            : base(jObject)
        {

        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return apertureConstruction;
            }

            set
            {
                apertureConstruction = value;
                OnPropertyChanged(nameof(ApertureConstruction));
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

            if (jsonObject["ApertureConstruction"] is JsonObject apertureConstructionJson)
            {
                apertureConstruction = Core.Query.IJSAMObject<ApertureConstruction>(apertureConstructionJson as JsonObject);
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

            if (apertureConstruction?.ToJsonObject() is JsonObject apertureConstructionJson)
            {
                result["ApertureConstruction"] = apertureConstructionJson.DeepClone();
            }

            if (caseSelection?.ToJsonObject() is JsonObject caseSelectionJson)
            {
                result["CaseSelection"] = caseSelectionJson.DeepClone();
            }

            return result;
        }
    }
}
