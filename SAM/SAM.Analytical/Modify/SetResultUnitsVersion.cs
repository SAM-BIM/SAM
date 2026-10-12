// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Stamps <see cref="AnalyticalModelSimulationResult.CurrentResultUnitsVersion"/> on a result its writer has just filled
        /// with consumption in kWh and peak loads in kW.
        /// <para>
        /// For writers only. It records how the values were written; it converts nothing. Stamping a result written in other
        /// units (a persisted result without the marker, for example) would make it lie, so persisted results are never
        /// stamped here.
        /// </para>
        /// </summary>
        public static bool SetResultUnitsVersion(this AnalyticalModelSimulationResult analyticalModelSimulationResult)
        {
            if (analyticalModelSimulationResult == null)
            {
                return false;
            }

            return analyticalModelSimulationResult.SetValue(AnalyticalModelSimulationResultParameter.ResultUnitsVersion, AnalyticalModelSimulationResult.CurrentResultUnitsVersion);
        }
    }
}
