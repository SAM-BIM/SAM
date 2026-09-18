// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.ComponentModel;

namespace SAM.Analytical.Enums
{
    /// <summary>
    /// How a manufacturer states the air temperature its unit delivers into the dwelling in one operating
    /// mode.
    /// <para>
    /// <b>The quantity every one of these produces is the same one: the PACKAGE supply temperature</b> -
    /// the air leaving the whole unit, downstream of every component inside it. It is not an exchanger's
    /// own effectiveness, not a coil's off-coil condition, and not a figure to be corrected for fan heat
    /// afterwards. A manufacturer writes one number for the air that arrives in the room, and that is what
    /// a rule of this kind reproduces.
    /// </para>
    /// </summary>
    [Description("Supply Temperature Rule Type")]
    public enum SupplyTemperatureRuleType
    {
        /// <summary>Nothing was stated. A rule in this state is unusable and refuses.</summary>
        [Description("Undefined")] Undefined,

        /// <summary>
        /// The unit delivers intake air: supply equals the outdoor / intake dry bulb. What a heat exchanger
        /// bypass does.
        /// </summary>
        [Description("Outdoor air")] OutdoorAir,

        /// <summary>
        /// The unit delivers a stated blend of its extract and intake air temperatures -
        /// <c>f * extract + (1 - f) * intake</c>.
        /// <para>
        /// <b>The blend fraction is a package supply-temperature rule, and is not, and must never be
        /// written as, a certified exchanger efficiency.</b> A manufacturer's simplified modelling guidance
        /// may state one figure for what a unit delivers; a certified EN 13141-7 / SAP figure is a measured
        /// property of the exchanger alone, on a stated test basis, and the two are not interchangeable
        /// even where they are numerically similar. See <see cref="SupplyTemperatureRule"/>.
        /// </para>
        /// </summary>
        [Description("Linear blend of extract and intake")] LinearBlend,

        /// <summary>
        /// The unit delivers what its published performance table states at this hour's intake temperature,
        /// extract temperature and airflow.
        /// <para>
        /// The table is the product's own data and stays where it already lives - on the template - rather
        /// than being copied into the rule. A rule of this type says "read the published table", and
        /// whoever resolves the strategy supplies it.
        /// </para>
        /// </summary>
        [Description("Published performance table")] PerformanceTable,
    }
}
