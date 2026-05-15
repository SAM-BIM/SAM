// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class ApertureConstructionCaseData : BuiltInCaseData
    {
        private ApertureConstruction apertureConstruction;

        public ApertureConstructionCaseData(ApertureConstruction apertureConstruction)
            : base(nameof(ApertureConstructionCaseData))
        {
            this.apertureConstruction = apertureConstruction?.Clone();
        }
        public ApertureConstructionCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public ApertureConstructionCaseData(ApertureConstructionCaseData apertureConstructionCaseData)
            : base(apertureConstructionCaseData)
        {
            if (apertureConstructionCaseData != null)
            {
                apertureConstruction = apertureConstructionCaseData.apertureConstruction?.Clone();
            }
        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return apertureConstruction?.Clone();
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

            return result;
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

            return result;
        }
    }
}
