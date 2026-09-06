// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Every space-to-space <see cref="SpaceAirMovement"/> in the model, keyed by the two spaces it
        /// connects - so a caller that needs many of them (a floor-plan overlay, one lookup per internal
        /// route) reads the model once rather than once per route.
        /// <para>
        /// <b>Space-to-space only.</b> A <see cref="SpaceAirMovement"/> whose <see cref="SpaceAirMovement.From"/>
        /// or <see cref="SpaceAirMovement.To"/> resolves to an <see cref="AirHandlingUnit"/> or to nothing
        /// (outside) is a supply, extract or exhaust leg, not transfer air, and is not included here - see
        /// <see cref="Modify.AddPartFTransferAirMovements"/> for where the two are told apart at source.
        /// </para>
        /// <para>
        /// <b>This is the DESIGN transfer authority, not Approved Document F's.</b> <c>AddPartFTransferAirMovements</c>
        /// writes exactly one of these per internal connection that carries design airflow, solved by the
        /// same <see cref="PartFAirflowNetwork"/> Approved Document F's own transfer requirement is solved
        /// over, but fed the CURRENT design terminal duties rather than Approved Document F's Table 1.2
        /// sizing. A <c>PartFDoorTransferData</c> and the entry here for the same two spaces can disagree in
        /// magnitude - that is the point, not a bug: raising a room's design airflow through Approved
        /// Document O changes what has to pass between the rooms, not what Approved Document F originally
        /// required of them.
        /// </para>
        /// <para>
        /// <b>Read-only, and derives nothing.</b> The movements themselves are written by
        /// <see cref="Modify.AddPartFTransferAirMovements"/>; this only reads what is already there, exactly
        /// as <see cref="VentilationTerminalDesignDuty_Lps"/> reads design terminals without recomputing
        /// them.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model.</param>
        /// <returns>
        /// One entry per connected space pair, keyed with the smaller <see cref="Guid"/> first so the same
        /// pair always hashes to the same key regardless of which space a caller asks about first. A pair
        /// with no <see cref="SpaceAirMovement"/> between them - because the design does not need to
        /// transfer air across that connection - is simply absent, never a zero-flow entry.
        /// </returns>
        public static Dictionary<(Guid, Guid), SpaceAirMovement> DesignTransferSpaceAirMovements(this AdjacencyCluster adjacencyCluster)
        {
            Dictionary<(Guid, Guid), SpaceAirMovement> result = [];

            if (adjacencyCluster is null)
            {
                return result;
            }

            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetObjects<SpaceAirMovement>() ?? [])
            {
                if (spaceAirMovement is null)
                {
                    continue;
                }

                if (adjacencyCluster.AirMovementEndpoint(spaceAirMovement.From) is not Space space_From
                    || adjacencyCluster.AirMovementEndpoint(spaceAirMovement.To) is not Space space_To)
                {
                    continue;
                }

                result[Key(space_From.Guid, space_To.Guid)] = spaceAirMovement;
            }

            return result;
        }

        /// <summary>
        /// The design transfer flow [l/s], and which way it actually moves, between two spaces - or null
        /// where the design has no transfer movement between them at all (never an invented zero).
        /// </summary>
        /// <param name="adjacencyCluster">The model.</param>
        /// <param name="spaceGuid_1">One of the two spaces. Order does not matter - see <paramref name="spaceGuid_From"/>.</param>
        /// <param name="spaceGuid_2">The other space.</param>
        /// <param name="spaceGuid_From">
        /// The space the design air actually leaves, once a movement is found. <see cref="Guid.Empty"/>
        /// where none is found.
        /// </param>
        /// <param name="spaceGuid_To">The space the design air actually arrives at. <see cref="Guid.Empty"/> where none is found.</param>
        /// <param name="dictionary_SpaceAirMovement">
        /// A dictionary already built by <see cref="DesignTransferSpaceAirMovements"/>, so a caller looking
        /// up many routes reads the model once rather than once per route. Built fresh where null - the
        /// convenience overload for a single lookup.
        /// </param>
        /// <returns>The magnitude [l/s], always non-negative - direction is carried by the out parameters, never by the sign.</returns>
        public static double? DesignTransferFlowRate_Lps(this AdjacencyCluster adjacencyCluster, Guid spaceGuid_1, Guid spaceGuid_2, out Guid spaceGuid_From, out Guid spaceGuid_To, Dictionary<(Guid, Guid), SpaceAirMovement> dictionary_SpaceAirMovement = null)
        {
            spaceGuid_From = Guid.Empty;
            spaceGuid_To = Guid.Empty;

            if (adjacencyCluster is null)
            {
                return null;
            }

            Dictionary<(Guid, Guid), SpaceAirMovement> dictionary = dictionary_SpaceAirMovement ?? adjacencyCluster.DesignTransferSpaceAirMovements();

            if (!dictionary.TryGetValue(Key(spaceGuid_1, spaceGuid_2), out SpaceAirMovement spaceAirMovement) || spaceAirMovement is null)
            {
                return null;
            }

            if (adjacencyCluster.AirMovementEndpoint(spaceAirMovement.From) is not Space space_From
                || adjacencyCluster.AirMovementEndpoint(spaceAirMovement.To) is not Space space_To)
            {
                return null;
            }

            spaceGuid_From = space_From.Guid;
            spaceGuid_To = space_To.Guid;

            //SpaceAirMovement.AirFlow [m3/s] -> l/s. Always positive: SpaceAirMovement is already written in
            //the direction the air actually travels - see Modify.AddPartFTransferAirMovements - so there is
            //no sign left to carry; the out parameters say which way it goes.
            return spaceAirMovement.AirFlow * 1000.0;
        }

        /// <summary>The two spaces' guids, ordered so the same pair always produces the same key.</summary>
        private static (Guid, Guid) Key(Guid spaceGuid_1, Guid spaceGuid_2)
        {
            return spaceGuid_1.CompareTo(spaceGuid_2) <= 0 ? (spaceGuid_1, spaceGuid_2) : (spaceGuid_2, spaceGuid_1);
        }
    }
}
