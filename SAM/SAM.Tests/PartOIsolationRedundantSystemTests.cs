// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Tests.Helpers;
using System.Collections.Generic;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <c>Modify.IsolateSpaces</c> and the estate-wide ventilation system a building is often drawn with.
    /// <para>
    /// The real model behind this: three flats, each on its own MVHR with its own design terminals, and
    /// beside them one <c>MV</c> system related to the rooms of two of those flats, naming a central unit
    /// with nothing on it at all. Isolating either flat was refused - "ventilation system 'MV 1' also
    /// serves 3 space(s) outside the isolation scope" - although that system supplies no terminal, moves no
    /// air, and has no product selected against its unit. Every one of its rooms is already served by the
    /// MVHR that carries that room's design.
    /// </para>
    /// <para>
    /// <b>The rule is redundancy, not bareness.</b> A bare system whose rooms nothing else serves is plant
    /// somebody has still to detail, and it must go on refusing - which is what
    /// <c>PartOIsolationTests.SharedVentilationSystem_Refuses</c> and
    /// <c>SharedAirHandlingUnit_AcrossTwoSystems_Refuses</c> assert, unchanged by this. The tests here are
    /// the other half: the two sides of that line, and every way a system can be real enough to keep
    /// refusing.
    /// </para>
    /// </summary>
    public class PartOIsolationRedundantSystemTests
    {
        // ---- The case this exists for --------------------------------------------------------------

        /// <summary>
        /// The estate-wide system, exactly as the real model carries it: related to the rooms of both
        /// flats, no terminal, no air movement, its named unit bare. Both flats are already on MVHRs that
        /// carry design terminals, so it states nothing about their air that is not already stated, and
        /// isolating one flat proceeds.
        /// </summary>
        [Fact]
        public void EstateWideSystem_OverRoomsAlreadyOnDesignTerminals_DoesNotRefuse()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.Empty(spaceIsolation.Refusals);
            Assert.True(spaceIsolation.IsIsolated);
        }

        /// <summary>
        /// The other flat isolates too - the estate-wide system straddles the cut whichever side is
        /// selected, so ignoring it has to be symmetric.
        /// </summary>
        [Fact]
        public void EstateWideSystem_EitherDwellingIsolates()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            Assert.True(partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel)).IsIsolated);
            Assert.True(partFModel.AdjacencyCluster.IsolateSpaces(Flat2(partFModel)).IsIsolated);
        }

        /// <summary>
        /// Ignoring it is a refusal decision and nothing else: the system is still extracted into the
        /// derived model, related to the rooms of the selected dwelling that were retained. Dropping it
        /// would be an edit to the model, which this is not allowed to be.
        /// </summary>
        [Fact]
        public void TheIgnoredSystem_IsStillCarriedIntoTheDerivedModel()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            VentilationSystem ventilationSystem = VentilationSystem_Named(spaceIsolation.AdjacencyCluster, "Central AHU");

            Assert.NotNull(ventilationSystem);
            Assert.Equal(
                ["Bathroom", "Studio"],
                Names(spaceIsolation.AdjacencyCluster.GetRelatedObjects<Space>(ventilationSystem)));
        }

        /// <summary>The source model is not touched by any of it.</summary>
        [Fact]
        public void TheSourceModel_IsUnchanged()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            int count_Space = partFModel.AdjacencyCluster.GetSpaces().Count;
            int count_System = partFModel.AdjacencyCluster.GetObjects<VentilationSystem>().Count;
            int count_Unit = partFModel.AdjacencyCluster.GetObjects<AirHandlingUnit>().Count;
            int count_Related = partFModel.AdjacencyCluster
                .GetRelatedObjects<Space>(VentilationSystem_Named(partFModel.AdjacencyCluster, "Central AHU")).Count;

            partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.Equal(count_Space, partFModel.AdjacencyCluster.GetSpaces().Count);
            Assert.Equal(count_System, partFModel.AdjacencyCluster.GetObjects<VentilationSystem>().Count);
            Assert.Equal(count_Unit, partFModel.AdjacencyCluster.GetObjects<AirHandlingUnit>().Count);
            Assert.Equal(count_Related, partFModel.AdjacencyCluster
                .GetRelatedObjects<Space>(VentilationSystem_Named(partFModel.AdjacencyCluster, "Central AHU")).Count);
        }

        // ---- Fail closed: every way a shared system stays real -------------------------------------

        /// <summary>
        /// The same estate-wide system over a room nothing else serves - the corridor. That room's
        /// ventilation is stated by this system alone, so it is plant awaiting detail rather than a second
        /// statement about air already accounted for, and the refusal stands.
        /// </summary>
        [Fact]
        public void SharedSystem_OverARoomNothingElseServes_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Corridor");

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Null(spaceIsolation.AdjacencyCluster);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV") && x.Contains("Corridor"));
        }

        /// <summary>
        /// A shared system that carries design terminals of its own is the design chain, however many other
        /// systems its rooms are also on. Refuses.
        /// </summary>
        [Fact]
        public void SharedSystem_WithItsOwnDesignTerminals_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            VentilationSystem ventilationSystem = EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            VentilationTerminal ventilationTerminal = new("Central supply terminal", FlowClassification.Supply, 50);
            partFModel.AdjacencyCluster.AddObject(ventilationTerminal);
            partFModel.AdjacencyCluster.AddRelation(ventilationSystem, ventilationTerminal);

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV"));
        }

        /// <summary>
        /// A shared system whose named unit has had a product selected against it has a capacity somebody
        /// chose, so splitting it between dwellings is a real decision. Refuses.
        /// </summary>
        [Fact]
        public void SharedSystem_WhoseUnitHasASelectedProduct_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            AirHandlingUnit airHandlingUnit = AirHandlingUnit_Named(partFModel.AdjacencyCluster, "Central AHU");
            airHandlingUnit.SetValue(AirHandlingUnitParameter.VentilationUnitReference, new VentilationUnitReference("Manufacturer", "Model 500", "MAN-500"));
            partFModel.AdjacencyCluster.AddObject(airHandlingUnit);

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV") || x.Contains("Central AHU"));
        }

        /// <summary>
        /// A shared system whose named unit carries its own supply condition - the object that becomes the
        /// unit's generated TAS plant zone - is plant that reaches the simulation. Refuses.
        /// </summary>
        [Fact]
        public void SharedSystem_WhoseUnitCarriesItsOwnAirMovement_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            AirHandlingUnit airHandlingUnit = AirHandlingUnit_Named(partFModel.AdjacencyCluster, "Central AHU");
            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new("Central AHU");
            partFModel.AdjacencyCluster.AddObject(airHandlingUnitAirMovement);
            partFModel.AdjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV") || x.Contains("Central AHU"));
        }

        /// <summary>
        /// A room whose only other system is itself bare is not covered: two weak statements about a room
        /// do not make either of them redundant. Refuses.
        /// </summary>
        [Fact]
        public void SharedSystem_WhoseRoomsOnlyOtherSystemIsAlsoBare_StillRefuses()
        {
            //Deliberately the fixture WITHOUT design terminals: the per-dwelling MVHRs are bare too.
            PartFModel partFModel = Fixture_WithoutDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV") || x.Contains("Central AHU"));
        }

        /// <summary>
        /// A shared unit reached through two bare systems, whose rooms ARE otherwise covered, is still
        /// refused where the unit itself is real - here through a design terminal hanging off it.
        /// </summary>
        [Fact]
        public void SharedUnit_ThatIsRealPlant_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            EstateWideSystem(partFModel, "Central AHU", "Studio", "Bathroom", "Bedroom", "Kitchen");

            AirHandlingUnit airHandlingUnit = AirHandlingUnit_Named(partFModel.AdjacencyCluster, "Central AHU");
            VentilationTerminal ventilationTerminal = new("Central plant terminal", FlowClassification.Extract, 50);
            partFModel.AdjacencyCluster.AddObject(ventilationTerminal);
            partFModel.AdjacencyCluster.AddRelation(airHandlingUnit, ventilationTerminal);

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Estate MV") || x.Contains("Central AHU"));
        }

        /// <summary>
        /// The dwelling's own MVHR straddling the cut is untouched by any of this: it carries the design
        /// terminals, so it is never redundant and never ignored.
        /// </summary>
        [Fact]
        public void ADesignSystem_StraddlingTheCut_StillRefuses()
        {
            PartFModel partFModel = Fixture_WithDesignTerminals();

            //Flat 1's own MVHR is put on the corridor too.
            partFModel.AdjacencyCluster.AddRelation(
                VentilationSystem_Named(partFModel.AdjacencyCluster, "AHU 1"),
                partFModel.Get("Corridor"));

            SpaceIsolation spaceIsolation = partFModel.AdjacencyCluster.IsolateSpaces(Flat1(partFModel));

            Assert.False(spaceIsolation.IsIsolated);
            Assert.Contains(spaceIsolation.Refusals, x => x.Contains("Corridor"));
        }

        // ---- Fixture -------------------------------------------------------------------------------

        /// <summary>
        /// Flat 1 (Studio, Bathroom), Flat 2 (Bedroom, Kitchen), Corridor - each flat on its own MVHR,
        /// and every one of those four rooms carrying a design ventilation terminal on its own system, the
        /// way the Part O preparation leaves the model. The corridor deliberately has none.
        /// </summary>
        private static PartFModel Fixture_WithDesignTerminals()
        {
            PartFModel partFModel = Fixture_WithoutDesignTerminals();

            DesignTerminal(partFModel, "AHU 1", "Studio", FlowClassification.Supply, 30);
            DesignTerminal(partFModel, "AHU 1", "Bathroom", FlowClassification.Extract, 8);
            DesignTerminal(partFModel, "AHU 2", "Bedroom", FlowClassification.Supply, 25);
            DesignTerminal(partFModel, "AHU 2", "Kitchen", FlowClassification.Extract, 13);

            return partFModel;
        }

        /// <summary>The same building with bare systems - no terminal anywhere.</summary>
        private static PartFModel Fixture_WithoutDesignTerminals()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 25, 62.5)
                .Space("Bathroom", 5, 12.5)
                .Space("Bedroom", 14, 35)
                .Space("Kitchen", 10, 25)
                .Space("Corridor", 20, 50)
                .Zone("Flat 1", "Dwelling", true, "Studio", "Bathroom")
                .Zone("Flat 2", "Dwelling", true, "Bedroom", "Kitchen")
                .Partition("Studio", "Bathroom", "Door Studio Bathroom")
                .Partition("Studio", "Corridor", "Door Flat 1 Corridor")
                .Partition("Bedroom", "Corridor", "Door Flat 2 Corridor")
                .Partition("Bedroom", "Kitchen", "Door Bedroom Kitchen")
                .ExternalWall("Studio")
                .ExternalWall("Bathroom", window: false)
                .ExternalWall("Bedroom")
                .ExternalWall("Kitchen");

            MVHR(partFModel, "AHU 1", "Studio", "Bathroom");
            MVHR(partFModel, "AHU 2", "Bedroom", "Kitchen");

            RealConstructions(partFModel.AdjacencyCluster);

            return partFModel;
        }

        private static void MVHR(PartFModel partFModel, string name_Unit, params string[] names_Space)
        {
            VentilationSystem ventilationSystem = new(name_Unit, new VentilationSystemType("MVHR", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, name_Unit);

            partFModel.AdjacencyCluster.AddObject(ventilationSystem);

            foreach (string name_Space in names_Space)
            {
                partFModel.AdjacencyCluster.AddRelation(ventilationSystem, partFModel.Get(name_Space));
            }

            partFModel.AdjacencyCluster.AddObject(new AirHandlingUnit(name_Unit, 20, 20));
        }

        /// <summary>
        /// One design ventilation terminal, related to its room and to the system that serves it - the
        /// shape the Part O preparation builds, and the one the design chain is recognised by.
        /// </summary>
        private static void DesignTerminal(PartFModel partFModel, string name_System, string name_Space, FlowClassification flowClassification, double flow_Lps)
        {
            VentilationTerminal ventilationTerminal = new(string.Format("{0} - {1} terminal", name_Space, flowClassification), flowClassification, flow_Lps);

            partFModel.AdjacencyCluster.AddObject(ventilationTerminal);
            partFModel.AdjacencyCluster.AddRelation(VentilationSystem_Named(partFModel.AdjacencyCluster, name_System), ventilationTerminal);
            partFModel.AdjacencyCluster.AddRelation(partFModel.Get(name_Space), ventilationTerminal);
        }

        /// <summary>
        /// The estate-wide MV system: related to the named rooms, naming a unit that carries nothing at all.
        /// </summary>
        private static VentilationSystem EstateWideSystem(PartFModel partFModel, string name_Unit, params string[] names_Space)
        {
            VentilationSystem ventilationSystem = new("Estate MV", new VentilationSystemType("MV", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, name_Unit);
            ventilationSystem.SetValue(VentilationSystemParameter.ExhaustUnitName, name_Unit);

            partFModel.AdjacencyCluster.AddObject(ventilationSystem);

            foreach (string name_Space in names_Space)
            {
                partFModel.AdjacencyCluster.AddRelation(ventilationSystem, partFModel.Get(name_Space));
            }

            partFModel.AdjacencyCluster.AddObject(new AirHandlingUnit(name_Unit, 20, 20));

            return ventilationSystem;
        }

        private static void RealConstructions(AdjacencyCluster adjacencyCluster)
        {
            foreach (Panel panel in adjacencyCluster.GetPanels() ?? [])
            {
                Construction construction = new(
                    panel.Construction.Guid,
                    panel.Construction.Name,
                    [new ConstructionLayer("Concrete", 0.2)]);

                adjacencyCluster.AddObject(SAM.Analytical.Create.Panel(panel, construction));
            }
        }

        private static List<Space> Flat1(PartFModel partFModel)
        {
            return [partFModel.Get("Studio"), partFModel.Get("Bathroom")];
        }

        private static List<Space> Flat2(PartFModel partFModel)
        {
            return [partFModel.Get("Bedroom"), partFModel.Get("Kitchen")];
        }

        private static VentilationSystem VentilationSystem_Named(AdjacencyCluster adjacencyCluster, string name)
        {
            return adjacencyCluster.GetObjects<VentilationSystem>()
                .Find(x => x.GetValue<string>(VentilationSystemParameter.SupplyUnitName) == name);
        }

        private static AirHandlingUnit AirHandlingUnit_Named(AdjacencyCluster adjacencyCluster, string name)
        {
            return adjacencyCluster.GetObjects<AirHandlingUnit>().Find(x => x.Name == name);
        }

        private static List<string> Names(IEnumerable<Space> spaces)
        {
            List<string> result = [];

            foreach (Space space in spaces ?? [])
            {
                result.Add(space?.Name);
            }

            result.Sort();

            return result;
        }
    }
}
