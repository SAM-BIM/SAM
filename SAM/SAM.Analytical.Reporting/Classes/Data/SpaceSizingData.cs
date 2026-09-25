// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;
using SAM.Units;

namespace SAM.Analytical.Reporting
{
    /// <summary>
    /// Persisted Tas design loads and sizing factors.
    /// <para>
    /// Design loads come from <c>SpaceParameter.DesignHeatingLoad</c> / <c>DesignCoolingLoad</c> (written from the
    /// TBD by <c>UpdateDesignLoads</c>). Nothing records which sizing run produced them, so when present they are
    /// Available, Source = TBD, Freshness = Unknown (Rev 3 R3.1). TSD provenance is never used for them.
    /// </para>
    /// </summary>
    public sealed class SpaceSizingData
    {
        public DesignLoadStatus DesignLoadStatus { get; init; }

        public ReportValue<Quantity> DesignHeatingLoad { get; init; }

        public ReportValue<Quantity> DesignCoolingLoad { get; init; }

        public ReportValue<Quantity> DesignHeatingLoadPerArea { get; init; }

        public ReportValue<Quantity> DesignCoolingLoadPerArea { get; init; }

        /// <summary>
        /// Heating sizing factor, a load multiplier (1.2 = +20 %). The space value wins; 0 means not set, in which
        /// case the model value applies, as in the Tas export.
        /// </summary>
        public ReportValue<Quantity> HeatingSizingFactor { get; init; }

        public ReportValue<Quantity> CoolingSizingFactor { get; init; }
    }
}
