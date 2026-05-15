// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class AnalyticalModelSimulationResult : Result, IAnalyticalResult
    {
        public AnalyticalModelSimulationResult(string name, string source, string reference)
            : base(name, source, reference)
        {

        }

        public AnalyticalModelSimulationResult(Guid guid, string name, string source, string reference)
            : base(guid, name, source, reference)
        {

        }

        public AnalyticalModelSimulationResult(AnalyticalModelSimulationResult analyticalModelSimulationResult)
            : base(analyticalModelSimulationResult)
        {

        }

        public AnalyticalModelSimulationResult(Guid guid, AnalyticalModelSimulationResult analyticalModelSimulationResult)
            : base(guid, analyticalModelSimulationResult)
        {

        }

        public AnalyticalModelSimulationResult(JObject jObject)
            : base(jObject?.Node as System.Text.Json.Nodes.JsonObject)
        {
        }


        public AnalyticalModelSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
