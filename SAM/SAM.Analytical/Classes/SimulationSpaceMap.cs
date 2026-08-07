// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// Maps the spaces of a simulation-derived model back onto the design model they came from, so a result
    /// can be attributed to the design object that produced it.
    /// <para>
    /// <b>Why this exists.</b> A simulation model is a rebuild, not the design model: reading a TAS TSD
    /// produces fresh <c>Space</c> objects with fresh guids, and the results carry those. Attributing them
    /// by <b>name</b> - which is what the existing Grasshopper path does - works right up until two rooms
    /// share a name, and in a block of flats that is the normal case rather than the exotic one: every flat
    /// has a "Bedroom 2". Getting it wrong does not fail loudly; it silently reports one dwelling's
    /// overheating against another's, which is the worst class of error this workflow can make.
    /// </para>
    /// <para>
    /// <b>It never guesses.</b> Where an identity cannot be resolved unambiguously the space is reported as
    /// unresolved or ambiguous and no mapping is offered. A missing result is a visible gap; a confidently
    /// misattributed one is a wrong engineering answer that looks right.
    /// </para>
    /// <para>
    /// The stable key is supplied by the caller rather than named here, because the identity that survives a
    /// round trip is engine-specific - for TAS it is the zone guid stamped onto the design spaces by the
    /// workflow - and <c>SAM.Analytical</c> must not know about any engine. Name matching remains as the
    /// fallback for models that predate the stamp, but only where the name is unique on both sides.
    /// </para>
    /// </summary>
    public class SimulationSpaceMap
    {
        private readonly Dictionary<Guid, Space> dictionary_Design = [];

        private readonly List<Space> spaces_Unresolved = [];

        private readonly List<Space> spaces_Ambiguous = [];

        /// <summary>
        /// Builds the map.
        /// </summary>
        /// <param name="spaces_Design">The spaces of the design model - the identities results must land on.</param>
        /// <param name="spaces_Simulation">The spaces of the model read back from the simulation.</param>
        /// <param name="func_StableKey">
        /// The engine-stable identity of a space, or null/blank where it has none. Applied to both sides. A
        /// caller with no such identity may pass null, which falls the whole map back to unique names.
        /// </param>
        public SimulationSpaceMap(IEnumerable<Space> spaces_Design, IEnumerable<Space> spaces_Simulation, Func<Space, string> func_StableKey = null)
        {
            List<Space> spaces_Design_Temp = Clean(spaces_Design);
            List<Space> spaces_Simulation_Temp = Clean(spaces_Simulation);

            Dictionary<string, List<Space>> dictionary_Key = Group(spaces_Design_Temp, func_StableKey);
            Dictionary<string, List<Space>> dictionary_Name = Group(spaces_Design_Temp, x => x.Name);

            foreach (Space space_Simulation in spaces_Simulation_Temp)
            {
                //The stable key first, and where it is present on both sides it is the whole answer: a name
                //disagreement then means the space was renamed, not that the match is wrong.
                string key = func_StableKey?.Invoke(space_Simulation);

                if (!string.IsNullOrWhiteSpace(key) && dictionary_Key.TryGetValue(key, out List<Space> spaces_Key))
                {
                    if (spaces_Key.Count == 1)
                    {
                        dictionary_Design[space_Simulation.Guid] = spaces_Key[0];
                        continue;
                    }

                    //One key on two design spaces is a broken model, not a naming coincidence. Say so rather
                    //than fall through to the weaker rule and appear to have resolved it.
                    spaces_Ambiguous.Add(space_Simulation);
                    continue;
                }

                if (dictionary_Name.TryGetValue(space_Simulation.Name ?? string.Empty, out List<Space> spaces_Name))
                {
                    if (spaces_Name.Count == 1)
                    {
                        dictionary_Design[space_Simulation.Guid] = spaces_Name[0];
                        continue;
                    }

                    //Two design spaces share this name - typically the same room in two flats. Refusing here
                    //is the entire point of this class.
                    spaces_Ambiguous.Add(space_Simulation);
                    continue;
                }

                spaces_Unresolved.Add(space_Simulation);
            }
        }

        /// <summary>
        /// The design space a simulation space came from, or null where it could not be resolved
        /// unambiguously.
        /// </summary>
        public Space Design(Space space_Simulation)
        {
            return space_Simulation != null && dictionary_Design.TryGetValue(space_Simulation.Guid, out Space result) ? result : null;
        }

        /// <summary>Simulation spaces with no counterpart in the design model at all.</summary>
        public List<Space> Unresolved => [.. spaces_Unresolved];

        /// <summary>
        /// Simulation spaces whose counterpart could not be told apart from another - a shared name, or one
        /// stable key on two design spaces. Reported, never guessed at.
        /// </summary>
        public List<Space> Ambiguous => [.. spaces_Ambiguous];

        /// <summary>Whether every simulation space resolved to exactly one design space.</summary>
        public bool IsComplete => spaces_Unresolved.Count == 0 && spaces_Ambiguous.Count == 0;

        private static List<Space> Clean(IEnumerable<Space> spaces)
        {
            List<Space> result = [];

            foreach (Space space in spaces ?? [])
            {
                if (space != null)
                {
                    result.Add(space);
                }
            }

            return result;
        }

        private static Dictionary<string, List<Space>> Group(IEnumerable<Space> spaces, Func<Space, string> func)
        {
            Dictionary<string, List<Space>> result = [];

            if (func == null)
            {
                return result;
            }

            foreach (Space space in spaces)
            {
                string key = func.Invoke(space);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!result.TryGetValue(key, out List<Space> spaces_Key))
                {
                    spaces_Key = [];
                    result[key] = spaces_Key;
                }

                spaces_Key.Add(space);
            }

            return result;
        }
    }
}
