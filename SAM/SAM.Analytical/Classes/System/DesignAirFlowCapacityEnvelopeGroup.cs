// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// One <b>serving equipment group</b>'s share of a selected-equipment capacity envelope: the air
    /// handling unit whose already-selected product is the ceiling, every ventilation system that unit
    /// supplies which the envelope targeted, how far the deliberate target vector could be scaled before
    /// that ceiling bound, and which side of the unit bound it.
    ///
    /// <para><b>Why the group is the unit and not the dwelling</b></para>
    /// <para>
    /// A capacity ceiling belongs to a product, and a product is selected on an air handling unit -
    /// which <see cref="Query.AirHandlingUnitDesignDuty"/> already judges on the sum over <i>every</i>
    /// system it supplies. Two flats sharing one unit therefore share one ceiling and one scale factor:
    /// scaling them independently would each spend headroom the other was also counting on, and the
    /// combined design would sit past a rating neither group thought it had reached. So the envelope is
    /// worked out per unit, and the Approved Document O one-unit-per-dwelling case is simply the shape this
    /// reduces to - never an assumption it depends on.
    /// </para>
    ///
    /// <para><b>The scale is a scale of the deliberate increments, not of the airflows</b></para>
    /// <para>
    /// <see cref="Scale"/> multiplies each target's <i>increment over the design it already carries</i>, so
    /// a kitchen and an ensuite each asked for +5 l/s against 7 l/s of remaining headroom become +3.5 and
    /// +3.5 rather than some room-by-room share of what happened to be left. Scaling the absolute airflows
    /// instead would move rooms in proportion to how much air they already carry, which is not what anybody
    /// asked for and would drag a room nobody targeted's figure into the answer.
    /// </para>
    ///
    /// <para><b>Diagnostic, and only that</b></para>
    /// <para>
    /// Nothing here is an accepted design. The product on <see cref="AirHandlingUnit"/> is read and never
    /// written, no Approved Document F requirement moves, and no operating or runtime airflow is touched -
    /// see <see cref="DesignAirFlowCapacityEnvelope"/>.
    /// </para>
    /// </summary>
    public class DesignAirFlowCapacityEnvelopeGroup
    {
        internal DesignAirFlowCapacityEnvelopeGroup(AirHandlingUnit airHandlingUnit)
        {
            AirHandlingUnit = airHandlingUnit;
        }

        /// <summary>
        /// The air handling unit whose <b>already-selected</b> product is this group's ceiling. Null only
        /// where the group exists to record that no unit resolved at all - see
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.CapacityUnresolved"/>.
        /// </summary>
        public AirHandlingUnit AirHandlingUnit { get; }

        /// <summary>The unit's name, so a report reads without resolving the object back through the model.</summary>
        public string Name => AirHandlingUnit?.Name;

        /// <summary>
        /// Every ventilation system this envelope targeted on this unit, in the order the envelope settled
        /// on - by system guid, so the report does not depend on the order the targets arrived in.
        /// </summary>
        public List<VentilationSystem> VentilationSystems { get; } = [];

        /// <summary>
        /// The product the unit is <b>currently</b> selected as. <b>Never changed by an envelope</b>: the
        /// whole question an envelope answers is what <i>this</i> product could deliver.
        /// </summary>
        public VentilationUnitReference VentilationUnitReference => AirHandlingUnit?.SelectedVentilationUnitReference();

        /// <summary>
        /// What that product can move, read from the catalogue the envelope was offered. Null where the
        /// capacity is not known - which is an unresolved ceiling, never an unlimited one.
        /// </summary>
        public VentilationUnitCapacityDescriptor VentilationUnitCapacityDescriptor { get; internal set; }

        /// <summary>The unit's design supply duty [l/s] before the envelope - the last ordinary accepted design's.</summary>
        public double SupplyDuty_Before_Lps { get; internal set; } = double.NaN;

        /// <summary>The unit's design extract duty [l/s] before the envelope.</summary>
        public double ExtractDuty_Before_Lps { get; internal set; } = double.NaN;

        /// <summary>The unit's design supply duty [l/s] the envelope produces. NaN where none was produced.</summary>
        public double SupplyDuty_After_Lps { get; internal set; } = double.NaN;

        /// <summary>The unit's design extract duty [l/s] the envelope produces.</summary>
        public double ExtractDuty_After_Lps { get; internal set; } = double.NaN;

        /// <summary>
        /// How much the unit's duty would move on each side per whole unscaled step of this group's target
        /// vector [l/s] - the quantity the headroom is divided by to get <see cref="Scale"/>.
        /// <para>
        /// The same on both sides by construction: a round moves a balanced dwelling's supply and extract
        /// together, so a coherent scaling of its targets moves the unit's two sides by the same amount and
        /// only the tighter of the two ratings can bind.
        /// </para>
        /// </summary>
        public double Movement_PerStep_Lps { get; internal set; } = double.NaN;

        /// <summary>
        /// The factor the group's deliberate <b>increments</b> were multiplied by. 1 is exactly the
        /// ordinary round; less than 1 is a partial step the ordinary optimiser would rightly have refused;
        /// more than 1 is several steps' worth of headroom the iteration guard stopped short of.
        /// <para>NaN where no envelope was produced for this group.</para>
        /// </summary>
        public double Scale { get; internal set; } = double.NaN;

        /// <summary>
        /// The largest scale the analytical capacity calculation says is feasible, before the round itself
        /// was asked to confirm it. <see cref="Scale"/> equals it on the ordinary path, and is smaller
        /// where the deterministic solve had to retreat from it for a reason that is not capacity - which
        /// is then on <see cref="Notes"/>.
        /// </summary>
        public double Scale_Capacity { get; internal set; } = double.NaN;

        /// <summary>
        /// What the selected product has left on the supply side at the envelope design - its maximum less
        /// the unit's design duty. At a scaled envelope this is 0 on the binding side, within tolerance.
        /// <para><b>Reported, never spent.</b></para>
        /// </summary>
        public double SupplyHeadroom_Lps { get; internal set; } = double.NaN;

        /// <summary>The same on the extract side. See <see cref="SupplyHeadroom_Lps"/>.</summary>
        public double ExtractHeadroom_Lps { get; internal set; } = double.NaN;

        /// <summary>
        /// Which side of the selected product's rating the scaling ran into.
        /// <see cref="FlowClassification.Undefined"/> where nothing bound - which on a
        /// <see cref="DesignAirFlowCapacityEnvelopeOutcome.Scaled"/> group should not happen and on any
        /// other is simply the absence of a scaling.
        /// </summary>
        public FlowClassification BindingFlowClassification { get; internal set; } = FlowClassification.Undefined;

        /// <summary>What this group's envelope came to, or why none was calculated for it.</summary>
        public DesignAirFlowCapacityEnvelopeOutcome Outcome { get; internal set; } = DesignAirFlowCapacityEnvelopeOutcome.Undefined;

        /// <summary>
        /// Why, in one sentence - stated for <b>every</b> outcome including the successful one, because the
        /// value an envelope has to an engineer is the sentence and not the number.
        /// </summary>
        public string Reason { get; internal set; }

        /// <summary>How the scale was arrived at, and anything the solve had to retreat from.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Whether this group produced a scaled envelope design.</summary>
        public bool IsScaled => Outcome == DesignAirFlowCapacityEnvelopeOutcome.Scaled;

        public override string ToString()
        {
            return string.Format(
                "{0}: x{1:0.####} -> {2:0.###}/{3:0.###} l/s ({4})",
                Name ?? "-",
                Scale,
                SupplyDuty_After_Lps,
                ExtractDuty_After_Lps,
                Core.Query.Description(Outcome));
        }
    }
}
