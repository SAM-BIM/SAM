// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Enums;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    /// <summary>
    /// Prepares a Part-F-sized <c>AnalyticalModel</c> for one Approved Document O mitigation stage: carries the
    /// Part F airflows onto the internal conditions the simulation reads, and states the scenarios the assessment
    /// will be attributed to.
    /// </summary>
    public class SAMAnalyticalPreparePartOIteration : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("6f2b4c81-3ad9-47e5-9b0c-1e8d5a37c264");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        public override GH_Exposure Exposure => GH_Exposure.primary;

        private const string description = @"
Prepares a Part-F-sized AnalyticalModel for one Approved Document O mitigation stage (iteration), and states the overheating scenarios for it.

WHERE THIS SITS
AnalyticalModel
  -> SAMAnalytical.AddVentilationPropertiesByPartF (or CheckPartFCompliance)   [sizes the dwelling]
  -> THIS COMPONENT                                                            [applies the rates, states the stage]
  -> To gbXML -> SAMAnalytical.WorkflowgbXML (Simulation = true)                [existing TSD simulation]
  -> Tas.TSDQueryTM59Results, with overheatingScenarios_ connected              [TM59 assessment]

WHAT IT DOES
1. Reads each space's Part F Space Data - it does NOT size anything. Run a Part F component first.
2. Works out which Approved Document F condition the stage runs at. BasePassive is the continuous design condition, which is the Approved Document F sizing case.
3. Writes that condition's supply and extract onto each sized space, in m3/s, so Query.CalculatedSupplyAirFlow and the TAS export can see them. Until now the Part F numbers were reporting only: the simulation never read them.
4. States one OverheatingScenario per zone at that iteration.

TWO THINGS IT DOES DELIBERATELY, AND REPORTS
- Part F becomes the AUTHORITATIVE rate. CalculatedSupplyAirFlow SUMS SupplyAirFlow with the per-person, per-area and air-changes bases, so a Part F rate written beside an existing one would ADD to it and over-ventilate the room. The other bases are cleared and every space where something was displaced is listed in notes.
- Each sized space gets its OWN internal condition. Part F rates are per room but internal conditions are routinely shared, so writing in place would let one bedroom's rate overwrite another's. Clones are named after the space.

A wet room receives its extract and an explicit ZERO supply: under the balanced MVHR arrangement Part F sizes, its make-up air is transfer air through the internal door.

ITERATIONS
- BasePassive        - continuous design condition. Available.
- AcousticRestricted - REFUSED. Its assumptions say boost is AVAILABLE, not continuous, and simulating a whole season at the Table 1.2 high rate is a much more favourable claim than making boost available to a control strategy. That is an engineering decision, not a mapping.
- ActiveTrimCooling  - REFUSED, not characterised yet.

WHAT IT STILL DOES NOT DO
- It does not select or apply a ventilation SYSTEM object. The capability selection exists but has no production caller yet.
- It does not touch aperture control, so 'openings operated without restriction' is not enforced on the model.
";

        public SAMAnalyticalPreparePartOIteration()
          : base("SAMAnalytical.PreparePartOIteration", "SAMAnalytical.PreparePartOIteration",
              description,
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM AnalyticalModel whose spaces already carry Part F Space Data. Run SAMAnalytical.AddVentilationPropertiesByPartF or SAMAnalytical.CheckPartFCompliance first.\n\nThe model you supply is not modified; an updated copy is returned.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_String @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_partOIteration", NickName = "_partOIteration", Description = "Part O mitigation stage. BasePassive is the only stage with a settled operating condition.", Access = GH_ParamAccess.item };
                @string.SetPersistentData(PartOIteration.BasePassive.ToString());
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "zones_", NickName = "zones_", Description = "Zones to state scenarios for. Leave unconnected to use every zone in the model.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                @string = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_ventilationStrategies", NickName = "_ventilationStrategies", Description = "Ventilation strategy per zone: NV, MV, MVRE or UV. One value applies to every zone. Required - never defaulted.", Access = GH_ParamAccess.list };
                result.Add(new GH_SAMParam(@string, ParamVisibility.Binding));

                return [.. result];
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "analyticalModel", NickName = "analyticalModel", Description = "Updated copy with the Part F rates on each sized space's own internal condition. Feed this to To gbXML.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "overheatingScenarios", NickName = "overheatingScenarios", Description = "Scenarios for this iteration. Feed into Tas.TSDQueryTM59Results overheatingScenarios_.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "Every space of the updated model, to align the airflow outputs against.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "supply m3/s", NickName = "supply m3/s", Description = "APPLIED supply air flow per space [m3/s], read back through Query.CalculatedSupplyAirFlow - the value the simulation will actually use. Matches the spaces output item for item.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "extract m3/s", NickName = "extract m3/s", Description = "APPLIED extract air flow per space [m3/s], from the space's internal condition. Matches the spaces output item for item.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "partF l/s", NickName = "partF l/s", Description = "The Part F sized rate the application read, per space [l/s] - supply for a habitable room, extract for a wet room. Compare against supply m3/s x 1000 to confirm what was applied.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "notes", NickName = "notes", Description = "What was applied to each space and what it displaced.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "refusals", NickName = "refusals", Description = "Everything that produced no result, and why.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "successful", NickName = "successful", Description = "Were the rates applied and scenarios stated with nothing refused?", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                return [.. result];
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index_Successful = Params.IndexOfOutputParam("successful");
            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, false);
            }

            int index;

            AnalyticalModel analyticalModel = null;
            index = Params.IndexOfInputParam("_analyticalModel");
            if (index == -1 || !dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_partOIteration");
            string text_PartOIteration = null;
            if (index == -1 || !dataAccess.GetData(index, ref text_PartOIteration) || string.IsNullOrWhiteSpace(text_PartOIteration))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!Core.Query.TryGetEnum(text_PartOIteration, out PartOIteration partOIteration))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("'{0}' is not a Part O iteration.", text_PartOIteration));
                return;
            }

            //The join between the two documents: which Approved Document F condition this stage runs at. A stage
            //with no settled condition refuses rather than being simulated at whichever rate happens to be handy.
            PartFOperatingMode? partFOperatingMode = partOIteration.PartOIterationOperatingMode(out string refusal_OperatingMode);
            if (!partFOperatingMode.HasValue)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, refusal_OperatingMode);
                return;
            }

            List<string> refusals = [];

            AnalyticalModel analyticalModel_Applied = analyticalModel.ApplyPartFVentilationRates(partFOperatingMode.Value, out List<string> refusals_Airflow, out List<string> notes);

            refusals.AddRange(refusals_Airflow);

            foreach (string refusal in refusals_Airflow)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, refusal);
            }

            if (analyticalModel_Applied == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No Part F rates could be applied, so there is nothing to simulate. Run a Part F component first.");
                return;
            }

            //Scenarios are stated over the APPLIED model, so a zone guid on a scenario and a zone guid in the model
            //that will be simulated are the same identity.
            List<Zone> zones_Model = analyticalModel_Applied.GetZones() ?? [];

            //Connected-but-every-guid-invalid is NOT the same statement as "unconnected", and must not
            //collapse to the same behaviour. Only an UNCONNECTED port means "assess the whole model" - once
            //a caller has explicitly named zones, an explicit selection that resolved to nothing is a stale
            //or wrong reference, and producing scenarios for every zone in the model instead would silently
            //broaden a targeted request into a whole-building assessment.
            bool zonesConnected = false;
            List<Zone> zones = null;
            index = Params.IndexOfInputParam("zones_");
            if (index != -1)
            {
                List<IAnalyticalObject> analyticalObjects = [];
                if (dataAccess.GetDataList(index, analyticalObjects) && analyticalObjects != null)
                {
                    zonesConnected = true;
                    zones = [];

                    foreach (Zone zone in analyticalObjects.FindAll(x => x is Zone).ConvertAll(x => (Zone)x))
                    {
                        Zone zone_Model = zones_Model.Find(x => x != null && x.Guid == zone.Guid);
                        if (zone_Model == null)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("Zone '{0}' is not in the supplied model, so it produces no scenario.", zone.Name));
                            continue;
                        }

                        zones.Add(zone_Model);
                    }
                }
            }

            if (!zonesConnected)
            {
                zones = zones_Model;
            }

            List<OverheatingScenario> overheatingScenarios = [];

            if (zonesConnected && zones.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "None of the zones supplied to zones_ are in the model, so no scenarios were stated. Correct the selection, or disconnect zones_ to assess the whole model.");
            }
            else if (zones.Count == 0)
            {
                //Not fatal: a single-house model may carry no zones at all, and the airflow application above is
                //still the useful half of this component.
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The model carries no zones, so no scenarios were stated. The Part F rates were still applied.");
            }
            else
            {
                List<string> ventilationStrategies = [];
                index = Params.IndexOfInputParam("_ventilationStrategies");
                if (index == -1 || !dataAccess.GetDataList(index, ventilationStrategies) || ventilationStrategies.Count == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "A ventilation strategy is required for every zone - NV, MV, MVRE or UV. It is not defaulted, because a silent default assessed a mechanically ventilated dwelling against the natural-ventilation criterion.");
                    return;
                }

                if (ventilationStrategies.Count != 1 && ventilationStrategies.Count != zones.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("{0} ventilation strategies were supplied for {1} zones. Supply one value to apply to every zone, or exactly one per zone.", ventilationStrategies.Count, zones.Count));
                    return;
                }

                Dictionary<Guid, string> dictionary_VentilationStrategy = [];
                for (int i = 0; i < zones.Count; i++)
                {
                    if (zones[i] != null)
                    {
                        dictionary_VentilationStrategy[zones[i].Guid] = ventilationStrategies.Count == 1 ? ventilationStrategies[0] : ventilationStrategies[i];
                    }
                }

                overheatingScenarios = Create.OverheatingScenarios(zones, partOIteration, dictionary_VentilationStrategy, out List<string> refusals_Scenarios);

                refusals.AddRange(refusals_Scenarios);

                foreach (string refusal in refusals_Scenarios)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, refusal);
                }
            }

            List<Space> spaces = analyticalModel_Applied.GetSpaces() ?? [];

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, new GooAnalyticalModel(analyticalModel_Applied));
            }

            index = Params.IndexOfOutputParam("overheatingScenarios");
            if (index != -1)
            {
                dataAccess.SetDataList(index, overheatingScenarios.ConvertAll(x => new GooAnalyticalObject(x)));
            }

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces.ConvertAll(x => new GooSpace(x)));
            }

            index = Params.IndexOfOutputParam("supply m3/s");
            if (index != -1)
            {
                //Read back through the query the SIMULATION uses, not off the value that was written - so what is
                //reported here is what the export will see.
                dataAccess.SetDataList(index, spaces.ConvertAll(x => Rounded(Analytical.Query.CalculatedSupplyAirFlow(x))));
            }

            index = Params.IndexOfOutputParam("extract m3/s");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces.ConvertAll(x => x?.InternalCondition != null && x.InternalCondition.TryGetValue(InternalConditionParameter.ExhaustAirFlow, out double value) ? Rounded(value) : 0));
            }

            index = Params.IndexOfOutputParam("partF l/s");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces.ConvertAll(x => x?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.ContinuousDesignFlowRate_Lps ?? 0));
            }

            index = Params.IndexOfOutputParam("notes");
            if (index != -1)
            {
                dataAccess.SetDataList(index, notes);
            }

            index = Params.IndexOfOutputParam("refusals");
            if (index != -1)
            {
                dataAccess.SetDataList(index, refusals);
            }

            if (index_Successful != -1)
            {
                dataAccess.SetData(index_Successful, refusals.Count == 0);
            }
        }

        /// <summary>NaN would surface in Grasshopper as a null item rather than a number; zero is honest here.</summary>
        private static double Rounded(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
        }
    }
}
