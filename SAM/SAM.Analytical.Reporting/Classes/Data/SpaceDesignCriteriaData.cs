// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Units;

namespace SAM.Analytical.Reporting
{
    /// <summary>
    /// Room set points (from the internal condition profiles) and outdoor design conditions (from the model's design
    /// days). These are authored assumptions, not simulated room states.
    /// </summary>
    public sealed class SpaceDesignCriteriaData
    {
        /// <summary>
        /// Heating set point [°C]: maximum of the heating profile.
        /// </summary>
        public ReportValue<Quantity> HeatingSetPoint { get; init; }

        /// <summary>
        /// Cooling set point [°C]: minimum of the cooling profile.
        /// </summary>
        public ReportValue<Quantity> CoolingSetPoint { get; init; }

        /// <summary>
        /// Humidification set point [%RH], the lower RH limit that applies in heating: maximum of the humidification
        /// profile.
        /// </summary>
        public ReportValue<Quantity> HumidificationSetPoint { get; init; }

        /// <summary>
        /// Dehumidification set point [%RH], the upper RH limit that applies in cooling: minimum of the
        /// dehumidification profile.
        /// </summary>
        public ReportValue<Quantity> DehumidificationSetPoint { get; init; }

        /// <summary>
        /// Lowest dry bulb [°C] over the heating design days.
        /// </summary>
        public ReportValue<Quantity> OutdoorHeatingDryBulb { get; init; }

        /// <summary>
        /// Relative humidity [%] coincident with <see cref="OutdoorHeatingDryBulb"/>.
        /// </summary>
        public ReportValue<Quantity> OutdoorHeatingRelativeHumidity { get; init; }

        /// <summary>
        /// Highest dry bulb [°C] over the cooling design days.
        /// </summary>
        public ReportValue<Quantity> OutdoorCoolingDryBulb { get; init; }

        /// <summary>
        /// Relative humidity [%] coincident with <see cref="OutdoorCoolingDryBulb"/>.
        /// </summary>
        public ReportValue<Quantity> OutdoorCoolingRelativeHumidity { get; init; }
    }
}
