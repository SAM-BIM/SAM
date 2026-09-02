// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <b>Approved Document O Iteration 2B - the selected-equipment capacity envelope.</b>
    /// <para>
    /// The ordinary optimisation stops. It stopped because the selected ventilation unit cannot carry
    /// another whole +5 l/s round, or because the iteration guard ran out - and rooms are still failing
    /// TM59. The engineer's next question is not "can it take another step?" but "what is the best this
    /// dwelling and the unit I have already bought could possibly do?".
    /// <c>Modify.EvaluateDesignAirFlowCapacityEnvelope</c> answers that one, and these tests pin what makes
    /// the answer trustworthy.
    /// </para>
    /// <para>
    /// <b>The ordinary round's all-or-nothing rule is not weakened, and this is not a way round it.</b> An
    /// envelope is a separate operation producing a separate, clearly diagnostic model - so the two tests
    /// that matter most here are the ones asserting that the source design is untouched and that a partial
    /// step never reaches <c>EvaluateTargetedDesignAirFlows</c> as though it were a step.
    /// </para>
    /// <para>
    /// <b>Not a second copy of the balancing or capacity arithmetic.</b> Every rule the envelope applies is
    /// applied by asking the ordinary round authority - the same allocator, the same Approved Document F
    /// floors, the same <c>Query.AirHandlingUnitDesignDuty</c>. What is tested here is what the envelope
    /// adds: one coherent scale factor per equipment group, solved against that group's whole duty, bounded
    /// by feasibility rather than by one step, and every "no" stated as its own outcome.
    /// </para>
    /// </summary>
    public class PartOCapacityEnvelopeTests
    {
        private const double tolerance_Lps = 0.001;

        private const string name_Bedroom = "Bedroom 1";

        private const string name_Kitchen = "Kitchen 1";

        private const string name_Bathroom = "Bathroom 1";

        // ---- 1, 2 and 4. Less than one round's worth of headroom, shared proportionally, never exceeded ---

        /// <summary>
        /// <b>1, 2, 4.</b> The brief's own example. Two failing extract rooms would each be asked for
        /// +5 l/s - a whole round of 10 l/s - and only 7 l/s of the selected unit's capacity remains. The
        /// envelope scales the <b>whole vector</b> by 0.7, so each room keeps its share of what was asked
        /// for, and the balancing supply moves the matching +7 once.
        /// <code>
        /// selected  MVHR-37, 37/37 l/s        dwelling at 30/30, so 7 l/s left
        /// asked     Kitchen  extract 22 -> 27   (+5)
        ///           Bathroom extract  8 -> 13   (+5)     one whole step moves the unit 10 l/s
        /// envelope  x0.7  Kitchen  -> 25.5      (+3.5)
        ///                 Bathroom -> 11.5      (+3.5)
        ///           derived Bedroom supply 30 -> 37 (+7)
        ///           unit 37/37 - exactly the rating, and not a thousandth past it
        /// </code>
        /// <para>
        /// The ordinary round would - correctly - have refused this outright, and that refusal is not
        /// weakened: the partial figures below exist only inside a diagnostic model.
        /// </para>
        /// </summary>
        [Fact]
        public void HeadroomBelowOneWholeRound_ScalesTheVectorProportionallyAndStopsExactlyAtTheRating()
        {
            AdjacencyCluster adjacencyCluster = Fixture(37, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //The ordinary round refuses this, and that is the premise of the whole operation.
            Assert.False(Round(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors).IsAccepted);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);
            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.Scaled, designAirFlowCapacityEnvelope.Outcome);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(0.7, designAirFlowCapacityEnvelopeGroup.Scale, 6);
            Assert.Equal(10, designAirFlowCapacityEnvelopeGroup.Movement_PerStep_Lps, 6);

            //Proportional: both rooms asked for the same increment, so both got the same share of it.
            Assert.Equal(25.5, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Kitchen, FlowClassification.Extract), 6);
            Assert.Equal(11.5, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bathroom, FlowClassification.Extract), 6);

            //And the single derived balancing consequence is the combined +7, not two separate ones.
            Assert.Equal(37, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);

            //The ceiling is REACHED and not exceeded: the duty sits on the rating, with nothing borrowed
            //from the comparison tolerance.
            Assert.Equal(37, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(37, designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps, 6);

            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps, 6);
            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.ExtractHeadroom_Lps, 6);

            Assert.True(designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps <= designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor.MaximumSupplyFlowRate_Lps);
            Assert.True(designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps <= designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor.MaximumExtractFlowRate_Lps);
        }

        /// <summary>
        /// <b>2, again, with the two rooms asking for different amounts.</b> Equal increments cannot tell a
        /// proportional scaling apart from "give each room half the headroom", and those are different
        /// rules. Here the kitchen is asked for +8 and the bathroom for +2, against 5 l/s of headroom: a
        /// proportional envelope gives +4 and +1, and an equal share would give +2.5 each.
        /// </summary>
        [Fact]
        public void UnequalIncrements_KeepTheirProportionsRatherThanSharingTheHeadroomEqually()
        {
            AdjacencyCluster adjacencyCluster = Fixture(35, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 30),
                Target(adjacencyCluster, name_Bathroom, FlowClassification.Extract, 10)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal(0.5, Assert.Single(designAirFlowCapacityEnvelope.Groups).Scale, 6);

            Assert.Equal(26, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Kitchen, FlowClassification.Extract), 6);
            Assert.Equal(9, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bathroom, FlowClassification.Extract), 6);

            Assert.Equal(35, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);
        }

        // ---- 3. More headroom than one step, because the iteration guard stopped the run -----------------

        /// <summary>
        /// <b>3.</b> An ordinary optimisation that stopped on its iteration guard can leave substantial
        /// headroom behind. The envelope is bound by <b>selected-equipment feasibility, not by
        /// <c>scale &lt;= 1</c></b>, so it scales the current vector by seven steps' worth to reach the
        /// ceiling.
        /// <code>
        /// selected  MVHR-100, 100/100 l/s     dwelling at 30/30, so 70 l/s left
        /// asked     Kitchen +5, Bathroom +5   one whole step moves the unit 10 l/s
        /// envelope  x7   Kitchen 22 -> 57, Bathroom 8 -> 43, derived Bedroom 30 -> 100
        /// </code>
        /// </summary>
        [Fact]
        public void HeadroomAboveOneWholeRound_ScalesPastOneStepToReachTheCeiling()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(7, designAirFlowCapacityEnvelopeGroup.Scale, 6);
            Assert.True(designAirFlowCapacityEnvelopeGroup.Scale > 1);

            Assert.Equal(57, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Kitchen, FlowClassification.Extract), 6);
            Assert.Equal(43, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bathroom, FlowClassification.Extract), 6);
            Assert.Equal(100, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, name_Bedroom, FlowClassification.Supply), 6);

            Assert.Equal(100, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps, 6);
        }

        // ---- 4. The ceiling is never exceeded, whatever was asked for ------------------------------------

        /// <summary>
        /// <b>4.</b> A vector asking for airflows far past the unit's rating does not produce a design past
        /// the unit's rating - it produces the largest coherent design that fits. The scale is well below 1
        /// and the answer is still exactly on the ceiling.
        /// </summary>
        [Fact]
        public void AVectorFarBeyondTheSelectedUnit_NeverProducesADesignBeyondIt()
        {
            AdjacencyCluster adjacencyCluster = Fixture(35, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 522),
                Target(adjacencyCluster, name_Bathroom, FlowClassification.Extract, 508)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(0.005, designAirFlowCapacityEnvelopeGroup.Scale, 9);

            Assert.Equal(35, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(35, designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps, 6);

            //And the round the envelope is built from kept the selected unit, rather than refusing it.
            Assert.All(designAirFlowCapacityEnvelope.RoundCandidate.DwellingRounds, x => Assert.Equal(VentilationUnitSelectionOutcome.Kept, x.VentilationUnitSelectionOutcome));
        }

        // ---- 5. Shared equipment is judged on its whole duty ---------------------------------------------

        /// <summary>
        /// <b>5.</b> One unit serving two systems has ONE ceiling and therefore ONE scale factor, solved
        /// against the sum of everything it moves. Judged per dwelling instead, Flat A - designed at 10/10
        /// against a 40/40 unit - would look as though it had 30 l/s to itself and scale by six, and the
        /// combined design would sit at 65/65 on a 40 l/s unit.
        /// <code>
        /// MVHR-40 rated 40/40, serving Flat A at 10/10 and Flat B at 25/25 - unit duty 35/35, 5 l/s left
        /// asked   Bedroom A supply +5, Bedroom B supply +5    one step moves the UNIT 10 l/s
        /// envelope x0.5 - Bedroom A 12.5, Bedroom B 27.5, each flat's kitchen extract following it
        ///          unit 40/40, exactly the rating
        /// </code>
        /// </summary>
        [Fact]
        public void ASharedVentilationUnit_IsScaledAgainstItsWholeDutyRatherThanEitherDwellings()
        {
            AdjacencyCluster adjacencyCluster = SharedUnitFixture(40, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, "Bedroom A", FlowClassification.Supply, 15),
                Target(adjacencyCluster, "Bedroom B", FlowClassification.Supply, 30)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            //ONE group, for the one unit - not one per dwelling.
            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(2, designAirFlowCapacityEnvelopeGroup.VentilationSystems.Count);

            Assert.Equal(0.5, designAirFlowCapacityEnvelopeGroup.Scale, 6);
            Assert.Equal(10, designAirFlowCapacityEnvelopeGroup.Movement_PerStep_Lps, 6);

            Assert.Equal(12.5, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, "Bedroom A", FlowClassification.Supply), 6);
            Assert.Equal(27.5, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, "Bedroom B", FlowClassification.Supply), 6);

            //The UNIT's whole duty is what landed on the rating - which is the assertion a per-dwelling
            //scaling could not pass.
            Assert.Equal(40, designAirFlowCapacityEnvelopeGroup.SupplyDuty_After_Lps, 6);
            Assert.Equal(40, designAirFlowCapacityEnvelopeGroup.ExtractDuty_After_Lps, 6);
        }

        /// <summary>
        /// <b>5, the other way round.</b> Two dwellings on two separate units each reach their own ceiling,
        /// at their own factor - so the per-unit solve is not accidentally a global one either.
        /// </summary>
        [Fact]
        public void SeparateVentilationUnits_EachReachTheirOwnCeilingAtTheirOwnScale()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellingFixture(out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, "Kitchen 1", FlowClassification.Extract, 27),
                Target(adjacencyCluster, "Kitchen 2", FlowClassification.Extract, 27)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal(2, designAirFlowCapacityEnvelope.Groups.Count);

            //AHU 1 is rated 35 and AHU 2 is rated 100, both dwellings designed at 30/30 and each asked for
            //one +5 step - so 1 in 1 and 14 in 2.
            Assert.Equal("AHU 1", designAirFlowCapacityEnvelope.Groups[0].Name);
            Assert.Equal(1, designAirFlowCapacityEnvelope.Groups[0].Scale, 6);

            Assert.Equal("AHU 2", designAirFlowCapacityEnvelope.Groups[1].Name);
            Assert.Equal(14, designAirFlowCapacityEnvelope.Groups[1].Scale, 6);

            //One whole step in flat 1, fourteen of them in flat 2 - and each flat's own duty lands on its
            //own unit's rating.
            Assert.Equal(27, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, "Kitchen 1", FlowClassification.Extract), 6);
            Assert.Equal(92, Design(designAirFlowCapacityEnvelope.AdjacencyCluster, "Kitchen 2", FlowClassification.Extract), 6);

            Assert.Equal(35, designAirFlowCapacityEnvelope.Groups[0].SupplyDuty_After_Lps, 6);
            Assert.Equal(100, designAirFlowCapacityEnvelope.Groups[1].SupplyDuty_After_Lps, 6);
        }

        // ---- 6. Independent of the order the targets arrived in ------------------------------------------

        /// <summary>
        /// <b>6.</b> The same targets in opposite orders produce the same envelope - the same scale, the
        /// same design to the last decimal, and the same report. An envelope that allocated remaining
        /// capacity to whichever room came out of the assessment first would fail this, and would be
        /// unusable as evidence.
        /// </summary>
        [Fact]
        public void TheEnvelope_IsIndependentOfTheOrderItsTargetsArrivedIn()
        {
            AdjacencyCluster adjacencyCluster = TwoDwellingFixture(out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope_Forward = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, "Kitchen 1", FlowClassification.Extract, 27),
                Target(adjacencyCluster, "Bathroom 1", FlowClassification.Extract, 13),
                Target(adjacencyCluster, "Kitchen 2", FlowClassification.Extract, 27)], ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope_Reversed = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, "Kitchen 2", FlowClassification.Extract, 27),
                Target(adjacencyCluster, "Bathroom 1", FlowClassification.Extract, 13),
                Target(adjacencyCluster, "Kitchen 1", FlowClassification.Extract, 27)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope_Forward.IsScaled);
            Assert.True(designAirFlowCapacityEnvelope_Reversed.IsScaled);

            Assert.Equal(Designs(designAirFlowCapacityEnvelope_Forward.AdjacencyCluster), Designs(designAirFlowCapacityEnvelope_Reversed.AdjacencyCluster));

            //And the reports agree too, not just the models.
            Assert.Equal(
                designAirFlowCapacityEnvelope_Forward.Groups.ConvertAll(x => x.ToString()),
                designAirFlowCapacityEnvelope_Reversed.Groups.ConvertAll(x => x.ToString()));

            Assert.Equal(
                designAirFlowCapacityEnvelope_Forward.TargetedAdjustments.ConvertAll(x => x.ToString()),
                designAirFlowCapacityEnvelope_Reversed.TargetedAdjustments.ConvertAll(x => x.ToString()));

            Assert.Equal(
                designAirFlowCapacityEnvelope_Forward.DerivedAdjustments.ConvertAll(x => x.ToString()),
                designAirFlowCapacityEnvelope_Reversed.DerivedAdjustments.ConvertAll(x => x.ToString()));
        }

        // ---- 7 and 12. Targeted and derived stay apart, and only the named rooms are targets -------------

        /// <summary>
        /// <b>7, 12.</b> The envelope's deliberate targets are exactly the rooms it was given, scaled; the
        /// rooms that move to keep the dwelling balanced are reported separately and are never promoted to
        /// targets. A room nobody named cannot become a deliberate figure by being on the balancing side of
        /// a diagnostic.
        /// </summary>
        [Fact]
        public void TargetedAndDerivedChanges_StayApartAndOnlyTheNamedRoomsAreTargets()
        {
            AdjacencyCluster adjacencyCluster = Fixture(37, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            List<string> names_Targeted = designAirFlowCapacityEnvelope.TargetedAdjustments.ConvertAll(x => x.SpaceName);

            names_Targeted.Sort(StringComparer.Ordinal);

            Assert.Equal([name_Bathroom, name_Kitchen], names_Targeted);
            Assert.All(designAirFlowCapacityEnvelope.TargetedAdjustments, x => Assert.False(x.IsDerived));

            DesignAirFlowAdjustment designAirFlowAdjustment = Assert.Single(designAirFlowCapacityEnvelope.DerivedAdjustments);

            Assert.Equal(name_Bedroom, designAirFlowAdjustment.SpaceName);
            Assert.True(designAirFlowAdjustment.IsDerived);
            Assert.Equal(37, designAirFlowAdjustment.After_Lps, 6);
        }

        /// <summary>
        /// <b>12, and the subtle half of it.</b> Under cooking priority a derived extract change goes to the
        /// local kitchen extract. An envelope targeting only the bedroom's supply must therefore still
        /// report the kitchen as <i>derived</i> - and must not scale the kitchen's own figure, which nobody
        /// chose, as though it were part of the request.
        /// </summary>
        [Fact]
        public void ARoomThatOnlyAbsorbsTheBalancingChange_IsNeverScaledAsADeliberateTarget()
        {
            AdjacencyCluster adjacencyCluster = Fixture(40, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Bedroom, FlowClassification.Supply, 35)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            //10 l/s of headroom over a 5 l/s step: two steps' worth.
            Assert.Equal(2, Assert.Single(designAirFlowCapacityEnvelope.Groups).Scale, 6);

            DesignAirFlowAdjustment designAirFlowAdjustment = Assert.Single(designAirFlowCapacityEnvelope.TargetedAdjustments);

            Assert.Equal(name_Bedroom, designAirFlowAdjustment.SpaceName);
            Assert.Equal(40, designAirFlowAdjustment.After_Lps, 6);

            Assert.All(designAirFlowCapacityEnvelope.DerivedAdjustments, x => Assert.True(x.IsDerived));
            Assert.Contains(designAirFlowCapacityEnvelope.DerivedAdjustments, x => x.SpaceName == name_Kitchen);
        }

        // ---- 8, 9 and 10. Requirement, runtime airflow and product all untouched -------------------------

        /// <summary>
        /// <b>8.</b> Not one Approved Document F requirement moves. The envelope raises design airflow, and
        /// the requirement is the floor it stays above - read, never written.
        /// </summary>
        [Fact]
        public void TheEnvelope_LeavesEveryApprovedDocumentFRequirementUnchanged()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            Dictionary<string, double> requirements_Before = Requirements(adjacencyCluster);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal(requirements_Before, Requirements(designAirFlowCapacityEnvelope.AdjacencyCluster));
            Assert.Equal(requirements_Before, Requirements(adjacencyCluster));

            //And every design the envelope wrote is still above its own floor.
            Assert.All(designAirFlowCapacityEnvelope.TargetedAdjustments, x => Assert.True(double.IsNaN(x.Requirement_Lps) || x.After_Lps + tolerance_Lps >= x.Requirement_Lps));
            Assert.All(designAirFlowCapacityEnvelope.DerivedAdjustments, x => Assert.True(double.IsNaN(x.Requirement_Lps) || x.After_Lps + tolerance_Lps >= x.Requirement_Lps));
        }

        /// <summary>
        /// <b>9.</b> No operating, profile or runtime airflow is written - which is the Iteration 3 boundary.
        /// The envelope changes design airflow; rebuilding the transfer and mechanical network around it is
        /// re-preparing the iteration, and is somebody else's job.
        /// </summary>
        [Fact]
        public void TheEnvelope_WritesNoRuntimeAirflow()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            List<string> airMovements_Before = AirMovements(adjacencyCluster);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal(airMovements_Before, AirMovements(designAirFlowCapacityEnvelope.AdjacencyCluster));
        }

        /// <summary>
        /// <b>10.</b> The selected product is the ceiling the envelope explores <i>within</i>, and is never
        /// reselected to make the answer bigger - not even when the catalogue offered contains a unit that
        /// would carry far more. Buying equipment is a deliberate decision, and a diagnostic is not one.
        /// </summary>
        [Fact]
        public void TheEnvelope_NeverReselectsTheVentilationUnit()
        {
            AdjacencyCluster adjacencyCluster = Fixture(35, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //A far bigger product is on offer, and is not taken.
            Assert.Contains(ventilationUnitCapacityDescriptors, x => x.MaximumSupplyFlowRate_Lps > 100);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 200)], ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal("Selected", SelectedModel(adjacencyCluster, "AHU 1"));
            Assert.Equal("Selected", SelectedModel(designAirFlowCapacityEnvelope.AdjacencyCluster, "AHU 1"));

            Assert.Equal("Selected", Assert.Single(designAirFlowCapacityEnvelope.Groups).VentilationUnitReference.Model);

            //And no dwelling of the scaled round reports a reselection.
            Assert.All(designAirFlowCapacityEnvelope.RoundCandidate.DwellingRounds, x => Assert.NotEqual(VentilationUnitSelectionOutcome.Reselected, x.VentilationUnitSelectionOutcome));
        }

        // ---- 11. The design the envelope was calculated from is untouched --------------------------------

        /// <summary>
        /// <b>11.</b> The last accepted ordinary design is not replaced, altered or in any way reachable
        /// from the envelope. This is the whole safety of the operation: an envelope is a partial step the
        /// ordinary policy refuses, and if the source model could be reached by it, a later round would be
        /// computed from a design nobody accepted.
        /// </summary>
        [Fact]
        public void TheSourceDesign_IsNeverTouchedByTheEnvelope()
        {
            AdjacencyCluster adjacencyCluster = Fixture(37, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            Dictionary<string, double> designs_Before = Designs(adjacencyCluster);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.True(designAirFlowCapacityEnvelope.IsScaled, designAirFlowCapacityEnvelope.Reason);

            Assert.Equal(designs_Before, Designs(adjacencyCluster));

            //A different object, and one whose designs really are different - so the comparison above is
            //not passing because nothing happened at all.
            Assert.NotSame(adjacencyCluster, designAirFlowCapacityEnvelope.AdjacencyCluster);
            Assert.NotEqual(designs_Before, Designs(designAirFlowCapacityEnvelope.AdjacencyCluster));
        }

        /// <summary>
        /// <b>11, and the bisection.</b> A retreating solve evaluates the vector many times over. Every one
        /// of those evaluations reads the source model and none of them writes to it, so the search leaves
        /// nothing behind - and running the same envelope twice gives the same answer.
        /// </summary>
        [Fact]
        public void RepeatedEnvelopes_OverTheSameDesignAgreeAndLeaveItUnchanged()
        {
            AdjacencyCluster adjacencyCluster = Fixture(37, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            Dictionary<string, double> designs_Before = Designs(adjacencyCluster);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope_First = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);
            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope_Second = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.Equal(designs_Before, Designs(adjacencyCluster));

            Assert.Equal(Designs(designAirFlowCapacityEnvelope_First.AdjacencyCluster), Designs(designAirFlowCapacityEnvelope_Second.AdjacencyCluster));
            Assert.Equal(designAirFlowCapacityEnvelope_First.Groups[0].Scale, designAirFlowCapacityEnvelope_Second.Groups[0].Scale, 12);
        }

        // ---- 13. Nothing to target ------------------------------------------------------------------------

        /// <summary>
        /// <b>13.</b> No eligible target means no envelope and no model to simulate - stated as
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.NoTargets"/> rather than as a refusal, because a
        /// design with nothing left to target has nothing to diagnose.
        /// </summary>
        [Fact]
        public void NoTargetAtAll_ProducesNoEnvelopeAndSaysSo()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [], ventilationUnitCapacityDescriptors);

            Assert.False(designAirFlowCapacityEnvelope.IsScaled);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);
            Assert.Null(designAirFlowCapacityEnvelope.RoundCandidate);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoTargets, designAirFlowCapacityEnvelope.Outcome);
            Assert.NotNull(designAirFlowCapacityEnvelope.Reason);

            //Not a refusal: nothing failed.
            Assert.Empty(designAirFlowCapacityEnvelope.Refusals);
        }

        /// <summary>
        /// <b>13, for a target the building has no lever for.</b> A failing room with no design terminal on
        /// the side asked for is dropped with its reason exactly as an ordinary round drops it - and where
        /// it is the only target, the envelope has nothing to scale and says which.
        /// </summary>
        [Fact]
        public void ATargetWithNoDesignTerminal_IsDroppedWithItsReasonAndLeavesNothingToScale()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //The bathroom has an extract terminal and no supply terminal at all.
            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Bathroom, FlowClassification.Supply, 20)], ventilationUnitCapacityDescriptors);

            Assert.False(designAirFlowCapacityEnvelope.IsScaled);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            //Dropped, not refused: the building has no lever for this request, which is a design finding
            //rather than a failure - and an engineer does something different about it than about an
            //exhausted unit.
            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoTargets, designAirFlowCapacityEnvelope.Outcome);
            Assert.Empty(designAirFlowCapacityEnvelope.Refusals);

            Assert.Contains(designAirFlowCapacityEnvelope.Notes, x => x.Contains(name_Bathroom));
        }

        // ---- 14. Zero headroom ----------------------------------------------------------------------------

        /// <summary>
        /// <b>14.</b> A design already sitting on its selected unit's rating has no envelope: the answer is
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom"/>, with the rating, the duty and the
        /// remaining headroom on the record - which is itself the diagnostic. "This design IS what that
        /// product can deliver" is a real engineering conclusion, and not a failure of the process.
        /// </summary>
        [Fact]
        public void NoRemainingHeadroom_ProducesAnExplicitDiagnosticOutcomeAndNoModel()
        {
            AdjacencyCluster adjacencyCluster = Fixture(30, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.False(designAirFlowCapacityEnvelope.IsScaled);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);
            Assert.Null(designAirFlowCapacityEnvelope.RoundCandidate);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelope.Outcome);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelopeGroup.Outcome);

            Assert.Equal(0, designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps, 6);
            Assert.Equal(30, designAirFlowCapacityEnvelopeGroup.SupplyDuty_Before_Lps, 6);

            Assert.NotNull(designAirFlowCapacityEnvelopeGroup.Reason);
            Assert.True(double.IsNaN(designAirFlowCapacityEnvelopeGroup.Scale));

            //Not a refusal: the equipment answering "nothing more" is an answer.
            Assert.Empty(designAirFlowCapacityEnvelope.Refusals);
        }

        /// <summary>
        /// <b>14, past the rating.</b> A design somehow already beyond its selected unit reports the same
        /// explicit "no headroom" answer with a negative headroom on the record, rather than a negative
        /// scale that would quietly design the dwelling <i>downwards</i> in the name of a diagnostic.
        /// </summary>
        [Fact]
        public void ADesignAlreadyPastItsSelectedUnit_ReportsNoHeadroomRatherThanScalingDownwards()
        {
            AdjacencyCluster adjacencyCluster = Fixture(25, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.Equal(-5, Assert.Single(designAirFlowCapacityEnvelope.Groups).SupplyHeadroom_Lps, 6);
        }

        /// <summary>
        /// <b>14, for a vector that does not raise the duty.</b> A target vector the dwelling absorbs
        /// without the unit's duty growing has no direction to be scaled in, and is reported as such
        /// instead of dividing a headroom by nothing.
        /// </summary>
        [Fact]
        public void AVectorThatDoesNotRaiseTheUnitDuty_HasNoDirectionToScaleIn()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //Exactly what the kitchen already designs: a coherent request that moves nothing.
            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 22)], ventilationUnitCapacityDescriptors);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.Equal(0, Assert.Single(designAirFlowCapacityEnvelope.Groups).Movement_PerStep_Lps, 6);
        }

        // ---- The capacity authority has to resolve, and an unknown ceiling is not an unlimited one -------

        /// <summary>
        /// No catalogue means no ceiling, and therefore no envelope. This is deliberately <b>not</b> the
        /// backward-compatible "equipment is no constraint" meaning a null catalogue has elsewhere in this
        /// area: there it frees a design to be explored, here it removes the only thing being explored
        /// towards.
        /// </summary>
        [Fact]
        public void NoCatalogue_IsAnUnresolvedCeilingRatherThanAnUnlimitedOne()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> _);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = adjacencyCluster.EvaluateDesignAirFlowCapacityEnvelope(Step(adjacencyCluster), PartFExtractAllocationStrategy.MinimumFirstCookingPriority, tolerance_Lps, null);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);
            Assert.NotNull(designAirFlowCapacityEnvelope.Reason);
        }

        /// <summary>
        /// A unit with nothing selected on it has no product to be taken to its ceiling, so its group is
        /// reported unresolved - and nothing is selected to create one.
        /// </summary>
        [Fact]
        public void AUnitWithNothingSelected_IsReportedUnresolvedAndNothingIsSelectedForIt()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, false);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, Assert.Single(designAirFlowCapacityEnvelope.Groups).Outcome);

            Assert.Null(SelectedModel(adjacencyCluster, "AHU 1"));
        }

        /// <summary>
        /// A unit selected as a product the catalogue offered does not contain has an <b>unknown</b>
        /// capacity, which is never an unlimited one - so the group is unresolved rather than scaled
        /// towards infinity.
        /// </summary>
        [Fact]
        public void AUnitSelectedAsSomethingNotOffered_HasAnUnknownCeilingRatherThanNoCeiling()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Something Else", null), 500, 500, 0)]);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.Null(Assert.Single(designAirFlowCapacityEnvelope.Groups).VentilationUnitCapacityDescriptor);
        }

        /// <summary>
        /// A catalogue entry that <b>exists</b> but does not state a usable capacity - a negative or
        /// non-finite maximum - is an <b>unknown</b> ceiling, not an exhausted one.
        /// <para>
        /// This is the sharper half of "an unknown capacity is never an unlimited one": left to the
        /// arithmetic, a NaN maximum gives a NaN headroom and a negative one gives a negative headroom, and
        /// both would fall into the no-headroom branch - reporting a malformed catalogue as a perfectly good
        /// unit with nothing left to give, which is a far more convincing wrong answer than no answer.
        /// </para>
        /// </summary>
        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(-1)]
        public void ASelectedProductWithNoUsableCapacity_IsUnresolvedRatherThanExhausted(double maximum_Lps)
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //The SAME product identity the unit is selected as, offered with a capacity that is not one.
            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), [
                new VentilationUnitCapacityDescriptor(ventilationUnitCapacityDescriptors[0].VentilationUnitReference, maximum_Lps, maximum_Lps, 0)]);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved, designAirFlowCapacityEnvelopeGroup.Outcome);

            //NOT NoHeadroom, which is what the arithmetic alone would have said - and never a scale.
            Assert.NotEqual(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelopeGroup.Outcome);
            Assert.True(double.IsNaN(designAirFlowCapacityEnvelopeGroup.Scale));

            //The descriptor IS resolved - the entry exists - and is reported, so an engineer can see the
            //figure that is wrong rather than only that something is.
            Assert.NotNull(designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor);
            Assert.False(designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor.IsValid);

            Assert.Contains("not a usable capacity", designAirFlowCapacityEnvelopeGroup.Reason);
        }

        /// <summary>
        /// A product rated at <b>zero</b>, on the other hand, is a perfectly valid catalogue entry that
        /// simply cannot carry anything - <c>VentilationUnitCapacityDescriptor.IsUsable</c> says so in those
        /// words - so it is genuinely exhausted rather than unknown, and the honest answer is no headroom.
        /// <para>
        /// Pinned beside the invalid cases because the distinction is the whole point of the check above:
        /// "this unit cannot give any more" and "nobody can say what this unit can give" are different
        /// findings, and only one of them is a number.
        /// </para>
        /// </summary>
        [Fact]
        public void ASelectedProductRatedAtZero_IsExhaustedRatherThanUnresolved()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), [
                new VentilationUnitCapacityDescriptor(ventilationUnitCapacityDescriptors[0].VentilationUnitReference, 0, 0, 0)]);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.NoHeadroom, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            DesignAirFlowCapacityEnvelopeGroup designAirFlowCapacityEnvelopeGroup = Assert.Single(designAirFlowCapacityEnvelope.Groups);

            Assert.True(designAirFlowCapacityEnvelopeGroup.VentilationUnitCapacityDescriptor.IsValid);

            //Already 30 l/s past a rating of nothing, and reported as exactly that.
            Assert.Equal(-30, designAirFlowCapacityEnvelopeGroup.SupplyHeadroom_Lps, 6);
        }

        /// <summary>
        /// A target vector that is not a coherent design at its own full step is not scaled into one. A
        /// request that is not a design airflow at all refuses the envelope for exactly the reason it
        /// refuses an ordinary round: quietly enveloping the rest would answer a question about a different
        /// vector.
        /// </summary>
        [Fact]
        public void AVectorThatIsNotADesignAirflow_RefusesTheEnvelopeRatherThanBeingPartlyScaled()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 27),
                Target(adjacencyCluster, name_Bathroom, FlowClassification.Extract, double.NaN)], ventilationUnitCapacityDescriptors);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.Refused, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.NotEmpty(designAirFlowCapacityEnvelope.Refusals);
        }

        /// <summary>
        /// A dwelling that is not a valid balanced, Approved Document F compliant design is not enveloped -
        /// it is refused, exactly as an ordinary round refuses it. An envelope adds airflow to a design; it
        /// does not repair one, because repairing a room nobody targeted would be an unrequested engineering
        /// decision made inside a diagnostic.
        /// </summary>
        [Fact]
        public void AnUnbalancedDwelling_IsRefusedRatherThanRepairedByAnEnvelope()
        {
            AdjacencyCluster adjacencyCluster = Fixture(100, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors);

            //Knocked out of balance directly - replaced by a same-guid terminal designed at 50 l/s against
            //the dwelling's 30 l/s of extract - so the precondition being tested is the one under test.
            VentilationTerminal ventilationTerminal = (adjacencyCluster.GetObjects<VentilationTerminal>() ?? []).Find(x => x?.Name == name_Bedroom + " terminal");

            Assert.NotNull(ventilationTerminal);

            VentilationTerminal ventilationTerminal_Unbalanced = new(ventilationTerminal.Guid, ventilationTerminal.Name, FlowClassification.Supply, 50);

            ventilationTerminal_Unbalanced.SetValue(VentilationTerminalParameter.PartFTerminalReference, ventilationTerminal.GetValue<PartFTerminalReference>(VentilationTerminalParameter.PartFTerminalReference));

            adjacencyCluster.AddObject(ventilationTerminal_Unbalanced);

            DesignAirFlowCapacityEnvelope designAirFlowCapacityEnvelope = Envelope(adjacencyCluster, Step(adjacencyCluster), ventilationUnitCapacityDescriptors);

            Assert.Equal(DesignAirFlowCapacityEnvelopeOutcome.Refused, designAirFlowCapacityEnvelope.Outcome);
            Assert.Null(designAirFlowCapacityEnvelope.AdjacencyCluster);

            Assert.NotEmpty(designAirFlowCapacityEnvelope.Refusals);
        }

        // ---- Fixture ---------------------------------------------------------------------------------------

        /// <summary>
        /// One flat, its own ventilation system and its own air handling unit, balanced at 30/30 l/s and
        /// meeting its Approved Document F requirements with headroom - with the unit selected as a product
        /// rated at whatever this test needs, which is what makes the remaining headroom the variable under
        /// test.
        /// <code>
        /// Bedroom 1  supply   requirement 13   design 30
        /// Kitchen 1  extract  requirement 13   design 22   (local kitchen extract)
        /// Bathroom 1 extract  requirement  8   design  8
        /// </code>
        /// </summary>
        private static AdjacencyCluster Fixture(double maximum_Lps, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            return Fixture(maximum_Lps, out ventilationUnitCapacityDescriptors, true);
        }

        private static AdjacencyCluster Fixture(double maximum_Lps, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors, bool select)
        {
            ventilationUnitCapacityDescriptors = Descriptors(maximum_Lps);

            AdjacencyCluster result = new();

            Dwelling(result, ventilationUnitCapacityDescriptors[0], "AHU 1", 1, select);

            return result;
        }

        /// <summary>
        /// Two flats on two <b>separate</b> units, rated 35/35 and 100/100 - so each one's own ceiling is a
        /// different distance away and a single global scale factor could not satisfy both.
        /// </summary>
        private static AdjacencyCluster TwoDwellingFixture(out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            ventilationUnitCapacityDescriptors =
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Selected", null), 35, 35, 0),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Selected Large", null), 100, 100, 1),
            ];

            AdjacencyCluster result = new();

            Dwelling(result, ventilationUnitCapacityDescriptors[0], "AHU 1", 1, true);
            Dwelling(result, ventilationUnitCapacityDescriptors[1], "AHU 2", 2, true);

            return result;
        }

        /// <summary>
        /// <b>One</b> air handling unit serving <b>two</b> ventilation systems - the general MEP arrangement
        /// the envelope has to stay correct for, and the one the Approved Document O one-unit-per-dwelling
        /// shape hides.
        /// <code>
        /// Flat A   Bedroom A supply 10 (requires 5)   Kitchen A extract 10 (requires 5)
        /// Flat B   Bedroom B supply 25 (requires 5)   Kitchen B extract 25 (requires 5)
        ///                                             unit duty 35/35
        /// </code>
        /// </summary>
        private static AdjacencyCluster SharedUnitFixture(double maximum_Lps, out List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            ventilationUnitCapacityDescriptors = Descriptors(maximum_Lps);

            AdjacencyCluster result = new();

            AirHandlingUnit airHandlingUnit = new("MVHR-S", 20, 20);

            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitCapacityDescriptors[0].VentilationUnitReference);

            result.AddObject(airHandlingUnit);

            Shared(result, airHandlingUnit, "A", 10);
            Shared(result, airHandlingUnit, "B", 25);

            return result;
        }

        /// <summary>
        /// The selected product at the rating a test needs, and a far larger one beside it that the
        /// envelope must never reach for.
        /// </summary>
        private static List<VentilationUnitCapacityDescriptor> Descriptors(double maximum_Lps)
        {
            return
            [
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Selected", null), maximum_Lps, maximum_Lps, 0),
                new VentilationUnitCapacityDescriptor(new VentilationUnitReference("Test Fixture", "Never Selected", null), 500, 500, 1),
            ];
        }

        private static void Dwelling(AdjacencyCluster adjacencyCluster, VentilationUnitCapacityDescriptor ventilationUnitCapacityDescriptor, string name_AirHandlingUnit, int index, bool select)
        {
            AirHandlingUnit airHandlingUnit = new(name_AirHandlingUnit, 20, 20);

            if (select)
            {
                //Written directly rather than through Modify.SelectVentilationUnit, so the fixture states
                //which product is selected instead of depending on the selection rule these tests are not
                //about - and so it can deliberately not be the smallest capable one.
                airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, ventilationUnitCapacityDescriptor.VentilationUnitReference);
            }

            adjacencyCluster.AddObject(airHandlingUnit);

            VentilationSystem ventilationSystem = new(string.Format("Flat {0}", index), new VentilationSystemType("Fixture MVHR", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit.Name);

            adjacencyCluster.AddObject(ventilationSystem);

            Space space_Bedroom = Room(adjacencyCluster, string.Format("Bedroom {0}", index), PartFTerminalRole.Supply, 13);
            Space space_Kitchen = Room(adjacencyCluster, string.Format("Kitchen {0}", index), PartFTerminalRole.LocalKitchenExtract, 13);
            Space space_Bathroom = Room(adjacencyCluster, string.Format("Bathroom {0}", index), PartFTerminalRole.GeneralExtract, 8);

            Terminal(adjacencyCluster, ventilationSystem, space_Bedroom, FlowClassification.Supply, 30);
            Terminal(adjacencyCluster, ventilationSystem, space_Kitchen, FlowClassification.Extract, 22);
            Terminal(adjacencyCluster, ventilationSystem, space_Bathroom, FlowClassification.Extract, 8);

            adjacencyCluster.AddRelation(ventilationSystem, space_Bedroom);
            adjacencyCluster.AddRelation(ventilationSystem, space_Kitchen);
            adjacencyCluster.AddRelation(ventilationSystem, space_Bathroom);
        }

        /// <summary>One dwelling of <see cref="SharedUnitFixture"/>, hung off the unit both of them share.</summary>
        private static void Shared(AdjacencyCluster adjacencyCluster, AirHandlingUnit airHandlingUnit, string suffix, double designFlowRate_Lps)
        {
            VentilationSystem ventilationSystem = new(string.Format("Flat {0}", suffix), new VentilationSystemType("Fixture MVHR", "Fixture"));

            //The SAME unit name for both, which is how Query.AirHandlingUnit resolves them onto one unit.
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, airHandlingUnit.Name);

            adjacencyCluster.AddObject(ventilationSystem);

            Space space_Bedroom = Room(adjacencyCluster, string.Format("Bedroom {0}", suffix), PartFTerminalRole.Supply, 5);
            Space space_Kitchen = Room(adjacencyCluster, string.Format("Kitchen {0}", suffix), PartFTerminalRole.LocalKitchenExtract, 5);

            Terminal(adjacencyCluster, ventilationSystem, space_Bedroom, FlowClassification.Supply, designFlowRate_Lps);
            Terminal(adjacencyCluster, ventilationSystem, space_Kitchen, FlowClassification.Extract, designFlowRate_Lps);

            adjacencyCluster.AddRelation(ventilationSystem, space_Bedroom);
            adjacencyCluster.AddRelation(ventilationSystem, space_Kitchen);
        }

        private static Space Room(AdjacencyCluster adjacencyCluster, string name, PartFTerminalRole partFTerminalRole, double requirement_Lps)
        {
            Space result = new(name);

            PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = new(name + " requirement", result.Guid, partFTerminalRole)
            {
                ContinuousDesignFlowRate_Lps = requirement_Lps,
            };

            PartFSpaceData partFSpaceData = new();
            partFSpaceData.Terminals.Add(partFVentilationTerminalRequirement);

            result.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            adjacencyCluster.AddObject(result);

            return result;
        }

        private static void Terminal(AdjacencyCluster adjacencyCluster, VentilationSystem ventilationSystem, Space space, FlowClassification flowClassification, double designFlowRate_Lps)
        {
            PartFVentilationTerminalRequirement partFVentilationTerminalRequirement = space.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData).Terminals[0];

            VentilationTerminal ventilationTerminal = new(space.Name + " terminal", flowClassification, designFlowRate_Lps);
            ventilationTerminal.SetValue(VentilationTerminalParameter.PartFTerminalReference, new PartFTerminalReference(partFVentilationTerminalRequirement));

            adjacencyCluster.AddObject(ventilationTerminal);
            adjacencyCluster.AddRelation(ventilationTerminal, space);
            adjacencyCluster.AddRelation(ventilationTerminal, ventilationSystem);
        }

        /// <summary>
        /// The deliberate target vector the ordinary +5 l/s policy would currently create for this
        /// fixture - the two failing extract rooms, each asked for one whole step.
        /// </summary>
        private static List<DesignAirFlowTarget> Step(AdjacencyCluster adjacencyCluster)
        {
            return
            [
                Target(adjacencyCluster, name_Kitchen, FlowClassification.Extract, 27),
                Target(adjacencyCluster, name_Bathroom, FlowClassification.Extract, 13),
            ];
        }

        private static DesignAirFlowCapacityEnvelope Envelope(AdjacencyCluster adjacencyCluster, List<DesignAirFlowTarget> designAirFlowTargets, List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            return adjacencyCluster.EvaluateDesignAirFlowCapacityEnvelope(designAirFlowTargets, PartFExtractAllocationStrategy.MinimumFirstCookingPriority, tolerance_Lps, ventilationUnitCapacityDescriptors);
        }

        private static DesignAirFlowRoundCandidate Round(AdjacencyCluster adjacencyCluster, List<DesignAirFlowTarget> designAirFlowTargets, List<VentilationUnitCapacityDescriptor> ventilationUnitCapacityDescriptors)
        {
            return adjacencyCluster.EvaluateTargetedDesignAirFlows(designAirFlowTargets, PartFExtractAllocationStrategy.MinimumFirstCookingPriority, tolerance_Lps, ventilationUnitCapacityDescriptors);
        }

        private static DesignAirFlowTarget Target(AdjacencyCluster adjacencyCluster, string name, FlowClassification flowClassification, double designFlowRate_Lps)
        {
            return new DesignAirFlowTarget(Space(adjacencyCluster, name), flowClassification, designFlowRate_Lps);
        }

        private static Space Space(AdjacencyCluster adjacencyCluster, string name)
        {
            Space result = (adjacencyCluster.GetSpaces() ?? []).Find(x => x?.Name == name);

            Assert.NotNull(result);

            return result;
        }

        /// <summary>Every room's design airflow on both sides, so a whole model can be compared before and after.</summary>
        private static Dictionary<string, double> Designs(AdjacencyCluster adjacencyCluster)
        {
            Dictionary<string, double> result = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                {
                    result[string.Format("{0} {1}", space.Name, flowClassification)] = System.Math.Round(Design(adjacencyCluster, space, flowClassification), 6);
                }
            }

            return result;
        }

        /// <summary>Every room's Approved Document F requirement on both sides - the values no envelope may move.</summary>
        private static Dictionary<string, double> Requirements(AdjacencyCluster adjacencyCluster)
        {
            Dictionary<string, double> result = [];

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                foreach (FlowClassification flowClassification in new[] { FlowClassification.Supply, FlowClassification.Extract })
                {
                    result[string.Format("{0} {1}", space.Name, flowClassification)] = adjacencyCluster.PartFRequiredFlowRate_Lps(space, flowClassification) ?? double.NaN;
                }
            }

            return result;
        }

        private static double Design(AdjacencyCluster adjacencyCluster, string name, FlowClassification flowClassification)
        {
            return Design(adjacencyCluster, Space(adjacencyCluster, name), flowClassification);
        }

        private static double Design(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification)
        {
            return adjacencyCluster.VentilationTerminals(space).VentilationTerminalDesignDuty_Lps(flowClassification) ?? 0;
        }

        private static string SelectedModel(AdjacencyCluster adjacencyCluster, string name)
        {
            AirHandlingUnit airHandlingUnit = (adjacencyCluster.GetObjects<AirHandlingUnit>() ?? []).Find(x => x?.Name == name);

            Assert.NotNull(airHandlingUnit);

            return airHandlingUnit.SelectedVentilationUnitReference()?.Model;
        }

        /// <summary>
        /// Every inter-zone air movement in the model, as text. A capacity envelope must not create, remove
        /// or re-rate one - that is the preparation's job, and doing it here would collapse design airflow
        /// into runtime airflow.
        /// </summary>
        private static List<string> AirMovements(AdjacencyCluster adjacencyCluster)
        {
            List<string> result = (adjacencyCluster.GetObjects<SpaceAirMovement>() ?? []).ConvertAll(x => string.Format("space|{0}|{1}", x.Name, x.AirFlow));

            result.AddRange((adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>() ?? []).ConvertAll(x => string.Format("ahu|{0}", x.Name)));

            result.Sort(StringComparer.Ordinal);

            return result;
        }
    }
}
