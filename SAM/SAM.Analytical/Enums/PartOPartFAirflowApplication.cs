// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Enums
{
    /// <summary>
    /// Whether the Approved Document F continuous mechanical airflows may be carried onto the internal
    /// conditions a Part O iteration simulates, decided from the ventilation strategy the assessed zones
    /// state.
    /// <para>
    /// <b>Why this decision has to exist.</b> <c>PartFCalculator</c> is unconditionally System 4 shaped:
    /// paragraph 1.67 gives every habitable room a mechanical supply terminal and every wet room a
    /// continuous extract terminal, with no input anywhere for how the dwelling is actually ventilated.
    /// <see cref="Modify.ApplyPartFVentilationRates(AnalyticalModel, PartFOperatingMode, out System.Collections.Generic.List{string}, out System.Collections.Generic.List{string})"/>
    /// then writes those rates onto <c>InternalConditionParameter.SupplyAirFlow</c> and
    /// <c>ExhaustAirFlow</c> - the values the simulation and the TAS export read. Applied to a naturally
    /// ventilated dwelling that produces a successful run of a building that does not exist, which is worse
    /// than a refusal: nothing in the result says the mechanical system was invented.
    /// </para>
    /// <para>
    /// <b>This is a gate, not a sizing rule.</b> Skipping the application means SAM has not invented an
    /// MVHR or MVRE system for a naturally ventilated dwelling. It does <b>not</b> mean the dwelling's
    /// natural-ventilation Part F design - System 1 background ventilators, purge provision - has been
    /// sized. Nothing here sizes anything.
    /// </para>
    /// </summary>
    [Description("Part O Part F Airflow Application.")]
    public enum PartOPartFAirflowApplication
    {
        /// <summary>No decision was reached.</summary>
        [Description("Undefined")] Undefined,

        /// <summary>
        /// Carry the Part F continuous mechanical rates onto the internal conditions, as this preparation
        /// has always done. Reached when no assessed zone states natural ventilation - including when
        /// there are no assessed zones and therefore no strategy to read, where the long-standing
        /// behaviour is preserved rather than changed on an absence.
        /// </summary>
        [Description("Apply")] Apply,

        /// <summary>
        /// Apply nothing. Every assessed zone states <c>NV</c>, so there is no continuous mechanical supply
        /// or extract to carry, and absence of <c>PartFSpaceData</c> is not an error on this path.
        /// </summary>
        [Description("Skip - Natural Ventilation")] SkipNaturalVentilation,

        /// <summary>
        /// Refuse. Some assessed zones state <c>NV</c> and others state a strategy that is not <c>NV</c>,
        /// and <see cref="Modify.ApplyPartFVentilationRates(AnalyticalModel, PartFOperatingMode, out System.Collections.Generic.List{string}, out System.Collections.Generic.List{string})"/>
        /// is whole-model rather than zone-scoped. Applying would put mechanical airflow into the
        /// naturally ventilated zones; skipping would strip it from the mechanical ones. Either way one
        /// half of the building is simulated as something it is not, so neither is done. Mixed models need
        /// a per-zone application, which is a separate change.
        /// </summary>
        [Description("Refuse - Mixed Strategies")] RefuseMixed,
    }
}
