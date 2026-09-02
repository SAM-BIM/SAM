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
        /// Calculates the <b>selected-equipment capacity envelope</b> of a design: what the already-selected
        /// ventilation units could deliver if each were taken to its own design-capacity ceiling, as one
        /// coherent scaling of the deliberate target vector an ordinary optimisation round would currently
        /// have asked for - without touching the model it was handed.
        ///
        /// <para><b>The question this answers, and the one it does not</b></para>
        /// <para>
        /// An ordinary Approved Document O Iteration 2B round is all-or-nothing at a fixed step: it designs
        /// every target at exactly the figure asked for or it refuses, and
        /// <see cref="EvaluateTargetedDesignAirFlows"/> is deliberately never allowed to settle for part of
        /// a step. <b>That rule is not weakened here and this operation is not a way round it.</b> An
        /// envelope is calculated only <i>after</i> the ordinary optimisation has stopped, and it answers a
        /// different question - not "can this design take another whole step?" but "what is the best this
        /// dwelling and this already-bought unit could do?".
        /// </para>
        /// <para>
        /// The two answers are therefore kept in two places. <see cref="DesignAirFlowCapacityEnvelope.AdjacencyCluster"/>
        /// is a model to look at; it is never the accepted design and must never become the baseline of a
        /// later round, because a round computed from it would be computed from a design the optimiser's own
        /// policy refused.
        /// </para>
        ///
        /// <para><b>What is scaled, and why it is the increments</b></para>
        /// <para>
        /// Each target's <i>increment over the design its room already carries</i> is multiplied by one
        /// factor per equipment group. A kitchen and an ensuite each asked for +5 l/s, with 7 l/s of unit
        /// headroom left, become +3.5 and +3.5 - and the balancing consequence is then derived <b>once</b>
        /// from the scaled vector by the ordinary round authority, so the derived supply moves the matching
        /// +7. Giving the first room its whole 5 l/s and the second whatever remained would make the answer
        /// depend on which room was enumerated first, which is precisely the defect the deterministic round
        /// exists to remove. Scaling the <i>absolute</i> airflows instead would move rooms in proportion to
        /// the air they already carry, which nobody asked for.
        /// </para>
        ///
        /// <para><b>Not capped at one step</b></para>
        /// <para>
        /// Where the ordinary optimisation stopped on its iteration guard with capacity still to spare, the
        /// scale goes past 1 and several steps' worth of headroom is taken up. The bound is
        /// selected-equipment feasibility, never <c>scale &lt;= 1</c>: an envelope that stopped at one step
        /// would be answering the iteration guard rather than the equipment.
        /// </para>
        ///
        /// <para><b>The group is the unit, and no dwelling-to-unit ownership is assumed</b></para>
        /// <para>
        /// A capacity ceiling belongs to a product, a product is selected on an air handling unit, and
        /// <see cref="Query.AirHandlingUnitDesignDuty"/> judges a unit on the sum over <b>every</b> system
        /// it supplies. So the scale is solved per unit over that whole duty: two flats sharing one unit
        /// share one ceiling and one factor, because scaling them separately would have each spend headroom
        /// the other was also counting on and land the combined design past a rating neither thought it had
        /// reached. The Approved Document O one-unit-per-dwelling arrangement is the shape this reduces to,
        /// never an assumption it rests on.
        /// </para>
        ///
        /// <para><b>How the factor is found - analytically, then confirmed</b></para>
        /// <para>
        /// An ordinary round moves a balanced dwelling's supply and extract by the same amount <c>m</c>, and
        /// <c>m</c> is positively homogeneous in the scale - so a group whose one unscaled step would move
        /// its unit's duty by <c>M</c> reaches its ceiling at exactly
        /// <c>scale = headroom / M</c>, where <c>headroom</c> is the tighter of the two sides of the
        /// selected product's rating less the duty the design already carries. That is calculated, not
        /// searched for; the round is then evaluated once at it to confirm the resulting design is valid.
        /// Where that is refused for a reason which is <i>not</i> capacity - an Approved Document F floor a
        /// scaled reduction would breach, say - a bounded, monotonic, deterministic bisection retreats
        /// within the same interval and the retreat is recorded.
        /// </para>
        /// <para>
        /// <b>No search state accumulates anywhere.</b> Every attempt is evaluated against the caller's own
        /// model, which <see cref="EvaluateTargetedDesignAirFlows"/> never modifies, so the source model is
        /// read many times and mutated never - and the same inputs always produce the same answer.
        /// </para>
        ///
        /// <para><b>Every authority stays where it was</b></para>
        /// <para>
        /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow.</c>
        /// Design airflow moves and nothing else: the requirement is read as a floor by the round this
        /// delegates to and never written, the selected product is read as a ceiling and <b>never
        /// reselected</b>, and no operating, profile or runtime airflow is touched at all - so nothing here
        /// is the Iteration 3 behaviour of running a room at one airflow normally and another during hot
        /// hours.
        /// </para>
        ///
        /// <para><b>Every "no" is stated</b></para>
        /// <para>
        /// No eligible target, no useful headroom, an unresolvable ceiling, a vector that cannot be formed
        /// safely - each is a different fact about the design and each is recorded as its own
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome"/> with its own sentence, per group and overall.
        /// A diagnostic that silently produces nothing is worse than no diagnostic.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">
        /// The <b>last accepted ordinary optimisation design</b> to envelope from. <b>Never modified.</b>
        /// </param>
        /// <param name="designAirFlowTargets">
        /// The deliberate target vector the normal policy would currently create - the same targets, at the
        /// same figures, that the next ordinary round would have been given. Order is irrelevant. A target
        /// the building has no lever for is dropped with its reason exactly as an ordinary round drops it;
        /// a target that is not a design airflow at all refuses the envelope, for the same reason it refuses
        /// a round.
        /// </param>
        /// <param name="partFExtractAllocationStrategy">How the derived balancing change is shared out - the
        /// same strategy, passed to the same allocator, as the ordinary round.</param>
        /// <param name="tolerance_Lps">Flow rate comparison tolerance [l/s].</param>
        /// <param name="ventilationUnitCapacityDescriptors">
        /// The products each unit's selected capacity is read from. <b>Required</b>: unlike an ordinary
        /// round, where a null catalogue means equipment is simply not a constraint, an envelope <i>is</i>
        /// the equipment ceiling, so without one there is nothing to calculate and the answer is
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved"/>.
        /// </param>
        /// <returns>
        /// What each equipment group could reach and why, the scaled round where one was reached, and the
        /// diagnostic model to simulate. Never null.
        /// </returns>
        public static DesignAirFlowCapacityEnvelope EvaluateDesignAirFlowCapacityEnvelope(this AdjacencyCluster adjacencyCluster, IEnumerable<DesignAirFlowTarget> designAirFlowTargets, PartFExtractAllocationStrategy partFExtractAllocationStrategy = PartFExtractAllocationStrategy.MinimumFirstCookingPriority, double tolerance_Lps = 0.001, IEnumerable<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors = null)
        {
            DesignAirFlowCapacityEnvelope result = new();

            if (adjacencyCluster is null)
            {
                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.Refused, "No model was supplied, so no selected-equipment capacity envelope could be calculated.", true);
            }

            //FIRST, because every headroom, movement and scale comparison below is made against it.
            if (!Query.IsValidFlowRateTolerance(tolerance_Lps))
            {
                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.Refused, Query.FlowRateToleranceRefusal(tolerance_Lps), true);
            }

            List<DesignAirFlowTarget> designAirFlowTargets_Temp = [];
            foreach (DesignAirFlowTarget designAirFlowTarget in designAirFlowTargets ?? [])
            {
                if (designAirFlowTarget is not null)
                {
                    designAirFlowTargets_Temp.Add(designAirFlowTarget);
                }
            }

            if (designAirFlowTargets_Temp.Count == 0)
            {
                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.NoTargets, "No deliberate target was supplied, so there is no target vector to scale towards the selected equipment's capacity. A capacity envelope is a scaling of what the ordinary optimisation would have asked for next, and with nothing to ask for there is nothing to envelope.", false);
            }

            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors_Temp = [];
            foreach (VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor in ventilationUnitCapacityDescriptors ?? [])
            {
                if (ventilationUnitCapacityDescriptor is not null)
                {
                    ventilationUnitCapacityDescriptors_Temp.Add(ventilationUnitCapacityDescriptor);
                }
            }

            if (ventilationUnitCapacityDescriptors_Temp.Count == 0)
            {
                //NOT the backward-compatible "equipment is no constraint" meaning a null catalogue has
                //everywhere else in this area. There, no catalogue means a design may be explored freely;
                //here the catalogue IS the thing being explored towards, and an unknown ceiling is never an
                //unlimited one - scaling towards it would produce a design airflow with no authority behind
                //it at all.
                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, "No ventilation unit products were offered, so no selected unit's capacity can be read and there is no ceiling for a capacity envelope to scale towards. An unknown capacity is not an unlimited one.", false);
            }

            // ---- The unscaled vector, measured through the ordinary round authority ---------------------

            //Deliberately WITHOUT the catalogue, so equipment is no constraint on this one evaluation. Its
            //purpose is only to measure what one whole step of this vector WOULD do - which dwellings it
            //falls in, what each one's duty would move by, and which unit each resolves to - and a capacity
            //refusal here would hide exactly the measurement the envelope needs. The design it produces is
            //never adopted; only its arithmetic is read.
            DesignAirFlowRoundCandidate designAirFlowRoundCandidate_Step = adjacencyCluster.EvaluateTargetedDesignAirFlows(designAirFlowTargets_Temp, partFExtractAllocationStrategy, tolerance_Lps, null);

            //Carried whatever happens next: a room the building has no lever for is a fact about the design
            //and the diagnostic has to say so, exactly as an ordinary round does.
            foreach (DesignAirFlowTargetRefusal designAirFlowTargetRefusal in designAirFlowRoundCandidate_Step.TargetRefusals)
            {
                result.Notes.Add(designAirFlowTargetRefusal.ToString());
            }

            if (!designAirFlowRoundCandidate_Step.IsAccepted && designAirFlowRoundCandidate_Step.TargetRefusals.Count == designAirFlowTargets_Temp.Count)
            {
                //EVERY target was dropped, and a dropped target is the building being unable to answer a
                //coherent request rather than anything failing. So this is "nothing eligible left to
                //envelope" and not a refusal - the same distinction an ordinary round draws, and the one an
                //engineer acts differently on: a room with no design terminal on the side that failed needs
                //a design decision, not a bigger unit. Each dropped target's own reason is already on
                //Notes.
                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.NoTargets, string.Format(
                    "None of the {0} deliberate target(s) this capacity envelope was given can be taken - each one is reported with its own reason - so there is no target vector to scale towards any selected unit's capacity.",
                    designAirFlowTargets_Temp.Count), false);
            }

            if (!designAirFlowRoundCandidate_Step.IsAccepted)
            {
                //The vector cannot be formed safely at ANY scale, because what refused is not capacity -
                //this evaluation was offered no catalogue. Reported rather than worked around: an envelope
                //that quietly dropped the rooms that would not hold together would answer a question about
                //a different design.
                result.Refusals.AddRange(designAirFlowRoundCandidate_Step.Refusals);

                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.Refused, string.Format(
                    "The deliberate target vector the ordinary optimisation would next have asked for is not a valid design at its own full step, so there is no coherent vector for a capacity envelope to scale: {0} Nothing was changed, and no envelope was calculated.",
                    string.Join(" ", designAirFlowRoundCandidate_Step.Refusals)), true);
            }

            // ---- Grouped by the serving equipment, not by the dwelling ----------------------------------

            Dictionary<Guid, AirHandlingUnit> dictionary_AirHandlingUnit = [];
            Dictionary<Guid, List<DwellingDesignAirFlowRound>> dictionary_Group = [];

            //Guid.Empty is the one bucket that is NOT a unit: the dwellings whose systems resolve to no air
            //handling unit at all. They are kept and reported rather than dropped, because "this dwelling
            //has no equipment authority to envelope against" is a diagnostic finding.
            List<DwellingDesignAirFlowRound> dwellingDesignAirFlowRounds_Unresolved = [];

            foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in designAirFlowRoundCandidate_Step.DwellingRounds)
            {
                AirHandlingUnit airHandlingUnit = dwellingDesignAirFlowRound.AirHandlingUnit;

                if (airHandlingUnit is null)
                {
                    dwellingDesignAirFlowRounds_Unresolved.Add(dwellingDesignAirFlowRound);

                    continue;
                }

                dictionary_AirHandlingUnit[airHandlingUnit.Guid] = airHandlingUnit;

                if (!dictionary_Group.TryGetValue(airHandlingUnit.Guid, out List<DwellingDesignAirFlowRound> dwellingDesignAirFlowRounds))
                {
                    dwellingDesignAirFlowRounds = [];
                    dictionary_Group[airHandlingUnit.Guid] = dwellingDesignAirFlowRounds;
                }

                dwellingDesignAirFlowRounds.Add(dwellingDesignAirFlowRound);
            }

            //Units in NAME order, so what the envelope reports - and the order the scaled targets are then
            //assembled and summed in - does not depend on the order the targets arrived in.
            List<Guid> guids = [.. dictionary_Group.Keys];

            guids.Sort((x, y) =>
            {
                int comparison = string.CompareOrdinal(dictionary_AirHandlingUnit[x].Name, dictionary_AirHandlingUnit[y].Name);

                return comparison != 0 ? comparison : x.CompareTo(y);
            });

            // ---- One factor per group, solved against that group's WHOLE equipment duty ------------------

            List<DesignAirFlowTarget> designAirFlowTargets_Envelope = [];

            foreach (Guid guid in guids)
            {
                DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Solve(
                    adjacencyCluster,
                    dictionary_AirHandlingUnit[guid],
                    dictionary_Group[guid],
                    partFExtractAllocationStrategy,
                    tolerance_Lps,
                    ventilationUnitCapacityDescriptors_Temp,
                    out List<DesignAirFlowTarget> designAirFlowTargets_Group);

                result.Groups.Add(designAirFlowCapacityEnvelopeGroup);

                if (designAirFlowCapacityEnvelopeGroup.IsScaled)
                {
                    designAirFlowTargets_Envelope.AddRange(designAirFlowTargets_Group);
                }
            }

            foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in dwellingDesignAirFlowRounds_Unresolved)
            {
                DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = new(null)
                {
                    Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved,
                    Reason = string.Format(
                        "Ventilation system '{0}' resolves to no air handling unit, so there is no selected product whose capacity a design could be scaled towards. No unit was invented and no ownership was assumed from any name.",
                        dwellingDesignAirFlowRound.VentilationSystem?.FullName ?? "?"),
                };

                if (dwellingDesignAirFlowRound.VentilationSystem is not null)
                {
                    designAirFlowCapacityEnvelopeGroup.VentilationSystems.Add(dwellingDesignAirFlowRound.VentilationSystem);
                }

                result.Groups.Add(designAirFlowCapacityEnvelopeGroup);
            }

            foreach (DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup in result.Groups)
            {
                result.Notes.Add(designAirFlowCapacityEnvelopeGroup.Reason);
                result.Notes.AddRange(designAirFlowCapacityEnvelopeGroup.Notes);
            }

            if (designAirFlowTargets_Envelope.Count == 0)
            {
                //No group reached a ceiling worth simulating. Which of the reasons that is decides the
                //overall answer, and every one of them is already on a group.
                return Stop(result, Outcome(result), Reason(result), false);
            }

            // ---- The envelope design itself: ONE round over every scaled group together -------------------

            //With the catalogue this time, so the model handed back has been through the same equipment
            //verdict an ordinary round applies. The groups are independent - each writes only its own
            //systems' rooms and is judged only on its own unit's duty - so a combined round over factors
            //that were each feasible alone is feasible; the refusal below is a loud statement of a drift
            //rather than an expected branch.
            DesignAirFlowRoundCandidate designAirFlowRoundCandidate = adjacencyCluster.EvaluateTargetedDesignAirFlows(designAirFlowTargets_Envelope, partFExtractAllocationStrategy, tolerance_Lps, ventilationUnitCapacityDescriptors_Temp);

            if (!designAirFlowRoundCandidate.IsAccepted)
            {
                result.Refusals.AddRange(designAirFlowRoundCandidate.Refusals);

                return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.Refused, string.Format(
                    "Every equipment group's capacity envelope was feasible on its own and the combined envelope was then refused, which should not be possible - the groups write different rooms and are judged on different units: {0} No envelope model was produced.",
                    string.Join(" ", designAirFlowRoundCandidate.Refusals)), true);
            }

            result.RoundCandidate = designAirFlowRoundCandidate;
            result.AdjacencyCluster = designAirFlowRoundCandidate.AdjacencyCluster;

            result.Warnings.AddRange(designAirFlowRoundCandidate.Warnings);

            //Read off the model the envelope actually produced, so the duties and headrooms a report shows
            //are the combined design's rather than the per-group solve's intermediate ones.
            foreach (DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup in result.Groups_Scaled)
            {
                if (!designAirFlowRoundCandidate.AdjacencyCluster.AirHandlingUnitDesignDuty(designAirFlowCapacityEnvelopeGroup.AirHandlingUnit, out double supplyDuty_Lps, out double extractDuty_Lps))
                {
                    continue;
                }

                designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps = supplyDuty_Lps;
                designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps = extractDuty_Lps;

                VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor = designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor;

                if (ventilationUnitCapacityDescriptor is not null)
                {
                    designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps = ventilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps - supplyDuty_Lps;
                    designAirFlowCapacityEnvelopeGroup.ExtractHeadroom_Lps = ventilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps - extractDuty_Lps;
                }
            }

            result.Notes.AddRange(designAirFlowRoundCandidate.Notes);

            return Stop(result, DesignAirFlowCapacityEnvelopeOutcome.Scaled, Reason(result), false);
        }

        /// <summary>
        /// One equipment group's factor: the analytical ceiling, then the round's confirmation of it, then -
        /// only where that was refused for a reason which is not capacity - a bounded deterministic retreat.
        ///
        /// <para><b>Why the arithmetic is a division and not a search</b></para>
        /// <para>
        /// An ordinary round moves a balanced dwelling by a single amount <c>m</c> derived from its
        /// deliberate deltas, and every one of those deltas scales linearly - so <c>m(scale) = scale *
        /// m(1)</c> for a non-negative scale, and a unit's duty, being the sum over its systems, moves by
        /// <c>scale * M</c> where <c>M</c> is the sum of its groups' one-step movements. The first binding
        /// constraint is therefore at exactly <c>headroom / M</c> on whichever side of the selected
        /// product's rating is tighter. Nothing is guessed at and nothing is iterated towards.
        /// </para>
        /// <para>
        /// <b>The headroom is not stretched by the tolerance.</b> Comparisons accept a duty a tolerance past
        /// the rating, and an envelope deliberately does not spend that: it exists to say what the product
        /// can carry, and a diagnostic that quietly borrowed a thousandth of a litre per second past the
        /// rating would be answering with a design the product is not rated for.
        /// </para>
        ///
        /// <para><b>Why there is a retreat at all</b></para>
        /// <para>
        /// Capacity is the only constraint the division accounts for. Any other - an Approved Document F
        /// floor a <i>scaled reduction</i> would breach, a balancing side with nowhere left to go - belongs
        /// to the round, and the round is asked. The retreat is a bisection on the same interval, monotonic
        /// in feasibility by assumption and bounded by a fixed number of halvings, so it terminates and
        /// gives the same answer every time. It is a fallback and its use is recorded.
        /// </para>
        /// </summary>
        /// <param name="adjacencyCluster">The caller's model. Read repeatedly, modified never.</param>
        /// <param name="designAirFlowTargets">The group's scaled target vector, where one was reached.</param>
        private static DesignAirFlowCapacityEnvelopeGroup Solve(
            AdjacencyCluster adjacencyCluster,
            AirHandlingUnit airHandlingUnit,
            List<DwellingDesignAirFlowRound> dwellingDesignAirFlowRounds,
            PartFExtractAllocationStrategy partFExtractAllocationStrategy,
            double tolerance_Lps,
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors,
            out List<DesignAirFlowTarget> designAirFlowTargets)
        {
            designAirFlowTargets = [];

            DesignAirFlowCapacityEnvelopeGroup result = new(airHandlingUnit);

            //Sorted by system guid, the same key the round orders a dwelling's targets on - so the movement
            //below is summed, and the targets assembled, in an order that does not depend on the caller's.
            dwellingDesignAirFlowRounds.Sort((x, y) => (x.VentilationSystem?.Guid ?? Guid.Empty).CompareTo(y.VentilationSystem?.Guid ?? Guid.Empty));

            foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in dwellingDesignAirFlowRounds)
            {
                if (dwellingDesignAirFlowRound.VentilationSystem is not null)
                {
                    result.VentilationSystems.Add(dwellingDesignAirFlowRound.VentilationSystem);
                }
            }

            // ---- What this group's selected product is rated at, and what it already carries -------------

            result.VentilationUnitCapacityDescriptor = airHandlingUnit.SelectedVentilationUnitCapacityDescriptor(ventilationUnitCapacityDescriptors);

            if (airHandlingUnit.SelectedVentilationUnitReference() is null)
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved;
                result.Reason = string.Format(
                    "Air handling unit '{0}' has no ventilation unit product selected, so there is no capacity ceiling for a design to be scaled towards. Nothing was selected to create one - buying equipment is a deliberate decision and never a consequence of a diagnostic.",
                    airHandlingUnit.Name);

                return result;
            }

            if (result.VentilationUnitCapacityDescriptor is null)
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved;
                result.Reason = string.Format(
                    "Air handling unit '{0}' is selected as '{1}', which is not among the ventilation unit products offered, so its capacity is unknown and no ceiling can be scaled towards. An unknown capacity is not an unlimited one.",
                    airHandlingUnit.Name,
                    airHandlingUnit.SelectedVentilationUnitReference());

                return result;
            }

            //A catalogue entry that EXISTS but does not state a usable capacity - a negative or non-finite
            //maximum, which VentilationUnitCapacityDescriptor.IsValid already rejects. Asked through that
            //property rather than restated, so what "a usable capacity" means cannot drift from what
            //selection means by it.
            //
            //Caught HERE, before the arithmetic, because the arithmetic would otherwise turn it into the
            //wrong answer rather than no answer: a NaN maximum gives a NaN headroom and a negative one gives
            //a negative headroom, and both fall into the NoHeadroom branch below - reporting a malformed
            //ceiling as a perfectly good unit with nothing left to give. An unknown capacity is not an
            //unlimited one, and it is not an exhausted one either.
            if (!result.VentilationUnitCapacityDescriptor.IsValid)
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved;
                result.Reason = string.Format(
                    "Air handling unit '{0}' is selected as '{1}', and the catalogue entry offered for it states {2:0.###}/{3:0.###} l/s, which is not a usable capacity - so its ceiling is unknown and no design can be scaled towards it. An unknown capacity is neither an unlimited one nor an exhausted one.",
                    airHandlingUnit.Name,
                    result.VentilationUnitReference,
                    result.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps,
                    result.VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps);

                return result;
            }

            //The duty of the LAST ACCEPTED ORDINARY DESIGN, read off the caller's own model - because that
            //is the design the envelope starts from and the headroom being divided up is what that design
            //has left. Summed over every system the unit supplies, including any this vector never
            //targeted: air the unit is already committed to moving is not headroom.
            if (!adjacencyCluster.AirHandlingUnitDesignDuty(airHandlingUnit, out double supplyDuty_Before_Lps, out double extractDuty_Before_Lps))
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved;
                result.Reason = string.Format(
                    "Air handling unit '{0}' supplies no ventilation system carrying design terminals in the model being envelopped, so there is no design duty to measure its remaining capacity from.",
                    airHandlingUnit.Name);

                return result;
            }

            result.SupplyDuty_Before_Lps = supplyDuty_Before_Lps;
            result.ExtractDuty_Before_Lps = extractDuty_Before_Lps;

            // ---- What one whole unscaled step of this group's vector would move the unit by ---------------

            double movement_Supply_Lps = 0;
            double movement_Extract_Lps = 0;

            foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in dwellingDesignAirFlowRounds)
            {
                movement_Supply_Lps += dwellingDesignAirFlowRound.SupplyDuty_After_Lps - dwellingDesignAirFlowRound.SupplyDuty_Before_Lps;
                movement_Extract_Lps += dwellingDesignAirFlowRound.ExtractDuty_After_Lps - dwellingDesignAirFlowRound.ExtractDuty_Before_Lps;
            }

            if (double.IsNaN(movement_Supply_Lps) || double.IsNaN(movement_Extract_Lps))
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved;
                result.Reason = string.Format(
                    "The duty one whole step of the target vector would move air handling unit '{0}' by could not be derived, so there is no movement to divide its remaining capacity by.",
                    airHandlingUnit.Name);

                return result;
            }

            if (System.Math.Abs(movement_Supply_Lps - movement_Extract_Lps) > tolerance_Lps)
            {
                //Unreachable by design: an ordinary round moves both sides of a balanced dwelling by the
                //same amount, which is what makes one scale factor coherent for both. Said loudly rather
                //than picked between, because reaching it means the round's balancing and this measurement
                //have drifted apart and either choice would silently envelope the wrong side.
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.Refused;
                result.Reason = string.Format(
                    "One step of the target vector would move air handling unit '{0}' by {1:0.###} l/s of supply and {2:0.###} l/s of extract, which should not be possible for a balanced design, so no single coherent scale factor exists for it. No envelope was calculated.",
                    airHandlingUnit.Name,
                    movement_Supply_Lps,
                    movement_Extract_Lps);

                return result;
            }

            result.Movement_PerStep_Lps = movement_Supply_Lps;

            if (movement_Supply_Lps <= tolerance_Lps)
            {
                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom;
                result.Reason = string.Format(
                    "The target vector does not raise air handling unit '{0}' design duty at all - one whole step would move it {1:0.###} l/s - so there is no direction for a capacity envelope to scale in and nothing its selected product '{2}' could additionally deliver.",
                    airHandlingUnit.Name,
                    movement_Supply_Lps,
                    result.VentilationUnitReference);

                return result;
            }

            // ---- The analytical ceiling ------------------------------------------------------------------

            double headroom_Supply_Lps = result.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps - supplyDuty_Before_Lps;
            double headroom_Extract_Lps = result.VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps - extractDuty_Before_Lps;

            result.SupplyHeadroom_Lps = headroom_Supply_Lps;
            result.ExtractHeadroom_Lps = headroom_Extract_Lps;

            //The TIGHTER side binds, because both move by the same amount. Extract only where it is
            //strictly tighter, so an exactly equal pair reports supply - arbitrary, but fixed, and a
            //diagnostic whose binding side depended on floating point noise would be unreadable.
            double headroom_Lps = System.Math.Min(headroom_Supply_Lps, headroom_Extract_Lps);

            result.BindingFlowClassification = headroom_Extract_Lps < headroom_Supply_Lps ? FlowClassification.Extract : FlowClassification.Supply;

            if (double.IsNaN(headroom_Lps) || headroom_Lps <= tolerance_Lps)
            {
                result.BindingFlowClassification = FlowClassification.Undefined;

                result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom;
                result.Reason = string.Format(
                    "Air handling unit '{0}' is selected as '{1}', rated {2:0.###}/{3:0.###} l/s, and the last accepted design already has it moving {4:0.###}/{5:0.###} l/s - leaving {6:0.###}/{7:0.###} l/s. There is no useful headroom for a capacity envelope to scale into: this design IS what that selected product can deliver.",
                    airHandlingUnit.Name,
                    result.VentilationUnitReference,
                    result.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps,
                    result.VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps,
                    supplyDuty_Before_Lps,
                    extractDuty_Before_Lps,
                    headroom_Supply_Lps,
                    headroom_Extract_Lps);

                return result;
            }

            result.Scale_Capacity = headroom_Lps / movement_Supply_Lps;

            // ---- Confirmed by the round, and retreated within only where something else refused ----------

            double scale = result.Scale_Capacity;

            DesignAirFlowRoundCandidate designAirFlowRoundCandidate = Round(adjacencyCluster, dwellingDesignAirFlowRounds, scale, partFExtractAllocationStrategy, tolerance_Lps, ventilationUnitCapacityDescriptors, out List<DesignAirFlowTarget> designAirFlowTargets_Scaled);

            if (!designAirFlowRoundCandidate.IsAccepted)
            {
                List<string> refusals = [.. designAirFlowRoundCandidate.Refusals];

                //A MONOTONIC deterministic retreat, on the interval the analytical ceiling bounds. The
                //upper end is known refused and zero is the design we started from, so the largest
                //accepted point of a fixed bisection is the answer - the same answer every time, without
                //the source model being written to once.
                scale = double.NaN;

                double scale_Low = 0;
                double scale_High = result.Scale_Capacity;

                for (int i = 0; i < 32; i++)
                {
                    double scale_Middle = (scale_Low + scale_High) / 2;

                    //Below this the scaled vector no longer moves the design by a quantity the tolerance
                    //can tell from nothing, so there is no envelope left to find on this interval.
                    if (scale_Middle * movement_Supply_Lps <= tolerance_Lps)
                    {
                        break;
                    }

                    DesignAirFlowRoundCandidate designAirFlowRoundCandidate_Middle = Round(adjacencyCluster, dwellingDesignAirFlowRounds, scale_Middle, partFExtractAllocationStrategy, tolerance_Lps, ventilationUnitCapacityDescriptors, out List<DesignAirFlowTarget> designAirFlowTargets_Middle);

                    if (designAirFlowRoundCandidate_Middle.IsAccepted)
                    {
                        scale = scale_Middle;
                        scale_Low = scale_Middle;

                        designAirFlowRoundCandidate = designAirFlowRoundCandidate_Middle;
                        designAirFlowTargets_Scaled = designAirFlowTargets_Middle;

                        continue;
                    }

                    scale_High = scale_Middle;
                }

                if (double.IsNaN(scale))
                {
                    result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.Refused;
                    result.Reason = string.Format(
                        "Air handling unit '{0}' has {1:0.###} l/s of headroom on its selected product '{2}', and no scaling of the target vector into it produces a valid design: {3} No envelope was calculated for this equipment, and nothing was repaired to make one possible.",
                        airHandlingUnit.Name,
                        headroom_Lps,
                        result.VentilationUnitReference,
                        string.Join(" ", refusals));

                    return result;
                }

                result.Notes.Add(string.Format(
                    "Air handling unit '{0}' could take a x{1:0.####} scaling of the target vector on its selected product's capacity alone, and that design was refused for a reason which is not capacity, so the envelope retreated deterministically to x{2:0.####}: {3}",
                    airHandlingUnit.Name,
                    result.Scale_Capacity,
                    scale,
                    string.Join(" ", refusals)));
            }

            result.Scale = scale;

            designAirFlowTargets = designAirFlowTargets_Scaled;

            result.Outcome = DesignAirFlowCapacityEnvelopeOutcome.Scaled;
            result.Reason = string.Format(
                "Air handling unit '{0}' keeps its selected product '{1}', rated {2:0.###}/{3:0.###} l/s, and the deliberate target vector was scaled coherently by x{4:0.####} - from the {5:0.###} l/s of {6} headroom the last accepted design left and the {7:0.###} l/s one whole step would have moved the unit by. Every target keeps its share of the vector; the balancing consequence is derived once from the scaled vector by the same authority an ordinary round uses. This is a diagnostic capacity envelope and not an accepted optimisation round.",
                airHandlingUnit.Name,
                result.VentilationUnitReference,
                result.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps,
                result.VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps,
                result.Scale,
                headroom_Lps,
                Core.Query.Description(result.BindingFlowClassification),
                movement_Supply_Lps);

            return result;
        }

        /// <summary>
        /// One group's target vector at a given scale, evaluated by the ordinary round authority against the
        /// caller's own model.
        ///
        /// <para><b>The increments are scaled, not the airflows</b></para>
        /// <para>
        /// Each target is rebuilt as <c>before + scale * (planned - before)</c> from the measuring round's
        /// own adjustments, so what is scaled is exactly the <b>deliberate increment</b> each room was asked
        /// for - which is what preserves the proportions of the request, and is why two rooms each asked for
        /// +5 l/s with 7 l/s to share come out at +3.5 and +3.5. <c>planned</c> is taken from the
        /// adjustment rather than from the caller's figure because the round may already have raised a
        /// request that sat a fraction below an Approved Document F floor up to it, and the vector being
        /// scaled has to be the one the round would actually have designed.
        /// </para>
        /// <para>
        /// <b>Only the targeted adjustments become targets.</b> A room that moved to keep its dwelling
        /// balanced was nobody's decision, and promoting it to a target would both freeze it against the
        /// next allocation and claim it had been chosen. The derived consequence of the scaled vector is
        /// derived afresh, once, by the round.
        /// </para>
        /// </summary>
        private static DesignAirFlowRoundCandidate Round(
            AdjacencyCluster adjacencyCluster,
            List<DwellingDesignAirFlowRound> dwellingDesignAirFlowRounds,
            double scale,
            PartFExtractAllocationStrategy partFExtractAllocationStrategy,
            double tolerance_Lps,
            List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors,
            out List<DesignAirFlowTarget> designAirFlowTargets)
        {
            designAirFlowTargets = [];

            List<Space> spaces = adjacencyCluster.GetSpaces() ?? [];

            foreach (DwellingDesignAirFlowRound dwellingDesignAirFlowRound in dwellingDesignAirFlowRounds)
            {
                //TargetedAdjustments only - never Adjustments, and never DerivedAdjustments.
                foreach (DesignAirFlowAdjustment designAirFlowAdjustment in dwellingDesignAirFlowRound.TargetedAdjustments)
                {
                    Space space = spaces.Find(x => x is not null && x.Guid == designAirFlowAdjustment.SpaceGuid);
                    if (space is null)
                    {
                        continue;
                    }

                    designAirFlowTargets.Add(new DesignAirFlowTarget(
                        space,
                        designAirFlowAdjustment.FlowClassification,
                        designAirFlowAdjustment.Before_Lps + (scale * (designAirFlowAdjustment.After_Lps - designAirFlowAdjustment.Before_Lps))));
                }
            }

            return adjacencyCluster.EvaluateTargetedDesignAirFlows(designAirFlowTargets, partFExtractAllocationStrategy, tolerance_Lps, ventilationUnitCapacityDescriptors);
        }

        /// <summary>
        /// The overall outcome, decided from the groups rather than restated - because every reason an
        /// envelope has is already a fact about one piece of equipment, and the run-level answer has to be
        /// the same one a reader would reach from the group rows.
        /// <para>
        /// A scaled group wins, because the envelope did produce a design worth simulating. Otherwise a
        /// refusal outranks the two "nothing to say" answers, which outrank an unresolved ceiling only in
        /// that a stated limit is more informative than an unknown one.
        /// </para>
        /// </summary>
        private static DesignAirFlowCapacityEnvelopeOutcome Outcome(DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope)
        {
            foreach (DesignAirFlowCapacityEnvelopeOutcome designAirFlowCapacityEnvelopeOutcome in new[]
            {
                DesignAirFlowCapacityEnvelopeOutcome.Scaled,
                DesignAirFlowCapacityEnvelopeOutcome.Refused,
                DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom,
                DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved,
            })
            {
                if (designAirFlowCapacityEnvelope.Groups.Exists(x => x.Outcome == designAirFlowCapacityEnvelopeOutcome))
                {
                    return designAirFlowCapacityEnvelopeOutcome;
                }
            }

            return DesignAirFlowCapacityEnvelopeOutcome.NoTargets;
        }

        /// <summary>The overall outcome in one sentence, counted off the groups.</summary>
        private static string Reason(DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope)
        {
            int count_Scaled = designAirFlowCapacityEnvelope.Groups_Scaled.Count;

            if (count_Scaled != 0)
            {
                return string.Format(
                    "{0} of {1} serving equipment group(s) reached the ceiling of the product already selected for it, and the resulting design is a DIAGNOSTIC capacity envelope - what this dwelling and this equipment could deliver, never an accepted optimisation round and never the design a later round is computed from. Each group's own reason states what bound it.",
                    count_Scaled,
                    designAirFlowCapacityEnvelope.Groups.Count);
            }

            return string.Format(
                "No selected-equipment capacity envelope was calculated for any of the {0} serving equipment group(s) considered. Each one's own reason says why - which is the diagnostic.",
                designAirFlowCapacityEnvelope.Groups.Count);
        }

        /// <summary>
        /// Records the envelope's overall answer and hands it back - and, on anything but a scaled one,
        /// makes sure no model goes with it.
        /// </summary>
        /// <param name="refusal">Whether <paramref name="reason"/> is a refusal rather than a finding.
        /// "The selected unit has nothing left to give" is an answer; "the vector could not be formed" is
        /// not.</param>
        private static DesignAirFlowCapacityEnvelope Stop(DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope, DesignAirFlowCapacityEnvelopeOutcome designAirFlowCapacityEnvelopeOutcome, string reason, bool refusal)
        {
            designAirFlowCapacityEnvelope.Outcome = designAirFlowCapacityEnvelopeOutcome;
            designAirFlowCapacityEnvelope.Reason = reason;

            if (refusal && !string.IsNullOrWhiteSpace(reason))
            {
                designAirFlowCapacityEnvelope.Refusals.Insert(0, reason);
            }

            if (designAirFlowCapacityEnvelopeOutcome != DesignAirFlowCapacityEnvelopeOutcome.Scaled)
            {
                designAirFlowCapacityEnvelope.AdjacencyCluster = null;
                designAirFlowCapacityEnvelope.RoundCandidate = null;
            }

            return designAirFlowCapacityEnvelope;
        }
    }
}
