// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Whether <paramref name="analyticalModel"/> is a clean pre-Part-O baseline that mixed dwelling
        /// strategies may be materialised from - see <see cref="PartOBaselineFindings(AnalyticalModel)"/>.
        /// </summary>
        public static bool IsPartOCleanBaseline(this AnalyticalModel analyticalModel, out List<PartOMaterialisationRefusal> findings)
        {
            findings = PartOBaselineFindings(analyticalModel);

            return findings.Count == 0;
        }

        /// <summary>
        /// Every reason <paramref name="analyticalModel"/> is <b>not</b> a clean pre-Part-O baseline. Empty means
        /// clean. Owner decision D1: mixed materialisation starts from a clean baseline, and nothing here
        /// cleans, repairs or "adopts" a model - it only says why the model is refused.
        ///
        /// <para><b>What a baseline IS</b></para>
        /// <para>
        /// The design layer: geometry, constructions, openings, authored internal conditions, zones with
        /// <c>IsDwelling</c>, Approved Document F requirements, design ventilation terminals the designer (or an
        /// accepted Iteration 2B outcome) stated, the model's weather and its <b>model-level</b> heating and
        /// cooling design days, project settings, and the dwelling strategy collection itself.
        /// </para>
        ///
        /// <para><b>What it is NOT - the two groups of signals</b></para>
        /// <list type="bullet">
        /// <item>
        /// <b>Materialisation</b> (<see cref="PartOMaterialisationRefusalReason.MaterialisedBaseline"/>): a
        /// ventilation system of the Part O MVHR type; any <c>SpaceAirMovement</c> or
        /// <c>AirHandlingUnitAirMovement</c>; an internal condition carrying the per-space Part F rate
        /// <c>Modify.ApplyPartFVentilationRates</c> writes (its <c>&lt;condition&gt; - &lt;space&gt;</c> name
        /// together with a supply or extract airflow); a <see cref="PartOMaterialisationRecord"/>; a
        /// <see cref="PartOIsolationContext"/> (an isolated derivative is a run artefact, not a building).
        /// </item>
        /// <item>
        /// <b>Run output</b> (<see cref="PartOMaterialisationRefusalReason.RunOutputBaseline"/>): overheating
        /// scenarios; a <see cref="SimulationResultProvenance"/>; <b>any</b> object in the cluster, or value on
        /// the model, deriving from <see cref="IResult"/> - the base type, not a list, so a later result type is
        /// covered the day it appears; and design-day records held <b>in the adjacency cluster</b>.
        /// </item>
        /// </list>
        ///
        /// <para><b>The DesignDay rule (the PR0 design gate, pinned by test)</b></para>
        /// <para>
        /// The model-level <c>AnalyticalModelParameter.HeatingDesignDays</c> / <c>CoolingDesignDays</c> are
        /// design inputs - derived from the model's weather, or an engineer's stated override - and a baseline
        /// may carry them. <c>DesignDay</c> objects <b>in the adjacency cluster</b> are written there by the TAS
        /// workflow after a run (<c>SAM_Tas Modify.ReplaceDesignDays</c> from <c>WorkflowCalculator</c>); no
        /// authoring path puts them there, and they are the records whose accumulation grew a re-run
        /// <c>.sam</c>. So they are run output and refuse. A false positive only refuses, which is the safe
        /// direction.
        /// </para>
        /// </summary>
        /// <returns>The findings, one per signal kind found; empty for a clean baseline.</returns>
        public static List<PartOMaterialisationRefusal> PartOBaselineFindings(this AnalyticalModel analyticalModel)
        {
            List<PartOMaterialisationRefusal> result = [];

            if (analyticalModel is null)
            {
                result.Add(new PartOMaterialisationRefusal(PartOMaterialisationRefusalReason.NoModel, "No analytical model was supplied, so there is no baseline to materialise from."));

                return result;
            }

            // ---- run output, model level -------------------------------------------------------------------

            if (analyticalModel.HasValue(AnalyticalModelParameter.OverheatingScenarios))
            {
                result.Add(RunOutput("The model carries overheating scenarios, so it has been through a Part O run: scenarios are stated for a run, not authored on a baseline."));
            }

            if (analyticalModel.HasValue(AnalyticalModelParameter.SimulationResultProvenance))
            {
                result.Add(RunOutput("The model carries a simulation result provenance record, so it is the output of a simulation, not a baseline."));
            }

            if (ModelResults(analyticalModel) is string name_Result)
            {
                result.Add(RunOutput(string.Format("The model carries simulation results as its own parameter '{0}', so it is the output of a simulation, not a baseline.", name_Result)));
            }

            // ---- materialisation, model level --------------------------------------------------------------

            if (analyticalModel.HasValue(AnalyticalModelParameter.PartOMaterialisationRecord))
            {
                result.Add(Materialised("The model carries a Part O materialisation record, so it is a materialised mixed model, not the baseline it was built from. Materialise again from that baseline instead."));
            }

            if (analyticalModel.HasValue(AnalyticalModelParameter.PartOIsolationContext))
            {
                result.Add(Materialised("The model carries a Part O isolation context, so it is an isolated derivative of a building prepared for a run, not the building itself."));
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster is null)
            {
                result.Add(new PartOMaterialisationRefusal(PartOMaterialisationRefusalReason.NoModel, "The model carries no adjacency cluster, so there is no baseline to materialise from."));

                return result;
            }

            // ---- run output, cluster -----------------------------------------------------------------------

            //By the TYPES the cluster stores, each tested against IResult: the cluster answers no interface
            //query (GetObjects<IResult>() is null), and a list of result classes would miss the next one.
            int count_Result = 0;
            string name_Type = null;
            foreach (Type type in adjacencyCluster.GetTypes() ?? [])
            {
                if (type is null || !typeof(IResult).IsAssignableFrom(type))
                {
                    continue;
                }

                int count = adjacencyCluster.GetObjects(type)?.Count ?? 0;
                if (count != 0)
                {
                    count_Result += count;
                    name_Type ??= type.Name;
                }
            }

            if (count_Result != 0)
            {
                result.Add(RunOutput(string.Format("The model's cluster carries {0} simulation result object(s) (for example {1}), so it is the output of a simulation, not a baseline. Accepting it would feed a run's output back in as its input.", count_Result, name_Type)));
            }

            List<DesignDay> designDays = adjacencyCluster.GetObjects<DesignDay>() ?? [];
            if (designDays.Count != 0)
            {
                result.Add(RunOutput(string.Format("The model's cluster carries {0} design-day record(s). The TAS workflow writes those into the cluster after a run (Modify.ReplaceDesignDays); a baseline states its design days as the model's own Heating/Cooling Design Days parameters, which are accepted.", designDays.Count)));
            }

            // ---- materialisation, cluster ------------------------------------------------------------------

            List<string> names_System = [];
            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                if (ventilationSystem?.Type is not null && ventilationSystem.Type.Guid == Modify.Guid_VentilationSystemType_PartOMVHR)
                {
                    names_System.Add(ventilationSystem.FullName ?? ventilationSystem.Name);
                }
            }

            if (names_System.Count != 0)
            {
                names_System.Sort(StringComparer.Ordinal);

                result.Add(Materialised(string.Format("The model already carries Part O MVHR system(s) {0}, so it has been prepared for a Part O run. Reopen the pre-Part-O source model; a materialised model is never cleaned back into a baseline.", string.Join(", ", names_System.ConvertAll(x => string.Format("'{0}'", x))))));
            }

            int count_AirMovement = (adjacencyCluster.GetObjects<SpaceAirMovement>()?.Count ?? 0) + (adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>()?.Count ?? 0);
            if (count_AirMovement != 0)
            {
                result.Add(Materialised(string.Format("The model carries {0} air movement object(s). Those are the runtime realisation of a mechanical design, derived at materialisation, so a baseline carries none.", count_AirMovement)));
            }

            List<string> names_Space = [];
            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                if (IsPartFAppliedInternalCondition(space))
                {
                    names_Space.Add(space.Name);
                }
            }

            if (names_Space.Count != 0)
            {
                names_Space.Sort(StringComparer.Ordinal);

                result.Add(Materialised(string.Format("{0} space(s) carry the per-space internal condition Modify.ApplyPartFVentilationRates writes (for example '{1}'), so the model's authored internal conditions have already been replaced by a Part O preparation. That rewrite is not reversible; reopen the pre-Part-O source model.", names_Space.Count, names_Space[0])));
            }

            return result;
        }

        /// <summary>
        /// Whether a sized space's internal condition is the per-space clone <c>Modify.ApplyPartFVentilationRates</c>
        /// wrote: its name is <c>&lt;condition&gt; - &lt;space&gt;</c> (optionally disambiguated
        /// <c>" (n)"</c>) AND it carries a supply or extract airflow. Both, so an authored condition that merely
        /// carries an airflow, or merely has such a name, is not mistaken for one.
        /// </summary>
        private static bool IsPartFAppliedInternalCondition(Space space)
        {
            InternalCondition internalCondition = space?.InternalCondition;
            if (internalCondition is null || string.IsNullOrWhiteSpace(space.Name) || !space.HasValue(SpaceParameter.PartFSpaceData))
            {
                return false;
            }

            if (!internalCondition.HasValue(Analytical.InternalConditionParameter.SupplyAirFlow) && !internalCondition.HasValue(Analytical.InternalConditionParameter.ExhaustAirFlow))
            {
                return false;
            }

            string name = internalCondition.Name ?? string.Empty;
            string suffix = string.Format(" - {0}", space.Name);

            if (name.EndsWith(")", StringComparison.Ordinal))
            {
                int index = name.LastIndexOf(" (", StringComparison.Ordinal);
                if (index > 0 && int.TryParse(name.Substring(index + 2, name.Length - index - 3), out int _))
                {
                    name = name.Substring(0, index);
                }
            }

            return name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length;
        }

        /// <summary>The name of the first model-level parameter holding a simulation result, or null.</summary>
        private static string ModelResults(AnalyticalModel analyticalModel)
        {
            foreach (ParameterSet parameterSet in analyticalModel.GetParameterSets() ?? [])
            {
                foreach (string name in parameterSet?.Names ?? [])
                {
                    object @object = parameterSet.ToObject(name);

                    if (@object is IResult)
                    {
                        return name;
                    }

                    if (@object is IEnumerable enumerable && @object is not string)
                    {
                        foreach (object item in enumerable)
                        {
                            if (item is IResult)
                            {
                                return name;
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static PartOMaterialisationRefusal RunOutput(string message) => new(PartOMaterialisationRefusalReason.RunOutputBaseline, message);

        private static PartOMaterialisationRefusal Materialised(string message) => new(PartOMaterialisationRefusalReason.MaterialisedBaseline, message);
    }
}
