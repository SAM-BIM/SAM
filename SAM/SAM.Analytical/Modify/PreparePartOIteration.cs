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
        /// Prepares a model for one Approved Document O base iteration: settles the ventilation route the
        /// assessment is being made over, decides from that route whether the Approved Document F airflows
        /// belong on the model, carries them where they do, reports how the model's authored opening
        /// behaviour compares with the stage, and states the scenarios the results are attributed to.
        /// <para>
        /// <b>This is the whole preparation, in the library.</b>
        /// <c>SAMAnalytical.PreparePartOIteration</c> reads Grasshopper parameters and raises Grasshopper
        /// messages; every decision it used to make inline is made here, so the decisions are testable
        /// without a Grasshopper assembly and the component and the tests cannot drift apart.
        /// </para>
        ///
        /// <para><b>The order of the four gates, and why it is that order</b></para>
        /// <list type="number">
        /// <item>
        /// <b>The route the assessment states.</b> <see cref="Enums.PartOVentilationMode"/>, resolved from
        /// what the assessed zones state and from nothing else. Settled first because it is the fact every
        /// later decision depends on, and because reaching the airflow application without it is the defect
        /// this whole design closes.
        /// </item>
        /// <item>
        /// <b>The route the iteration is defined over.</b> An iteration is not neutral about how the
        /// dwelling is ventilated - its operating assumptions go into the permanent scenario key - so a
        /// stage and a route that disagree produce a true simulation filed under a false claim.
        /// </item>
        /// <item>
        /// <b>Whether the Part F airflows belong on the model</b>, which on the MVHR route additionally
        /// needs the Approved Document F condition the stage runs at.
        /// </item>
        /// <item>
        /// <b>How the authored openings compare with the stage.</b> Reported, never acted on.
        /// </item>
        /// </list>
        ///
        /// <para>
        /// <b>The route is stated, never inferred, and never written.</b> No <c>SAM_System</c>,
        /// <c>SystemTemplate</c> or <c>InternalCondition.VentilationSystemTypeName</c> on the model is read
        /// to decide what is simulated - they are metadata that may be stale, may predate the assessment,
        /// and may describe a different design stage. Nor is any of them mutated to force a route: that
        /// would put the decision straight back into the metadata it was taken out of, and would make the
        /// model on disk a lie about the building. See <c>documentation/PartO-ARCHITECTURE.md</c>.
        /// </para>
        /// <para>
        /// <b>The model is never rewritten to fit the stage.</b> An <c>OpeningRestriction</c> is authored
        /// building data and <c>PartOOpeningProperties.Schedule</c> is derived from it, so resetting a
        /// restriction to match the stage's assumption deletes the aperture's <c>PartO_DayOpen_HH_HH</c>
        /// availability schedule from the model that reaches TAS. The disagreement is reported instead.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// The model to prepare. <b>Not modified</b> - a prepared copy is returned on the result.
        /// </param>
        /// <param name="partOIteration">
        /// The base iteration being prepared - <c>BasePassive</c> (1a, MVHR) or
        /// <c>BaseNaturalVentilation</c> (1b). These are alternatives, not successive stages.
        /// </param>
        /// <param name="zones">
        /// The zones to assess, already resolved against <paramref name="analyticalModel"/>. Null means
        /// every zone the model carries; an empty sequence means the caller named zones and none of them
        /// resolved. Either way, no assessed zone means no stated route, which refuses.
        /// </param>
        /// <param name="dictionary_VentilationStrategy">
        /// The Part O ventilation route each assessed zone states, by zone guid - <c>NV</c> /
        /// <c>NaturalVentilation</c>, or <c>MVHR</c> / <c>MVRE</c>. Required, never defaulted.
        /// </param>
        public static PartOIterationPreparation PreparePartOIteration(this AnalyticalModel analyticalModel, PartOIteration partOIteration, IEnumerable<Zone> zones, Dictionary<Guid, string> dictionary_VentilationStrategy)
        {
            PartOIterationPreparation result = new();

            if (analyticalModel == null)
            {
                result.Refusal = "No analytical model was supplied, so there is nothing to prepare.";

                return result;
            }

            //Resolved off the SUPPLIED model rather than the applied one, because the route has to be known
            //before anything is applied. The airflow application never adds, removes or re-guids a zone -
            //it only replaces spaces - so the two sets are the same set, and the scenarios below are still
            //stated over zone objects taken from the model that will be simulated.
            List<Zone> zones_Assessed = [];
            if (zones == null)
            {
                zones_Assessed = analyticalModel.GetZones() ?? [];
            }
            else
            {
                foreach (Zone zone in zones)
                {
                    if (zone != null)
                    {
                        zones_Assessed.Add(zone);
                    }
                }
            }

            // ---- 1. The route the assessment states -----------------------------------------------------

            result.VentilationMode = Query.PartOVentilationMode(zones_Assessed, dictionary_VentilationStrategy, out string refusal_VentilationMode);

            result.AirflowApplication = Query.PartOPartFAirflowApplication(result.VentilationMode, out string diagnostic_Airflow);

            if (result.VentilationMode == Enums.PartOVentilationMode.Undefined)
            {
                //Refused BEFORE anything is applied, and with no model on the result. Continuing here is
                //materially different from continuing past an opening-restriction disagreement: that one
                //only mislabels which assumption a true result was obtained under, while this one would put
                //continuous mechanical supply and extract into a dwelling that may have none.
                result.Refusal = refusal_VentilationMode;

                return result;
            }

            // ---- 2. The route the iteration is defined over ---------------------------------------------

            PartOVentilationMode partOVentilationMode_Iteration = partOIteration.PartOIterationVentilationMode(out string refusal_Iteration);

            if (partOVentilationMode_Iteration == Enums.PartOVentilationMode.Undefined)
            {
                result.Refusal = refusal_Iteration;

                return result;
            }

            if (partOVentilationMode_Iteration != result.VentilationMode)
            {
                //Not a formality. The iteration's operating assumptions are part of the derived
                //OverheatingScenario.Key, so this pairing would not merely be confusing - it would mint a
                //permanent identity asserting something false about the building, and every result ever
                //attributed to that key would carry the assertion with it.
                result.Refusal = string.Format(
                    "The {0} iteration is the base configuration for the {1} route, but the assessed zones state {2}. These are alternative base configurations of the same dwelling, not successive stages, so one of the two statements is wrong. Assess a {2} dwelling at {3}. Preparing it here would attribute a true {2} result to a scenario whose permanent identity asserts the {1} route's operating assumptions.",
                    partOIteration,
                    partOVentilationMode_Iteration,
                    result.VentilationMode,
                    result.VentilationMode == Enums.PartOVentilationMode.NaturalVentilation ? PartOIteration.BaseNaturalVentilation : PartOIteration.BasePassive);

                return result;
            }

            // ---- 3. The Approved Document F airflows ----------------------------------------------------

            AnalyticalModel analyticalModel_Applied;

            if (result.AirflowApplication == PartOPartFAirflowApplication.SkipNaturalVentilation)
            {
                //A copy, so the "not modified, a prepared copy is returned" contract holds on this path too
                //and a caller comparing the two objects sees the same thing either way. Nothing is written
                //to it: absence of PartFSpaceData is not an error here, because there are no continuous
                //mechanical rates to look for.
                analyticalModel_Applied = new AnalyticalModel(analyticalModel);

                result.Notes.Add(diagnostic_Airflow);
                result.Warnings.Add(diagnostic_Airflow);
            }
            else
            {
                //The join between the two documents: which Approved Document F condition this stage runs at.
                //Asked only on the MVHR route, because it is the only route with a continuous mechanical
                //rate to have a condition for. A stage with no settled condition refuses rather than being
                //simulated at whichever rate happens to be handy.
                PartFOperatingMode? partFOperatingMode = partOIteration.PartOIterationOperatingMode(out string refusal_OperatingMode);
                if (!partFOperatingMode.HasValue)
                {
                    result.Refusal = refusal_OperatingMode;

                    return result;
                }

                analyticalModel_Applied = analyticalModel.ApplyPartFVentilationRates(partFOperatingMode.Value, out List<string> refusals_Airflow, out List<string> notes_Airflow);

                result.Refusals.AddRange(refusals_Airflow);
                result.Notes.AddRange(notes_Airflow);

                if (analyticalModel_Applied == null)
                {
                    result.Refusal = "No Part F rates could be applied, so there is nothing to simulate. Run a Part F component first.";

                    return result;
                }
            }

            // ---- 4. The authored openings, reported ------------------------------------------------------

            //The join between the stage's "Openings Restricted" assumption and the opening behaviour the
            //model carries. The model is REPORTED ON, never rewritten to fit the stage.
            result.OpeningCompatibility = analyticalModel_Applied.PartOIterationOpeningCompatibility(partOIteration, out string summary_Openings, out List<string> evidence_Openings);

            if (evidence_Openings != null)
            {
                result.Notes.AddRange(evidence_Openings);
            }

            if (summary_Openings != null)
            {
                result.Notes.Add(summary_Openings);
                result.Warnings.Add(summary_Openings);
            }

            result.AnalyticalModel = analyticalModel_Applied;

            // ---- 5. The scenarios the results are attributed to ------------------------------------------

            //Stated over zone objects taken from the APPLIED model, so a zone guid on a scenario and a zone
            //guid in the model that will be simulated are the same identity.
            List<Zone> zones_Applied = analyticalModel_Applied.GetZones() ?? [];

            List<Zone> zones_Scenario = [];
            foreach (Zone zone in zones_Assessed)
            {
                zones_Scenario.Add(zones_Applied.Find(x => x != null && x.Guid == zone.Guid) ?? zone);
            }

            result.OverheatingScenarios.AddRange(Create.OverheatingScenarios(zones_Scenario, partOIteration, dictionary_VentilationStrategy, out List<string> refusals_Scenarios));

            result.Refusals.AddRange(refusals_Scenarios);

            return result;
        }
    }
}
