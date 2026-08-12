// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <b>Iteration 0 step 7: the <c>OverheatingScenario</c> is authoritative over ventilation strategy.</b>
    /// <para>
    /// The defect these tests close. Three places decided how a space was ventilated and disagreed, and the
    /// one that picks the TM59 criterion read the space's internal condition, then matched a <i>zone's name</i>
    /// against a system-type library, then defaulted to <c>"NV"</c>. A zone called "Flat 1" matches nothing in
    /// that library, so a real MVRE dwelling was assessed against the natural-ventilation criterion - a wrong
    /// engineering answer that is indistinguishable from a right one. Supplying a
    /// <c>VentilationStrategyMap</c> replaces the whole chain, and where nothing states a strategy the space is
    /// refused rather than defaulted.
    /// </para>
    /// <para>
    /// Each override test has a <b>control</b> that runs the same model without the map and shows the old
    /// derivation reaching the opposite answer. Without those controls a passing test would prove only that
    /// the map agreed with a derivation that was already right.
    /// </para>
    /// <para>
    /// The fixture is the three-flat validation shape and, deliberately, does not fix the name matching in
    /// <c>TM59AssessmentCalculator</c>: result association by identity is step 8.
    /// </para>
    /// </summary>
    public class VentilationStrategyTests
    {
        //The TAS spelling, which is what the TSD conversion writes.
        private const string key_Tas_OccupantSensibleGain = "Occupant Sensible Gain";

        // ---------------------------------------------------------------------------------------------
        // The scenario overrides every derivation it replaces
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>An MVRE scenario beats an internal condition saying NV.</b> Derivation #1: the space's
        /// <c>InternalCondition.VentilationSystemTypeName</c> used to be the first and usually the only word on
        /// the subject. A dwelling whose design data still says NV - carried over, defaulted, or simply never
        /// filled in - is assessed mechanically because the scenario says the dwelling is MVRE.
        /// </summary>
        [Fact]
        public void AnMVREScenario_OverridesAnInternalConditionSayingNV()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "NV");

            //Control: the internal condition really does drive the old derivation to natural ventilation.
            Assert.Equal("Natural", Criterion(analyticalModel, null));

            Assert.Equal("Mechanical", Criterion(analyticalModel, Map(analyticalModel, "MVRE")));
        }

        /// <summary>
        /// <b>An NV scenario beats a mechanical <c>VentilationSystem</c> in the model.</b> Derivation #2: the
        /// TM59 XML export took any related <c>VentilationSystem</c> that <c>IsMechanicalVentilation()</c> and
        /// let it override the internal condition. Here the model carries an MVRE ventilation system <i>and</i>
        /// an internal condition saying MVRE, and the scenario states NV - so the natural criterion applies.
        /// <para>
        /// The export side of derivation #2 is covered in <c>SAM_Tas</c>, where it lives. What this pins is
        /// that a mechanical system on the model cannot reach the criterion behind the scenario's back.
        /// </para>
        /// </summary>
        [Fact]
        public void AnNVScenario_OverridesAMechanicalVentilationSystemOnTheModel()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "MVRE", ventilationSystem: true);

            //Control: without the scenario this model is assessed mechanically.
            Assert.Equal("Mechanical", Criterion(analyticalModel, null));

            Assert.Equal("Natural", Criterion(analyticalModel, Map(analyticalModel, "NV")));

            //And the mechanical system really is on the model - the control is not passing for another reason.
            Space space = analyticalModel.GetSpaces()[0];
            Assert.NotEmpty(analyticalModel.AdjacencyCluster.MechanicalSystems<VentilationSystem>(space));
        }

        /// <summary>
        /// <b>A misleading zone name cannot override the scenario.</b> Derivation #3's middle step looked a
        /// zone's name up in <c>Query.DefaultSystemTypeLibrary()</c> - so a zone that happens to be called "NV"
        /// or "NV Wing" was read as a statement that the dwelling is naturally ventilated. It is not a
        /// statement about anything; it is a name.
        /// </summary>
        [Theory]
        [InlineData("NV")]
        [InlineData("NV Wing")]
        public void AMisleadingZoneName_CannotOverrideTheScenario(string zoneName)
        {
            //No internal-condition system type at all, so the old derivation reaches the zone-name lookup.
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: null, zoneName: zoneName);

            //Control: the zone NAME is what decides it today - by exact match, then by StartsWith.
            Assert.Equal("Natural", Criterion(analyticalModel, null));

            Assert.Equal("Mechanical", Criterion(analyticalModel, Map(analyticalModel, "MVRE")));
        }

        /// <summary>
        /// <b>And the "NV" default itself.</b> A dwelling with nothing said about ventilation anywhere and a
        /// zone name that matches no library entry - "Flat 1", the real run's shape - is assessed as naturally
        /// ventilated today. That is the defect in its purest form, pinned here so the fix is visibly a fix.
        /// </summary>
        [Fact]
        public void TheNaturalVentilationDefault_IsWhatTheScenarioReplaces()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: null, zoneName: "Flat 1");

            //Control: nothing in this model says NV, and it is assessed against the NV criterion regardless.
            Assert.Equal("Natural", Criterion(analyticalModel, null));

            Assert.Equal("Mechanical", Criterion(analyticalModel, Map(analyticalModel, "MVRE")));
        }

        // ---------------------------------------------------------------------------------------------
        // Refusal, not fallback
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>A scenario stating no ventilation strategy refuses, explicitly.</b> It does not fall back to the
        /// zone-name chain and it does not default to NV: the space produces no result at all, and the reason
        /// names the space and says the scenario stated nothing. A visible gap is recoverable; a number
        /// measured against a guessed criterion is not.
        /// </summary>
        [Fact]
        public void AScenarioWithNoVentilationStrategy_RefusesExplicitly()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: null, zoneName: "Flat 1");
            Space space = analyticalModel.GetSpaces()[0];

            //A perfectly valid scenario - a dwelling over a real design zone - that simply names no system.
            OverheatingScenario overheatingScenario = new(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.Undefined);

            Assert.True(overheatingScenario.IsValid);
            Assert.False(overheatingScenario.HasVentilationStrategy);

            VentilationStrategyMap ventilationStrategyMap = new();
            Assert.True(ventilationStrategyMap.Add(overheatingScenario, [space]));

            TM59AssessmentResult tM59AssessmentResult = Calculator(analyticalModel, ventilationStrategyMap).Calculate([space]);

            Assert.Empty(tM59AssessmentResult.MechanicalVentilationResults);
            Assert.Empty(tM59AssessmentResult.NaturalVentilationResults);
            Assert.Empty(tM59AssessmentResult.CorridorResults);

            string reason = Assert.Single(tM59AssessmentResult.VentilationStrategyRefusals);
            Assert.Contains(space.Name, reason);
            Assert.Contains("states no ventilation strategy", reason);
        }

        /// <summary>
        /// A space no scenario covers is refused too, and with a different sentence: "nothing mentions this
        /// space" and "the scenario that covers it said nothing" are different mistakes with different fixes.
        /// </summary>
        [Fact]
        public void ASpaceNoScenarioCovers_IsRefusedAndSaysSoDifferently()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: null, zoneName: "Flat 1");
            Space space = analyticalModel.GetSpaces()[0];

            //An empty map covers nothing at all.
            TM59AssessmentResult tM59AssessmentResult = Calculator(analyticalModel, new VentilationStrategyMap()).Calculate([space]);

            string reason = Assert.Single(tM59AssessmentResult.VentilationStrategyRefusals);
            Assert.Contains(space.Name, reason);
            Assert.Contains("No overheating scenario covers", reason);

            Assert.Empty(tM59AssessmentResult.MechanicalVentilationResults);
            Assert.Empty(tM59AssessmentResult.NaturalVentilationResults);
            Assert.Empty(tM59AssessmentResult.CorridorResults);
        }

        /// <summary>
        /// <b>One refused space does not lose the rest of the building.</b> Three flats, one of them with no
        /// strategy stated: the other two are assessed and the third is reported. Throwing would have made an
        /// incomplete scenario set cost every dwelling's assessment.
        /// </summary>
        [Fact]
        public void ARefusedSpace_DoesNotLoseTheOtherDwellings()
        {
            AnalyticalModel analyticalModel = Model_ThreeFlats();
            List<Space> spaces = analyticalModel.GetSpaces();

            VentilationStrategyMap ventilationStrategyMap = new();

            //Flat 1 and Flat 2 state MVRE; Flat 3's scenario states nothing.
            ventilationStrategyMap.Add(Scenario("MVRE"), [Find(spaces, "Flat 1 Bedroom 2"), Find(spaces, "Flat 2 Bedroom 2")]);
            ventilationStrategyMap.Add(Scenario(null), [Find(spaces, "Flat 3 Bedroom 2")]);

            TM59AssessmentResult tM59AssessmentResult = Calculator(analyticalModel, ventilationStrategyMap).Calculate(spaces);

            Assert.Equal(2, tM59AssessmentResult.MechanicalVentilationResults.Count);
            Assert.Empty(tM59AssessmentResult.NaturalVentilationResults);

            string reason = Assert.Single(tM59AssessmentResult.VentilationStrategyRefusals);
            Assert.Contains("Flat 3 Bedroom 2", reason);
        }

        /// <summary>
        /// <b>Two scenarios claiming one space with different strategies refuse.</b> The same rule
        /// <c>SimulationSpaceMap</c> and <c>SelectPreferredCapableSystem</c> already follow: where the input
        /// has not settled the question, nothing is chosen. The same strategy stated twice is not a conflict -
        /// one answer said twice is still one answer.
        /// </summary>
        [Fact]
        public void TwoScenariosDisagreeingOverOneSpace_Refuse()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: null, zoneName: "Flat 1");
            Space space = analyticalModel.GetSpaces()[0];

            VentilationStrategyMap ventilationStrategyMap_Conflict = new();
            ventilationStrategyMap_Conflict.Add(Scenario("MVRE"), [space]);
            ventilationStrategyMap_Conflict.Add(Scenario("NV"), [space]);

            Assert.Equal(1, ventilationStrategyMap_Conflict.Count);

            VentilationStrategySelection ventilationStrategySelection = ventilationStrategyMap_Conflict.Selection(space);
            Assert.False(ventilationStrategySelection.IsSelected);
            Assert.Contains("different ventilation strategies", ventilationStrategySelection.Reason);

            //Stated twice, identically: not ambiguous.
            VentilationStrategyMap ventilationStrategyMap_Agree = new();
            ventilationStrategyMap_Agree.Add(Scenario("MVRE"), [space]);
            ventilationStrategyMap_Agree.Add(Scenario("MVRE"), [space]);

            Assert.Equal("MVRE", ventilationStrategyMap_Agree.Selection(space).VentilationStrategy);

            //A strategy and a silence over one space is a disagreement too - one of them is wrong.
            VentilationStrategyMap ventilationStrategyMap_Silence = new();
            ventilationStrategyMap_Silence.Add(Scenario("MVRE"), [space]);
            ventilationStrategyMap_Silence.Add(Scenario(null), [space]);

            Assert.False(ventilationStrategyMap_Silence.Selection(space).IsSelected);
        }

        /// <summary>
        /// An invalid scenario records nothing: it names nothing assessable, so a claim from it would be a
        /// claim no caller could act on. The map is empty, and every space is then refused as uncovered.
        /// </summary>
        [Fact]
        public void AnInvalidScenario_RecordsNothing()
        {
            Space space = new("Flat 1 Bedroom 2");

            VentilationStrategyMap ventilationStrategyMap = new();

            //Undefined scope, so IsValid is false however complete the rest of it is.
            OverheatingScenario overheatingScenario = new(PartOAssessmentScope.Undefined, Guid.NewGuid(), PartOIteration.Undefined, new SystemTemplate("MVRE", null, null, null, null, null));

            Assert.False(overheatingScenario.IsValid);
            Assert.False(ventilationStrategyMap.Add(overheatingScenario, [space]));
            Assert.False(ventilationStrategyMap.Add(null, [space]));
            Assert.False(ventilationStrategyMap.Add(Scenario("MVRE"), null));
            Assert.False(ventilationStrategyMap.Add(Scenario("MVRE"), []));

            Assert.Equal(0, ventilationStrategyMap.Count);
            Assert.False(ventilationStrategyMap.Selection(space).IsSelected);
            Assert.False(ventilationStrategyMap.Selection(null).IsSelected);
        }

        // ---------------------------------------------------------------------------------------------
        // What the strategy must NOT come from
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b>Provenance and routing have no effect on the criterion.</b> The model's name, the assembly a
        /// result reports as its <c>Source</c>, and which series keys were used to read the data all change
        /// freely without moving a space between criteria. Where the numbers came from is not a statement about
        /// how a building is ventilated, and a TSD route and a TPD route must assess the same dwelling the same
        /// way.
        /// </summary>
        [Fact]
        public void ProvenanceAndRouting_DoNotAffectTheCriterion()
        {
            //Two models identical except for their name, one carrying the analytical series spelling and one
            //TAS's, read with matching keys.
            AnalyticalModel analyticalModel_Tas = Model(ventilationSystemTypeName: "NV", name: "From a TSD", key_OccupancySensibleGain: key_Tas_OccupantSensibleGain);
            AnalyticalModel analyticalModel_Analytical = Model(ventilationSystemTypeName: "NV", name: "From a TPD", key_OccupancySensibleGain: Core.Query.Name(SpaceSimulationResultParameter.OccupancySensibleGain));

            TM59AssessmentCalculator tM59AssessmentCalculator_Tas = Calculator(analyticalModel_Tas, Map(analyticalModel_Tas, "MVRE"));
            tM59AssessmentCalculator_Tas.SourceFallback = "SAM.Analytical.Tas";

            TM59AssessmentCalculator tM59AssessmentCalculator_Analytical = new(analyticalModel_Analytical)
            {
                VentilationStrategyMap = Map(analyticalModel_Analytical, "MVRE"),
                SourceFallback = "SAM.Analytical"
            };

            Assert.Single(tM59AssessmentCalculator_Tas.Calculate(analyticalModel_Tas.GetSpaces()).MechanicalVentilationResults);
            Assert.Single(tM59AssessmentCalculator_Analytical.Calculate(analyticalModel_Analytical.GetSpaces()).MechanicalVentilationResults);

            //And the same scenario read through the natural strategy gives the natural criterion on both
            //routes - the criterion tracks the scenario, not the route.
            tM59AssessmentCalculator_Tas.VentilationStrategyMap = Map(analyticalModel_Tas, "NV");
            tM59AssessmentCalculator_Analytical.VentilationStrategyMap = Map(analyticalModel_Analytical, "NV");

            Assert.Single(tM59AssessmentCalculator_Tas.Calculate(analyticalModel_Tas.GetSpaces()).NaturalVentilationResults);
            Assert.Single(tM59AssessmentCalculator_Analytical.Calculate(analyticalModel_Analytical.GetSpaces()).NaturalVentilationResults);
        }

        /// <summary>
        /// <b>Step 7 does not touch scenario identity.</b> Recording a scenario in a map is a read of it, so
        /// its <c>Key</c> before and after must be the same guid - and it must still be the guid
        /// <c>OverheatingScenarioTests.Key_IsStableAcrossBuilds</c> pins.
        /// </summary>
        [Fact]
        public void RecordingAScenario_DoesNotChangeItsIdentity()
        {
            OverheatingScenario overheatingScenario = Scenario("MVRE");

            Guid guid_Before = overheatingScenario.Key;

            VentilationStrategyMap ventilationStrategyMap = new();
            ventilationStrategyMap.Add(overheatingScenario, [new Space("Flat 1 Bedroom 2")]);

            Assert.Equal(guid_Before, overheatingScenario.Key);
            Assert.Equal(guid_Before, new OverheatingScenario(overheatingScenario).Key);
        }

        // ---------------------------------------------------------------------------------------------
        // The seam, and what is left alone
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// <b><c>UV</c> still routes to the corridor criterion.</b> The strategy the scenario states is read
        /// with the vocabulary the criterion selection already had - <c>UV</c> corridor, <c>NV</c> natural,
        /// anything else mechanical - so making the scenario authoritative changed which strategy applies, not
        /// what a strategy means.
        /// </summary>
        [Fact]
        public void UV_StillRoutesToTheCorridorCriterion()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "MVRE");

            Assert.Equal("Corridor", Criterion(analyticalModel, Map(analyticalModel, "UV")));

            //Whitespace and case are normalised on the way in, because SystemTemplate's copy and JSON
            //constructors do not strip spaces the way its setters do.
            Assert.Equal("Corridor", Criterion(analyticalModel, Map(analyticalModel, " uv ")));
        }

        /// <summary>
        /// <b>No map means no change.</b> The old derivation is untouched for every caller that has no scenario
        /// to state - the Grasshopper components, the user interface, <c>OverheatingCalculator</c> - and no
        /// refusal is reported, because nothing was asked of a map that was not supplied.
        /// </summary>
        [Fact]
        public void WithoutAMap_NothingChangesAndNothingIsRefused()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "MVRE");

            TM59AssessmentCalculator tM59AssessmentCalculator = Calculator(analyticalModel, null);

            TM59AssessmentResult tM59AssessmentResult = tM59AssessmentCalculator.Calculate(analyticalModel.GetSpaces());

            Assert.Single(tM59AssessmentResult.MechanicalVentilationResults);
            Assert.Empty(tM59AssessmentResult.VentilationStrategyRefusals);
        }

        /// <summary>
        /// Refusals belong to the call that produced them: a second run with a map that refuses nothing reports
        /// nothing, and a stale reason is never read as this call's.
        /// </summary>
        [Fact]
        public void Refusals_BelongToTheCallThatProducedThem()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "MVRE");
            List<Space> spaces = analyticalModel.GetSpaces();

            TMOverheatingCalculator tMOverheatingCalculator = new(analyticalModel)
            {
                OccupancySensibleGainSeriesKey = key_Tas_OccupantSensibleGain,
                TextMap = TextMap(),
                VentilationStrategyMap = new VentilationStrategyMap()
            };

            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(spaces));
            Assert.Single(tMOverheatingCalculator.VentilationStrategyRefusals);

            tMOverheatingCalculator.VentilationStrategyMap = Map(analyticalModel, "MVRE");

            Assert.Single(tMOverheatingCalculator.Calculate_TM59(spaces));
            Assert.Empty(tMOverheatingCalculator.VentilationStrategyRefusals);

            //And a call that fails before it reaches any space clears them too.
            tMOverheatingCalculator.VentilationStrategyMap = new VentilationStrategyMap();
            Assert.Empty(tMOverheatingCalculator.Calculate_TM59(spaces));
            Assert.Single(tMOverheatingCalculator.VentilationStrategyRefusals);

            Assert.Null(tMOverheatingCalculator.Calculate_TM59(null));
            Assert.Empty(tMOverheatingCalculator.VentilationStrategyRefusals);
        }

        /// <summary>
        /// The reported refusals are a copy, so a caller cannot edit the calculator's record of what it
        /// refused - the same rule every other list-returning property in this area follows.
        /// </summary>
        [Fact]
        public void ReportedRefusals_AreACopy()
        {
            AnalyticalModel analyticalModel = Model(ventilationSystemTypeName: "MVRE");

            TMOverheatingCalculator tMOverheatingCalculator = new(analyticalModel)
            {
                OccupancySensibleGainSeriesKey = key_Tas_OccupantSensibleGain,
                TextMap = TextMap(),
                VentilationStrategyMap = new VentilationStrategyMap()
            };

            tMOverheatingCalculator.Calculate_TM59(analyticalModel.GetSpaces());

            tMOverheatingCalculator.VentilationStrategyRefusals.Clear();

            Assert.Single(tMOverheatingCalculator.VentilationStrategyRefusals);
        }

        // ---------------------------------------------------------------------------------------------
        // Fixture
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Which criterion a one-space model was assessed against, as a word. Fails the calling test rather
        /// than returning something ambiguous where the space produced no result or more than one.
        /// </summary>
        private static string Criterion(AnalyticalModel analyticalModel, VentilationStrategyMap ventilationStrategyMap)
        {
            TM59AssessmentResult tM59AssessmentResult = Calculator(analyticalModel, ventilationStrategyMap).Calculate(analyticalModel.GetSpaces());

            List<string> result = [];

            if (tM59AssessmentResult.MechanicalVentilationResults.Count != 0)
            {
                result.Add("Mechanical");
            }

            if (tM59AssessmentResult.NaturalVentilationResults.Count != 0)
            {
                result.Add("Natural");
            }

            if (tM59AssessmentResult.CorridorResults.Count != 0)
            {
                result.Add("Corridor");
            }

            return Assert.Single(result);
        }

        private static TM59AssessmentCalculator Calculator(AnalyticalModel analyticalModel, VentilationStrategyMap ventilationStrategyMap)
        {
            return new TM59AssessmentCalculator(analyticalModel)
            {
                OccupancySensibleGainSeriesKey = key_Tas_OccupantSensibleGain,
                VentilationStrategyMap = ventilationStrategyMap
            };
        }

        /// <summary>A scenario stating the given ventilation strategy, or none where it is null.</summary>
        private static OverheatingScenario Scenario(string ventilationStrategy)
        {
            SystemTemplate systemTemplate = ventilationStrategy == null ? null : new SystemTemplate(ventilationStrategy, null, null, null, null, null);

            return new OverheatingScenario(PartOAssessmentScope.Dwelling, Guid.NewGuid(), PartOIteration.Undefined, systemTemplate);
        }

        /// <summary>A map stating one strategy for every space in the model.</summary>
        private static VentilationStrategyMap Map(AnalyticalModel analyticalModel, string ventilationStrategy)
        {
            VentilationStrategyMap result = new();

            result.Add(Scenario(ventilationStrategy), analyticalModel.GetSpaces());

            return result;
        }

        private static Space Find(List<Space> spaces, string name)
        {
            return spaces.Find(x => x.Name == name);
        }

        /// <summary>
        /// One habitable space carrying hourly series, in a zone, with a weather year - the minimum a TM59
        /// assessment needs.
        /// </summary>
        private static AnalyticalModel Model(string ventilationSystemTypeName, string zoneName = "Flat 1", bool ventilationSystem = false, string name = "Three Flats", string key_OccupancySensibleGain = key_Tas_OccupantSensibleGain)
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space = Space("Flat 1 Bedroom 2", ventilationSystemTypeName, key_OccupancySensibleGain);
            Zone zone = new(zoneName);

            adjacencyCluster.AddObject(space);
            adjacencyCluster.AddObject(zone);
            adjacencyCluster.AddRelation(zone, space);

            if (ventilationSystem)
            {
                VentilationSystem ventilationSystem_Temp = new("1", new VentilationSystemType("MVRE", "Mechanical Ventilation with Recirculation"));

                adjacencyCluster.AddObject(ventilationSystem_Temp);
                adjacencyCluster.AddRelation(ventilationSystem_Temp, space);
            }

            return Model(adjacencyCluster, name);
        }

        /// <summary>Three flats each with a "Bedroom 2", all stating MVRE in their design data.</summary>
        private static AnalyticalModel Model_ThreeFlats()
        {
            AdjacencyCluster adjacencyCluster = new();

            foreach (string name in new[] { "Flat 1", "Flat 2", "Flat 3" })
            {
                Space space = Space(name + " Bedroom 2", "MVRE", key_Tas_OccupantSensibleGain);
                Zone zone = new(name);

                adjacencyCluster.AddObject(space);
                adjacencyCluster.AddObject(zone);
                adjacencyCluster.AddRelation(zone, space);
            }

            return Model(adjacencyCluster, "Three Flats");
        }

        private static AnalyticalModel Model(AdjacencyCluster adjacencyCluster, string name)
        {
            AnalyticalModel result = new(name, null, null, null, adjacencyCluster);

            result.SetValue(AnalyticalModelParameter.WeatherData, new WeatherData("Test", "Test", 51.5, -0.1, 0, WeatherYear()));

            return result;
        }

        /// <summary>
        /// A space with its hourly series stored the way the TSD converter stores them, and an internal
        /// condition whose name makes it a bedroom under <see cref="TextMap"/> so that it is a habitable room
        /// rather than a corridor.
        /// </summary>
        private static Space Space(string name, string ventilationSystemTypeName, string key_OccupancySensibleGain)
        {
            Space result = new(name);

            InternalCondition internalCondition = new(name);

            if (!string.IsNullOrEmpty(ventilationSystemTypeName))
            {
                internalCondition.SetValue(InternalConditionParameter.VentilationSystemTypeName, ventilationSystemTypeName);
            }

            result.InternalCondition = internalCondition;

            ParameterSet parameterSet = new("SAM.Analytical.Tas.dll");

            parameterSet.Add(Core.Query.Name(SpaceSimulationResultParameter.ResultantTemperature), Values([21.0, 24.5, 27.5, 29.0]));
            parameterSet.Add(key_OccupancySensibleGain, Values([0, 80.0, 80.0, 0]));

            result.Add(parameterSet);

            return result;
        }

        /// <summary>
        /// An explicit TM59 <c>TextMap</c> mapping any name containing "Bedroom" to the sleeping application,
        /// so the criterion selection is deterministic without depending on a shipped resource file being
        /// installed on the machine running the tests.
        /// </summary>
        private static TextMap TextMap()
        {
            TextMap result = Core.Create.TextMap("TM59");

            result.Add("Sleeping", "Bedroom");

            return result;
        }

        private static WeatherYear WeatherYear()
        {
            WeatherYear result = new(2018);

            for (int day = 0; day < 365; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    result.Add(day, hour, new Dictionary<string, double> { { WeatherDataType.DryBulbTemperature.ToString(), 20.0 } });
                }
            }

            return result;
        }

        private static JsonArray Values(IEnumerable<double> values)
        {
            JsonArray result = [];

            foreach (double value in values)
            {
                result.Add(value);
            }

            return result;
        }
    }
}
