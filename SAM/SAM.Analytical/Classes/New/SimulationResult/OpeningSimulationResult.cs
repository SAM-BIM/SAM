// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class OpeningSimulationResult : Result, IAnalyticalObject
    {
        public OpeningSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public OpeningSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public OpeningSimulationResult(OpeningSimulationResult openingSimulationResult)
            : base(openingSimulationResult)
        {

        }
        public OpeningSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
