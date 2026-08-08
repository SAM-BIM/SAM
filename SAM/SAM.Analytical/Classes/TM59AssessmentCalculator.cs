// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    /// <summary>
    /// The TM59 assessment recipe: restore the design internal conditions onto a simulated model, choose the
    /// spaces to assess from a set of spaces and zones, calculate, and split the results by criterion.
    /// <para>
    /// <b>Lifted from the <c>Tas.TSDQueryTM59Results</c> Grasshopper component, without changing what it
    /// does.</b> That component held the only working statement of this sequence, inside a
    /// <c>SolveInstance</c> interleaved with parameter plumbing - so nothing could call it, nothing could
    /// test it, and the headless runner would have had to restate it and drift. Everything here was already
    /// working; it has been moved, not redesigned.
    /// </para>
    /// <para>
    /// <b>Engine-free, and that is the point of putting it here.</b> Reading a TSD needs TAS; restoring
    /// internal conditions, selecting spaces, calculating TM59 and splitting the results do not. Only the
    /// read stays in <c>SAM.Analytical.Tas</c>, so the recipe is testable without a licensed TAS install -
    /// the same split that <c>TMOverheatingCalculator</c> already makes, for the same reason.
    /// </para>
    ///
    /// <para><b>Two things preserved verbatim that are known to be wrong</b></para>
    /// <list type="bullet">
    /// <item><b>Spaces are matched by NAME</b>, in <see cref="RestoreDesignInternalConditions"/> and in
    /// <see cref="Spaces"/>. Every flat in a block has a "Bedroom 2", so this silently pairs one dwelling's
    /// room with another's. <c>SimulationSpaceMap</c> exists to fix it - by identity, refusing on ambiguity -
    /// and doing so here would make the extraction unverifiable. It is the next step, not this one.</item>
    /// <item><b>The criterion is chosen by <c>TMOverheatingCalculator</c>'s existing derivation</b>, which
    /// falls back to matching a zone's name against a system library and then defaults to natural
    /// ventilation - so an MVHR dwelling can be assessed against the wrong criterion. Making the scenario
    /// authoritative is also a later step.</item>
    /// </list>
    /// </summary>
    public class TM59AssessmentCalculator
    {
        private AnalyticalModel analyticalModel = null;

        /// <param name="analyticalModel">
        /// The model read back from a simulation - fresh spaces carrying hourly series, not the design model.
        /// </param>
        public TM59AssessmentCalculator(AnalyticalModel analyticalModel)
        {
            this.analyticalModel = analyticalModel;
        }

        /// <summary>The simulated model the assessment reads, as it currently stands.</summary>
        public AnalyticalModel AnalyticalModel => analyticalModel;

        /// <summary>The TM52 building category the comfort limits are derived for.</summary>
        public TM52BuildingCategory TM52BuildingCategory { get; set; } = TM52BuildingCategory.CategoryII;

        /// <summary>
        /// The key each space's hourly operative-temperature series is stored under. Passed straight to
        /// <see cref="TMOverheatingCalculator"/>; see its documentation for why it is supplied rather than
        /// assumed.
        /// </summary>
        public string ResultantTemperatureSeriesKey { get; set; } = Core.Query.Name(SpaceSimulationResultParameter.ResultantTemperature);

        /// <summary>
        /// The key for the hourly occupancy-gain series. <b>This is the one the two vocabularies disagree
        /// about</b> - the TAS conversion writes "Occupant Sensible Gain" - so a TAS caller must supply its
        /// own, exactly as it does for <c>OverheatingCalculator</c>.
        /// </summary>
        public string OccupancySensibleGainSeriesKey { get; set; } = Core.Query.Name(SpaceSimulationResultParameter.OccupancySensibleGain);

        /// <summary>
        /// Copies each design space's <c>InternalCondition</c> onto the simulated space of the same name.
        /// <para>
        /// <b>Why the assessment needs it.</b> A model read back from a simulation carries results but not
        /// the design intent, and TM59 chooses its criterion and its occupancy profile from the internal
        /// condition. Without this every simulated space would be assessed as though nothing had been said
        /// about how it is used.
        /// </para>
        /// <para>
        /// <b>By name, preserved.</b> This is the component's behaviour unchanged, and it is wrong in the way
        /// described on the class: two flats' "Bedroom 2" are indistinguishable. Fixing it means
        /// <c>SimulationSpaceMap</c>, which refuses on ambiguity rather than guessing, and that is the next
        /// step - changing it here would mean the extraction could not be shown to preserve behaviour.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel_Design">The design model whose internal conditions are authoritative.</param>
        /// <returns>Whether anything was restored.</returns>
        public bool RestoreDesignInternalConditions(AnalyticalModel analyticalModel_Design)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster == null)
            {
                return false;
            }

            List<Space> spaces_Design = analyticalModel_Design?.GetSpaces();
            if (spaces_Design == null)
            {
                return false;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null)
            {
                return false;
            }

            foreach (Space space in spaces)
            {
                Space space_Design = spaces_Design.Find(x => x.Name == space.Name);
                if (space_Design != null)
                {
                    space.InternalCondition = space_Design.InternalCondition;
                    adjacencyCluster.AddObject(space);
                }
            }

            analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);

            return true;
        }

        /// <summary>
        /// The simulated spaces an assessment of the given spaces and zones covers.
        /// <para>
        /// <b>No spaces and no zones means the whole model</b> - which is what the component does, and is why
        /// the real TAS run put a communal corridor into a domestic overheating export as an ordinary room.
        /// Scoping that properly is the Part O assessment-scope work, not this.
        /// </para>
        /// <para>
        /// A zone contributes every space related to it. Spaces already present are not added twice, matched
        /// by name - the component's behaviour, and the same name-matching caveat applies.
        /// </para>
        /// </summary>
        public List<Space> Spaces(IEnumerable<Space> spaces, IEnumerable<Zone> zones)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            List<Space> result;

            if (spaces == null)
            {
                result = analyticalModel.GetSpaces();
            }
            else
            {
                result = [];

                foreach (Space space in spaces)
                {
                    Space space_Result = analyticalModel.GetSpaces()?.Find(x => x.Name == space?.Name);
                    if (space_Result == null)
                    {
                        continue;
                    }

                    result.Add(space_Result);
                }
            }

            if (zones == null)
            {
                return result;
            }

            if (result == null)
            {
                result = [];
            }

            foreach (Zone zone in zones)
            {
                Zone zone_Temp = analyticalModel.GetZones()?.Find(x => x.Name == zone?.Name);
                if (zone_Temp == null)
                {
                    continue;
                }

                List<Space> spaces_Zone = analyticalModel.AdjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
                if (spaces_Zone == null)
                {
                    continue;
                }

                foreach (Space space_Zone in spaces_Zone)
                {
                    if (result.Find(x => x.Name == space_Zone.Name) != null)
                    {
                        continue;
                    }

                    result.Add(space_Zone);
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates TM59 for the given spaces and splits the results by the criterion that applied.
        /// </summary>
        /// <param name="spaces">
        /// The simulated spaces to assess - normally what <see cref="Spaces"/> returned.
        /// </param>
        /// <param name="extended">
        /// Whether to return the extended results. False simplifies each one, which is the component's
        /// default and drops the per-hour detail.
        /// </param>
        public TM59AssessmentResult Calculate(IEnumerable<Space> spaces, bool extended = false)
        {
            if (analyticalModel == null || spaces == null)
            {
                return null;
            }

            List<Space> spaces_Temp = [.. spaces];

            TMOverheatingCalculator tMOverheatingCalculator = new(analyticalModel)
            {
                TM52BuildingCategory = TM52BuildingCategory,
                ResultantTemperatureSeriesKey = ResultantTemperatureSeriesKey,
                OccupancySensibleGainSeriesKey = OccupancySensibleGainSeriesKey
            };

            List<TM59ExtendedResult> tM59ExtendedResults = tMOverheatingCalculator.Calculate_TM59(spaces_Temp);
            if (tM59ExtendedResults == null)
            {
                return null;
            }

            List<TMResult> tMResults_MechanicalVentilation = Split<TM59MechanicalVentilationExtendedResult>(tM59ExtendedResults, extended);
            List<TMResult> tMResults_NaturalVentilation = Split<TM59NaturalVentilationExtendedResult>(tM59ExtendedResults, extended);
            List<TMResult> tMResults_Corridor = Split<TM59CorridorExtendedResult>(tM59ExtendedResults, extended);

            //Whole year, as the component asks for - the comfort limits are a running mean over the year and
            //are reported alongside the summer assessment rather than clipped to it.
            IndexedDoubles indexedDoubles_Max = tMOverheatingCalculator.GetMaxIndoorComfortTemperatures(0, 364);
            IndexedDoubles indexedDoubles_Min = tMOverheatingCalculator.GetMinIndoorComfortTemperatures(0, 364);

            return new TM59AssessmentResult(spaces_Temp, tMResults_MechanicalVentilation, tMResults_NaturalVentilation, tMResults_Corridor, indexedDoubles_Max, indexedDoubles_Min);
        }

        /// <summary>
        /// The results of one criterion, simplified unless the extended form was asked for.
        /// <para>
        /// One method for all three, where the component repeated the pair of lines three times and - by
        /// accident - null-guarded two of them and not the third. <c>FindAll</c> never returns null, so the
        /// guard never did anything and the behaviour is unchanged either way.
        /// </para>
        /// </summary>
        private static List<TMResult> Split<T>(List<TM59ExtendedResult> tM59ExtendedResults, bool extended) where T : TM59ExtendedResult
        {
            List<TMResult> result = tM59ExtendedResults.FindAll(x => x is T).ConvertAll(x => (TMResult)x);

            return extended ? result : result.ConvertAll(x => (x as TM59ExtendedResult)?.Simplify());
        }
    }
}
