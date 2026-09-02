// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// The <b>selected-equipment capacity envelope</b> of a design: what the already-selected ventilation
    /// units could deliver if each were taken to its own design-capacity ceiling, worked out as one
    /// coherent scaling of the deliberate target vector an ordinary optimisation round would currently
    /// have asked for.
    ///
    /// <para><b>A diagnostic, and emphatically not another optimisation round</b></para>
    /// <para>
    /// An ordinary Approved Document O Iteration 2B round is all-or-nothing at a fixed step - see
    /// <see cref="Modify.EvaluateTargetedDesignAirFlows"/> - and that rule is not weakened here. An
    /// envelope is calculated <i>after</i> the ordinary optimisation has stopped, and it answers a
    /// different question: not "can this design take another whole step?" but "what is the best this
    /// dwelling and this already-bought unit could do?". Those are different questions and their answers
    /// are kept in different places. <see cref="AdjacencyCluster"/> is a model to <b>look at</b>; it is
    /// never the accepted design, never fed back into a later round, and a caller that adopts it as one has
    /// misread it. The clamp an ordinary round refuses is exactly what an envelope is <i>for</i>, which is
    /// why it is a separate operation with a separate result rather than a flag on the round.
    /// </para>
    ///
    /// <para><b>What "coherent" means, and why it is not room-by-room</b></para>
    /// <para>
    /// Two failing extract rooms in one flat would each be asked for +5 l/s, and 7 l/s of unit headroom
    /// remains. Giving the first room its whole 5 and the second the remaining 2 makes the answer depend on
    /// which room was enumerated first, which is the very defect the deterministic round exists to remove.
    /// So the whole target vector is scaled by one factor per equipment group - here 0.7, giving +3.5 and
    /// +3.5 - and the balancing consequence is derived <b>once</b> from the scaled vector by the ordinary
    /// round authority. The proportions the failing rooms were asked for survive; nothing is allocated by
    /// arrival order.
    /// </para>
    ///
    /// <para><b>The scale is not capped at one step</b></para>
    /// <para>
    /// Where the ordinary optimisation stopped on its iteration guard rather than on capacity, several
    /// steps' worth of headroom may remain, and the envelope scales past 1 to reach the ceiling. The bound
    /// is selected-equipment feasibility, never <c>scale &lt;= 1</c> - an envelope that stopped at one step
    /// would answer the iteration guard's question rather than the equipment's.
    /// </para>
    ///
    /// <para><b>Every authority stays where it was</b></para>
    /// <para>
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow.</c> The
    /// envelope moves design airflow and nothing else: the Approved Document F requirement is read as a
    /// floor by the round it delegates to and never written; the selected product is read as a ceiling and
    /// <b>never reselected</b>; no operating, profile or runtime airflow is touched, so nothing here is
    /// Iteration 3 behaviour. Capacity is judged by <see cref="Query.AirHandlingUnitDesignDuty"/>, which
    /// sums every system a unit supplies, so no dwelling-to-unit ownership is assumed.
    /// </para>
    ///
    /// <para><b>Nothing is searched by mutating the model</b></para>
    /// <para>
    /// The feasible scale is calculated analytically from the headroom and the movement one whole step
    /// would cause, then confirmed by evaluating the round once. Where that is refused for a reason which
    /// is not capacity, a bounded deterministic solve retreats within the same interval. Every attempt is
    /// evaluated against the <i>caller's own</i> model, which <see cref="Modify.EvaluateTargetedDesignAirFlows"/>
    /// never modifies - so no search state accumulates anywhere and the same input always gives the same
    /// answer.
    /// </para>
    /// </summary>
    public class DesignAirFlowCapacityEnvelope
    {
        /// <summary>
        /// The envelope model - the design the selected units' ceilings permit.
        /// <b>Null unless <see cref="IsScaled"/></b>.
        /// <para>
        /// <b>Diagnostic. Never the accepted design.</b> An ordinary optimisation's last accepted model is a
        /// separate thing that this does not replace, and feeding this back into a later round would make
        /// the next round's baseline a design the optimiser's own policy refused.
        /// </para>
        /// </summary>
        public AdjacencyCluster AdjacencyCluster { get; internal set; }

        /// <summary>
        /// The ordinary round that produced <see cref="AdjacencyCluster"/>, evaluated over the <b>scaled</b>
        /// target vector by <see cref="Modify.EvaluateTargetedDesignAirFlows"/> - so the targeted and
        /// derived adjustments, the duties and the per-dwelling equipment verdicts of the envelope are the
        /// same shape, from the same authority, as an ordinary round's. Null where no group scaled.
        /// </summary>
        public DesignAirFlowRoundCandidate RoundCandidate { get; internal set; }

        /// <summary>
        /// One entry per serving equipment group the envelope considered - <b>including</b> the groups it
        /// produced nothing for, each with the reason. Ordered by unit name then guid, so the report does
        /// not depend on the order the targets arrived in.
        /// </summary>
        public List<DesignAirFlowCapacityEnvelopeGroup> Groups { get; } = [];

        /// <summary>The groups that reached a scaled envelope design.</summary>
        public List<DesignAirFlowCapacityEnvelopeGroup> Groups_Scaled => Groups.FindAll(x => x.IsScaled);

        /// <summary>
        /// Every deliberate adjustment the envelope's scaled round made. <b>Not an accepted optimisation
        /// step</b> - these are the diagnostic's targets.
        /// </summary>
        public List<DesignAirFlowAdjustment> TargetedAdjustments => RoundCandidate?.TargetedAdjustments ?? [];

        /// <summary>
        /// Every balancing adjustment the scaled round derived. Kept apart from the targeted ones for the
        /// same reason an ordinary round keeps them apart: a report that merged the two would claim every
        /// room that moved was chosen.
        /// </summary>
        public List<DesignAirFlowAdjustment> DerivedAdjustments => RoundCandidate?.DerivedAdjustments ?? [];

        /// <summary>
        /// The overall outcome - the best any group reached, or the single reason none did. Never inferred
        /// from prose: a caller has to be able to tell "the units have nothing left" from "the envelope
        /// could not be worked out".
        /// </summary>
        public DesignAirFlowCapacityEnvelopeOutcome Outcome { get; internal set; } = DesignAirFlowCapacityEnvelopeOutcome.Undefined;

        /// <summary>Why the envelope came to what it did, in one sentence.</summary>
        public string Reason { get; internal set; }

        /// <summary>How each group's ceiling was reached, and what the scaled round then reported.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Advisories that do not stop the envelope.</summary>
        public List<string> Warnings { get; } = [];

        /// <summary>
        /// Why no envelope model was produced. Empty where one was - and empty on a
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom"/> or
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.NoTargets"/> answer too, because neither of those
        /// is a failure: they are the diagnostic reporting that the design is already at the limit of what
        /// this operation can say anything about.
        /// </summary>
        public List<string> Refusals { get; } = [];

        /// <summary>
        /// Whether the envelope produced a design worth simulating - and therefore whether
        /// <see cref="AdjacencyCluster"/> is there to simulate.
        /// </summary>
        public bool IsScaled => Outcome == DesignAirFlowCapacityEnvelopeOutcome.Scaled && AdjacencyCluster is not null;

        public override string ToString()
        {
            return string.Format(
                "{0} group(s), {1} scaled, {2} targeted, {3} derived ({4})",
                Groups.Count,
                Groups_Scaled.Count,
                TargetedAdjustments.Count,
                DerivedAdjustments.Count,
                Core.Query.Description(Outcome));
        }
    }
}
