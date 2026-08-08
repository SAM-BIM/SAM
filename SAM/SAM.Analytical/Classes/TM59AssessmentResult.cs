// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// What a TM59 assessment of a simulated model produces: the spaces assessed, their results split by
    /// the criterion that applied, and the comfort temperature limits the criteria were measured against.
    /// <para>
    /// <b>The three lists are the three TM59 criteria, not three arbitrary groupings.</b> A mechanically
    /// ventilated space, a naturally ventilated space and a corridor are judged against different rules, so
    /// they cannot be reported in one list without losing the thing that makes the number mean something.
    /// </para>
    /// <para>
    /// A plain carrier - it holds what the calculation produced and decides nothing. Deliberately not a
    /// <c>Result</c>: these are not attached to a model, and attributing them to design objects is separate
    /// work (<c>SimulationSpaceMap</c>, and by identity rather than by name).
    /// </para>
    /// </summary>
    public class TM59AssessmentResult
    {
        internal TM59AssessmentResult(List<Space> spaces, List<TMResult> tMResults_MechanicalVentilation, List<TMResult> tMResults_NaturalVentilation, List<TMResult> tMResults_Corridor, IndexedDoubles indexedDoubles_MaxIndoorComfortTemperatures, IndexedDoubles indexedDoubles_MinIndoorComfortTemperatures)
        {
            Spaces = spaces;
            MechanicalVentilationResults = tMResults_MechanicalVentilation;
            NaturalVentilationResults = tMResults_NaturalVentilation;
            CorridorResults = tMResults_Corridor;
            MaxIndoorComfortTemperatures = indexedDoubles_MaxIndoorComfortTemperatures;
            MinIndoorComfortTemperatures = indexedDoubles_MinIndoorComfortTemperatures;
        }

        /// <summary>
        /// The spaces the assessment covered - the <b>simulation</b> spaces, not the design ones. Attributing
        /// them back to the design model is separate work and must go by identity, never by name.
        /// </summary>
        public List<Space> Spaces { get; }

        /// <summary>Results judged against the mechanically ventilated criterion.</summary>
        public List<TMResult> MechanicalVentilationResults { get; }

        /// <summary>Results judged against the naturally ventilated criterion.</summary>
        public List<TMResult> NaturalVentilationResults { get; }

        /// <summary>
        /// Results judged against the corridor criterion. A communal corridor is assessed in its own right
        /// and belongs to no dwelling.
        /// </summary>
        public List<TMResult> CorridorResults { get; }

        /// <summary>The upper comfort limit series the criteria were measured against.</summary>
        public IndexedDoubles MaxIndoorComfortTemperatures { get; }

        /// <summary>The lower comfort limit series.</summary>
        public IndexedDoubles MinIndoorComfortTemperatures { get; }
    }
}
