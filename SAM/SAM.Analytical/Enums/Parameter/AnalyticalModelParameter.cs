// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(AnalyticalModel)), Description("AnalyticalModel Parameter")]
    public enum AnalyticalModelParameter
    {
        [ParameterProperties("North Angle", "North Angle"), ParameterValue(ParameterType.Double)] NorthAngle,
        [ParameterProperties("Cooling Sizing Factor", "Cooling Sizing Factor"), DoubleParameterValue(0)] CoolingSizingFactor,
        [ParameterProperties("Heating Sizing Factor", "Heating Sizing Factor"), DoubleParameterValue(0)] HeatingSizingFactor,
        [ParameterProperties("Weather Data", "Weather Data"), SAMObjectParameterValue(typeof(Weather.WeatherData))] WeatherData,
        [ParameterProperties("Heating Design Days", "Heating Design Days"), SAMObjectParameterValue(typeof(SAMCollection<DesignDay>))] HeatingDesignDays,
        [ParameterProperties("Cooling Design Days", "Cooling Design Days"), SAMObjectParameterValue(typeof(SAMCollection<DesignDay>))] CoolingDesignDays,
        [ParameterProperties("Case Data Collection", "Case Data Collection"), SAMObjectParameterValue(typeof(CaseDataCollection))] CaseDataCollection,
        [ParameterProperties("Solar Model", "Solar Model"), ParameterValue(ParameterType.IJSAMObject)] SolarModel,
        [ParameterProperties("Overheating Scenarios", "Overheating Scenarios"), SAMObjectParameterValue(typeof(SAMCollection<OverheatingScenario>))] OverheatingScenarios,
        [ParameterProperties("Simulation Result Provenance", "Simulation Result Provenance"), SAMObjectParameterValue(typeof(SimulationResultProvenance))] SimulationResultProvenance,
        [ParameterProperties("Part O Isolation Context", "Part O Isolation Context"), SAMObjectParameterValue(typeof(PartOIsolationContext))] PartOIsolationContext,

        /// <summary>
        /// The project's Approved Document O equipment preselection - how ventilation units are chosen, and
        /// which products the engineer has permitted them to be chosen from. Absent until stated, which
        /// reads as the historic default: automatic selection over the whole catalogue.
        /// <para>
        /// <b>Configuration, not an assignment and not a capability.</b> What each dwelling is fitted with
        /// is <see cref="AirHandlingUnitParameter.VentilationUnitReference"/> on its own air handling unit;
        /// what that product can move stays in the catalogue. This parameter is on the project rather than
        /// on an engineering object because a procurement preference belongs to the project - and here
        /// rather than in an application setting because one project's permitted products must not become
        /// the next project's. <see cref="Analytical.PartOEquipmentSelection"/> sets that out in full.
        /// </para>
        /// </summary>
        [ParameterProperties("Part O Equipment Selection", "Part O Equipment Selection"), SAMObjectParameterValue(typeof(PartOEquipmentSelection))] PartOEquipmentSelection,
    }
}
