// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// What Approved Document F requires a dwelling's system to be able to do, read off the Part F
        /// assessment that sized it.
        /// <para>
        /// <b>Two capabilities, and only ever two.</b> Part F asks for continuous ventilation at the whole
        /// dwelling design rate, and for the ability to raise that rate where a wet room's Table 1.2 high
        /// rate exceeds it. It asks for nothing else. In particular it does <b>not</b> ask for summer
        /// bypass or heat recovery: those are mitigation an Approved Document O scenario states, and a rule
        /// reading a Part F result must never invent them - a dwelling credited with mitigation its design
        /// does not have would pass an overheating assessment it should fail.
        /// </para>
        /// <para>
        /// <b>Boost is a comparison, not a flag.</b> The dwelling needs a system that can go above its
        /// continuous rate exactly when the balanced high-rate extract does go above it. Intermittent
        /// extract - a cooker hood, a separate fan - is deliberately excluded: it is not part of the
        /// balanced system, so its rate says nothing about what that system has to be able to do. This
        /// mirrors <c>PartFDwellingResult.TotalHighExtract_Lps</c>, which excludes it for the same reason.
        /// </para>
        /// <para>
        /// <b>The requirement, not the answer.</b> Which system meets it is decided by
        /// <see cref="SelectMinimumCapableSystem"/> over the systems a caller offers, and which systems
        /// exist is not a fact this assembly knows.
        /// </para>
        /// </summary>
        /// <param name="partFDwellingResult">A sized Part F dwelling assessment.</param>
        /// <param name="tolerance">
        /// The margin in l/s by which the high rate must exceed the continuous rate before boost counts as
        /// required, so that two rates equal to within rounding are not read as a demand for extra plant.
        /// A literal rather than <c>Core.Tolerance.MacroDistance</c>, which is a distance and happens to
        /// have the same value - these are flow rates and borrowing a length's tolerance for them would be
        /// a coincidence, not a reason.
        /// </param>
        public static SystemCapabilityRequirement PartFSystemCapabilityRequirement(this PartFDwellingResult partFDwellingResult, double tolerance = 0.001)
        {
            SystemCapabilityRequirement result = new();

            if (partFDwellingResult == null)
            {
                return result;
            }

            double continuous = partFDwellingResult.ContinuousDesignSystemRate_Lps;

            if (continuous <= tolerance)
            {
                //Nothing was sized, so nothing is required. An empty requirement is refused by the
                //selector rather than answered with the smallest system on the shelf.
                return result;
            }

            result = result.With(SystemCapability.ContinuousVentilation);

            if (partFDwellingResult.TotalHighExtract_Lps > continuous + tolerance)
            {
                result = result.With(SystemCapability.Boost);
            }

            return result;
        }
    }
}
