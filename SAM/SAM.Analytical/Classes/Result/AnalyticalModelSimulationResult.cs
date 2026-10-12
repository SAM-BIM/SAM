// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;

namespace SAM.Analytical
{
    public class AnalyticalModelSimulationResult : Result, IAnalyticalResult
    {
        /// <summary>
        /// The <see cref="AnalyticalModelSimulationResultParameter.ResultUnitsVersion"/> a writer stamps on a new result:
        /// consumption in kWh, peak loads in kW. Set it with <see cref="Modify.SetResultUnitsVersion"/>.
        /// </summary>
        public const int CurrentResultUnitsVersion = 1;

        /// <summary>
        /// The <see cref="Result.Source"/> SAM_Tas has written on every result since it first wrote one (its assembly name).
        /// Without a units marker, such a result holds consumption in Wh and peak loads in W.
        /// </summary>
        public const string TasSource = "SAM.Analytical.Tas";

        /// <summary>
        /// The <see cref="Result.Source"/> SAM_OpenStudio has written on every result since it first wrote one (its assembly
        /// name). Such a result holds consumption in kWh and peak loads in kW, with or without a units marker.
        /// </summary>
        public const string OpenStudioSource = "SAM.Analytical.OpenStudio";

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
        public AnalyticalModelSimulationResult(System.Text.Json.Nodes.JsonObject jsonObject)

            : base(jsonObject)

        {

        }

    }
}
