// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// Which ventilation strategy governs which space, as stated by <see cref="OverheatingScenario"/> - the
    /// authoritative answer a TM59 assessment reads instead of deriving one.
    /// <para>
    /// <b>Why this exists.</b> Three different places in this workspace decided how a space is ventilated and
    /// they disagreed. The one that picks the TM59 criterion took the space's internal condition, then fell
    /// back to matching a <i>zone's name</i> against a system-type library, then defaulted to <c>"NV"</c>. A
    /// zone called "Flat 1" matches nothing in that library, so a real MVRE dwelling was assessed against the
    /// natural-ventilation criterion - a wrong answer that looks like a right one. The other two fed the TM59
    /// XML from the internal condition and from any related mechanical <c>VentilationSystem</c>, which is how
    /// one building came back with "Nat Vent" and "Mech Vent" mixed across three identical flats.
    /// </para>
    /// <para>
    /// <b>The scenario wins over all of them.</b> A scenario is a statement of engineering intent - this
    /// dwelling, this mitigation stage, this system - and the model's internal conditions, its mechanical
    /// systems and its zone names are inputs to a simulation, not statements about which Approved Document O
    /// criterion applies. Where this map is supplied, none of the three derivations is consulted.
    /// </para>
    /// <para>
    /// <b>And it refuses rather than falls back.</b> Where no scenario covers a space, or the scenario that
    /// does states no strategy, or two scenarios state different ones, the space is refused with a reason. It
    /// then produces no assessment - a visible gap - instead of an assessment against a guessed criterion.
    /// Refusing is the whole point: the defect being fixed is a silent default, not the absence of one.
    /// </para>
    /// <para>
    /// <b>Nothing here is inferred from provenance.</b> The strategy comes from the scenario and from nowhere
    /// else - not from a model's <c>Source</c>, not from whether the results came through a TSD or a TPD, not
    /// from which engine wrote them. Provenance says where a number came from; it says nothing about how a
    /// building is ventilated.
    /// </para>
    /// <para>
    /// <b>Keyed on <see cref="Space.Guid"/>, never on a name</b> - every flat in a block has a "Bedroom 2".
    /// The caller supplies the spaces each scenario governs and must supply the <b>same space objects the
    /// assessment will run over</b>, because those carry the guids the assessment looks up. Resolving a
    /// simulation space back to its design space is <c>SimulationSpaceMap</c>'s job and a separate step; this
    /// class deliberately does no matching of its own so that it cannot quietly reintroduce matching by name.
    /// </para>
    /// </summary>
    public class VentilationStrategyMap
    {
        /// <summary>What one scenario said about one space.</summary>
        private class Claim
        {
            public string SpaceName;

            public Guid ZoneGuid;

            /// <summary>The normalised strategy, or null where the scenario stated none.</summary>
            public string VentilationStrategy;

            /// <summary>Whether a second scenario claimed this space and said something different.</summary>
            public bool IsConflicted;
        }

        private readonly Dictionary<Guid, Claim> dictionary_Claim = [];

        /// <summary>
        /// Records that a scenario governs the given spaces.
        /// <para>
        /// <b>A scenario stating no strategy is still recorded</b>, as a claim that states nothing. It has to
        /// be: "you did not say how this dwelling is ventilated" and "no scenario mentions this space at all"
        /// are different mistakes with different fixes, and collapsing them into one refusal would send
        /// somebody looking in the wrong place. Both refuse.
        /// </para>
        /// <para>
        /// <b>Two scenarios over one space refuse unless they agree.</b> Identical strategies are not a
        /// conflict - the same thing said twice is still one answer. Anything else means the scenarios have
        /// not said how the space is ventilated, and the same rule applies when one of them states nothing.
        /// </para>
        /// </summary>
        /// <param name="overheatingScenario">The scenario. Ignored where null or not <c>IsValid</c>.</param>
        /// <param name="spaces">
        /// The spaces it governs - the objects the assessment will run over, for their guids.
        /// </param>
        /// <returns>Whether anything was recorded.</returns>
        public bool Add(OverheatingScenario overheatingScenario, IEnumerable<Space> spaces)
        {
            if (overheatingScenario == null || !overheatingScenario.IsValid || spaces == null)
            {
                //A scenario that names nothing assessable states nothing about ventilation either. Recording
                //it would put a claim in the map that no caller could act on.
                return false;
            }

            //Normalised here rather than at every read: SystemTemplate's copy and JSON constructors do not
            //strip spaces the way its setters do, so "MV RE" can reach this point where "MVRE" was meant.
            string ventilationStrategy = overheatingScenario.HasVentilationStrategy
                ? overheatingScenario.VentilationStrategy.Trim().ToUpper()
                : null;

            bool result = false;

            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                result = true;

                if (!dictionary_Claim.TryGetValue(space.Guid, out Claim claim))
                {
                    dictionary_Claim[space.Guid] = new Claim
                    {
                        SpaceName = space.Name,
                        ZoneGuid = overheatingScenario.ZoneGuid,
                        VentilationStrategy = ventilationStrategy
                    };

                    continue;
                }

                if (claim.IsConflicted || string.Equals(claim.VentilationStrategy, ventilationStrategy, StringComparison.Ordinal))
                {
                    //Already refused, or the same answer again. Neither changes anything.
                    continue;
                }

                claim.IsConflicted = true;
            }

            return result;
        }

        /// <summary>
        /// The strategy governing a space, or a refusal naming what was missing.
        /// </summary>
        public VentilationStrategySelection Selection(Space space)
        {
            if (space == null)
            {
                return VentilationStrategySelection.Refused("No space was given, so no ventilation strategy can apply to one.");
            }

            if (!dictionary_Claim.TryGetValue(space.Guid, out Claim claim))
            {
                return VentilationStrategySelection.Refused(string.Format("No overheating scenario covers space '{0}', so nothing states how it is ventilated and it cannot be assessed against a TM59 criterion.", space.Name));
            }

            if (claim.IsConflicted)
            {
                return VentilationStrategySelection.Refused(string.Format("More than one overheating scenario covers space '{0}' and they state different ventilation strategies, so it is not settled how it is ventilated.", claim.SpaceName));
            }

            if (claim.VentilationStrategy == null)
            {
                return VentilationStrategySelection.Refused(string.Format("The overheating scenario for design zone {0} states no ventilation strategy, so space '{1}' cannot be assessed against a TM59 criterion.", claim.ZoneGuid, claim.SpaceName));
            }

            return VentilationStrategySelection.Selected(claim.VentilationStrategy);
        }

        /// <summary>How many spaces a scenario has claimed, whether or not it stated a strategy for them.</summary>
        public int Count => dictionary_Claim.Count;
    }
}
