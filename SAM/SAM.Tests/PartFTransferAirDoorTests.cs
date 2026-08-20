// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Analytical.Enums;
using SAM.Geometry.Spatial;
using SAM.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using AnalyticalCreate = SAM.Analytical.Create;

namespace SAM.Tests
{
    /// <summary>
    /// Tests for <see cref="Modify.AddTransferAirDoorsByPartF"/>: the creation of the internal
    /// transfer-air doors Approved Document F, Volume 1: Dwellings (2021 edition) paragraph 1.25
    /// (page 10) requires where the model carries none.
    /// <para>
    /// The door's width is the paragraph's own reference width - 760mm, so a 10mm undercut across it is
    /// exactly the required 7,600mm2 free area. The requirement the door carries, the flows on its route
    /// and the assessment of whatever is later provided all come from the same
    /// <see cref="PartFCalculator"/> the rest of the Part F workflow runs; these tests assert that
    /// sharing, not a second copy of it.
    /// </para>
    /// </summary>
    //One test below temporarily replaces ActiveSetting's default aperture construction library to prove
    //the refusal it drives, so every test class that reads that library names this collection: xUnit runs
    //collections in parallel and the classes within one collection in sequence, and the swap must never be
    //observable from another test. QuadraticScanRegressionTests is the only other reader.
    [Collection("SAM.Analytical.ActiveSetting default aperture construction library")]
    public class PartFTransferAirDoorTests
    {
        private const double tolerance = 1e-6;

        // ------------------------------------------------------------------
        // A - transfer air required, a suitable door already exists
        // ------------------------------------------------------------------

        /// <summary>
        /// A route with a modelled door is never duplicated: no door is created, the existing door keeps
        /// its identity, and its record carries the refreshed paragraph 1.25 requirement.
        /// </summary>
        [Fact]
        public void ExistingDoor_NoDoorCreated_IdentityKept()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300)
                .Space("Bathroom", 25, 100)
                .Partition("Studio", "Bathroom", "D01");

            Guid guid_Door = DoorGuids(partFModel.AdjacencyCluster).Single();

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out List<string> notes, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(doors_Created);
            Assert.Empty(refusals);
            Assert.Contains(notes, x => x.Contains("existing internal door"));

            //The existing door is still there, alone, with its guid and its refreshed record.
            AdjacencyCluster adjacencyCluster = result.AdjacencyCluster;
            Aperture aperture = Assert.Single(Doors(adjacencyCluster));
            Assert.Equal(guid_Door, aperture.Guid);

            PartFDoorTransferData partFDoorTransferData = adjacencyCluster.GetPartFDoorTransferData()[guid_Door];
            Assert.True(partFDoorTransferData.IsDoorRepresented);
            Assert.Equal(7600, partFDoorTransferData.MinimumRequiredFreeArea_mm2.Value, tolerance);
            Assert.Equal(10, partFDoorTransferData.RequiredUndercutHeightFinished_mm.Value, tolerance);
            Assert.Equal(20, partFDoorTransferData.RequiredUndercutHeightBeforeFloorFinish_mm.Value, tolerance);
            Assert.Equal(900, partFDoorTransferData.ClearDoorWidth_mm.Value, 1e-3);
            Assert.Equal(8, partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps.Value, tolerance);
        }

        /// <summary>
        /// The engineering inputs recorded on an existing door - here a provided 12mm undercut with the
        /// floor finish fitted - survive the operation untouched, and the door still passes.
        /// </summary>
        [Fact]
        public void ExistingDoor_EngineeringInputsSurvive()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300)
                .Space("Bathroom", 25, 100)
                .Partition("Studio", "Bathroom", "D01")
                .DoorInput("D01", providedUndercutHeight_mm: 12, isFloorFinishFitted: true);

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out _);

            Assert.Empty(doors_Created);

            PartFDoorTransferData partFDoorTransferData = result.AdjacencyCluster.GetPartFDoorTransferData().Values.Single();
            Assert.Equal(12, partFDoorTransferData.ProvidedUndercutHeight_mm.Value, tolerance);
            Assert.True(partFDoorTransferData.IsFloorFinishFitted);
            Assert.Equal(PartFComplianceStatus.Pass, partFDoorTransferData.ComplianceStatus);
        }

        // ------------------------------------------------------------------
        // B - transfer air required, no door modelled
        // ------------------------------------------------------------------

        /// <summary>
        /// Two adjacent spaces that must pass 8 l/s between them and have a shared internal wall but no
        /// door get exactly one door: in that wall, of the default geometry, with the paragraph 1.25
        /// requirement recorded on it.
        /// </summary>
        [Fact]
        public void NoDoor_OneDoorCreated_WithPartFRequirement()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300)
                .Space("Bathroom", 25, 100)
                .Partition("Studio", "Bathroom");

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out List<string> notes, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(refusals);

            Aperture aperture = Assert.Single(doors_Created);
            Assert.Equal(ApertureType.Door, aperture.ApertureType);

            //The default internal-door construction from the seeded library.
            Assert.Equal("SIM_INT_SLD", aperture.ApertureConstruction?.Name);

            //The default geometry: the paragraph 1.25 reference width, the programme's documented default
            //height, standing on the bottom edge of the wall.
            BoundingBox3D boundingBox3D = aperture.GetFace3D().GetBoundingBox();
            Assert.Equal(0.76, System.Math.Max(boundingBox3D.Max.X - boundingBox3D.Min.X, boundingBox3D.Max.Y - boundingBox3D.Min.Y), 1e-3);
            Assert.Equal(0, boundingBox3D.Min.Z, 1e-3);
            Assert.Equal(2.1, boundingBox3D.Max.Z - boundingBox3D.Min.Z, 1e-3);

            //In the one shared wall of the two spaces, and persisted in the returned model.
            AdjacencyCluster adjacencyCluster = result.AdjacencyCluster;
            Panel panel = adjacencyCluster.GetPanel(aperture);
            Assert.NotNull(panel);
            Assert.Equal(PanelType.WallInternal, panel.PanelType);
            Assert.Single(panel.Apertures);

            //The record is on the door, written through the persisting path: a requirement, the route's
            //calculated flow, and NOTHING provided - created is not compliant.
            PartFDoorTransferData partFDoorTransferData = adjacencyCluster.GetPartFDoorTransferData()[aperture.Guid];
            Assert.True(partFDoorTransferData.IsDoorRepresented);
            Assert.Equal(aperture.Guid, partFDoorTransferData.ApertureGuid);
            Assert.Equal(7600, partFDoorTransferData.MinimumRequiredFreeArea_mm2.Value, tolerance);
            Assert.Equal(10, partFDoorTransferData.RequiredUndercutHeightFinished_mm.Value, tolerance);
            Assert.Equal(20, partFDoorTransferData.RequiredUndercutHeightBeforeFloorFinish_mm.Value, tolerance);
            Assert.Equal(8, partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps.Value, tolerance);
            Assert.Equal(760, partFDoorTransferData.ClearDoorWidth_mm.Value, 1e-3);
            Assert.Null(partFDoorTransferData.ProvidedUndercutHeight_mm);
            Assert.Equal(PartFComplianceStatus.CannotBeDetermined, partFDoorTransferData.ComplianceStatus);

            //A 10mm undercut across the created door's width is exactly the required free area - the door
            //is sized so the paragraph 1.25a arrangement complies by construction.
            Assert.Equal(PartFDoorTransferData.NominalEquivalentFreeArea_mm2,
                PartFDoorTransferData.ReferenceUndercutHeight_mm * partFDoorTransferData.ClearDoorWidth_mm.Value, tolerance);

            Assert.Contains(notes, x => x.Contains("Studio to Bathroom") && x.Contains("760") && x.Contains("2100"));

            //The supplied model was not modified: still no aperture, still no Part F space data.
            Assert.Empty(Doors(partFModel.AdjacencyCluster));
            Assert.Null(partFModel.Get("Studio").GetValue<PartFSpaceData>(SpaceParameter.PartFSpaceData));
        }

        /// <summary>
        /// A window already in the partition is not a transfer door - the route is still unrepresented -
        /// and the created door is placed clear of it.
        /// </summary>
        [Fact]
        public void NoDoor_ExistingWindow_DoorPlacedClear()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom", 25, 100);

            //A 4m wall with a 0.6m window centred in it, crossing the door's vertical band.
            Panel panel = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(0, 4, 3));

            double x_Window = 1.7;
            panel.AddAperture(AnalyticalCreate.Aperture(new ApertureConstruction("Window", ApertureType.Window), new Face3D(new Polygon3D(
            [
                new Point3D(x_Window, 0, 1.0),
                new Point3D(x_Window + 0.6, 0, 1.0),
                new Point3D(x_Window + 0.6, 0, 2.2),
                new Point3D(x_Window, 0, 2.2),
            ]))));

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space_Studio, panel);
            adjacencyCluster.AddRelation(space_Bathroom, panel);

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.Empty(refusals);
            Aperture aperture = Assert.Single(doors_Created);

            //The panel now carries the window AND the door, and they do not overlap.
            Panel panel_Result = result.AdjacencyCluster.GetPanel(aperture);
            Assert.Equal(2, panel_Result.Apertures.Count);

            BoundingBox3D boundingBox3D_Door = aperture.GetFace3D().GetBoundingBox();
            BoundingBox3D boundingBox3D_Window = panel_Result.Apertures.First(x => x.ApertureType == ApertureType.Window).GetFace3D().GetBoundingBox();

            bool clear = boundingBox3D_Door.Max.X <= boundingBox3D_Window.Min.X + 1e-3 || boundingBox3D_Door.Min.X >= boundingBox3D_Window.Max.X - 1e-3;
            Assert.True(clear, "The created door overlaps the existing window.");
        }

        // ------------------------------------------------------------------
        // C - reruns
        // ------------------------------------------------------------------

        /// <summary>
        /// Running the operation on its own output creates nothing: the created door serves the route, so
        /// the second run sees a represented route and stops.
        /// </summary>
        [Fact]
        public void Rerun_CreatesNothing()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300)
                .Space("Bathroom", 25, 100)
                .Partition("Studio", "Bathroom");

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result_1 = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_1, out _, out _);
            Aperture aperture = Assert.Single(doors_1);

            AnalyticalModel result_2 = result_1.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_2, out List<string> notes_2, out List<string> refusals_2);

            Assert.Empty(doors_2);
            Assert.Empty(refusals_2);
            Assert.Contains(notes_2, x => x.Contains("existing internal door"));

            AdjacencyCluster adjacencyCluster = result_2.AdjacencyCluster;
            Aperture aperture_2 = Assert.Single(Doors(adjacencyCluster));
            Assert.Equal(aperture.Guid, aperture_2.Guid);
            Assert.True(adjacencyCluster.GetPartFDoorTransferData().ContainsKey(aperture.Guid));
        }

        // ------------------------------------------------------------------
        // D - no transfer-air requirement
        // ------------------------------------------------------------------

        /// <summary>One space on its own has no internal route at all, so nothing changes.</summary>
        [Fact]
        public void SingleSpace_NothingChanges()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300);

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out List<string> notes, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(doors_Created);
            Assert.Empty(refusals);
            Assert.Contains(notes, x => x.Contains("No transfer-air doors were required"));
            Assert.Empty(Doors(result.AdjacencyCluster));
        }

        /// <summary>
        /// Two adjacent bedrooms are both supplied, so nothing flows between them: the route carries no
        /// transfer air and earns no door. Paragraph 1.25 requirements attach to doors, not to partitions
        /// nobody needs to open.
        /// </summary>
        [Fact]
        public void NoTransferFlow_NoDoorCreated()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Bedroom 1", 40, 100)
                .Space("Bedroom 2", 40, 100)
                .Partition("Bedroom 1", "Bedroom 2");

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(doors_Created);
            Assert.Empty(refusals);
            Assert.Empty(Doors(result.AdjacencyCluster));
        }

        // ------------------------------------------------------------------
        // E - unresolved topology
        // ------------------------------------------------------------------

        /// <summary>
        /// Two spaces adjacent through a FLOOR - stacked rooms - have a transfer route but no wall a door
        /// could be hung in. No door is manufactured into the floor; the route is refused with the reason.
        /// </summary>
        [Fact]
        public void NoSharedWall_Refused()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom", 25, 100);

            Panel panel = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Floor"), PanelType.FloorInternal, new Face3D(new Polygon3D(
            [
                new Point3D(0, 0, 3),
                new Point3D(4, 0, 3),
                new Point3D(4, 5, 3),
                new Point3D(0, 5, 3),
            ])));

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space_Studio, panel);
            adjacencyCluster.AddRelation(space_Bathroom, panel);

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(doors_Created);

            string refusal = Assert.Single(refusals);
            Assert.Contains("Studio to Bathroom", refusal);
            Assert.Contains("no internal door could be created", refusal);
            Assert.Contains("share no internal wall", refusal);

            Assert.Empty(Doors(result.AdjacencyCluster));
        }

        /// <summary>
        /// A partition too narrow for the default door takes no door, and the refusal says so.
        /// </summary>
        [Fact]
        public void WallTooNarrow_Refused()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom", 25, 100);

            //0.5m of wall: the 0.76m door cannot fit.
            Panel panel = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(0, 0.5, 3));

            adjacencyCluster.AddObject(panel);
            adjacencyCluster.AddRelation(space_Studio, panel);
            adjacencyCluster.AddRelation(space_Bathroom, panel);

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.NotNull(result);
            Assert.Empty(doors_Created);

            string refusal = Assert.Single(refusals);
            Assert.Contains("0.5", refusal);
            Assert.Contains("does not fit", refusal);

            Assert.Empty(Doors(result.AdjacencyCluster));
        }

        // ------------------------------------------------------------------
        // F - identity, not names
        // ------------------------------------------------------------------

        /// <summary>
        /// Two flats, each with a Studio and a Bathroom - identical names. Flat 1's partition carries no
        /// door and Flat 2's carries one. The door must be created in FLAT 1's partition and nowhere else:
        /// matching is by space identity and adjacency, never by room name.
        /// </summary>
        [Fact]
        public void DuplicateRoomNames_DoorLandsInTheRightDwelling()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio_1 = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom_1 = Space(adjacencyCluster, "Bathroom", 25, 100);
            Space space_Studio_2 = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom_2 = Space(adjacencyCluster, "Bathroom", 25, 100);

            Panel panel_1 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(0, 4, 3));
            adjacencyCluster.AddObject(panel_1);
            adjacencyCluster.AddRelation(space_Studio_1, panel_1);
            adjacencyCluster.AddRelation(space_Bathroom_1, panel_1);

            Panel panel_2 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(10, 4, 3));
            panel_2.AddAperture(AnalyticalCreate.Aperture(new ApertureConstruction("D01", ApertureType.Door), Door(10, 0.9)));
            adjacencyCluster.AddObject(panel_2);
            adjacencyCluster.AddRelation(space_Studio_2, panel_2);
            adjacencyCluster.AddRelation(space_Bathroom_2, panel_2);

            Zone(adjacencyCluster, "Flat 1", space_Studio_1, space_Bathroom_1);
            Zone(adjacencyCluster, "Flat 2", space_Studio_2, space_Bathroom_2);

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF("Flats", null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.Empty(refusals);
            Aperture aperture = Assert.Single(doors_Created);

            //On Flat 1's partition, by guid.
            Panel panel_Created = result.AdjacencyCluster.GetPanel(aperture);
            Assert.Equal(panel_1.Guid, panel_Created.Guid);

            //Flat 2's partition kept exactly its one hand-modelled door.
            Panel panel_2_Result = result.AdjacencyCluster.GetObject<Panel>(panel_2.Guid);
            Assert.Single(panel_2_Result.Apertures);
            Assert.Equal("D01", panel_2_Result.Apertures[0].Name);
        }

        // ------------------------------------------------------------------
        // G - several shared walls
        // ------------------------------------------------------------------

        /// <summary>
        /// Two spaces separated by TWO internal wall panels, each of which could take the standard
        /// transfer door. Which of them the door belongs in is an architectural fact the model does not
        /// carry, so the route is refused as ambiguous and both panels are named. Nothing is decided by
        /// wall area, panel name, enumeration order or guid order - a defensible location is not a tie to
        /// be broken.
        /// <para>
        /// Run twice with the two panels created in opposite orders, because a resolution that depended on
        /// creation order would still look deterministic from a single run.
        /// </para>
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TwoSharedWallsCanBothTakeTheDoor_RefusedAsAmbiguous(bool reverseCreationOrder)
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom", 25, 100);

            //Two partitions between the same two spaces, both 4m x 3m: the door fits either one.
            Panel panel_1 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(0, 4, 3));
            Panel panel_2 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(10, 4, 3));

            foreach (Panel panel in reverseCreationOrder ? new Panel[] { panel_2, panel_1 } : new Panel[] { panel_1, panel_2 })
            {
                adjacencyCluster.AddObject(panel);
                adjacencyCluster.AddRelation(space_Studio, panel);
                adjacencyCluster.AddRelation(space_Bathroom, panel);
            }

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.NotNull(result);

            //No door created, and neither the returned model nor the supplied one carries a generated one.
            Assert.Empty(doors_Created);
            Assert.Empty(Doors(result.AdjacencyCluster));
            Assert.Empty(Doors(adjacencyCluster));

            //Both candidate walls are untouched - the candidates were tested, not modified.
            Assert.Null(result.AdjacencyCluster.GetObject<Panel>(panel_1.Guid).Apertures);
            Assert.Null(result.AdjacencyCluster.GetObject<Panel>(panel_2.Guid).Apertures);

            //One actionable refusal, naming both candidate panels by guid and saying what to do.
            string refusal = Assert.Single(refusals);
            Assert.Contains("Studio to Bathroom", refusal);
            Assert.Contains("2 shared wall panels can each take the transfer door", refusal);
            Assert.Contains("does not establish which of them it belongs in", refusal);
            Assert.Contains("model the door in the intended wall", refusal);
            Assert.Contains(panel_1.Guid.ToString(), refusal);
            Assert.Contains(panel_2.Guid.ToString(), refusal);

            //The panels are listed in guid order whichever order they were created in: the diagnostics are
            //stable, which is the ONLY thing guid ordering is used for.
            bool panel_1_First = panel_1.Guid.CompareTo(panel_2.Guid) < 0;
            Assert.True(refusal.IndexOf((panel_1_First ? panel_1 : panel_2).Guid.ToString()) < refusal.IndexOf((panel_1_First ? panel_2 : panel_1).Guid.ToString()),
                "The candidate panels are not listed in guid order.");
        }

        /// <summary>
        /// Three internal wall panels separate the two spaces and exactly ONE of them can take the standard
        /// door - the other two are too low for it. The door is created in that one, and in nothing else.
        /// <para>
        /// The panel that fits is the SMALLEST of the three, so a sole candidate established geometrically
        /// cannot be confused with the largest shared wall being picked.
        /// </para>
        /// </summary>
        [Fact]
        public void SeveralSharedWalls_OnlyOneCanTakeTheDoor_DoorCreatedThere()
        {
            AdjacencyCluster adjacencyCluster = new();

            Space space_Studio = Space(adjacencyCluster, "Studio", 75, 300);
            Space space_Bathroom = Space(adjacencyCluster, "Bathroom", 25, 100);

            //15m2 and 12m2 of wall, both only 1.5m high - the 2.1m door fits in neither.
            Panel panel_Low_1 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(0, 10, 1.5));
            Panel panel_Low_2 = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(20, 8, 1.5));

            //3m2 of wall, and the only one the 0.76m x 2.1m door fits in.
            Panel panel_Fits = AnalyticalCreate.Panel(new Construction(Guid.NewGuid(), "Internal Partition"), PanelType.WallInternal, Wall(40, 1, 3));

            foreach (Panel panel in new Panel[] { panel_Low_1, panel_Low_2, panel_Fits })
            {
                adjacencyCluster.AddObject(panel);
                adjacencyCluster.AddRelation(space_Studio, panel);
                adjacencyCluster.AddRelation(space_Bathroom, panel);
            }

            AnalyticalModel analyticalModel = new("Test", null, null, null, adjacencyCluster);

            AnalyticalModel result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out List<Aperture> doors_Created, out _, out List<string> refusals);

            Assert.Empty(refusals);
            Aperture aperture = Assert.Single(doors_Created);

            //In the one wall that can hold it.
            Panel panel_Created = result.AdjacencyCluster.GetPanel(aperture);
            Assert.Equal(panel_Fits.Guid, panel_Created.Guid);

            //And in nothing else: the two walls that cannot take the door are untouched.
            Assert.Null(result.AdjacencyCluster.GetObject<Panel>(panel_Low_1.Guid).Apertures);
            Assert.Null(result.AdjacencyCluster.GetObject<Panel>(panel_Low_2.Guid).Apertures);

            //The chosen wall really is the smallest of the three.
            Assert.True(panel_Fits.GetArea() < panel_Low_1.GetArea(), "The wall that fits is not smaller than the first wall that does not.");
            Assert.True(panel_Fits.GetArea() < panel_Low_2.GetArea(), "The wall that fits is not smaller than the second wall that does not.");
        }

        // ------------------------------------------------------------------
        // H - no established door construction
        // ------------------------------------------------------------------

        /// <summary>
        /// With no internal-door construction in the active aperture construction library, the route is
        /// refused. A construction is a real specification of a real building element, and one is not
        /// manufactured here merely so the geometry could be created - the model would then carry a door
        /// build-up that no library, no specification and no engineer ever established.
        /// <para>
        /// The library is swapped for an empty one and restored, which is why this class and the only other
        /// reader of that library share an xUnit collection - see the note on the class.
        /// </para>
        /// </summary>
        [Fact]
        public void NoDefaultInternalDoorConstruction_Refused()
        {
            PartFModel partFModel = new PartFModel()
                .Space("Studio", 75, 300)
                .Space("Bathroom", 25, 100)
                .Partition("Studio", "Bathroom");

            AnalyticalModel analyticalModel = new("Test", null, null, null, partFModel.AdjacencyCluster);

            Core.Setting setting = Analytical.ActiveSetting.Setting;
            ApertureConstructionLibrary apertureConstructionLibrary = setting.GetValue<ApertureConstructionLibrary>(AnalyticalSettingParameter.DefaultApertureConstructionLibrary);

            AnalyticalModel result;
            List<Aperture> doors_Created;
            List<string> refusals;
            try
            {
                setting.SetValue(AnalyticalSettingParameter.DefaultApertureConstructionLibrary, new ApertureConstructionLibrary("Empty"));

                result = analyticalModel.AddTransferAirDoorsByPartF(null, null, out doors_Created, out _, out refusals);
            }
            finally
            {
                setting.SetValue(AnalyticalSettingParameter.DefaultApertureConstructionLibrary, apertureConstructionLibrary);
            }

            Assert.NotNull(result);
            Assert.Empty(doors_Created);
            Assert.Empty(Doors(result.AdjacencyCluster));

            string refusal = Assert.Single(refusals);
            Assert.Contains("Studio to Bathroom", refusal);
            Assert.Contains("no default internal door construction could be resolved", refusal);
            Assert.DoesNotContain("Internal Door", refusal);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Space Space(AdjacencyCluster adjacencyCluster, string name, double area_M2, double volume_M3)
        {
            Space space = new(name, new Point3D(0, 0, 1.5));
            space.SetValue(SpaceParameter.Area, area_M2);
            space.SetValue(SpaceParameter.Volume, volume_M3);
            adjacencyCluster.AddObject(space);
            return space;
        }

        private static Zone Zone(AdjacencyCluster adjacencyCluster, string name, params Space[] spaces)
        {
            Zone zone = new(name);
            zone.SetValue(ZoneParameter.ZoneCategory, "Flats");
            zone.SetValue(ZoneParameter.IsDwelling, true);
            adjacencyCluster.AddObject(zone);

            foreach (Space space in spaces)
            {
                adjacencyCluster.AddRelation(zone, space);
            }

            return zone;
        }

        /// <summary>A vertical wall of the given width and height along X, standing at the origin.</summary>
        private static Face3D Wall(double x, double width, double height)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x, 0, 0),
                new Point3D(x + width, 0, 0),
                new Point3D(x + width, 0, height),
                new Point3D(x, 0, height),
            ]));
        }

        /// <summary>A door aperture of the given width, 2m high, in the wall at x.</summary>
        private static Face3D Door(double x, double width)
        {
            return new Face3D(new Polygon3D(
            [
                new Point3D(x + 0.5, 0, 0),
                new Point3D(x + 0.5 + width, 0, 0),
                new Point3D(x + 0.5 + width, 0, 2),
                new Point3D(x + 0.5, 0, 2),
            ]));
        }

        private static List<Aperture> Doors(AdjacencyCluster adjacencyCluster)
        {
            return [.. (adjacencyCluster.GetPanels() ?? []).SelectMany(x => x.Apertures ?? []).Where(x => x is not null && x.ApertureType == ApertureType.Door)];
        }

        private static List<Guid> DoorGuids(AdjacencyCluster adjacencyCluster)
        {
            return Doors(adjacencyCluster).ConvertAll(x => x.Guid);
        }
    }
}
