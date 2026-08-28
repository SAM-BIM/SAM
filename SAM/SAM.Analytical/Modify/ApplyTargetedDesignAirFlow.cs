// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Sets <b>one</b> room's design airflow and rebalances the dwelling around it, as a single
        /// transaction.
        /// <para>
        /// <b>This is the architectural operation Approved Document O optimisation is built on, and the
        /// separation it draws is the point of it.</b> A failing bedroom is raised from 20 to 24 l/s. That
        /// is the <i>targeted</i> change, and it is the only room anybody chose: bedroom 2 and the living
        /// room pass, are not targets, and do not move. The dwelling's supply then exceeds its extract by
        /// 4 l/s, and a mechanical ventilation with heat recovery unit moves the air it takes in, so
        /// 4 l/s of extract has to appear somewhere. Where it appears is a <i>derived</i> consequence of
        /// the balanced network, decided by the allocation strategy the Part F calculation already names -
        /// never a second optimisation target.
        /// </para>
        /// <code>
        /// targeted:   Bedroom 1 supply 20 -> 24
        /// derived:    extract +4, allocated across the dwelling's extract terminals
        ///             transfer paths recalculated on the next preparation
        ///             air handling unit design duty follows
        /// unchanged:  every Approved Document F requirement, and every room nobody targeted
        /// </code>
        ///
        /// <para><b>The allocation rule is borrowed, not invented</b></para>
        /// <para>
        /// Extract above what Approved Document F requires is shared by
        /// <see cref="PartFExtractAllocationStrategy.MinimumFirstCookingPriority"/> - the surplus goes to
        /// the local kitchen extract, because the cooking function is the dwelling's largest single source
        /// of moisture and pollutants and removing them closest to source is the stated aim of requirement
        /// F1(1)(a). Where the dwelling has no local kitchen extract, and on the supply side where the
        /// Approved Document names no equivalent priority, the change is shared in proportion to what the
        /// rooms already carry, which preserves whatever balance a designer had already struck.
        /// <b><c>PartFCalculator.AllocateContinuousExtract</c> makes exactly this choice for exactly this
        /// reason</b>; this is the design-side application of it, and it is applied to the <i>change</i>
        /// rather than recomputed from scratch so that a deliberate imbalance a designer authored survives.
        /// </para>
        ///
        /// <para><b>All or nothing</b></para>
        /// <para>
        /// The whole plan - the targeted room and every derived one - is computed and checked against
        /// every Approved Document F floor <b>before a single terminal is written</b>. A dwelling that
        /// cannot be balanced is refused with nothing changed, rather than left with a raised bedroom and
        /// no matching extract. That state is exactly what
        /// <c>Modify.PreparePartOIteration</c>'s conservation check refuses to simulate, and producing it
        /// and then reporting it would be a worse answer than never producing it.
        /// </para>
        ///
        /// <para><b>What this does NOT do</b></para>
        /// <para>
        /// It writes design airflows and nothing else. The transfer paths, the inter-zone air movements
        /// and the air handling unit's derived duty are rebuilt by re-preparing the iteration, which is
        /// idempotent and already refuses where the dwelling's adjacencies cannot route the result - see
        /// <c>Modify.AddPartFTransferAirMovements</c>. No Approved Document F requirement is touched, no
        /// ventilation unit is selected or re-selected, and no runtime or profile airflow is written.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The model. <b>Modified in place on success, and untouched on a refusal.</b></param>
        /// <param name="space">The one room being targeted.</param>
        /// <param name="flowClassification">Which side of that room is being set.</param>
        /// <param name="designFlowRate_Lps">The room's new design airflow [l/s], as a total across its terminals.</param>
        /// <param name="partFExtractAllocationStrategy">
        /// How a derived extract change is shared out. Defaults to the same strategy
        /// <c>PartFCalculator</c> defaults to, so a design rebalance and the sizing that preceded it agree
        /// about where surplus extract belongs.
        /// </param>
        /// <returns>
        /// What was targeted, what was derived, and the duties that resulted - or a refusal with nothing
        /// written. Never null.
        /// </returns>
        public static DwellingDesignAirFlowChange ApplyTargetedDesignAirFlow(this AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification, double designFlowRate_Lps, PartFExtractAllocationStrategy partFExtractAllocationStrategy = PartFExtractAllocationStrategy.MinimumFirstCookingPriority, double tolerance_Lps = 0.001)
        {
            DwellingDesignAirFlowChange result = new();

            // ---- Resolve and validate, writing nothing --------------------------------------------------

            if (adjacencyCluster is null || space is null)
            {
                result.Refusals.Add("No space was supplied, so no design airflow could be applied.");

                return result;
            }

            if (flowClassification != FlowClassification.Supply && flowClassification != FlowClassification.Extract)
            {
                result.Refusals.Add(string.Format("A design airflow has to be supply or extract, and '{0}' is neither. Nothing was changed.", Core.Query.Description(flowClassification)));

                return result;
            }

            if (double.IsNaN(designFlowRate_Lps) || double.IsInfinity(designFlowRate_Lps) || designFlowRate_Lps < 0)
            {
                result.Refusals.Add(string.Format("Space '{0}': {1} l/s is not a design airflow. Nothing was changed.", space.Name, designFlowRate_Lps));

                return result;
            }

            //Taken from the cluster rather than trusted as handed in: a caller may be holding a space from
            //before the Part F rates were applied, and that one carries a different parameter set.
            Space space_Target = (adjacencyCluster.GetSpaces() ?? []).Find(x => x is not null && x.Guid == space.Guid);
            if (space_Target is null)
            {
                result.Refusals.Add(string.Format("Space '{0}' is not in the model, so its design airflow could not be applied.", space.Name));

                return result;
            }

            //The dwelling is the ventilation system the targeted room's terminals belong to. Resolved
            //through the terminals rather than through the space, because a model routinely still carries
            //the system assignment it was authored with and that assignment may describe a different design
            //stage - Modify.AddPartOBaseMVHRSystem keys reuse on the design terminals for the same reason.
            VentilationSystem ventilationSystem = VentilationSystem(adjacencyCluster, space_Target, flowClassification, result);
            if (ventilationSystem is null)
            {
                return result;
            }

            result.VentilationSystem = ventilationSystem;

            double duty_Target_Before_Lps = adjacencyCluster.VentilationTerminals(space_Target).VentilationTerminalDesignDuty_Lps(flowClassification) ?? 0;

            //The regulatory floor, read and never written. The targeted room may be raised as far as anyone
            //likes and lowered only as far as Approved Document F allows.
            double? requirement_Target_Lps = adjacencyCluster.PartFRequiredFlowRate_Lps(space_Target, flowClassification);

            if (requirement_Target_Lps.HasValue && designFlowRate_Lps + tolerance_Lps < requirement_Target_Lps.Value)
            {
                result.Refusals.Add(string.Format(
                    "Space '{0}': a design {1} airflow of {2:0.###} l/s is below the {3:0.###} l/s Approved Document F requires of that room. Design airflow is chosen above the regulatory minimum, never below it, so nothing was changed.",
                    space_Target.Name,
                    Core.Query.Description(flowClassification),
                    designFlowRate_Lps,
                    requirement_Target_Lps.Value));

                return result;
            }

            double change_Lps = designFlowRate_Lps - duty_Target_Before_Lps;

            // ---- Plan the derived consequence, still writing nothing ------------------------------------

            FlowClassification flowClassification_Opposite = flowClassification == FlowClassification.Supply ? FlowClassification.Extract : FlowClassification.Supply;

            //Every room of the dwelling that carries a terminal on the other side, with what it carries now
            //and what Approved Document F requires of it. These are the rooms the balancing consequence can
            //be spread over, and no others - a room in another dwelling is never one of them.
            List<Space> spaces_Opposite = [];
            Dictionary<Guid, double> dictionary_Duty = [];
            Dictionary<Guid, double> dictionary_Requirement = [];

            foreach (Space space_Related in adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [])
            {
                List<VentilationTerminal> ventilationTerminals = Query.VentilationTerminals(adjacencyCluster.VentilationTerminals(space_Related), flowClassification_Opposite) ?? [];
                if (ventilationTerminals.Count == 0)
                {
                    continue;
                }

                spaces_Opposite.Add(space_Related);
                dictionary_Duty[space_Related.Guid] = ventilationTerminals.VentilationTerminalDesignDuty_Lps(flowClassification_Opposite) ?? 0;
                dictionary_Requirement[space_Related.Guid] = adjacencyCluster.PartFRequiredFlowRate_Lps(space_Related, flowClassification_Opposite) ?? 0;
            }

            Dictionary<Guid, double> dictionary_Planned = [];

            if (System.Math.Abs(change_Lps) > tolerance_Lps)
            {
                if (spaces_Opposite.Count == 0)
                {
                    result.Refusals.Add(string.Format(
                        "Space '{0}' can be designed at {1:0.###} l/s of {2}, but ventilation system '{3}' has no {4} terminal for the resulting {5:0.###} l/s to be balanced by, so the dwelling would gain air it never loses and TAS would refuse to simulate it. Nothing was changed.",
                        space_Target.Name,
                        designFlowRate_Lps,
                        Core.Query.Description(flowClassification),
                        ventilationSystem.FullName,
                        Core.Query.Description(flowClassification_Opposite),
                        System.Math.Abs(change_Lps)));

                    return result;
                }

                if (!Allocate(spaces_Opposite, dictionary_Duty, dictionary_Requirement, change_Lps, flowClassification_Opposite, partFExtractAllocationStrategy, adjacencyCluster, tolerance_Lps, out dictionary_Planned, out string note_Allocation, out string refusal_Allocation))
                {
                    result.Refusals.Add(refusal_Allocation);

                    return result;
                }

                result.Notes.Add(note_Allocation);
            }

            // ---- Apply. Every floor is already checked, so nothing below can refuse ---------------------

            List<string> refusals = [];

            List<VentilationTerminal> ventilationTerminals_Target = adjacencyCluster.SetSpaceDesignFlowRate(space_Target, flowClassification, designFlowRate_Lps, out List<string> notes_Target, out List<string> refusals_Target, tolerance_Lps);

            refusals.AddRange(refusals_Target);

            if (ventilationTerminals_Target is not null)
            {
                result.Notes.AddRange(notes_Target);

                result.TargetedAdjustment = new DesignAirFlowAdjustment(
                    space_Target.Guid,
                    space_Target.Name,
                    flowClassification,
                    duty_Target_Before_Lps,
                    designFlowRate_Lps,
                    requirement_Target_Lps ?? double.NaN,
                    false);
            }

            foreach (Space space_Opposite in spaces_Opposite)
            {
                if (!dictionary_Planned.TryGetValue(space_Opposite.Guid, out double planned_Lps))
                {
                    continue;
                }

                double before_Lps = dictionary_Duty[space_Opposite.Guid];

                if (System.Math.Abs(planned_Lps - before_Lps) <= tolerance_Lps)
                {
                    continue;
                }

                if (adjacencyCluster.SetSpaceDesignFlowRate(space_Opposite, flowClassification_Opposite, planned_Lps, out List<string> notes_Opposite, out List<string> refusals_Opposite, tolerance_Lps) is null)
                {
                    refusals.AddRange(refusals_Opposite);

                    continue;
                }

                result.Notes.AddRange(notes_Opposite);

                result.DerivedAdjustments.Add(new DesignAirFlowAdjustment(
                    space_Opposite.Guid,
                    space_Opposite.Name,
                    flowClassification_Opposite,
                    before_Lps,
                    planned_Lps,
                    dictionary_Requirement[space_Opposite.Guid],
                    true));
            }

            if (refusals.Count != 0)
            {
                //Unreachable by design: the plan above checks exactly the conditions SetSpaceDesignFlowRate
                //checks, so a write that refuses here means the two have drifted apart. Reported rather than
                //rolled back, because a rollback nobody can trigger is untested code, and a loud, specific
                //message is what would actually get the drift fixed.
                result.Refusals.Add(string.Format(
                    "The design change to space '{0}' was validated and then partly refused while being written, which should not be possible: {1} The model may now hold a partly applied change - re-run the Part F calculation and the iteration preparation before trusting it.",
                    space_Target.Name,
                    string.Join(" ", refusals)));

                return result;
            }

            adjacencyCluster.VentilationSystemDesignDuty(ventilationSystem, out double supplyDuty_Lps, out double extractDuty_Lps);

            result.SupplyDuty_Lps = supplyDuty_Lps;
            result.ExtractDuty_Lps = extractDuty_Lps;

            if (System.Math.Abs(supplyDuty_Lps - extractDuty_Lps) > tolerance_Lps)
            {
                //The dwelling did not balance even after the derived allocation - it was already out of
                //balance before this call. Reported, not refused: this call did not cause it, the change it
                //made is correct in itself, and Modify.PreparePartOIteration refuses the model that reaches
                //a simulation.
                result.Warnings.Add(string.Format(
                    "Ventilation system '{0}' now designs {1:0.###} l/s of supply against {2:0.###} l/s of extract. The targeted change was balanced, so the dwelling was already out of balance before it - the preparation will refuse to simulate this model until that is resolved.",
                    ventilationSystem.FullName,
                    supplyDuty_Lps,
                    extractDuty_Lps));
            }

            result.Notes.Add(string.Format(
                "Ventilation system '{0}' now designs {1:0.###} l/s of supply and {2:0.###} l/s of extract, from one targeted change to space '{3}' and {4} derived balancing change(s). Every Approved Document F requirement is unchanged, and the transfer network and the unit's design duty are recalculated by re-preparing the iteration.",
                ventilationSystem.FullName,
                supplyDuty_Lps,
                extractDuty_Lps,
                space_Target.Name,
                result.DerivedAdjustments.Count));

            return result;
        }

        /// <summary>
        /// The one ventilation system the targeted room's terminals of this direction belong to.
        /// <para>
        /// Zero refuses - there is nothing to set and nothing to balance against. More than one refuses as
        /// ambiguous, because which dwelling is being rebalanced would be a guess, and
        /// <c>Modify.AddPartOBaseMVHRSystem</c> refuses on exactly the same condition.
        /// </para>
        /// </summary>
        private static VentilationSystem VentilationSystem(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification, DwellingDesignAirFlowChange result)
        {
            List<VentilationTerminal> ventilationTerminals = Query.VentilationTerminals(adjacencyCluster.VentilationTerminals(space), flowClassification) ?? [];

            if (ventilationTerminals.Count == 0)
            {
                result.Refusals.Add(string.Format(
                    "Space '{0}' has no design {1} terminal, so there is no design airflow to set. Realize the Approved Document F requirements first, or add a terminal deliberately - creating one here would invent a duty the assessment did not size.",
                    space.Name,
                    Core.Query.Description(flowClassification)));

                return null;
            }

            List<VentilationSystem> ventilationSystems = [];

            foreach (VentilationTerminal ventilationTerminal in ventilationTerminals)
            {
                foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetRelatedObjects<VentilationSystem>(ventilationTerminal) ?? [])
                {
                    if (ventilationSystem is not null && ventilationSystems.Find(x => x.Guid == ventilationSystem.Guid) is null)
                    {
                        ventilationSystems.Add(ventilationSystem);
                    }
                }
            }

            if (ventilationSystems.Count == 0)
            {
                result.Refusals.Add(string.Format(
                    "The design {0} terminals of space '{1}' belong to no ventilation system, so there is no dwelling to rebalance the change over. Prepare the iteration first. Nothing was changed.",
                    Core.Query.Description(flowClassification),
                    space.Name));

                return null;
            }

            if (ventilationSystems.Count > 1)
            {
                result.Refusals.Add(string.Format(
                    "The design {0} terminals of space '{1}' belong to {2} ventilation systems ({3}), so which dwelling the change would be rebalanced over is ambiguous. Nothing was changed.",
                    Core.Query.Description(flowClassification),
                    space.Name,
                    ventilationSystems.Count,
                    string.Join(", ", ventilationSystems.ConvertAll(x => string.Format("'{0}'", x.FullName)))));

                return null;
            }

            return ventilationSystems[0];
        }

        /// <summary>
        /// Shares <paramref name="change_Lps"/> across the rooms on the other side of the system, and says
        /// whether it can be done without taking any of them below what Approved Document F requires.
        /// <para>
        /// <b>Applied to the change, never recomputed from scratch.</b> Re-deriving every room's share from
        /// its requirement would silently undo any imbalance a designer had deliberately authored between
        /// them, which is a design decision this has no business overwriting.
        /// </para>
        /// </summary>
        private static bool Allocate(
            List<Space> spaces,
            Dictionary<Guid, double> dictionary_Duty,
            Dictionary<Guid, double> dictionary_Requirement,
            double change_Lps,
            FlowClassification flowClassification,
            PartFExtractAllocationStrategy partFExtractAllocationStrategy,
            AdjacencyCluster adjacencyCluster,
            double tolerance_Lps,
            out Dictionary<Guid, double> dictionary_Planned,
            out string note,
            out string refusal)
        {
            dictionary_Planned = [];
            note = null;
            refusal = null;

            //Cooking priority, exactly as PartFCalculator.AllocateContinuousExtract applies it to the
            //surplus above the Table 1.2 minimums: extract beyond what the Approved Document requires
            //belongs at the cooking function, the dwelling's largest single source of moisture and cooking
            //pollutants. It applies only to extract, and only where the dwelling actually has a local
            //kitchen extract terminal to put the air in.
            List<Space> spaces_Target = [];
            string basis;

            if (flowClassification == FlowClassification.Extract && partFExtractAllocationStrategy == PartFExtractAllocationStrategy.MinimumFirstCookingPriority)
            {
                foreach (Space space in spaces)
                {
                    if (IsLocalKitchenExtract(adjacencyCluster, space))
                    {
                        spaces_Target.Add(space);
                    }
                }
            }

            if (spaces_Target.Count != 0)
            {
                basis = "the minimum-first, cooking-priority strategy, which puts extract above the Approved Document F requirement at the cooking function";
            }
            else
            {
                //Either the strategy is volume weighted, or the side being balanced is supply, or the
                //dwelling has no local kitchen extract. Sharing in proportion to what the rooms already
                //carry is the neutral rule: it is independent of the order the rooms are listed in, and it
                //preserves whatever proportion the design already had between them.
                spaces_Target = spaces;
                basis = "sharing in proportion to the design airflow those rooms already carry";
            }

            double weight_Total = 0;
            foreach (Space space in spaces_Target)
            {
                weight_Total += dictionary_Duty[space.Guid];
            }

            foreach (Space space in spaces_Target)
            {
                double share_Lps = weight_Total > tolerance_Lps
                    ? change_Lps * (dictionary_Duty[space.Guid] / weight_Total)
                    : change_Lps / spaces_Target.Count;

                dictionary_Planned[space.Guid] = dictionary_Duty[space.Guid] + share_Lps;
            }

            //Every floor checked before anything is written. Only a reduction can breach one, and when it
            //does the dwelling simply cannot be balanced at the requested figure - which is an answer, and
            //a better one than a model that is quietly non-compliant.
            List<string> breaches = [];

            foreach (Space space in spaces_Target)
            {
                double requirement_Lps = dictionary_Requirement[space.Guid];

                if (dictionary_Planned[space.Guid] + tolerance_Lps < requirement_Lps)
                {
                    breaches.Add(string.Format("'{0}' would fall to {1:0.###} l/s against the {2:0.###} l/s Approved Document F requires of it", space.Name, dictionary_Planned[space.Guid], requirement_Lps));
                }
            }

            if (breaches.Count != 0)
            {
                refusal = string.Format(
                    "Balancing the change would need {0:0.###} l/s less {1} than the dwelling can give up: {2}. Approved Document F requirements are not reduced to make a design balance, so nothing was changed.",
                    System.Math.Abs(change_Lps),
                    Core.Query.Description(flowClassification),
                    string.Join("; ", breaches));

                return false;
            }

            List<string> names = spaces_Target.ConvertAll(x => x.Name);
            names.Sort(StringComparer.Ordinal);

            note = string.Format(
                "The {0:0.###} l/s of {1} needed to keep the dwelling balanced was allocated across {2} by {3}. This is a derived consequence of the targeted change, not a room selected for optimisation.",
                System.Math.Abs(change_Lps),
                Core.Query.Description(flowClassification),
                string.Join(", ", names.ConvertAll(x => string.Format("'{0}'", x))),
                basis);

            return true;
        }

        /// <summary>
        /// Whether a room's extract is local to the cooking function, read off the Approved Document F
        /// lineage its design terminals carry rather than guessed from the room's name or use.
        /// </summary>
        private static bool IsLocalKitchenExtract(AdjacencyCluster adjacencyCluster, Space space)
        {
            foreach (VentilationTerminal ventilationTerminal in Query.VentilationTerminals(adjacencyCluster.VentilationTerminals(space), FlowClassification.Extract) ?? [])
            {
                if (ventilationTerminal?.GetValue<PartFTerminalReference>(VentilationTerminalParameter.PartFTerminalReference)?.TerminalRole == PartFTerminalRole.LocalKitchenExtract)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
