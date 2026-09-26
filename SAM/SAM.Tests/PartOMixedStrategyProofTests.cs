// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace SAM.Tests
{
    /// <summary>
    /// <b>PR0 investigation evidence - mixed Part O dwelling strategies. Not production pins.</b>
    /// <para>
    /// These tests record what the CURRENT <c>Modify.PreparePartOIteration</c> does when it is called once per
    /// dwelling, in sequence, over one model - the only way today's API can be driven towards a model in
    /// which Flat 1 is naturally ventilated and Flats 2 and 3 are MVHR. They assert observed behaviour,
    /// including behaviour the PR0 report classifies as a defect, so each passing test is a fact the report
    /// cites. See <c>documentation/PartO-MixedDwellingStrategies-PR0.md</c>.
    /// </para>
    /// <para>
    /// The fixture is three Part-F-sized dwellings (Flat 1 from the calculator, Flats 2 and 3 authored) and
    /// a communal corridor zone (<c>IsDwelling = false</c>) adjacent to two of them. State is compared by a
    /// GUID-insensitive structural signature labelled by DWELLING rather than by object name, because the
    /// generic unit names (<c>MVHR-01</c>, <c>MVHR-02</c>) are assigned in call order.
    /// </para>
    /// </summary>
    [Trait("Category", "PR0Investigation")]
    public class PartOMixedStrategyProofTests
    {
        private const string Flat1 = "Flat 1";
        private const string Flat2 = "Flat 2";
        private const string Flat3 = "Flat 3";
        private const string Corridor = "Corridor";

        private readonly ITestOutputHelper output;

        public PartOMixedStrategyProofTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        // -------------------------------------------------------------------------------------------------
        // P0. One call cannot carry two routes (existing behaviour, restated over this fixture).
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P0_OneCallWithMixedRoutes_IsRefusedAndReturnsNoModel()
        {
            AnalyticalModel baseline = Baseline();

            PartOIterationPreparation preparation = baseline.PreparePartOIteration(
                PartOIteration.BasePassive,
                Zones(baseline, Flat1, Flat2),
                new Dictionary<Guid, string> { { Zone(baseline, Flat1).Guid, "NV" }, { Zone(baseline, Flat2).Guid, "MVRE" } });

            Assert.NotNull(preparation.Refusal);
            Assert.Null(preparation.AnalyticalModel);
            output.WriteLine(preparation.Refusal);
        }

        // -------------------------------------------------------------------------------------------------
        // P1. A = MVHR, then B = NV on the output. A must be intact; what happens to B is recorded.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P1_MvhrThenNv_LeavesTheMvhrDwellingIntact_ButTheNvDwellingCarriesPartFRates()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel model_A = Prepare(baseline, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;
            PartOIterationPreparation preparation_B = Prepare(model_A, PartOIteration.BaseNaturalVentilation, "NV", null, Flat2);
            AnalyticalModel model_AB = preparation_B.AnalyticalModel;

            //Isolation of A: B's NV preparation is a plain copy, so A's system, unit, terminals, movements and
            //internal-condition rates are exactly what A's own preparation produced.
            Assert.Equal(Signature(model_A, Flat1), Signature(model_AB, Flat1));

            //FACT (defect): A's whole-model Part F application wrote System-4 supply/extract onto B's
            //internal conditions, and whole-model terminal realisation gave B design terminals - B was never
            //stated MVHR. Nothing connects them (no system, no air movement), and B's NV preparation neither
            //reports nor removes them.
            List<Space> spaces_B = Spaces(model_AB, Flat2);
            Assert.All(spaces_B, x => Assert.True(x.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double _)));
            Assert.NotEmpty(Terminals(model_AB, Flat2));
            Assert.Empty(SystemsServing(model_AB, Flat2));
            Assert.Empty(AirMovementsTouching(model_AB, Flat2));

            //Before A was prepared, B carried none of that.
            Assert.All(Spaces(baseline, Flat2), x => Assert.False(x.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double _)));
            Assert.Empty(Terminals(baseline, Flat2));

            //B's scenario is attributed to the NV base iteration over Flat 2 only.
            Assert.Single(preparation_B.OverheatingScenarios);
            Assert.Equal(PartOIteration.BaseNaturalVentilation, preparation_B.OverheatingScenarios[0].Iteration);

            Dump(model_AB);
        }

        // -------------------------------------------------------------------------------------------------
        // P2. Order dependence: A MVHR -> B NV -> C MVHR  versus  C MVHR -> A MVHR -> B NV.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P2_ReorderedIndependentOperations_GiveTheSameEngineeringState_ButDifferentUnitNames()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel model_X = Chain(baseline, (Flat1, "MVRE"), (Flat2, "NV"), (Flat3, "MVRE"));
            AnalyticalModel model_Y = Chain(baseline, (Flat3, "MVRE"), (Flat1, "MVRE"), (Flat2, "NV"));

            List<string> signature_X = Signature(model_X);
            List<string> signature_Y = Signature(model_Y);
            signature_X.Except(signature_Y).ToList().ForEach(x => output.WriteLine("only X: " + x));
            signature_Y.Except(signature_X).ToList().ForEach(x => output.WriteLine("only Y: " + x));

            Assert.Equal(signature_X, signature_Y);

            //FACT: generic unit names are handed out in call order, so the same dwelling's unit is named
            //differently depending on the order. The name is also the system -> unit link
            //(VentilationSystemParameter.SupplyUnitName), so it is identity-bearing, not cosmetic.
            Dictionary<string, string> names_X = UnitNamesByDwelling(model_X);
            Dictionary<string, string> names_Y = UnitNamesByDwelling(model_Y);
            output.WriteLine("X: " + string.Join(", ", names_X.Select(x => x.Key + "=" + x.Value)));
            output.WriteLine("Y: " + string.Join(", ", names_Y.Select(x => x.Key + "=" + x.Value)));
            Assert.NotEqual(names_X[Flat1], names_Y[Flat1]);

            //FACT: the order-dependent name also leaks into the unit movement's control profile names
            //("MVHR-01 Humidification"), while their values are identical in both orders.
            string ProfileName_Flat1(AnalyticalModel analyticalModel)
            {
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                AirHandlingUnitAirMovement airHandlingUnitAirMovement = adjacencyCluster.GetRelatedObjects<AirHandlingUnitAirMovement>(UnitOf(analyticalModel, Flat1)).Single();
                return airHandlingUnitAirMovement.Humidification?.Name;
            }

            output.WriteLine("Flat 1 humidification profile: X '" + ProfileName_Flat1(model_X) + "', Y '" + ProfileName_Flat1(model_Y) + "'");
            Assert.NotEqual(ProfileName_Flat1(model_X), ProfileName_Flat1(model_Y));
        }

        // -------------------------------------------------------------------------------------------------
        // P3. Different products: A auto-selected, C manually assigned another product.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P3_DifferentProductsPerDwelling_AreIndependent_UntilAnAutomaticCallCoversBoth()
        {
            AnalyticalModel baseline = Baseline();

            VentilationUnitCapacityDescriptor small = new(new VentilationUnitReference("Proof", "Small", null), 500, 500);
            VentilationUnitCapacityDescriptor large = new(new VentilationUnitReference("Proof", "Large", null), 1000, 1000);
            List<VentilationUnitCapacityDescriptor> catalogue = [small, large];

            AnalyticalModel model_A = Prepare(baseline, PartOIteration.BasePassive, "MVRE", catalogue, Flat1).AnalyticalModel;
            Assert.Equal("Small", UnitOf(model_A, Flat1).SelectedVentilationUnitReference()?.Model);

            AnalyticalModel model_AC = Prepare(model_A, PartOIteration.BasePassive, "MVRE", null, Flat3).AnalyticalModel;

            AdjacencyCluster adjacencyCluster = model_AC.AdjacencyCluster;
            Assert.True(adjacencyCluster.AssignVentilationUnit(UnitOf(model_AC, Flat3), large.VentilationUnitReference, out _, out List<string> refusals_Assign), string.Join(" ", refusals_Assign));
            model_AC = new AnalyticalModel(model_AC, adjacencyCluster);

            //Each dwelling holds its own product on its own unit.
            Assert.Equal("Small", UnitOf(model_AC, Flat1).SelectedVentilationUnitReference()?.Model);
            Assert.Equal("Large", UnitOf(model_AC, Flat3).SelectedVentilationUnitReference()?.Model);

            //Scoped re-preparation of A with the catalogue does not touch C's manual product.
            AnalyticalModel model_A2 = Prepare(model_AC, PartOIteration.BasePassive, "MVRE", catalogue, Flat1).AnalyticalModel;
            Assert.Equal("Large", UnitOf(model_A2, Flat3).SelectedVentilationUnitReference()?.Model);

            //FACT: an automatic call whose scope covers C re-selects C's product, silently replacing the
            //designer's manual choice - selection mode is a per-CALL fact, not a per-dwelling one.
            AnalyticalModel model_Both = Prepare(model_AC, PartOIteration.BasePassive, "MVRE", catalogue, Flat1, Flat3).AnalyticalModel;
            Assert.Equal("Small", UnitOf(model_Both, Flat3).SelectedVentilationUnitReference()?.Model);
        }

        // -------------------------------------------------------------------------------------------------
        // P4. Switching one dwelling MVHR -> NV. There is no dematerialisation path.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P4_ReStatingAnMvhrDwellingAsNv_KeepsItsWholeMechanicalDesign()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel model_Mvhr = Prepare(baseline, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;
            PartOIterationPreparation preparation_Nv = Prepare(model_Mvhr, PartOIteration.BaseNaturalVentilation, "NV", null, Flat1);

            //FACT (blocker): the NV preparation succeeds and returns the MVHR design unchanged - system, unit,
            //terminals, directional and transfer movements, and the Part F internal-condition rates.
            Assert.Null(preparation_Nv.Refusal);
            Assert.Equal(Signature(model_Mvhr, Flat1), Signature(preparation_Nv.AnalyticalModel, Flat1));
            Assert.Single(SystemsServing(preparation_Nv.AnalyticalModel, Flat1));
            Assert.NotEmpty(AirMovementsTouching(preparation_Nv.AnalyticalModel, Flat1));

            //...while the scenario it states for the same dwelling says natural ventilation, which is the
            //criterion TM59 would assess the mechanically ventilated flat against.
            Assert.All(preparation_Nv.OverheatingScenarios, x => Assert.Equal(PartOIteration.BaseNaturalVentilation, x.Iteration));
            Assert.All(preparation_Nv.OverheatingScenarios, x => Assert.Equal("NV", x.SystemTemplate?.Ventilation));
        }

        // -------------------------------------------------------------------------------------------------
        // P5. A retained design airflow (a 2B outcome) on A survives preparing C, and re-preparing A.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P5_ARaisedDesignAirflow_SurvivesPreparingAnotherDwellingAndRePreparingItsOwn()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel model_A = Prepare(baseline, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;

            AdjacencyCluster adjacencyCluster = model_A.AdjacencyCluster;
            Space bedroom = adjacencyCluster.GetSpaces().Find(x => x.Name == "Bedroom 1");
            double supply_Before = SpaceDesignFlow(adjacencyCluster, bedroom, FlowClassification.Supply);
            double supply_Raised = supply_Before + 2.0;

            List<VentilationTerminal> changed = adjacencyCluster.SetSpaceDesignFlowRate(bedroom, FlowClassification.Supply, supply_Raised, out _, out List<string> refusals_Set);
            Assert.Empty(refusals_Set);
            Assert.NotEmpty(changed);
            AnalyticalModel model_Raised = new(model_A, adjacencyCluster);

            //Preparing C leaves A exactly as it was left - including its now-stale movements.
            AnalyticalModel model_RaisedC = Prepare(model_Raised, PartOIteration.BasePassive, "MVRE", null, Flat3).AnalyticalModel;
            Assert.Equal(Signature(model_Raised, Flat1), Signature(model_RaisedC, Flat1));

            //FACT: a raised supply on its own is refused on re-preparation - the dwelling no longer balances
            //and nothing is rescaled to make it. A retained design airflow is only valid as a BALANCED set
            //over the dwelling (which is what 2B's targeted evaluation produces), never a single value.
            PartOIterationPreparation preparation_Unbalanced = model_RaisedC.PreparePartOIteration(PartOIteration.BasePassive, Zones(model_RaisedC, Flat1), Strategies(model_RaisedC, "MVRE", Flat1));
            output.WriteLine(preparation_Unbalanced.Refusal ?? "(re-prepared)");
            Assert.NotNull(preparation_Unbalanced.Refusal);
            Assert.Contains("do not balance", preparation_Unbalanced.Refusal);

            adjacencyCluster = model_RaisedC.AdjacencyCluster;
            Space kitchen = adjacencyCluster.GetSpaces().Find(x => x.Name == "Kitchen");
            adjacencyCluster.SetSpaceDesignFlowRate(kitchen, FlowClassification.Extract, SpaceDesignFlow(adjacencyCluster, kitchen, FlowClassification.Extract) + 2.0, out _, out List<string> refusals_Extract);
            Assert.Empty(refusals_Extract);
            model_RaisedC = new AnalyticalModel(model_RaisedC, adjacencyCluster);

            //Re-preparing A with the balanced raise keeps both raised terminal values (terminals are
            //re-linked, never re-sized) and rebuilds A's movements from them.
            PartOIterationPreparation preparation_A2 = model_RaisedC.PreparePartOIteration(PartOIteration.BasePassive, Zones(model_RaisedC, Flat1), Strategies(model_RaisedC, "MVRE", Flat1));
            output.WriteLine(preparation_A2.Refusal ?? "(re-prepared)");
            Assert.Null(preparation_A2.Refusal);

            AdjacencyCluster adjacencyCluster_A2 = preparation_A2.AnalyticalModel.AdjacencyCluster;
            Space bedroom_A2 = adjacencyCluster_A2.GetSpaces().Find(x => x.Name == "Bedroom 1");
            Assert.Equal(supply_Raised, SpaceDesignFlow(adjacencyCluster_A2, bedroom_A2, FlowClassification.Supply), 6);

            //FACT: the Part F internal-condition rate is re-applied from the requirement, so after 2B the
            //two runtime-adjacent copies disagree: terminal = raised, internal condition = Part F.
            Assert.True(bedroom_A2.InternalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double supply_IC));
            output.WriteLine(string.Format(CultureInfo.InvariantCulture, "Bedroom 1: terminal {0} l/s, internal condition {1} l/s", supply_Raised, supply_IC * 1000));
            Assert.NotEqual(supply_Raised, supply_IC * 1000, 3);
        }

        // -------------------------------------------------------------------------------------------------
        // P6. Idempotence of re-preparing one dwelling.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P6_PreparingTheSameDwellingTwice_IsIdempotent()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel model_1 = Prepare(baseline, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;
            AnalyticalModel model_2 = Prepare(model_1, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;

            Assert.Equal(Signature(model_1), Signature(model_2));
            Assert.Single(SystemsServing(model_2, Flat1));
            Assert.Single(model_2.AdjacencyCluster.GetObjects<AirHandlingUnit>());
        }

        // -------------------------------------------------------------------------------------------------
        // P7. An authored system shared by two dwellings.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P7_AnAuthoredSharedSystem_IsWarnedAboutAndLeftAlone_AndRefusesIsolation()
        {
            AnalyticalModel baseline = Baseline();

            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            VentilationSystem shared = new("Legacy MV", new VentilationSystemType("MV", "Authored legacy mechanical ventilation"));
            adjacencyCluster.AddObject(shared);
            adjacencyCluster.AddRelation(shared, adjacencyCluster.GetSpaces().Find(x => x.Name == "Living Room"));
            adjacencyCluster.AddRelation(shared, adjacencyCluster.GetSpaces().Find(x => x.Name == "Flat 2 Bedroom"));
            AnalyticalModel model_Shared = new(baseline, adjacencyCluster);

            PartOIterationPreparation preparation = Prepare(model_Shared, PartOIteration.BasePassive, "MVRE", null, Flat1);
            output.WriteLine(string.Join("\n", preparation.Warnings));

            //Warned, not changed: the shared system still serves both dwellings' rooms.
            Assert.Contains(preparation.Warnings, x => x.Contains("Legacy MV"));
            VentilationSystem shared_After = preparation.AnalyticalModel.AdjacencyCluster.GetObject<VentilationSystem>(shared.Guid);
            Assert.NotNull(shared_After);
            Assert.Equal(2, preparation.AnalyticalModel.AdjacencyCluster.GetRelatedObjects<Space>(shared_After).Count);

            //Isolating Flat 1 refuses, naming the straddling system.
            PartOIterationPreparation preparation_Isolated = model_Shared.PreparePartOIteration(PartOIteration.BasePassive, Zones(model_Shared, Flat1), Strategies(model_Shared, "MVRE", Flat1), null, true);
            output.WriteLine(preparation_Isolated.Refusal ?? "(isolated)");
            Assert.NotNull(preparation_Isolated.Refusal);
        }

        // -------------------------------------------------------------------------------------------------
        // P8. The union of per-dwelling scenarios governs a mixed model space by space.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P8_ScenariosFromSeparateCalls_ComposeIntoOnePerSpaceStrategyMap()
        {
            AnalyticalModel baseline = Baseline();

            PartOIterationPreparation preparation_A = Prepare(baseline, PartOIteration.BasePassive, "MVRE", null, Flat1);
            PartOIterationPreparation preparation_B = Prepare(preparation_A.AnalyticalModel, PartOIteration.BaseNaturalVentilation, "NV", null, Flat2);
            PartOIterationPreparation preparation_C = Prepare(preparation_B.AnalyticalModel, PartOIteration.BasePassive, "MVRE", null, Flat3);

            AnalyticalModel model = preparation_C.AnalyticalModel;
            List<OverheatingScenario> scenarios = [.. preparation_A.OverheatingScenarios, .. preparation_B.OverheatingScenarios, .. preparation_C.OverheatingScenarios];

            OverheatingScenarioMap overheatingScenarioMap = new(scenarios, model, SimulationSpaceMap.Identity(model.GetSpaces()));
            Assert.Empty(overheatingScenarioMap.Refusals);

            Assert.All(Spaces(model, Flat1), x => Assert.Equal("MVRE", overheatingScenarioMap.VentilationStrategyMap.Selection(x).VentilationStrategy));
            Assert.All(Spaces(model, Flat2), x => Assert.Equal("NV", overheatingScenarioMap.VentilationStrategyMap.Selection(x).VentilationStrategy));
            Assert.All(Spaces(model, Flat3), x => Assert.Equal("MVRE", overheatingScenarioMap.VentilationStrategyMap.Selection(x).VentilationStrategy));

            //FACT: nothing in the per-dwelling workflow states a scenario for the communal corridor, so it has
            //no strategy and TM59 would refuse it rather than apply the corridor criterion.
            Assert.All(Spaces(model, Corridor), x => Assert.False(overheatingScenarioMap.VentilationStrategyMap.Selection(x).IsSelected));
        }

        // -------------------------------------------------------------------------------------------------
        // P9. The composed model survives JSON serialisation.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P9_TheComposedModel_RoundTripsThroughJsonWithItsEngineeringState()
        {
            AnalyticalModel baseline = Baseline();

            VentilationUnitCapacityDescriptor small = new(new VentilationUnitReference("Proof", "Small", null), 500, 500);
            AnalyticalModel model = Prepare(baseline, PartOIteration.BasePassive, "MVRE", [small], Flat1).AnalyticalModel;
            model = Prepare(model, PartOIteration.BaseNaturalVentilation, "NV", null, Flat2).AnalyticalModel;
            model = Prepare(model, PartOIteration.BasePassive, "MVRE", [small], Flat3).AnalyticalModel;

            AnalyticalModel model_RoundTrip = new(model.ToJsonObject());

            Assert.Equal(Signature(model), Signature(model_RoundTrip));
            Assert.Equal("Small", UnitOf(model_RoundTrip, Flat3).SelectedVentilationUnitReference()?.Model);
        }

        // -------------------------------------------------------------------------------------------------
        // P10. Deterministic reconstruction from two independent clones of one clean baseline.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P10_TheSameStrategySetOnTwoIndependentBaselineClones_GivesEquivalentState_ButNewGuids()
        {
            AnalyticalModel baseline = Baseline();

            AnalyticalModel clone_1 = new(baseline.ToJsonObject());
            AnalyticalModel clone_2 = new(baseline.ToJsonObject());

            AnalyticalModel model_1 = Chain(clone_1, (Flat1, "MVRE"), (Flat2, "NV"), (Flat3, "MVRE"));
            AnalyticalModel model_2 = Chain(clone_2, (Flat1, "MVRE"), (Flat2, "NV"), (Flat3, "MVRE"));

            Assert.Equal(Signature(model_1), Signature(model_2));

            //FACT: equivalent engineering state, but every generated system/unit/terminal/movement has a
            //fresh guid - so provenance or staleness keyed on those guids would see two different models.
            Assert.NotEqual(UnitOf(model_1, Flat1).Guid, UnitOf(model_2, Flat1).Guid);
        }

        // -------------------------------------------------------------------------------------------------
        // P11. Can a materialised model be sanitised back to its baseline? The internal-condition rewrite.
        // -------------------------------------------------------------------------------------------------

        [Fact]
        public void P11_PreparingAnyMvhrDwelling_IrreversiblyRewritesEverySizedSpacesInternalCondition()
        {
            AnalyticalModel baseline = Baseline();

            //An authored supply basis on the NV-to-be Flat 2 bedroom - e.g. from the project's template.
            AdjacencyCluster adjacencyCluster = baseline.AdjacencyCluster;
            Space bedroom_2 = adjacencyCluster.GetSpaces().Find(x => x.Name == "Flat 2 Bedroom");
            InternalCondition internalCondition_Authored = bedroom_2.InternalCondition;
            internalCondition_Authored.SetValue(InternalConditionParameter.SupplyAirFlowPerArea, 0.0015);
            adjacencyCluster.AddObject(new Space(bedroom_2) { InternalCondition = internalCondition_Authored });
            AnalyticalModel model_Authored = new(baseline, adjacencyCluster);

            AnalyticalModel model_A = Prepare(model_Authored, PartOIteration.BasePassive, "MVRE", null, Flat1).AnalyticalModel;

            InternalCondition internalCondition_After = model_A.AdjacencyCluster.GetSpaces().Find(x => x.Name == "Flat 2 Bedroom").InternalCondition;
            output.WriteLine(string.Format("before: '{0}' {1}; after: '{2}' {3}", internalCondition_Authored.Name, internalCondition_Authored.Guid, internalCondition_After.Name, internalCondition_After.Guid));

            //FACT (decides the baseline question): Flat 2 was not assessed, yet its internal condition was
            //replaced by a renamed per-space clone with a new guid, and the authored basis was zeroed. The
            //model keeps no record of the value it replaced, so no deterministic sanitiser can restore it.
            Assert.NotEqual(internalCondition_Authored.Guid, internalCondition_After.Guid);
            Assert.NotEqual(internalCondition_Authored.Name, internalCondition_After.Name);
            Assert.True(internalCondition_After.TryGetValue(InternalConditionParameter.SupplyAirFlowPerArea, out double perArea_After));
            Assert.Equal(0.0, perArea_After);
            Assert.True(internalCondition_After.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double supply_After));
            Assert.Equal(0.008, supply_After, 6);
        }

        // =================================================================================================
        // Fixture
        // =================================================================================================

        /// <summary>
        /// Flats 1 and 2 from <c>PartOIterationPreparationTests.ModelWithTwoAssessedDwellings</c> (reached
        /// by reflection so the two fixtures cannot drift), plus Flat 3 (authored the same way as Flat 2) and
        /// a communal corridor adjacent to Flats 1 and 2. Every zone is marked, so the corridor's
        /// <c>IsDwelling = false</c> is read as a common space rather than making the flats unmarked.
        /// </summary>
        private static AnalyticalModel Baseline()
        {
            MethodInfo methodInfo = typeof(PartOIterationPreparationTests).GetMethod("ModelWithTwoAssessedDwellings", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            object[] arguments = [null, null];
            AnalyticalModel analyticalModel = (AnalyticalModel)methodInfo.Invoke(null, arguments);

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;

            Space space_Supply = AuthoredSpace("Flat 3 Bedroom", PartFType.Habitable, PartFVentilationType.supply, PartFTerminalRole.Supply, 10.0);
            Space space_Extract = AuthoredSpace("Flat 3 Bathroom", PartFType.WetRoom, PartFVentilationType.extract, PartFTerminalRole.GeneralExtract, 10.0);
            adjacencyCluster.AddObject(space_Supply);
            adjacencyCluster.AddObject(space_Extract);
            Helpers.DwellingPartitions.Partition(adjacencyCluster, space_Supply.Name, space_Extract.Name, 200);

            Zone zone_3 = new(Flat3);
            adjacencyCluster.AddObject(zone_3);
            adjacencyCluster.AddRelation(zone_3, space_Supply);
            adjacencyCluster.AddRelation(zone_3, space_Extract);

            Space space_Corridor = new(Corridor);
            space_Corridor.SetValue(SpaceParameter.Area, 20.0);
            space_Corridor.SetValue(SpaceParameter.Volume, 50.0);
            space_Corridor.InternalCondition = new InternalCondition("Corridor IC");
            adjacencyCluster.AddObject(space_Corridor);
            Helpers.DwellingPartitions.Partition(adjacencyCluster, Corridor, "Living Room", 300);
            Helpers.DwellingPartitions.Partition(adjacencyCluster, Corridor, "Flat 2 Bedroom", 310);

            Zone zone_Corridor = new(Corridor);
            adjacencyCluster.AddObject(zone_Corridor);
            adjacencyCluster.AddRelation(zone_Corridor, space_Corridor);

            foreach (Zone zone in adjacencyCluster.GetObjects<Zone>())
            {
                zone.SetValue(ZoneParameter.IsDwelling, zone.Name != Corridor);
                adjacencyCluster.AddObject(zone);
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        private static Space AuthoredSpace(string name, PartFType partFType, PartFVentilationType partFVentilationType, PartFTerminalRole partFTerminalRole, double flow_Lps)
        {
            Space space = new(name);
            space.SetValue(SpaceParameter.Area, 10.0);
            space.SetValue(SpaceParameter.Volume, 25.0);

            InternalCondition internalCondition = new(name + " IC");
            internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, "Ventilation System");
            space.InternalCondition = internalCondition;

            bool supply = partFVentilationType == PartFVentilationType.supply;
            PartFSpaceData partFSpaceData = new(name, partFType, partFVentilationType, supply, null, true, true, supply, false, "Volume", flow_Lps);

            partFSpaceData.Terminals.Add(new PartFVentilationTerminalRequirement(name + (supply ? " - Supply" : " - Extract"), space.Guid, partFTerminalRole)
            {
                SpaceName = name,
                OperatingMode = PartFOperatingMode.ContinuousDesign,
                ContinuousDesignFlowRate_Lps = flow_Lps,
                IsInBalancedFlow = true,
                IsRequired = true,
                SourceReference = "PR0 investigation fixture",
            });

            space.SetValue(SpaceParameter.PartFSpaceData, partFSpaceData);

            return space;
        }

        // =================================================================================================
        // Driving the current API
        // =================================================================================================

        private static PartOIterationPreparation Prepare(AnalyticalModel analyticalModel, PartOIteration partOIteration, string ventilationStrategy, IEnumerable<VentilationUnitCapacityDescriptor> descriptors, params string[] names_Zone)
        {
            PartOIterationPreparation result = analyticalModel.PreparePartOIteration(partOIteration, Zones(analyticalModel, names_Zone), Strategies(analyticalModel, ventilationStrategy, names_Zone), descriptors);

            Assert.True(result.Refusal == null, result.Refusal);
            Assert.NotNull(result.AnalyticalModel);

            return result;
        }

        private static AnalyticalModel Chain(AnalyticalModel analyticalModel, params (string Zone, string Strategy)[] steps)
        {
            AnalyticalModel result = analyticalModel;

            foreach ((string name_Zone, string strategy) in steps)
            {
                PartOIteration partOIteration = strategy == "NV" ? PartOIteration.BaseNaturalVentilation : PartOIteration.BasePassive;
                result = Prepare(result, partOIteration, strategy, null, name_Zone).AnalyticalModel;
            }

            return result;
        }

        private static Zone Zone(AnalyticalModel analyticalModel, string name) => analyticalModel.GetZones().Find(x => x.Name == name);

        private static List<Zone> Zones(AnalyticalModel analyticalModel, params string[] names) => names.Select(x => Zone(analyticalModel, x)).ToList();

        private static Dictionary<Guid, string> Strategies(AnalyticalModel analyticalModel, string ventilationStrategy, params string[] names)
        {
            return names.ToDictionary(x => Zone(analyticalModel, x).Guid, x => ventilationStrategy);
        }

        // =================================================================================================
        // Reading state
        // =================================================================================================

        private static List<Space> Spaces(AnalyticalModel analyticalModel, string name_Zone)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            return adjacencyCluster.GetRelatedObjects<Space>(Zone(analyticalModel, name_Zone)) ?? [];
        }

        private static List<VentilationTerminal> Terminals(AnalyticalModel analyticalModel, string name_Zone)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            return Spaces(analyticalModel, name_Zone).SelectMany(x => adjacencyCluster.GetRelatedObjects<VentilationTerminal>(x) ?? []).ToList();
        }

        private static List<VentilationSystem> SystemsServing(AnalyticalModel analyticalModel, string name_Zone)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            HashSet<Guid> guids = Spaces(analyticalModel, name_Zone).Select(x => x.Guid).ToHashSet();

            return (adjacencyCluster.GetObjects<VentilationSystem>() ?? []).FindAll(x => (adjacencyCluster.GetRelatedObjects<Space>(x) ?? []).Exists(y => guids.Contains(y.Guid)));
        }

        private static AirHandlingUnit UnitOf(AnalyticalModel analyticalModel, string name_Zone)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            string name_Unit = SystemsServing(analyticalModel, name_Zone).Single().GetValue<string>(VentilationSystemParameter.SupplyUnitName);

            return adjacencyCluster.GetObjects<AirHandlingUnit>().Single(x => x.Name == name_Unit);
        }

        private static List<SpaceAirMovement> AirMovementsTouching(AnalyticalModel analyticalModel, string name_Zone)
        {
            HashSet<string> references = Spaces(analyticalModel, name_Zone).SelectMany(x => new[] { new Core.ObjectReference(x).ToString(), x.Name }).ToHashSet();

            return (analyticalModel.AdjacencyCluster.GetObjects<SpaceAirMovement>() ?? []).FindAll(x => (x.From != null && references.Contains(x.From)) || (x.To != null && references.Contains(x.To)));
        }

        private static double SpaceDesignFlow(AdjacencyCluster adjacencyCluster, Space space, FlowClassification flowClassification)
        {
            return (adjacencyCluster.GetRelatedObjects<VentilationTerminal>(space) ?? []).Where(x => x.FlowClassification == flowClassification).Sum(x => x.DesignFlowRate_Lps ?? 0);
        }

        private static Dictionary<string, string> UnitNamesByDwelling(AnalyticalModel analyticalModel)
        {
            return new[] { Flat1, Flat2, Flat3 }.Where(x => SystemsServing(analyticalModel, x).Count != 0).ToDictionary(x => x, x => UnitOf(analyticalModel, x).Name);
        }

        /// <summary>
        /// A GUID- and name-order-insensitive statement of the engineering state, one sorted line per fact,
        /// each prefixed by the dwelling (zone) it belongs to. Generic unit names are replaced by the
        /// dwelling the unit serves, so two models that differ only in call order compare equal.
        /// </summary>
        private static List<string> Signature(AnalyticalModel analyticalModel, string name_Zone = null)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

            Dictionary<Guid, string> dwelling_Space = [];
            foreach (Zone zone in adjacencyCluster.GetObjects<Zone>() ?? [])
            {
                foreach (Space space in adjacencyCluster.GetRelatedObjects<Space>(zone) ?? [])
                {
                    dwelling_Space[space.Guid] = zone.Name;
                }
            }

            string DwellingOf(Space space) => space != null && dwelling_Space.TryGetValue(space.Guid, out string name) ? name : "(none)";

            //Unit name -> label of the dwelling(s) its systems serve.
            Dictionary<string, string> label_Unit = [];
            List<string> result = [];

            foreach (VentilationSystem ventilationSystem in adjacencyCluster.GetObjects<VentilationSystem>() ?? [])
            {
                List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(ventilationSystem) ?? [];
                string label = string.Join("+", spaces.Select(DwellingOf).Distinct().OrderBy(x => x, StringComparer.Ordinal));
                string name_Unit = ventilationSystem.GetValue<string>(VentilationSystemParameter.SupplyUnitName);
                if (name_Unit != null)
                {
                    label_Unit[name_Unit] = label;
                }

                result.Add(string.Format("{0}|system|{1}|serves {2}", label, ventilationSystem.Type?.Name, string.Join(",", spaces.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal))));
            }

            Dictionary<string, string> name_Reference = [];
            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                name_Reference[new Core.ObjectReference(space).ToString()] = space.Name;
                name_Reference[space.Name] = space.Name;
            }

            foreach (AirHandlingUnit airHandlingUnit in adjacencyCluster.GetObjects<AirHandlingUnit>() ?? [])
            {
                string label = label_Unit.TryGetValue(airHandlingUnit.Name, out string value) ? value : "(unlinked)";
                string alias = "unit[" + label + "]";
                name_Reference[new Core.ObjectReference(airHandlingUnit).ToString()] = alias;
                name_Reference[airHandlingUnit.Name] = alias;

                VentilationUnitReference ventilationUnitReference = airHandlingUnit.SelectedVentilationUnitReference();
                result.Add(string.Format("{0}|unit|product {1}", label, ventilationUnitReference == null ? "-" : ventilationUnitReference.Manufacturer + " " + ventilationUnitReference.Model));
            }

            foreach (Space space in adjacencyCluster.GetSpaces() ?? [])
            {
                string dwelling = DwellingOf(space);
                InternalCondition internalCondition = space.InternalCondition;
                string supply = internalCondition != null && internalCondition.TryGetValue(InternalConditionParameter.SupplyAirFlow, out double s) ? F(s * 1000) : "-";
                string extract = internalCondition != null && internalCondition.TryGetValue(InternalConditionParameter.ExhaustAirFlow, out double e) ? F(e * 1000) : "-";
                result.Add(string.Format("{0}|ic|{1}|supply {2} extract {3}", dwelling, space.Name, supply, extract));

                foreach (VentilationTerminal ventilationTerminal in adjacencyCluster.GetRelatedObjects<VentilationTerminal>(space) ?? [])
                {
                    List<VentilationSystem> systems = adjacencyCluster.GetRelatedObjects<VentilationSystem>(ventilationTerminal) ?? [];
                    result.Add(string.Format("{0}|terminal|{1}|{2} {3} l/s|connected {4}", dwelling, space.Name, ventilationTerminal.FlowClassification, F(ventilationTerminal.DesignFlowRate_Lps ?? double.NaN), systems.Count));
                }
            }

            string Resolve(string reference) => reference == null ? "outside" : name_Reference.TryGetValue(reference, out string name) ? name : "?" + reference;

            string DwellingOfReference(string reference)
            {
                string name = Resolve(reference);
                if (name.StartsWith("unit[", StringComparison.Ordinal))
                {
                    return name.Substring(5, name.Length - 6);
                }

                Space space = adjacencyCluster.GetSpaces()?.Find(x => x.Name == name);
                return space == null ? null : DwellingOf(space);
            }

            //A profile by name, type, length and value sum - enough to see it lost, swapped or altered. The unit
            //movement's profiles are NAMED after the generic unit ("MVHR-01 Humidification"), and those names
            //follow call order (P2), so a unit name inside a profile name is replaced by the dwelling it serves.
            string ProfileText(Profile profile)
            {
                if (profile == null)
                {
                    return "-";
                }

                string name = profile.Name ?? string.Empty;
                foreach (KeyValuePair<string, string> keyValuePair in label_Unit.OrderByDescending(x => x.Key.Length))
                {
                    name = name.Replace(keyValuePair.Key, "unit[" + keyValuePair.Value + "]");
                }

                double[] values = profile.GetValues() ?? [];
                return string.Format("{0}/{1}[{2}:{3}]", name, profile.ProfileType, values.Length, F(values.Sum()));
            }

            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetObjects<SpaceAirMovement>() ?? [])
            {
                string dwelling = DwellingOfReference(spaceAirMovement.To) ?? DwellingOfReference(spaceAirMovement.From) ?? "(none)";
                result.Add(string.Format("{0}|movement|{1} -> {2}|{3} l/s|profile {4}", dwelling, Resolve(spaceAirMovement.From), Resolve(spaceAirMovement.To), F(spaceAirMovement.AirFlow * 1000), ProfileText(spaceAirMovement.Profile)));
            }

            //The unit's own movement carries the plant-zone supply condition TAS is given (heating, cooling,
            //humidity limits and density), so it is part of the engineering state, not decoration.
            foreach (AirHandlingUnitAirMovement airHandlingUnitAirMovement in adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>() ?? [])
            {
                List<AirHandlingUnit> airHandlingUnits = adjacencyCluster.GetRelatedObjects<AirHandlingUnit>(airHandlingUnitAirMovement) ?? [];
                string label = string.Join("+", airHandlingUnits.Select(x => label_Unit.TryGetValue(x.Name, out string value) ? value : "(unlinked)").OrderBy(x => x, StringComparer.Ordinal));
                result.Add(string.Format(
                    "{0}|unit-movement|units {1}|heating {2} cooling {3} humidification {4} dehumidification {5} density {6}",
                    label.Length == 0 ? "(none)" : label,
                    airHandlingUnits.Count,
                    ProfileText(airHandlingUnitAirMovement.Heating),
                    ProfileText(airHandlingUnitAirMovement.Cooling),
                    ProfileText(airHandlingUnitAirMovement.Humidification),
                    ProfileText(airHandlingUnitAirMovement.Dehumidification),
                    ProfileText(airHandlingUnitAirMovement.Density)));
            }

            result.Sort(StringComparer.Ordinal);

            return name_Zone == null ? result : result.FindAll(x => x.StartsWith(name_Zone + "|", StringComparison.Ordinal));
        }

        private void Dump(AnalyticalModel analyticalModel)
        {
            foreach (string line in Signature(analyticalModel))
            {
                output.WriteLine(line);
            }
        }
    }
}
