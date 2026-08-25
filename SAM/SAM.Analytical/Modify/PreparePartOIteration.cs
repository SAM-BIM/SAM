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
        /// Prepares a model for one Approved Document O mitigation stage: decides whether the Approved
        /// Document F airflows belong on it, carries them where they do, reports how the model's authored
        /// opening behaviour compares with the stage, and states the scenarios the results are attributed to.
        /// <para>
        /// <b>This is the whole preparation, in the library.</b>
        /// <c>SAMAnalytical.PreparePartOIteration</c> reads Grasshopper parameters and raises Grasshopper
        /// messages; every decision it used to make inline is made here, so the decisions are testable
        /// without a Grasshopper assembly and the component and the tests cannot drift apart.
        /// </para>
        /// <para>
        /// <b>The ventilation strategy gates the airflow.</b> <c>PartFCalculator</c> is unconditionally
        /// System 4 shaped - paragraph 1.67 gives every habitable room a mechanical supply terminal, with no
        /// input for how the dwelling is actually ventilated - so applying its rates to a naturally
        /// ventilated dwelling simulates an MVHR system nobody described, successfully and silently. See
        /// <see cref="Query.PartOPartFAirflowApplication(IEnumerable{Zone}, Dictionary{Guid, string}, out string)"/>
        /// for the decision and <see cref="PartOPartFAirflowApplication"/> for what each answer means.
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
        /// <param name="partOIteration">The mitigation stage being prepared.</param>
        /// <param name="zones">
        /// The zones to assess, already resolved against <paramref name="analyticalModel"/>. Null means
        /// every zone the model carries; an empty sequence means the caller named zones and none of them
        /// resolved, which states no scenarios rather than quietly assessing the whole building.
        /// </param>
        /// <param name="dictionary_VentilationStrategy">
        /// The strategy each assessed zone states, by zone guid - <c>NV</c>, <c>MV</c>, <c>MVRE</c>,
        /// <c>UV</c>. Required, never defaulted: a silent default assessed a mechanically ventilated
        /// dwelling against the natural-ventilation criterion.
        /// </param>
        public static PartOIterationPreparation PreparePartOIteration(this AnalyticalModel analyticalModel, PartOIteration partOIteration, IEnumerable<Zone> zones, Dictionary<Guid, string> dictionary_VentilationStrategy)
        {
            PartOIterationPreparation result = new();

            if (analyticalModel == null)
            {
                result.Refusal = "No analytical model was supplied, so there is nothing to prepare.";

                return result;
            }

            //The join between the two documents: which Approved Document F condition this stage runs at. A
            //stage with no settled condition refuses rather than being simulated at whichever rate happens
            //to be handy.
            PartFOperatingMode? partFOperatingMode = partOIteration.PartOIterationOperatingMode(out string refusal_OperatingMode);
            if (!partFOperatingMode.HasValue)
            {
                result.Refusal = refusal_OperatingMode;

                return result;
            }

            //Resolved off the SUPPLIED model rather than the applied one, because the airflow decision needs
            //the strategies before the airflow is applied. The airflow application never adds, removes or
            //re-guids a zone - it only replaces spaces - so the two sets are the same set, and the scenarios
            //below are still stated over zone objects taken from the model that will be simulated.
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

            result.AirflowApplication = Query.PartOPartFAirflowApplication(zones_Assessed, dictionary_VentilationStrategy, out string diagnostic_Airflow);

            //Refused BEFORE anything is applied, and with no model on the result. Continuing here is
            //materially different from continuing past an opening-restriction disagreement: that one only
            //mislabels which assumption a true result was obtained under, while this one would put
            //continuous mechanical supply and extract into a dwelling that has none.
            if (result.AirflowApplication == PartOPartFAirflowApplication.RefuseMixed)
            {
                result.Refusal = diagnostic_Airflow;

                return result;
            }

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
                analyticalModel_Applied = analyticalModel.ApplyPartFVentilationRates(partFOperatingMode.Value, out List<string> refusals_Airflow, out List<string> notes_Airflow);

                result.Refusals.AddRange(refusals_Airflow);
                result.Notes.AddRange(notes_Airflow);

                if (analyticalModel_Applied == null)
                {
                    result.Refusal = "No Part F rates could be applied, so there is nothing to simulate. Run a Part F component first.";

                    return result;
                }
            }

            //The join between the scenario's "Openings Restricted" assumption and the opening behaviour the
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

            if (zones_Assessed.Count == 0)
            {
                //Not fatal: a single-house model may carry no zones at all, and the airflow half above is
                //still the useful part. Worded for both reasons a caller reaches this - a model with no
                //zones, and a caller that named zones none of which resolved - because only the caller
                //knows which of the two it is.
                result.Warnings.Add("No zones were assessed, so no scenarios were stated.");

                return result;
            }

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
