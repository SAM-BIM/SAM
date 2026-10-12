// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(AnalyticalModelSimulationResult)), Description("AnalyticalModel Simulation Result Parameter")]
    public enum AnalyticalModelSimulationResultParameter
    {
        [ParameterProperties("Consumption Heating", "Consumption Heating [kWh]"), ParameterValue(ParameterType.Double)] ConsumptionHeating,
        [ParameterProperties("Peak Heating Load", "Peak Heating Load [kW]"), ParameterValue(ParameterType.Double)] PeakHeatingLoad,
        [ParameterProperties("Peak Heating Hour", "Peak Heating Hour [hr]"), ParameterValue(ParameterType.Integer)] PeakHeatingHour,

        [ParameterProperties("Consumption Cooling", "Consumption Cooling [kWh]"), ParameterValue(ParameterType.Double)] ConsumptionCooling,
        [ParameterProperties("Peak Cooling Load", "Peak Cooling Load [kW]"), ParameterValue(ParameterType.Double)] PeakCoolingLoad,
        [ParameterProperties("Peak Cooling Hour", "Peak Cooling Hour [hr]"), ParameterValue(ParameterType.Integer)] PeakCoolingHour,

        [ParameterProperties("Floor Area", "Floor Area [m2]"), ParameterValue(ParameterType.Double)] FloorArea,
        [ParameterProperties("Volume", "Volume [m3]"), ParameterValue(ParameterType.Double)] Volume,

        // Appended, never reordered: parameters are stored by name, and existing names must not change.
        // 1 = consumption in kWh and peak loads in kW, as labelled above. A result without it was written before the
        // units correction; read the four values through Query.TryGetConsumptionOrPeakLoad, never directly.
        [ParameterProperties("Result Units Version", "Result Units Version (1 = consumption in kWh, peak loads in kW)"), ParameterValue(ParameterType.Integer)] ResultUnitsVersion,
    }
}
