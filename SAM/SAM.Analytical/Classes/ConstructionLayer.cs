// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Architectural;

namespace SAM.Analytical
{
    public class ConstructionLayer : MaterialLayer, IAnalyticalObject
    {
        public ConstructionLayer(string name, double thickness)
            : base(name, thickness)
        {
        }

        public ConstructionLayer(ConstructionLayer constructionLayer)
            : base(constructionLayer)
        {

        }

        public ConstructionLayer(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public ConstructionLayer(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public bool IsSoil
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return false;
                }

                return Name.ToLower().Contains("soil");
            }
        }

    }
}
