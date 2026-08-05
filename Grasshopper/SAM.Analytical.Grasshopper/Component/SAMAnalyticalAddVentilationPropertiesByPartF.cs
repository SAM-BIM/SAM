// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalAddVentilationPropertiesByPartF : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("644bf0f8-ea02-4ea3-aa03-5b7579d7ce38");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.5";

        private const string description =
            "SUMMARY\n" +
            "Calculates the mechanical ventilation air flow rates for a NEW dwelling to Approved Document F - Ventilation, Volume 1: Dwellings (2021 edition, for use in England, in effect from 15 June 2022) and writes them onto each space in the SAM AnalyticalModel. Room uses are recognised through the shared space-use classification, the whole-dwelling continuous design rate is established, and that rate is distributed as supply to the habitable rooms and extract to the wet rooms. A setback operating rate is written alongside each continuous design rate. The component sizes either one house or every dwelling in a block independently.\n" +
            "\n" +
            "INPUTS\n" +
            "_analyticalModel - AnalyticalModel. Required, one model. Each space should carry a positive Area in m2 and Volume in m3, and be named so the shared space-use classification recognises it.\n" +
            "zoneCategoryName_ - text, optional, no default. Zone category containing zones that represent individual flats or dwellings. When supplied, explicitly identified dwelling zones are processed independently. Leave empty when the complete AnalyticalModel represents one house.\n" +
            "setbackFlowRateFactor_ - number, optional, default 0.30, valid range greater than 0 and no greater than 1. Setback operating-rate factor applied to the continuous design flow rates.\n" +
            "\n" +
            "OUTPUTS\n" +
            "analyticalModel - AnalyticalModel, one item. An updated copy; the supplied model is never modified. Every classified space carries a Part F Space Data parameter holding its room category, shared space use, supply or extract role, Table 1.2 minimum extract rate, continuous design flow rate and setback flow rate, plus a Space Semantics parameter recording what the space is and how it was recognised.\n" +
            "spaces - Space, list. Every space in the updated model, classified or not.\n" +
            "l/s - number, list, litres per second, matching the spaces output item for item. The CONTINUOUS DESIGN flow rate: the Approved Document F sizing condition. Select equipment on this value. Circulation, storage, plant rooms, voids, unclassified spaces, spaces excluded as not part of a dwelling and spaces outside the selected dwelling zones all report 0.\n" +
            "setback l/s - number, list, litres per second, matching the spaces output item for item. The SETBACK flow rate, i.e. the continuous design rate multiplied by setbackFlowRateFactor_.\n" +
            "\n" +
            "PART F BASIS\n" +
            "Approved Document F - Ventilation, Volume 1: Dwellings, 2021 edition, for use in England, in effect from 15 June 2022. Requirement F1: Means of ventilation. This is the edition currently in force.\n" +
            "Scope: NEW DWELLINGS ONLY. Section 3 of the Approved Document, covering work on existing dwellings, is not implemented and there is no new/existing dwelling switch.\n" +
            "Habitable-room count (Appendix A, page 36). A habitable room is a room used for dwelling purposes but not SOLELY a kitchen, utility room, bathroom, cellar or sanitary accommodation. A studio, an open-plan living-kitchen, a bedroom, a living room and a study are therefore habitable. A bathroom, ensuite, utility room, sanitary accommodation, circulation space, store, plant room or void is NOT habitable and never increases the count. A room that is solely a kitchen is also not habitable.\n" +
            "Bedroom count (Table 1.3, page 10). Every space classified as a bedroom counts as one. A studio counts as one bedroom equivalent, because it combines sleeping, living and cooking in a single room.\n" +
            "One-habitable-room rate (Table 1.3 note 1, page 10). Where a dwelling contains exactly one habitable room, the component applies the 13 l/s minimum from Approved Document F Table 1.3 note 1. The final continuous design rate may be higher where the floor-area rate or extract requirements govern. This REPLACES the Table 1.3 bedroom rate, so a studio with a separate bathroom is sized from 13 l/s rather than the one-bedroom figure of 19 l/s. Adding any second habitable room, such as a separate living room or a study, returns the dwelling to Table 1.3.\n" +
            "Bedroom-based rate (Table 1.3, page 10). 19, 25, 31, 37 and 43 l/s for one to five bedrooms, plus 6 l/s for each further bedroom (note 2).\n" +
            "Internal floor area (paragraph 1.24a, page 10). 0.3 l/s for every square metre of internal floor area across all floors. Areas of all classified rooms are summed, except voids, open-to-below spaces and communal circulation.\n" +
            "Wet-room extract (Table 1.2 high rate column, page 10, and paragraph 1.70, page 17). Each wet room receives at least its minimum: kitchen 13 l/s, utility room 8 l/s, bathroom 8 l/s, ensuite 8 l/s, sanitary accommodation 6 l/s.\n" +
            "Governing-rate precedence (paragraph 1.24 and paragraph 1.69, pages 10 and 16). The continuous design rate is the GREATEST of the bedroom-or-one-habitable-room rate, the floor-area rate, and the total of the wet-room minimums.\n" +
            "Supply distribution (paragraph 1.67, page 16). The full continuous design rate is distributed between the habitable rooms in proportion to their volume.\n" +
            "Balance. Total supply and total extract each equal the continuous design rate, and each equal the setback rate at the setback condition. Each wet room retains at least its Table 1.2 minimum, and the balance above the minimums is distributed between the wet rooms in proportion to volume.\n" +
            "All rates and room categories are read from the settings file SAM_PartFSpaceRulesUKDwellingsMVHR.json and may be edited there.\n" +
            "\n" +
            "DWELLING GROUPING\n" +
            "Single house. Leave zoneCategoryName_ empty. The complete model is treated as one new dwelling: one habitable-room count, one bedroom count, one internal floor area, one continuous design rate, one setback rate, wet-room requirements assessed across the whole house, and supply and extract balanced across the whole house. No zone and no Is Dwelling flag is required, and an empty input is never reported as a problem. An unzoned model is never assumed to contain more than one dwelling.\n" +
            "Block of dwellings. Place each dwelling in its own zone, give those zones a shared zone category, and enter that category in zoneCategoryName_. Each included zone is sized as one independent dwelling, with its own habitable-room count, bedroom count, floor area, continuous design and setback rates, wet-room requirements and supply/extract balance. Results are written only to the spaces of that dwelling, so one flat can never affect another.\n" +
            "Is Dwelling filtering. A zone category alone does not identify a dwelling, because a shared corridor, a landlord area or a commercial unit can sit in the same category as the dwellings it serves. Set the Is Dwelling parameter on each zone. Is Dwelling = true is processed as a dwelling. Is Dwelling = false is NEVER processed as a dwelling. Where some zones in the category carry the parameter and others do not, only the explicit true zones are processed and the remainder are reported. Where no zone in the category carries the parameter at all, the previous category-only behaviour is preserved for compatibility and a warning recommends setting explicit flags.\n" +
            "Spaces outside the dwelling zones, such as shared corridors, stairs, plant and other landlord areas, are listed in a warning and given no ventilation properties. Size those to Approved Document F, Volume 2: Buildings other than dwellings.\n" +
            "A space placed in two dwelling zones is sized once for each and only the last result is kept; this is reported.\n" +
            "\n" +
            "SPACE CLASSIFICATION\n" +
            "Room uses are recognised through the shared semantic space-use classification, which describes WHAT a space is independently of the standard assessing it. The same classification is consumed by this Part F calculation and is available to Approved Document O and CIBSE TM59, so a space can be classified once and reused. The vocabulary is the settings file SAM_SpaceUseTextMap.json, merged with any Synonyms in the Part F rule set.\n" +
            "Recognised space uses: bedroom, studio, living room, open-plan living-kitchen, kitchen, bathroom, ensuite, utility room, sanitary accommodation, circulation, communal circulation, storage, plant room, void and non-dwelling.\n" +
            "Classification precedence, highest first: 1. an explicit Space Use Override on the space; 2. an explicit stored space-use classification from a previous mapping; 3. deterministic classification from the space name, by exact configured alias and then by whole-word or whole-phrase match, longest phrase first; 4. classification derived from the space InternalCondition; 5. unclassified.\n" +
            "There is NO unrestricted substring matching. An alias only matches as a whole token or a whole contiguous phrase, so a space is never classified because its name merely contains a fragment of another room name - Server Room is not classified as a living room. Normalisation is case-insensitive, trims whitespace, and treats spaces, underscores and hyphens consistently, and a single trailing room number is ignored.\n" +
            "Conflicts and overrides. An InternalCondition is frequently a bulk-assigned thermal template, so it must not silently redefine a clearly named bathroom, ensuite or corridor as a studio. Where the name-derived and InternalCondition-derived classifications disagree, the higher-priority result (the space name) is used, BOTH source values are preserved, and the conflict is reported and shown in the SAM_UI mapping dialog. Neither source is overwritten. A Space Use Override forces either answer.\n" +
            "Unclassified spaces. A space that matches nothing, or whose name matches two space uses equally well, is reported and left out of the dwelling entirely - out of the habitable-room count, the bedroom count, the internal floor area and the flow rates. Review and correct uncertain mappings in the SAM_UI internal-condition mapping dialog, which shows each space proposed classification, its semantic flags, the classification source, the matching method, any conflict, and whether its zone is a dwelling.\n" +
            "\n" +
            "CONTINUOUS DESIGN RATE\n" +
            "The continuous design flow rate is the Approved Document F sizing condition used by this calculation. Every applicable minimum implemented here is established at this condition before any setback value is derived.\n" +
            "Separate values are maintained for the whole-dwelling continuous design flow, each room continuous design supply flow, and each room continuous design extract flow.\n" +
            "Equipment should be selected on the continuous design rate.\n" +
            "\n" +
            "SETBACK RATE\n" +
            "The default setback flow rate is 30% of the calculated continuous design flow rate. The continuous design rate remains unchanged and is retained as the regulatory sizing condition.\n" +
            "Setback flow rate = continuous design flow rate x setbackFlowRateFactor_, default 0.30, equivalent to a 70% reduction.\n" +
            "Separate setback values are maintained for the whole dwelling and for each room supply and extract flow. Setback supply and setback extract totals remain balanced.\n" +
            "The setback rate is a SAM reduced-operation convention, NOT a regulatory rate. Neither the 2021 nor the 2026 edition of Approved Document F specifies a reduced operating rate for mechanical ventilation with heat recovery. It is deliberately called a setback rate rather than a background rate, because in Approved Document F a background ventilator is a trickle ventilator and whole dwelling (general) ventilation is the continuous requirement.\n" +
            "Setback rates are not checked against the Table 1.2 minimums, which apply at the continuous design condition.\n" +
            "The factor must be greater than 0 and no greater than 1. Zero, a negative value, a value above 1, or a value that is not a number is rejected, the default is used instead, and a warning is given.\n" +
            "\n" +
            "SUPPORTED SYSTEMS\n" +
            "Mechanical ventilation with heat recovery (paragraphs 1.67 to 1.73) ONLY. This is the only system the implemented calculation sizes.\n" +
            "Natural ventilation with background ventilators and intermittent extract fans (Table 1.1 and paragraphs 1.47 to 1.59) is NOT implemented.\n" +
            "Continuous mechanical extract ventilation (paragraphs 1.60 to 1.66) is NOT implemented.\n" +
            "\n" +
            "LIMITATIONS\n" +
            "England only. Wales, Scotland and Northern Ireland have their own ventilation guidance.\n" +
            "New dwellings only. Section 3, work on existing dwellings, is not assessed.\n" +
            "Local kitchen extract. A studio and an open-plan living-kitchen are habitable rooms and are given mechanical supply, so they receive no extract from this component. Approved Document F paragraph 1.17a and Table 1.2 still require extract ventilation of at least 13 l/s from the room containing the cooking function. SAM assigns one terminal role per space and has no cooker-hood or other local cooking-space extract concept, so that extract cannot be represented here. This is a deliberate SAM design assumption.\n" +
            "The limitation is specifically the absence of an explicitly modelled LOCAL kitchen or cooker extract, not the absence of general dwelling extract. A cooking space counts as having explicit local kitchen extract only where that space itself takes an extract terminal - a room classified as solely a kitchen, carrying the Table 1.2 kitchen rate. A studio or open-plan living-kitchen never does.\n" +
            "Extract from a bathroom, ensuite, utility room or sanitary accommodation may balance the dwelling airflow, but it is NOT evidence of local kitchen extract and does not suppress the warning. A studio or living-kitchen therefore raises ENGINEERING CHECK REQUIRED even where a bathroom or ensuite provides the dwelling's general extract. The warning never claims the dwelling has no extract terminal; a dwelling with no extract terminal at all is reported by a separate paragraph 1.17 warning.\n" +
            "Local kitchen-extract compliance may require separate modelling or engineering verification, and this component does not by itself demonstrate it.\n" +
            "Not assessed: purge ventilation (paragraphs 1.26 to 1.31 and Table 1.4), background ventilators and their equivalent areas (Table 1.7 and paragraph 1.64), internal door undercuts (paragraph 1.25), air permeability and airtightness (Table 1.6), basements (paragraphs 1.38 to 1.41), internal rooms ventilated through another room (paragraphs 1.42 to 1.44), ductwork, noise, controls and installation (paragraphs 1.5 to 1.8 and 1.33 to 1.83), outdoor air quality (Section 2), and commissioning (Section 4).\n" +
            "Rates are not rounded. Round them when scheduling equipment.\n" +
            "A 2026 edition of Approved Document F, Volume 1 has been published, taking effect on 24 March 2027 (24 September 2027 for building work in connection with higher-risk building work). This component implements the 2021 edition only, and requirements from the two editions are not combined. The 2026 edition leaves Table 1.2, Table 1.3 including note 1, the 0.3 l/s per m2 rate and MVHR paragraphs 1.67, 1.69 and 1.70 unchanged; it renumbers the extract-provision paragraph from 1.17 to 1.13 and adds explicit cooker-hood guidance.\n" +
            "\n" +
            "NOTES\n" +
            "Areas are in square metres, volumes in cubic metres and all flow rates in litres per second. No unit conversion is applied.\n" +
            "Areas and volumes should be positive. A dwelling with no floor area, or supply rooms with no volume, is reported and no rate is invented. No flow rate is ever returned as NaN or infinity.\n" +
            "Warnings identify the dwelling and the room wherever possible so they can be traced back to the model.\n" +
            "The supplied AnalyticalModel is never modified; an updated copy is returned.\n" +
            "Using this component does not by itself demonstrate or guarantee compliance with Building Regulations Part F. Results must be checked by a suitably qualified engineer against the full Approved Document.\n" +
            "\n" +
            "EXAMPLE\n" +
            "Single house: AnalyticalModel to AddVentilationPropertiesByPartF to updated AnalyticalModel. Leave zoneCategoryName_ empty; no zone is required. The complete model is treated as one new dwelling. A three-bedroom house of 90 m2 with a living room, kitchen, bathroom and WC has four habitable rooms, so Table 1.3 note 1 does not apply: the bedroom-based rate is 31 l/s and the floor-area rate is 0.3 x 90 = 27 l/s. The wet-room minimums total 13 + 8 + 6 = 27 l/s. The continuous design rate is therefore 31 l/s, distributed as supply between the living room and the three bedrooms in proportion to volume, with the 4 l/s above the wet-room minimums distributed between the kitchen, bathroom and WC. At the default factor the setback rate is 30% of continuous design, so 9.3 l/s. The original model remains unchanged and an updated copy is returned.\n" +
            "Multiple flats: AnalyticalModel with dwelling zones plus zoneCategoryName_ to AddVentilationPropertiesByPartF to updated AnalyticalModel. In the example model SAM_zoningAM.sam the zones are Flat 1, Corridor, Flat 2 and Flat 3, all in the zone category Flats, so enter Flats into zoneCategoryName_. Set Is Dwelling = true on Flat 1, Flat 2 and Flat 3 and Is Dwelling = false on Corridor; the communal corridor is then excluded and reported instead of being sized as a dwelling, and no flat is affected. Each flat is processed independently. Flat 1 holds Studio 1_0 and Bathroom_2: one habitable room, so Table 1.3 note 1 gives 13 l/s, against a floor-area rate of 0.3 x 100 = 30 l/s and a wet-room minimum of 8 l/s, so the continuous design rate is 30 l/s with a setback rate of 9 l/s. Flat 2 holds Bedroom 2_3, Kitchen_4 and Ensuite_5: one habitable room, so note 1 gives 13 l/s, against 0.3 x 210 = 63 l/s and wet-room minimums of 13 + 8 = 21 l/s, so the continuous design rate is 63 l/s with a setback rate of 18.9 l/s. Flat 3 is calculated entirely separately from the same starting model. Results are written only to the spaces of the relevant flat, and the input model remains unchanged.\n";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalAddVentilationPropertiesByPartF()
          : base("SAMAnalytical.AddVentilationPropertiesByPartF", "SAMAnalytical.AddVentilationPropertiesByPartF",
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
                List<GH_SAMParam> result =
                [
                    new GH_SAMParam(new GooAnalyticalModelParam() { Name = "_analyticalModel", NickName = "_analyticalModel", Description = "SAM AnalyticalModel. REQUIRED, one item. No default.\n\nApplies to NEW dwellings in England only.\n\nEach space should carry an Area parameter in m2 and a Volume parameter in m3, both positive, and be named so the shared space use classification recognises it (bedroom, studio, living room, open plan living kitchen, kitchen, bathroom, ensuite, utility room, sanitary accommodation, circulation, communal circulation, storage, plant room, void, non-dwelling).\n\nPRECEDENCE for each space: an explicit Space Use Override wins; then a stored space use classification; then the space name by exact alias and whole word or whole phrase match; then the space InternalCondition. Matching never uses partial text, so a name is not classified because it merely contains a fragment of another room name.\n\nEFFECT: a space that cannot be classified, or whose name matches two space uses equally well, is reported and left out of the dwelling entirely - out of the habitable room count, the bedroom count, the internal floor area and the flow rates.\n\nThe model you supply is not modified; an updated copy is returned on the analyticalModel output.", Access = GH_ParamAccess.item }, ParamVisibility.Binding),
                    new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "zoneCategoryName_", NickName = "zoneCategoryName_", Description = "Optional zone category containing zones that represent individual flats or dwellings. When supplied, explicitly identified dwelling zones are processed independently. Leave empty when the complete AnalyticalModel represents one house.\n\nText, one item, OPTIONAL, no default.\n\nEMPTY: the complete model is sized as one new dwelling - one habitable room count, one bedroom count, one internal floor area, one continuous design rate, one setback rate, and supply and extract balanced across the whole house. No zone and no Is Dwelling flag is required, and an empty input is never reported as a problem.\n\nSUPPLIED: each dwelling zone in the category is sized independently. A zone category alone does not identify a dwelling, so zones are filtered on their Is Dwelling parameter - Is Dwelling = true is processed; Is Dwelling = false is NEVER processed; where some zones carry the parameter and others do not, only the explicit true zones are processed and the rest are reported; where no zone in the category carries it at all, the previous category only behaviour is preserved and a compatibility warning recommends explicit flags.\n\nThe name is case sensitive and must match the Zone Category parameter exactly; if nothing matches, the categories present in the model are listed in a warning. Spaces outside the dwelling zones are given no ventilation properties and are listed in a warning.\n\nIn the example model SAM_zoningAM.sam the category is Flats, and that model shared Corridor zone is also in it - mark the corridor Is Dwelling = false to exclude it.", Access = GH_ParamAccess.item, Optional = true}, ParamVisibility.Binding),
                ];

                global::Grasshopper.Kernel.Parameters.Param_Number number = new() { Name = "setbackFlowRateFactor_", NickName = "setbackFlowRateFactor_", Description = "Optional setback operating-rate factor applied to the continuous design flow rates. Default: 0.30, meaning the setback rate is 30% of continuous design. Valid range: greater than 0 and no greater than 1.\n\nNumber, dimensionless, one item, OPTIONAL.\n\nSetback flow rate = continuous design flow rate x factor. A factor of 0.30 is equivalent to a 70% reduction.\n\nThe continuous design flow rate is the Approved Document F sizing condition and is NEVER changed by this factor - every applicable minimum (the bedroom or one habitable room rate, the floor area rate, and the total of the wet room minimums) is established at the continuous design condition first, and the factor is applied only afterwards. Setback rates are not checked against the Table 1.2 minimums.\n\nThis is a SAM reduced-operation convention, not a regulatory rate: neither the 2021 nor the 2026 edition of Approved Document F specifies a reduced operating rate for mechanical ventilation with heat recovery.\n\nZero, a negative value, a value above 1, or a value that is not a number is rejected, the documented default is used instead, and a warning is given.", Access = GH_ParamAccess.item, Optional = true };
                number.SetPersistentData(PartFData.DefaultSetbackFlowRateFactor);
                result.Add(new GH_SAMParam(number, ParamVisibility.Voluntary));

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
                return
                [
                    new GH_SAMParam(new GooAnalyticalModelParam { Name = "analyticalModel", NickName = "analyticalModel", Description = "Updated copy of the AnalyticalModel. AnalyticalModel, one item. The supplied model is left unchanged.\n\nEvery classified space carries a Part F Space Data parameter holding its Approved Document F room category, its shared space use, whether it is supply or extract, its Table 1.2 minimum extract rate in l/s, its CONTINUOUS DESIGN flow rate in l/s and its SETBACK flow rate in l/s.\n\nEvery space the shared classification resolved also carries a Space Semantics parameter recording what the space is, its independent semantic roles, the classification source, the matched alias, both the name derived and InternalCondition derived space uses, and whether those two conflict. Approved Document O and CIBSE TM59 can read the same classification, so a space is classified once and reused.\n\nSpaces that could not be classified, spaces outside the selected dwelling zones, and spaces excluded as not part of any dwelling are returned without ventilation properties.", Access = GH_ParamAccess.item }, ParamVisibility.Binding),
                    new GH_SAMParam(new GooSpaceParam() { Name = "spaces", NickName = "spaces", Description = "Spaces of the updated model. Space, list. Includes every space in the model, whether or not it was classified or sized. Use this list to align the l/s and setback l/s outputs.", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "l/s", NickName = "l/s", Description = "CONTINUOUS DESIGN ventilation flow rate per space. Number, list, litres per second, matching the spaces output item for item.\n\nThis is the Approved Document F sizing condition - select equipment on this rate. It is the greatest of the bedroom or one habitable room rate, the floor area rate, and the total of the wet room minimums, distributed to the rooms.\n\nHabitable rooms (studio, open plan living kitchen, bedroom, living room) report their supply rate; wet rooms (bathroom, ensuite, utility room, sanitary accommodation, kitchen) report their extract rate. Circulation, storage, plant rooms, voids, unclassified rooms, spaces excluded as not part of any dwelling and spaces outside the selected dwelling zones all report 0.\n\nValues are not rounded, and are never NaN or infinity.", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "setback l/s", NickName = "setback l/s", Description = "SETBACK ventilation flow rate per space. Number, list, litres per second, matching the spaces output item for item.\n\nThe continuous design rate multiplied by setbackFlowRateFactor_ (0.30 by default, so 30% of continuous design).\n\nA SAM reduced-operation convention, not a sizing condition. It does not reduce or replace the continuous design calculation, and it is not checked against the Table 1.2 minimums. Total setback supply and total setback extract balance each other, exactly as the continuous design rates do.\n\nValues are not rounded, and are never NaN or infinity.", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                ];
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            index = Params.IndexOfInputParam("_analyticalModel");
            AnalyticalModel analyticalModel = null;
            if (!dataAccess.GetData(index, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if(adjacencyCluster is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            PartFCalculator partFCalculator = Analytical.Query.DefaultPartFCalculator();
            if(partFCalculator is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not load PartF Calculator");
                return;
            }

            index = Params.IndexOfInputParam("zoneCategoryName_");
            string zoneCategoryName = null;
            if (index != -1)
            {
                dataAccess.GetData(index, ref zoneCategoryName);
            }

            index = Params.IndexOfInputParam("setbackFlowRateFactor_");
            double setbackFlowRateFactor = PartFData.DefaultSetbackFlowRateFactor;
            if (index != -1)
            {
                double setbackFlowRateFactor_Input = double.NaN;
                if (dataAccess.GetData(index, ref setbackFlowRateFactor_Input))
                {
                    if (PartFData.IsValidSetbackFlowRateFactor(setbackFlowRateFactor_Input))
                    {
                        setbackFlowRateFactor = setbackFlowRateFactor_Input;
                    }
                    else
                    {
                        //Reported rather than silently substituted: a factor above 1 would give a
                        //setback rate above the continuous design rate, and NaN would poison every rate.
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("setbackFlowRateFactor_ must be greater than 0 and no greater than 1. '{0}' was ignored and the default {1} used instead.", setbackFlowRateFactor_Input, PartFData.DefaultSetbackFlowRateFactor));
                    }
                }
            }

            partFCalculator.AdjacencyCluster = adjacencyCluster;

            //Set on the calculator rather than on the rule set, so the shared default PartFData held by
            //ActiveSetting is not mutated for the rest of the session.
            partFCalculator.SetbackFlowRateFactor = setbackFlowRateFactor;

            //An empty zoneCategoryName_ means the whole model is one dwelling. That is the normal
            //single house workflow and is not reported as a problem.
            partFCalculator.Calculate(zoneCategoryName);

            analyticalModel = new AnalyticalModel(analyticalModel, partFCalculator.AdjacencyCluster);

            foreach (string remark in partFCalculator.Remarks)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, remark);
            }

            foreach (string warning in partFCalculator.Warnings)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, warning);
            }

            List<Space> spaces = analyticalModel.GetSpaces();

            index = Params.IndexOfOutputParam("analyticalModel");
            if (index != -1)
            {
                dataAccess.SetData(index, analyticalModel);
            }

            index = Params.IndexOfOutputParam("spaces");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces);
            }

            index = Params.IndexOfOutputParam("l/s");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces?.ConvertAll(x => x?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.ContinuousDesignFlowRate_Lps ?? 0));
            }

            index = Params.IndexOfOutputParam("setback l/s");
            if (index != -1)
            {
                dataAccess.SetDataList(index, spaces?.ConvertAll(x => x?.GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData)?.SetbackFlowRate_Lps ?? 0));
            }
        }
    }
}
