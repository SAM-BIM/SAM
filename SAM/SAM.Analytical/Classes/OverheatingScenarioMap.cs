// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// Ties <see cref="OverheatingScenario"/>s to the simulated spaces they govern, and TM59 results back to the
    /// scenario that asked for them - <b>entirely by identity</b>.
    /// <para>
    /// <b>The chain, and every link in it is a guid.</b> A scenario names a design zone, not a simulated space,
    /// and the simulated model is a rebuild whose objects a scenario cannot name:
    /// </para>
    /// <code>
    /// scenario.ZoneGuid
    ///   -> design Zone            (design model, by Guid)
    ///   -> design Spaces          (design model relations)
    ///   -> simulated Spaces       (SimulationSpaceMap, design -> simulation)
    ///   -> VentilationStrategyMap (keyed on simulated Space.Guid)
    ///
    /// TMResult.Reference == simulated Space.Guid
    ///   -> scenario               (the reverse of the above)
    /// </code>
    /// <para>
    /// <b>Not one step of it consults a name</b>, and nothing in it consults a file path, a TSD or TPD value, or
    /// a result's <c>Source</c>. Provenance says where a number came from; it is not an identity and it must
    /// never decide which dwelling a number belongs to. A whole block of flats is typically one model where
    /// every flat has a "Bedroom 2", so a name is not a weaker identity here - it is not an identity at all.
    /// </para>
    /// <para>
    /// <b>It refuses, and a refusal is not a smaller answer.</b> A scenario whose zone is missing from the
    /// design model, a zone with no spaces, a design space that no simulated space resolves to, a design space
    /// that two of them resolve to, two scenarios claiming one space - each is refused with a sentence naming
    /// what could not be tied together. Misattributing one dwelling's overheating to another is the worst error
    /// this workflow can make, and it is invisible: the numbers look right and belong to the wrong flat.
    /// </para>
    /// <para>
    /// <b>The corridor is in this map, and it is not in any dwelling.</b> A communal corridor gets its own
    /// scenario at <c>PartOAssessmentScope.CommonSpace</c> - assessed in its own right, attributed to no flat -
    /// so it is separated by which scenario owns it, not by being dropped. See
    /// <c>Query.PartOClassifyAssessmentZones</c>, which is where that split is decided.
    /// </para>
    /// </summary>
    public class OverheatingScenarioMap
    {
        private readonly AnalyticalModel analyticalModel_Design = null;

        private readonly SimulationSpaceMap simulationSpaceMap = null;

        /// <summary>Simulated space guid to the scenario governing it. One scenario, or none.</summary>
        private readonly Dictionary<Guid, OverheatingScenario> dictionary_Scenario = [];

        /// <summary>Scenario key to the simulated spaces it governs.</summary>
        private readonly Dictionary<Guid, List<Space>> dictionary_Spaces = [];

        private readonly List<OverheatingScenario> overheatingScenarios = [];

        private readonly List<string> refusals = [];

        private readonly VentilationStrategyMap ventilationStrategyMap = new();

        /// <param name="overheatingScenarios">
        /// The scenarios being assessed - typically one per dwelling plus one per common space.
        /// </param>
        /// <param name="analyticalModel_Design">
        /// The design model. It is what says which rooms make up which zone, and a scenario is stated against
        /// its zones.
        /// </param>
        /// <param name="simulationSpaceMap">How a design space is known to be a given simulated space.</param>
        public OverheatingScenarioMap(IEnumerable<OverheatingScenario> overheatingScenarios, AnalyticalModel analyticalModel_Design, SimulationSpaceMap simulationSpaceMap)
        {
            this.analyticalModel_Design = analyticalModel_Design;
            this.simulationSpaceMap = simulationSpaceMap;

            if (analyticalModel_Design == null)
            {
                refusals.Add("There is no design model, so no scenario can be tied to the spaces it governs.");

                return;
            }

            if (simulationSpaceMap == null)
            {
                refusals.Add("There is no simulation space map, so no design space can be tied to a simulated one. Matching by name is not an alternative.");

                return;
            }

            foreach (OverheatingScenario overheatingScenario in overheatingScenarios ?? [])
            {
                Add(overheatingScenario);
            }
        }

        /// <summary>The scenarios that were successfully tied to at least one simulated space.</summary>
        public List<OverheatingScenario> OverheatingScenarios => overheatingScenarios.ConvertAll(x => new OverheatingScenario(x));

        /// <summary>
        /// The ventilation strategies the scenarios state, keyed on the <b>simulated</b> space guids resolved
        /// here - ready to hand to <c>TMOverheatingCalculator</c> or the TM59 export.
        /// <para>
        /// This is the join step 7 could not make on its own: step 7 made the scenario authoritative but had no
        /// way to know which simulated space a scenario's design zone had become, so a real caller could not
        /// build a usable map. This is that way.
        /// </para>
        /// </summary>
        public VentilationStrategyMap VentilationStrategyMap => ventilationStrategyMap;

        /// <summary>What could not be tied together, one sentence each. A copy.</summary>
        public List<string> Refusals => [.. refusals];

        /// <summary>Whether every scenario given was tied to its spaces with nothing refused.</summary>
        public bool IsComplete => refusals.Count == 0;

        /// <summary>
        /// The simulated spaces a scenario governs. Empty where the scenario was refused - never a partial set
        /// standing in for the whole dwelling.
        /// </summary>
        public List<Space> Spaces(OverheatingScenario overheatingScenario)
        {
            return overheatingScenario != null && dictionary_Spaces.TryGetValue(overheatingScenario.Key, out List<Space> result) ? [.. result] : [];
        }

        /// <summary>
        /// The scenario governing a simulated space, or null where none does or more than one claimed it.
        /// </summary>
        public OverheatingScenario Scenario(Space space_Simulation)
        {
            return space_Simulation != null && dictionary_Scenario.TryGetValue(space_Simulation.Guid, out OverheatingScenario result) && result != null ? new OverheatingScenario(result) : null;
        }

        /// <summary>
        /// The scenario a TM59 result belongs to, or null where it cannot be tied to one.
        /// <para>
        /// <b>Through <c>Reference</c>, which holds the simulated space's guid</b> - the identity the assessment
        /// stamped on the result as it produced it. Not the result's <c>Name</c>, which is the room's name and
        /// is shared across flats, and not its <c>Source</c>, which is provenance.
        /// </para>
        /// </summary>
        public OverheatingScenario Scenario(TMResult tMResult)
        {
            if (tMResult == null || !Guid.TryParse(tMResult.Reference, out Guid guid_Space))
            {
                return null;
            }

            return dictionary_Scenario.TryGetValue(guid_Space, out OverheatingScenario result) && result != null ? new OverheatingScenario(result) : null;
        }

        /// <summary>
        /// Every TM59 result in an assessment, grouped by the scenario that asked for it.
        /// <para>
        /// A result that cannot be tied to a scenario goes to <paramref name="tMResults_Unassociated"/> rather
        /// than into the nearest group. Silence there would be the misattribution this class exists to prevent.
        /// </para>
        /// </summary>
        public Dictionary<OverheatingScenario, List<TMResult>> Associate(TM59AssessmentResult tM59AssessmentResult, out List<TMResult> tMResults_Unassociated)
        {
            Dictionary<OverheatingScenario, List<TMResult>> result = [];

            tMResults_Unassociated = [];

            if (tM59AssessmentResult == null)
            {
                return result;
            }

            List<TMResult> tMResults = [];
            tMResults.AddRange(tM59AssessmentResult.MechanicalVentilationResults ?? []);
            tMResults.AddRange(tM59AssessmentResult.NaturalVentilationResults ?? []);
            tMResults.AddRange(tM59AssessmentResult.CorridorResults ?? []);

            foreach (TMResult tMResult in tMResults)
            {
                if (tMResult == null)
                {
                    continue;
                }

                OverheatingScenario overheatingScenario = Scenario(tMResult);
                if (overheatingScenario == null)
                {
                    tMResults_Unassociated.Add(tMResult);

                    continue;
                }

                if (!result.TryGetValue(overheatingScenario, out List<TMResult> tMResults_Scenario))
                {
                    tMResults_Scenario = [];
                    result[overheatingScenario] = tMResults_Scenario;
                }

                tMResults_Scenario.Add(tMResult);
            }

            return result;
        }

        /// <summary>
        /// Ties one scenario to the simulated spaces of its design zone, refusing at the first link that does
        /// not hold rather than recording a partial dwelling.
        /// </summary>
        private void Add(OverheatingScenario overheatingScenario)
        {
            if (overheatingScenario == null || !overheatingScenario.IsValid)
            {
                refusals.Add("A scenario names nothing assessable - no scope, or no design zone - so it cannot be tied to any space.");

                return;
            }

            Zone zone_Design = analyticalModel_Design.GetZones()?.Find(x => x != null && x.Guid == overheatingScenario.ZoneGuid);
            if (zone_Design == null)
            {
                refusals.Add(string.Format("The scenario for design zone {0} names a zone the design model does not hold, so the spaces it covers cannot be identified.", overheatingScenario.ZoneGuid));

                return;
            }

            List<Space> spaces_Design = analyticalModel_Design.AdjacencyCluster.GetRelatedObjects<Space>(zone_Design);
            if (spaces_Design == null || spaces_Design.Count == 0)
            {
                refusals.Add(string.Format("Design zone '{0}' holds no spaces, so the scenario for it covers nothing.", zone_Design.Name));

                return;
            }

            List<Space> spaces = [];

            foreach (Space space_Design in spaces_Design)
            {
                if (space_Design == null)
                {
                    continue;
                }

                Space space = simulationSpaceMap.Simulation(space_Design);
                if (space == null)
                {
                    //Unresolved, or claimed by two simulated spaces. Either way the scenario is refused whole:
                    //assessing three of a flat's four rooms and reporting it as the flat would be worse than
                    //saying the flat could not be assessed.
                    refusals.Add(string.Format("Design space '{0}' in zone '{1}' does not resolve to exactly one simulated space, so the scenario for that zone cannot be tied to its spaces.", space_Design.Name, zone_Design.Name));

                    return;
                }

                if (dictionary_Scenario.TryGetValue(space.Guid, out OverheatingScenario overheatingScenario_Existing))
                {
                    //Two scenarios over one simulated space. Neither wins - the same rule SimulationSpaceMap and
                    //SelectPreferredCapableSystem already follow.
                    refusals.Add(string.Format("Simulated space '{0}' is claimed by more than one scenario - design zones {1} and {2} - so which assessment it belongs to is not settled.", space.Name, overheatingScenario_Existing?.ZoneGuid, overheatingScenario.ZoneGuid));

                    dictionary_Scenario[space.Guid] = null;

                    return;
                }

                spaces.Add(space);
            }

            OverheatingScenario overheatingScenario_Stored = new(overheatingScenario);

            foreach (Space space in spaces)
            {
                dictionary_Scenario[space.Guid] = overheatingScenario_Stored;
            }

            dictionary_Spaces[overheatingScenario_Stored.Key] = spaces;
            overheatingScenarios.Add(overheatingScenario_Stored);

            //The strategy the scenario states, now against identities the assessment will actually see.
            ventilationStrategyMap.Add(overheatingScenario_Stored, spaces);
        }
    }
}
