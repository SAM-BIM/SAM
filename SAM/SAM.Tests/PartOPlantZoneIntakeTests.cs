// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using Xunit;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Tests
{
    /// <summary>
    /// <b>The generated TAS plant zone has to balance, and an isolated model used to hand it a zone that
    /// could not.</b>
    ///
    /// <para><b>What the plant zone is, and what balances it</b></para>
    /// <para>
    /// <c>SAM.Analytical.Tas.Modify.UpdateIZAMs</c> builds one small TAS zone per air handling unit - the
    /// unit's own plant zone, named after it ("MVHR-01") - and writes it one inter-zone air movement per
    /// room the unit <b>supplies</b>, plus one <c>"IZAM &lt;unit&gt; FROM OUTSIDE"</c> that brings in what
    /// the unit therefore has to draw. That intake is sized by <c>Analytical.Query.AirFlow</c>, which reads the
    /// unit's own deliveries; where it resolves nothing, <b>the intake is simply not written</b> and the
    /// zone delivers the dwelling's whole supply while gaining nothing. TAS refuses to simulate a zone
    /// whose air movements do not balance, and reports it as nothing more informative than
    /// <i>"Simulation Failed"</i>.
    /// </para>
    ///
    /// <para><b>The isolation defect</b></para>
    /// <para>
    /// <c>Modify.IsolateSpaces</c> depended on two <c>IJSAMObject</c> lookups that cannot work on an
    /// <see cref="AdjacencyCluster"/>: <c>AdjacencyCluster.IsValid(Type)</c> asks whether a type is
    /// assignable TO one of the analytical families it admits, which <c>IJSAMObject</c> - broader than all
    /// of them - is not, and both <c>RelationCluster.GetObjects(Type)</c> and
    /// <c>RelationCluster.GetObject(Type, Guid)</c> gate on it. So
    /// <c>result.GetObjects&lt;IJSAMObject&gt;()</c> answered null however much the cluster held, and
    /// <c>result.GetObject&lt;IJSAMObject&gt;(guid)</c> answered null for every guid in it.
    /// </para>
    /// <para>
    /// Two consequences, both fixed here. The relation restoration returned at its first line and restored
    /// <b>nothing</b>; and the plant carry's existence guard was unsatisfiable, so the unit arrived in the
    /// derived model related to nothing but its own <see cref="AirHandlingUnitAirMovement"/>. On the
    /// licensed acceptance model that produced a plant zone delivering 36.3 l/s and taking in nothing, and
    /// TAS refused the isolated Flat 1 run - with a 22 KB stub of a results file where a full year belongs.
    /// </para>
    /// <para>
    /// A third gap in the same family: the unit's own exhaust is a <see cref="SpaceAirMovement"/> related to
    /// <b>no space at all</b>, so nothing reachable from the selection carried it.
    /// </para>
    ///
    /// <para><b>And SAM Check now sees it first</b></para>
    /// <para>
    /// The state is deterministic and visible in the analytical model long before a TBD exists, so it is a
    /// <c>Create.Log</c> rule rather than something to rediscover in TAS. See the Part O pre-simulation gate
    /// in SAM_UI (<c>PartOPreSimulationCheck</c>), which refuses the run on it.
    /// </para>
    /// </summary>
    public class PartOPlantZoneIntakeTests
    {
        // ---- A. The Check rule -------------------------------------------------------------------------

        /// <summary>
        /// A unit that delivers supply somewhere resolves an intake, so the plant zone will balance and
        /// nothing is reported.
        /// </summary>
        [Fact]
        public void AUnitThatDeliversSupply_ReportsNothing()
        {
            AdjacencyCluster adjacencyCluster = Plant(out AirHandlingUnitAirMovement airHandlingUnitAirMovement, supply: true, exhaust: true);

            Assert.Empty(AnalyticalCreate.Log(airHandlingUnitAirMovement, adjacencyCluster));
        }

        /// <summary>
        /// <b>A unit that delivers nothing is an error</b>, naming the movement, the unit and both Guids -
        /// because a plant zone with no intake is a model TAS will not simulate.
        /// </summary>
        [Fact]
        public void AUnitThatDeliversNothing_IsAnError()
        {
            AdjacencyCluster adjacencyCluster = Plant(out AirHandlingUnitAirMovement airHandlingUnitAirMovement, supply: false, exhaust: true);

            LogRecord logRecord = Assert.Single(AnalyticalCreate.Log(airHandlingUnitAirMovement, adjacencyCluster));

            Assert.Equal(LogRecordType.Error, logRecord.LogRecordType);
            Assert.Contains("MVHR-01", logRecord.Text);
            Assert.Contains(airHandlingUnitAirMovement.Guid.ToString(), logRecord.Text);
            Assert.Contains("resolves no intake air flow", logRecord.Text);
            Assert.Contains("air movements do not balance", logRecord.Text);
        }

        /// <summary>
        /// <b>The unit's own exhaust is not a delivery.</b> It names the unit as its source and names no
        /// destination - it is air leaving the building - so counting it as intake would have the unit draw
        /// its own duty twice. A unit with nothing but an exhaust still has no intake to size.
        /// </summary>
        [Fact]
        public void AUnitWithOnlyAnExhaust_IsStillAnError()
        {
            AdjacencyCluster adjacencyCluster = Plant(out AirHandlingUnitAirMovement airHandlingUnitAirMovement, supply: false, exhaust: true);

            //The exhaust IS there - so this is not "the unit has no air movements at all".
            Assert.Single(adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(Unit(adjacencyCluster)) ?? []);

            Assert.Single(AnalyticalCreate.Log(airHandlingUnitAirMovement, adjacencyCluster));
        }

        /// <summary>
        /// An air movement related to no unit at all names nothing whose supply condition it carries, and
        /// no plant zone can be generated from it.
        /// </summary>
        [Fact]
        public void AnAirMovementRelatedToNoUnit_IsAnError()
        {
            AdjacencyCluster adjacencyCluster = new();

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);

            LogRecord logRecord = Assert.Single(AnalyticalCreate.Log(airHandlingUnitAirMovement, adjacencyCluster));

            Assert.Equal(LogRecordType.Error, logRecord.LogRecordType);
            Assert.Contains("related to no AirHandlingUnit", logRecord.Text);
        }

        /// <summary>
        /// <b>Asked without a cluster, the rule says nothing about the intake.</b> Whether a unit delivers
        /// anything is a question about the model's relations, and an object handed over on its own cannot
        /// answer it - so it must not guess, in either direction.
        /// </summary>
        [Fact]
        public void WithNoCluster_NothingIsSaidAboutTheIntake()
        {
            Plant(out AirHandlingUnitAirMovement airHandlingUnitAirMovement, supply: false, exhaust: false);

            Assert.Empty(AnalyticalCreate.Log(airHandlingUnitAirMovement));
        }

        /// <summary>The model level Check reaches the rule, which is what the Part O gate runs.</summary>
        [Fact]
        public void TheModelCheck_ReportsAUnitThatDeliversNothing()
        {
            AdjacencyCluster adjacencyCluster = Plant(out AirHandlingUnitAirMovement _, supply: false, exhaust: true);

            AnalyticalModel analyticalModel = new("Block", null, null, null, adjacencyCluster, new MaterialLibrary("M"), new ProfileLibrary("P"));

            Assert.Contains(
                AnalyticalCreate.Log(analyticalModel),
                x => x.LogRecordType == LogRecordType.Error && x.Text.Contains("resolves no intake air flow", StringComparison.Ordinal));
        }

        // ---- B. The isolation regression ---------------------------------------------------------------

        /// <summary>
        /// <b>The derived unit keeps the air movements it supplies.</b> This is the relation the isolation
        /// lost, and losing it is what left the plant zone with no intake.
        /// </summary>
        [Fact]
        public void IsolatedUnit_KeepsTheAirMovementsItSupplies()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel partFModel);

            AdjacencyCluster adjacencyCluster = spaceIsolation.AdjacencyCluster;
            AirHandlingUnit airHandlingUnit = Unit(adjacencyCluster);

            Assert.NotNull(airHandlingUnit);

            List<string> names = [];
            foreach (SpaceAirMovement spaceAirMovement in adjacencyCluster.GetRelatedObjects<SpaceAirMovement>(airHandlingUnit) ?? [])
            {
                names.Add(spaceAirMovement.Name);
            }

            names.Sort(StringComparer.Ordinal);

            Assert.Equal(["Bathroom extract", "MVHR-01 exhaust", "Studio supply"], names);
        }

        /// <summary>
        /// <b>And therefore resolves an intake air flow.</b> This is the one assertion that stands between
        /// an isolated run and a TAS refusal: no profile here means no
        /// <c>"IZAM &lt;unit&gt; FROM OUTSIDE"</c> in the TBD.
        /// </summary>
        [Fact]
        public void IsolatedUnit_ResolvesAnIntakeAirFlow()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel _);

            AdjacencyCluster adjacencyCluster = spaceIsolation.AdjacencyCluster;

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = Assert.Single(adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>() ?? []);

            double airFlow = Analytical.Query.AirFlow(adjacencyCluster, airHandlingUnitAirMovement, out Profile profile);

            Assert.NotNull(profile);
            Assert.Equal(0.03, airFlow, 6);
        }

        /// <summary>
        /// <b>The unit's own plant side air movements come across.</b> The exhaust is related to no space,
        /// so nothing reachable from the selection carries it - and a unit that gains the extract duty and
        /// never loses it is exactly the zone TAS refuses.
        /// </summary>
        [Fact]
        public void IsolatedUnit_CarriesItsOwnPlantSideAirMovements()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel _);

            Assert.Contains(
                spaceIsolation.AdjacencyCluster.GetObjects<SpaceAirMovement>() ?? [],
                x => x.Name == "MVHR-01 exhaust");

            Assert.Contains(spaceIsolation.Notes, x => x.Contains("plant side air movement", StringComparison.Ordinal));
        }

        /// <summary>
        /// A movement that belongs to an <b>excluded</b> dwelling's rooms is not dragged in by the plant
        /// carry. Only movements related to no space at all are plant side.
        /// </summary>
        [Fact]
        public void ThePlantCarry_DoesNotBringInAnExcludedDwellingsRoomAirMovements()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel _);

            foreach (SpaceAirMovement spaceAirMovement in spaceIsolation.AdjacencyCluster.GetObjects<SpaceAirMovement>() ?? [])
            {
                Assert.DoesNotContain("Bedroom", spaceAirMovement.Name, StringComparison.Ordinal);
                Assert.DoesNotContain("Kitchen", spaceAirMovement.Name, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// <b>The derived relation graph is more than what the filter made.</b> The supply movement is
        /// related to its space, which the geometric filter did, <b>and</b> to its unit, which only the
        /// relation restoration can do.
        /// </summary>
        [Fact]
        public void IsolatedModel_RestoresSourceRelationsBetweenCarriedObjects()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel _);

            AdjacencyCluster adjacencyCluster = spaceIsolation.AdjacencyCluster;

            SpaceAirMovement spaceAirMovement = (adjacencyCluster.GetObjects<SpaceAirMovement>() ?? []).Find(x => x.Name == "Studio supply");

            Assert.NotNull(spaceAirMovement);
            Assert.NotEmpty(adjacencyCluster.GetRelatedObjects<Space>(spaceAirMovement) ?? []);
            Assert.NotEmpty(adjacencyCluster.GetRelatedObjects<AirHandlingUnit>(spaceAirMovement) ?? []);
        }

        /// <summary>The isolated model passes the intake rule, which is the regression stated as the Check sees it.</summary>
        [Fact]
        public void IsolatedModel_PassesTheIntakeCheck()
        {
            SpaceIsolation spaceIsolation = Isolated(out PartFModel _);

            AdjacencyCluster adjacencyCluster = spaceIsolation.AdjacencyCluster;

            foreach (AirHandlingUnitAirMovement airHandlingUnitAirMovement in adjacencyCluster.GetObjects<AirHandlingUnitAirMovement>() ?? [])
            {
                Assert.Empty(AnalyticalCreate.Log(airHandlingUnitAirMovement, adjacencyCluster));
            }
        }

        /// <summary>The source model is untouched by any of it.</summary>
        [Fact]
        public void SourceModel_IsUnchangedByThePlantCarry()
        {
            PartFModel partFModel = PlantFixture();

            string before = partFModel.AdjacencyCluster.ToJsonObject().ToJsonString();

            partFModel.AdjacencyCluster.IsolateSpaces([partFModel.Get("Studio"), partFModel.Get("Bathroom")]);

            Assert.Equal(before, partFModel.AdjacencyCluster.ToJsonObject().ToJsonString());
        }

        // ---- The fixture -------------------------------------------------------------------------------

        /// <summary>
        /// A unit, its air movement, and optionally the two movements a real one has: the supply it
        /// delivers to a room, and its own exhaust.
        /// </summary>
        private static AdjacencyCluster Plant(out AirHandlingUnitAirMovement airHandlingUnitAirMovement, bool supply, bool exhaust)
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space = new("Studio");
            adjacencyCluster.AddObject(space);

            AirHandlingUnit airHandlingUnit = new("MVHR-01", 20, 20);
            adjacencyCluster.AddObject(airHandlingUnit);

            airHandlingUnitAirMovement = new AirHandlingUnitAirMovement("MVHR-01");
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            string reference_Unit = new ObjectReference(airHandlingUnit).ToString();
            string reference_Space = new ObjectReference(space).ToString();

            if (supply)
            {
                SpaceAirMovement spaceAirMovement = new("Studio supply", 0.03, reference_Unit, reference_Space);
                adjacencyCluster.AddObject(spaceAirMovement);
                adjacencyCluster.AddRelation(spaceAirMovement, airHandlingUnit);
                adjacencyCluster.AddRelation(spaceAirMovement, space);
            }

            if (exhaust)
            {
                //From the unit, to nowhere. That is how "leaving the building" is said, and it is why the
                //exhaust must not be read as a delivery.
                SpaceAirMovement spaceAirMovement = new("MVHR-01 exhaust", 0.03, reference_Unit, null);
                adjacencyCluster.AddObject(spaceAirMovement);
                adjacencyCluster.AddRelation(spaceAirMovement, airHandlingUnit);
            }

            return adjacencyCluster;
        }

        private static AirHandlingUnit Unit(AdjacencyCluster adjacencyCluster)
        {
            return (adjacencyCluster?.GetObjects<AirHandlingUnit>() ?? []).Find(x => x.Name == "MVHR-01");
        }

        /// <summary>Flat 1 (Studio, Bathroom) isolated out of the plant fixture.</summary>
        private static SpaceIsolation Isolated(out PartFModel partFModel)
        {
            partFModel = PlantFixture();

            SpaceIsolation result = partFModel.AdjacencyCluster.IsolateSpaces([partFModel.Get("Studio"), partFModel.Get("Bathroom")]);

            Assert.True(result.IsIsolated, string.Join(" ", result.Refusals ?? []));

            return result;
        }

        /// <summary>
        /// Flat 1 (Studio, Bathroom) and Flat 2 (Bedroom, Kitchen) with a corridor between them, Flat 1 on
        /// MVHR-01 with the air movements a realized Part O dwelling has - the unit's supply into the
        /// Studio, the Bathroom's extract back to the unit, the transfer air between them, and the unit's
        /// own exhaust - and Flat 2 on MVHR-02, so the isolation has something to leave behind.
        /// </summary>
        private static PartFModel PlantFixture()
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

            RealConstructions(partFModel.AdjacencyCluster);

            Dwelling(partFModel, "MVHR-01", "Studio", "Bathroom");
            Dwelling(partFModel, "MVHR-02", "Bedroom", "Kitchen");

            return partFModel;
        }

        /// <summary>
        /// One dwelling on one dedicated unit, realized: the system, the unit, the unit's supply condition,
        /// and the four air movements the TAS export reads.
        /// </summary>
        private static void Dwelling(PartFModel partFModel, string name_Unit, string name_Supplied, string name_Extracted)
        {
            AdjacencyCluster adjacencyCluster = partFModel.AdjacencyCluster;

            Space space_Supplied = partFModel.Get(name_Supplied);
            Space space_Extracted = partFModel.Get(name_Extracted);

            VentilationSystem ventilationSystem = new(name_Unit, new VentilationSystemType("MVHR", "Fixture"));
            ventilationSystem.SetValue(VentilationSystemParameter.SupplyUnitName, name_Unit);
            adjacencyCluster.AddObject(ventilationSystem);
            adjacencyCluster.AddRelation(ventilationSystem, space_Supplied);
            adjacencyCluster.AddRelation(ventilationSystem, space_Extracted);

            AirHandlingUnit airHandlingUnit = new(name_Unit, 20, 20);
            adjacencyCluster.AddObject(airHandlingUnit);

            AirHandlingUnitAirMovement airHandlingUnitAirMovement = new(name_Unit);
            adjacencyCluster.AddObject(airHandlingUnitAirMovement);
            adjacencyCluster.AddRelation(airHandlingUnit, airHandlingUnitAirMovement);

            string reference_Unit = new ObjectReference(airHandlingUnit).ToString();
            string reference_Supplied = new ObjectReference(space_Supplied).ToString();
            string reference_Extracted = new ObjectReference(space_Extracted).ToString();

            Add(adjacencyCluster, string.Format("{0} supply", name_Supplied), 0.03, reference_Unit, reference_Supplied, airHandlingUnit, space_Supplied);
            Add(adjacencyCluster, string.Format("{0} extract", name_Extracted), 0.03, reference_Extracted, reference_Unit, airHandlingUnit, space_Extracted);
            Add(adjacencyCluster, string.Format("{0} to {1} transfer", name_Supplied, name_Extracted), 0.03, reference_Supplied, reference_Extracted, null, space_Extracted);

            //From the unit, to nowhere - the extract air leaving the building. Related to the unit and to
            //no space at all, which is exactly why a space-shaped filter cannot carry it.
            Add(adjacencyCluster, string.Format("{0} exhaust", name_Unit), 0.03, reference_Unit, null, airHandlingUnit, null);
        }

        private static void Add(AdjacencyCluster adjacencyCluster, string name, double airFlow, string from, string to, AirHandlingUnit airHandlingUnit, Space space)
        {
            SpaceAirMovement spaceAirMovement = new(name, airFlow, from, to);
            adjacencyCluster.AddObject(spaceAirMovement);

            if (airHandlingUnit is not null)
            {
                adjacencyCluster.AddRelation(spaceAirMovement, airHandlingUnit);
            }

            if (space is not null)
            {
                adjacencyCluster.AddRelation(spaceAirMovement, space);
            }
        }

        /// <summary>
        /// Gives every panel a construction with a real thickness, so <c>Query.Adiabatic</c> does not
        /// report every zero-thickness surface as already adiabatic and hide the isolation cut.
        /// </summary>
        private static void RealConstructions(AdjacencyCluster adjacencyCluster)
        {
            foreach (Panel panel in adjacencyCluster.GetPanels() ?? [])
            {
                Construction construction = new(
                    panel.Construction.Guid,
                    panel.Construction.Name,
                    [new ConstructionLayer("Concrete", 0.2)]);

                adjacencyCluster.AddObject(AnalyticalCreate.Panel(panel, construction));
            }
        }
    }
}
