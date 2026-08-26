// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// What a ventilation system has to be able to move, summed from the design terminals connected
        /// to it.
        /// <para>
        /// <b>Derived, never stored.</b> A duty written onto the system would be a second answer that
        /// goes stale the moment a terminal is added, removed or re-balanced - and subdividing terminals
        /// is exactly what the design stage does. Asking the terminals every time is the only way the
        /// answer cannot be wrong.
        /// </para>
        /// <para>
        /// <b>Supply and extract are reported separately and are not required to be equal.</b> A balanced
        /// heat recovery system balances at the <i>system</i>, not in each room: a bedroom is supplied and
        /// not extracted, a bathroom is extracted and not supplied, and the air moves between them as
        /// transfer air. Forcing them equal per room is the mistake that would put an extract terminal in
        /// every bedroom.
        /// </para>
        /// <para>
        /// This is the magnitude half of what Iteration 2 needs. <see cref="PartFSystemCapabilityRequirement"/>
        /// already states what a system must be <i>able</i> to do; this states how much air it has to move
        /// while doing it.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model.</param>
        /// <param name="ventilationSystem">The system whose connected terminals are summed.</param>
        /// <param name="supplyDuty_Lps">Total design supply duty [l/s]. Zero where the system has no supply terminal.</param>
        /// <param name="extractDuty_Lps">Total design extract duty [l/s]. Zero where the system has no extract terminal.</param>
        /// <returns>Whether any terminal at all was found to sum.</returns>
        public static bool VentilationSystemDesignDuty(this AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, out double supplyDuty_Lps, out double extractDuty_Lps)
        {
            supplyDuty_Lps = 0;
            extractDuty_Lps = 0;

            List<VentilationTerminal> ventilationTerminals = adjacencyCluster.VentilationTerminals(ventilationSystem);
            if (ventilationTerminals is null || ventilationTerminals.Count == 0)
            {
                return false;
            }

            supplyDuty_Lps = ventilationTerminals.VentilationTerminalDesignDuty_Lps(FlowClassification.Supply) ?? 0;
            extractDuty_Lps = ventilationTerminals.VentilationTerminalDesignDuty_Lps(FlowClassification.Extract) ?? 0;

            return true;
        }

        /// <summary>
        /// Checks the design duty of a system's terminals against the Approved Document F requirement its
        /// spaces carry, and says where they disagree.
        /// <para>
        /// <b>Two independent derivations, compared rather than reconciled.</b> The duty comes from the
        /// design terminals; the requirement comes from <c>PartFSpaceData</c> on the spaces, which is what
        /// <c>PartFCalculator</c> wrote and what <c>Modify.ApplyPartFVentilationRates</c> read. Neither is
        /// allowed to correct the other. A disagreement at the system total is a <b>refusal</b>, because
        /// the model would then simulate a dwelling ventilated to a figure nobody sized.
        /// </para>
        /// <para>
        /// <b>A room-level disagreement is reported, not refused.</b> Two terminals of 10 l/s realizing a
        /// 20 l/s requirement agree; two of 15 do not, and the design may still be deliberate. Naming the
        /// room and both numbers puts that in front of the engineer without deciding it for them - and any
        /// divergence that is not cancelled out elsewhere reaches the system total anyway, where it does
        /// refuse.
        /// </para>
        /// </summary>
        /// <param name="tolerance_Lps">
        /// The margin in l/s within which two rates count as agreeing. A flow-rate literal rather than a
        /// borrowed distance tolerance - see <see cref="PartFSystemCapabilityRequirement"/> for the same
        /// reasoning.
        /// </param>
        /// <returns>Whether the totals agree.</returns>
        public static bool ReconcileVentilationSystemDesignDuty(this AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, out List<string> notes, out List<string> warnings, out List<string> refusals, double tolerance_Lps = 0.001)
        {
            notes = [];
            warnings = [];
            refusals = [];

            if (adjacencyCluster is null || ventilationSystem is null)
            {
                refusals.Add("No ventilation system was supplied, so its design duty could not be checked against the Approved Document F requirement.");

                return false;
            }

            adjacencyCluster.VentilationSystemDesignDuty(ventilationSystem, out double supplyDuty_Lps, out double extractDuty_Lps);

            double requirement_Supply_Lps = 0;
            double requirement_Extract_Lps = 0;

            List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [];

            foreach (Space space in spaces)
            {
                PartFSpaceData partFSpaceData = space?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData);
                if (partFSpaceData is null)
                {
                    continue;
                }

                double space_Requirement_Supply_Lps = partFSpaceData.ContinuousSupplyFlowRate_Lps ?? 0;
                double space_Requirement_Extract_Lps = partFSpaceData.ContinuousExtractFlowRate_Lps ?? 0;

                requirement_Supply_Lps += space_Requirement_Supply_Lps;
                requirement_Extract_Lps += space_Requirement_Extract_Lps;

                List<VentilationTerminal> ventilationTerminals_Space = adjacencyCluster.VentilationTerminals(space);

                double space_Duty_Supply_Lps = ventilationTerminals_Space.VentilationTerminalDesignDuty_Lps(FlowClassification.Supply) ?? 0;
                double space_Duty_Extract_Lps = ventilationTerminals_Space.VentilationTerminalDesignDuty_Lps(FlowClassification.Extract) ?? 0;

                Note(notes, warnings, space, "supply", space_Duty_Supply_Lps, space_Requirement_Supply_Lps, tolerance_Lps);
                Note(notes, warnings, space, "extract", space_Duty_Extract_Lps, space_Requirement_Extract_Lps, tolerance_Lps);
            }

            bool result = true;

            result &= Refuse(refusals, ventilationSystem, "supply", supplyDuty_Lps, requirement_Supply_Lps, tolerance_Lps);
            result &= Refuse(refusals, ventilationSystem, "extract", extractDuty_Lps, requirement_Extract_Lps, tolerance_Lps);

            if (result)
            {
                notes.Add(string.Format(
                    "Ventilation system '{0}' design duty: supply {1:0.###} l/s, extract {2:0.###} l/s, summed from {3} design terminal(s) and agreeing with the Approved Document F requirement its spaces carry.",
                    ventilationSystem.FullName,
                    supplyDuty_Lps,
                    extractDuty_Lps,
                    (adjacencyCluster.VentilationTerminals(ventilationSystem) ?? []).Count));
            }

            return result;
        }

        private static void Note(List<string> notes, List<string> warnings, Space space, string direction, double duty_Lps, double requirement_Lps, double tolerance_Lps)
        {
            if (System.Math.Abs(duty_Lps - requirement_Lps) <= tolerance_Lps)
            {
                return;
            }

            string note = string.Format(
                "Space '{0}': the design {1} terminals total {2:0.###} l/s but Approved Document F sized {3:0.###} l/s. The design terminals are what will be simulated; this is reported so the difference is a decision rather than a surprise.",
                space.Name,
                direction,
                duty_Lps,
                requirement_Lps);

            notes.Add(note);
            warnings.Add(note);
        }

        private static bool Refuse(List<string> refusals, VentilationSystem ventilationSystem, string direction, double duty_Lps, double requirement_Lps, double tolerance_Lps)
        {
            if (System.Math.Abs(duty_Lps - requirement_Lps) <= tolerance_Lps)
            {
                return true;
            }

            refusals.Add(string.Format(
                "Ventilation system '{0}' has a design {1} duty of {2:0.###} l/s but the Approved Document F requirement on the spaces it serves totals {3:0.###} l/s. These are two independent statements of the same quantity and neither may be preferred silently, so nothing was prepared. Re-run the Part F calculation, or correct the design terminal duties, so that the two agree.",
                ventilationSystem.FullName,
                direction,
                duty_Lps,
                requirement_Lps));

            return false;
        }
    }
}
