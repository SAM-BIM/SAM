// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Core.Optimisation
{
    /// <summary>
    /// The physical quantity a design variable or output represents. It is declared by the definition (an engine such
    /// as a Tas script decides what a value means), and it selects which units are valid and how a value is displayed.
    /// JSON: kebab-case, for example "temperature-difference".
    /// </summary>
    public enum OptimisationQuantity
    {
        /// <summary>Not declared.</summary>
        Unspecified,
        Dimensionless,
        Temperature,
        TemperatureDifference,
        Percent,
        Time,
        Power,
        Energy,
        Mass,

        /// <summary>A mass of carbon dioxide (equivalent), for example kgCO2e.</summary>
        Carbon,

        /// <summary>Money, in an ISO 4217 currency such as GBP. Never converted between currencies.</summary>
        Currency,
        Angle,
        Length,
        Area,
    }
}
