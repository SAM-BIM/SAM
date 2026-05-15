// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;

namespace SAM.Analytical
{
    public abstract class BuiltInCaseData : CaseData
    {
        public BuiltInCaseData(string name)
            : base(name)
        {

        }

        public BuiltInCaseData(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {

        }


        public BuiltInCaseData(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

        public BuiltInCaseData(BuiltInCaseData builtInCaseData)
            : base(builtInCaseData)
        {

        }

    }
}
