// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// What one run of
    /// <see cref="Modify.PreparePartOIteration(AnalyticalModel, PartOIteration, IEnumerable{Zone}, Dictionary{System.Guid, string})"/>
    /// produced - the model to simulate, the scenarios to attribute the results to, and every reason it
    /// gives for what it did.
    /// <para>
    /// <b>A refusal returns no model at all.</b> Where <see cref="Refusal"/> is set,
    /// <see cref="AnalyticalModel"/> is null and <see cref="OverheatingScenarios"/> is empty. That is the
    /// contract, not an implementation detail: a half-prepared model handed back beside a refusal is a
    /// model somebody simulates.
    /// </para>
    /// </summary>
    public class PartOIterationPreparation
    {
        /// <summary>The prepared copy to simulate, or null where nothing was prepared.</summary>
        public AnalyticalModel AnalyticalModel { get; internal set; }

        /// <summary>
        /// The one reason nothing was prepared, or null. Fatal - unlike <see cref="Refusals"/>, which name
        /// individual items that produced no result inside a run that otherwise succeeded.
        /// </summary>
        public string Refusal { get; internal set; }

        /// <summary>One scenario per assessed zone, or empty where none could be stated.</summary>
        public List<OverheatingScenario> OverheatingScenarios { get; } = [];

        /// <summary>
        /// How the model's authored opening behaviour compares with the stage's <c>Openings Restricted</c>
        /// assumption. Advisory: it is reported and never acted on, and it never gates anything.
        /// </summary>
        public PartOOpeningCompatibility OpeningCompatibility { get; internal set; }

        /// <summary>
        /// The Approved Document O ventilation route the assessment settled on, or
        /// <see cref="PartOVentilationMode.Undefined"/> where it settled on none - in which case
        /// <see cref="Refusal"/> is set and nothing was prepared.
        /// <para>
        /// <b>This is the fact every other decision on this result follows from</b>, and it is on the
        /// result so a caller can see what was believed rather than inferring it from the airflow answer.
        /// It is what the assessment STATED, never what any system object on the model says.
        /// </para>
        /// </summary>
        public PartOVentilationMode VentilationMode { get; internal set; }

        /// <summary>
        /// Whether the Approved Document F continuous mechanical airflows were carried onto the model, and
        /// why not where they were not. A function of <see cref="VentilationMode"/> alone.
        /// </summary>
        public PartOPartFAirflowApplication AirflowApplication { get; internal set; }

        /// <summary>
        /// The design ventilation terminals realized for this iteration, or empty where the route has none.
        /// <para>
        /// Realized only on the MVHR route, and only because the Base MVHR operating scenario asks for
        /// design-rate operation - never because a space happens to carry an airflow. On the natural
        /// ventilation route this is empty, which is the honest answer: Iteration 1b has no continuous
        /// mechanical terminals to realize.
        /// </para>
        /// <para>
        /// <b>Zero, one or many per space per direction.</b> Read the sum, never the count.
        /// </para>
        /// </summary>
        public List<VentilationTerminal> VentilationTerminals { get; } = [];

        /// <summary>
        /// The generic ventilation system the design terminals were connected to, or null where the route
        /// has none. No manufacturer unit is selected at this iteration.
        /// </summary>
        public VentilationSystem VentilationSystem { get; internal set; }

        /// <summary>
        /// The generic air handling unit that system supplies from, or null where the route has none.
        /// </summary>
        public AirHandlingUnit AirHandlingUnit { get; internal set; }

        /// <summary>
        /// The system's total design supply duty [l/s], summed from its connected supply terminals.
        /// <see cref="double.NaN"/> where no system was built.
        /// <para>
        /// This is the figure a real unit will have to meet at Iteration 2. It is derived on demand from
        /// the terminals and never stored on the system, so it cannot go stale when a terminal is added,
        /// removed or re-balanced - see <c>Query.VentilationSystemDesignDuty</c>.
        /// </para>
        /// </summary>
        public double DesignSupplyDuty_Lps { get; internal set; } = double.NaN;

        /// <summary>
        /// The system's total design extract duty [l/s], summed from its connected extract terminals.
        /// <see cref="double.NaN"/> where no system was built.
        /// <para>
        /// Reported separately from <see cref="DesignSupplyDuty_Lps"/> and not required to equal it in any
        /// one room: a balanced heat recovery system balances at the system, with transfer air moving
        /// between the supplied and the extracted rooms.
        /// </para>
        /// </summary>
        public double DesignExtractDuty_Lps { get; internal set; } = double.NaN;

        /// <summary>What was applied to each space, what it displaced, and what was deliberately not applied.</summary>
        public List<string> Notes { get; } = [];

        /// <summary>Advisories that do not make the run unsuccessful - the opening-compatibility summary among them.</summary>
        public List<string> Warnings { get; } = [];

        /// <summary>Every individual item that produced no result, and why. One sentence each.</summary>
        public List<string> Refusals { get; } = [];

        /// <summary>Was everything prepared and stated with nothing refused?</summary>
        public bool Successful => Refusal == null && Refusals.Count == 0;
    }
}
