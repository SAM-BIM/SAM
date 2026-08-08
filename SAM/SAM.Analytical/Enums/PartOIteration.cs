// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Enums
{
    /// <summary>
    /// The Approved Document O operating iteration a scenario states - the mitigation stage the building is
    /// being assessed at.
    /// <para>
    /// Part O is assessed in stages: a building is tested with its base provision first, and further
    /// mitigation is added only where that fails. Each stage is a genuinely different set of operating
    /// assumptions over the same fabric, so it is a different assessment of the same dwelling and must
    /// carry a different identity. That is the whole reason this appears in the scenario key.
    /// </para>
    /// <para>
    /// <b>Identity only.</b> No member of this enum causes any behaviour anywhere. Naming a stage here is
    /// not implementing it: the operating assumptions that make a stage what it is live in
    /// <c>OverheatingOperatingAssumptions</c>, and the ones for the mitigated stages are not written yet.
    /// </para>
    /// <para>
    /// <b>There is deliberately no foundation-stage member.</b> The foundation work is a development stage
    /// of SAM, not an operating scenario of a building, and giving it a member would put a fact about this
    /// codebase's schedule into an engineering identity that outlives it - and would then have to be
    /// migrated or kept forever. A scenario built during that work states <see cref="Undefined"/>, which is
    /// true: it has not chosen an operating iteration.
    /// </para>
    /// </summary>
    public enum PartOIteration
    {
        /// <summary>
        /// No operating iteration is stated. The honest value for a scenario built to exercise the
        /// machinery rather than to assess a building against a mitigation stage.
        /// </summary>
        Undefined,

        /// <summary>
        /// Base provision: openings operated without restriction, mechanical ventilation at its design
        /// continuous rate. Nothing has been added to mitigate overheating.
        /// </summary>
        BasePassive,

        /// <summary>
        /// Openings restricted for noise, with the mechanical system's boost and summer bypass states
        /// available to compensate.
        /// </summary>
        AcousticRestricted,

        /// <summary>
        /// Active trim cooling added on top of the ventilation provision.
        /// </summary>
        ActiveTrimCooling
    }
}
