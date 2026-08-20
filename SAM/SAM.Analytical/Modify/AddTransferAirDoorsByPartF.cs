// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Width [m] of an internal door this method creates: the Approved Document F, Volume 1: Dwellings
        /// (2021 edition) paragraph 1.25 reference door width, so a 10mm undercut across a door this wide
        /// provides exactly the 7,600mm2 free area the paragraph requires. There is no other door width
        /// convention anywhere in the Part F workflow - the Approved Document's own reference door is the
        /// source of truth, and no new assumption is introduced.
        /// </summary>
        public const double DefaultTransferAirDoorWidth_M = PartFDoorTransferData.ReferenceDoorWidth_mm / 1000;

        /// <summary>
        /// Height [m] of an internal door this method creates. Approved Document F sets no door height;
        /// 2.1m is the programme's documented default for a created transfer door (PartF-HANDOVER-ARCHIVE
        /// &sect;6a: "a 760 x 2100 mm internal door"). Geometry only - nothing in the Part F assessment
        /// reads it.
        /// </summary>
        public const double DefaultTransferAirDoorHeight_M = 2.1;

        /// <summary>
        /// Adds the internal transfer-air doors Approved Document F requires but the model does not carry.
        /// <para>
        /// The transfer-air requirement is established by running the SAME <see cref="PartFCalculator"/>
        /// that <c>SAMAnalytical.AddVentilationPropertiesByPartF</c>, <c>SAMAnalytical.CheckPartFCompliance</c>
        /// and SAM_UI's Part F assessment run - the calculation is not duplicated here, only its result is
        /// acted on. The calculator deep-clones the model, re-sizes the dwelling(s) identically and
        /// refreshes every internal door's <see cref="PartFDoorTransferData"/>, carrying the engineer's
        /// recorded inputs (provided undercut, provided free area, transfer device type, flow override)
        /// forward. This method then walks the dwelling transfer-air schedules and, for every route that
        /// has to carry air but has no modelled door
        /// (<see cref="PartFDoorTransferData.IsDoorRepresented"/> false with a real transfer flow), creates
        /// ONE internal door in a shared internal wall between the two spaces and writes the route's
        /// paragraph 1.25 record onto it.
        /// </para>
        /// <para>
        /// <b>Existing suitable doors are never duplicated.</b> A route with a modelled door already carries
        /// its refreshed record from the calculator and is left alone, which is also what makes a second
        /// run of this method a no-op.
        /// </para>
        /// <para>
        /// <b>The door.</b> The default internal-door construction from the active
        /// <see cref="ApertureConstructionLibrary"/> is used where it provides one (a plain
        /// <c>Internal Door</c> construction is substituted where it does not, and the substitution is
        /// noted); the door is <see cref="DefaultTransferAirDoorWidth_M"/> wide and
        /// <see cref="DefaultTransferAirDoorHeight_M"/> high, sits on the bottom edge of the wall, and is
        /// centred on the clearest horizontal position of the panel - the panel centre where it is free,
        /// otherwise as close to it as the existing apertures allow.
        /// </para>
        /// <para>
        /// <b>The undercut is a requirement, not a manufactured fact.</b> The created door's record carries
        /// the paragraph 1.25 requirement - a minimum free area of 7,600mm2, equivalent to a 10mm undercut
        /// across the 760mm door width, achieved 10mm above a fitted floor finish or 20mm above an
        /// unfinished floor surface - and its provided undercut stays UNRECORDED, because an analytical
        /// model does not represent the gap under a door leaf and absence of evidence is never compliance.
        /// The route therefore reports <see cref="Enums.PartFComplianceStatus.CannotBeDetermined"/> until
        /// the engineer records what is actually provided, exactly as a hand-modelled door does.
        /// </para>
        /// <para>
        /// <b>Refuse, never guess.</b> Where the two spaces share no internal wall, or none of the
        /// candidate walls can geometrically fit the door, no door is created and the route is returned in
        /// <paramref name="refusals"/> with the reason. The supplied model is never modified; an updated
        /// copy is returned.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">
        /// A model whose spaces already carry <c>PartFSpaceData</c> - normally the output of
        /// <c>SAMAnalytical.AddVentilationPropertiesByPartF</c>. <b>Not modified</b>: an updated copy is
        /// returned. (The calculation re-derives the same Part F data itself, so an unprocessed model is
        /// accepted too; running the sizing component first remains the intended workflow.)
        /// </param>
        /// <param name="zoneCategoryName">
        /// Zone category containing the dwelling zones, exactly as supplied to
        /// <c>SAMAnalytical.AddVentilationPropertiesByPartF</c>. Null or empty sizes the whole model as one
        /// dwelling. Passing a different category than the sizing run would re-group the dwellings, so the
        /// same value should be supplied.
        /// </param>
        /// <param name="setbackFlowRateFactor">
        /// Setback operating-rate factor, again exactly as supplied upstream. Null uses the rule set's own
        /// factor. Because this method re-runs the sizing, passing a different factor here than upstream
        /// would change the setback rates written on the spaces.
        /// </param>
        /// <param name="doors_Created">The door apertures created, one per resolved route.</param>
        /// <param name="notes">What was done, one sentence each - doors created, existing doors reused.</param>
        /// <param name="refusals">Routes where a transfer path is required but no defensible door could be created, with the reason.</param>
        /// <returns>The updated model, or null where no Part F calculation could run at all.</returns>
        public static AnalyticalModel AddTransferAirDoorsByPartF(this AnalyticalModel analyticalModel, string zoneCategoryName, double? setbackFlowRateFactor, out List<Aperture> doors_Created, out List<string> notes, out List<string> refusals)
        {
            doors_Created = [];
            notes = [];
            refusals = [];

            if (analyticalModel == null)
            {
                refusals.Add("No analytical model was supplied.");
                return null;
            }

            AdjacencyCluster adjacencyCluster_Input = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster_Input == null)
            {
                refusals.Add("The analytical model carries no adjacency cluster.");
                return null;
            }

            PartFCalculator partFCalculator = Query.DefaultPartFCalculator();
            if (partFCalculator == null)
            {
                refusals.Add("Could not load the Part F rule set. Run SAMAnalytical.AddVentilationPropertiesByPartF first and check the Part F resources are available.");
                return null;
            }

            partFCalculator.AdjacencyCluster = adjacencyCluster_Input;

            if (setbackFlowRateFactor != null && setbackFlowRateFactor.HasValue)
            {
                //The property validates and substitutes the documented default for an invalid factor, so an
                //out-of-range value can never poison the setback rates.
                partFCalculator.SetbackFlowRateFactor = setbackFlowRateFactor.Value;
            }

            if (!partFCalculator.Calculate(zoneCategoryName))
            {
                refusals.Add("The Part F calculation did not run, so no transfer-air doors could be resolved.");
                return null;
            }

            //The calculator's own deep clone: every write below lands on the copy, never on the caller's
            //model, and every existing door aperture already carries its refreshed record.
            AdjacencyCluster adjacencyCluster = partFCalculator.AdjacencyCluster;

            int count_Reused = 0;

            foreach (PartFDwellingResult partFDwellingResult in partFCalculator.DwellingResults ?? [])
            {
                List<PartFDoorTransferData> transferPaths = partFDwellingResult?.ComplianceResult?.TransferPaths;
                if (transferPaths == null)
                {
                    continue;
                }

                foreach (PartFDoorTransferData partFDoorTransferData in transferPaths)
                {
                    if (partFDoorTransferData == null || !partFDoorTransferData.RequiresTransferAirPath || !partFDoorTransferData.IsInternalDwellingDoor)
                    {
                        continue;
                    }

                    if (partFDoorTransferData.IsDoorRepresented)
                    {
                        //The calculator has already refreshed this door's record and written it back onto
                        //the aperture; an existing suitable door is never duplicated.
                        count_Reused++;
                        continue;
                    }

                    //A route only earns a door where air actually has to move through it. Paragraph 1.25
                    //puts a requirement on every internal door, but a partition between two rooms with no
                    //transfer flow - two adjacent bedrooms, both supplied - needs no opening manufactured
                    //in it.
                    double flow_Continuous = System.Math.Abs(partFDoorTransferData.ContinuousDesignTransferFlowRate_Lps ?? 0);
                    double flow_High = System.Math.Abs(partFDoorTransferData.HighTransferFlowRate_Lps ?? 0);
                    if (flow_Continuous <= PartFAirflowNetwork.Tolerance_Lps && flow_High <= PartFAirflowNetwork.Tolerance_Lps)
                    {
                        continue;
                    }

                    Aperture aperture = AddTransferAirDoor(adjacencyCluster, partFDoorTransferData, notes, out string refusal);
                    if (aperture == null)
                    {
                        refusals.Add(string.Format("{0}{1} to {2}: a transfer path is required ({3:0.##} l/s at the continuous design condition) but no internal door could be created - {4}.",
                            string.IsNullOrWhiteSpace(partFDwellingResult.Name) ? string.Empty : partFDwellingResult.Name + ": ",
                            partFDoorTransferData.UpstreamSpaceName,
                            partFDoorTransferData.DownstreamSpaceName,
                            flow_Continuous,
                            refusal));
                        continue;
                    }

                    //The route record itself is updated and written onto the new aperture through the shared
                    //helper, because Panel.Apertures hands out clones and that is the only path that
                    //persists. ApertureGuid and IsDoorRepresented describe the door now in the model; the
                    //clear width is read back from its geometry exactly as the calculation reads any other
                    //modelled door; Assess re-judges the record, which with nothing provided stays
                    //CannotBeDetermined - created is not compliant.
                    partFDoorTransferData.ApertureGuid = aperture.Guid;
                    partFDoorTransferData.IsDoorRepresented = true;
                    partFDoorTransferData.ClearDoorWidth_mm = PartFAirflowNetwork.ClearDoorWidth_mm(aperture);
                    PartFTransferPathBuilder.Assess(partFDoorTransferData);

                    adjacencyCluster.SetPartFDoorTransferData(aperture.Guid, partFDoorTransferData);

                    doors_Created.Add(aperture);

                    notes.Add(string.Format("{0}{1} to {2}: created a {3:0} mm x {4:0} mm internal door ({5}) in the shared wall and recorded the paragraph 1.25 requirement on it - a minimum free area of {6:0.##} mm2, equivalent to a {7:0.##} mm undercut across the {3:0} mm door width. The provided undercut is not recorded and remains to be confirmed.",
                        string.IsNullOrWhiteSpace(partFDwellingResult.Name) ? string.Empty : partFDwellingResult.Name + ": ",
                        partFDoorTransferData.UpstreamSpaceName,
                        partFDoorTransferData.DownstreamSpaceName,
                        DefaultTransferAirDoorWidth_M * 1000,
                        DefaultTransferAirDoorHeight_M * 1000,
                        aperture.Name,
                        PartFDoorTransferData.NominalEquivalentFreeArea_mm2,
                        PartFDoorTransferData.ReferenceUndercutHeight_mm));
                }
            }

            if (count_Reused != 0)
            {
                notes.Add(string.Format("{0} existing internal door(s) already serve the dwelling transfer paths; their Part F records were refreshed and no duplicates were created.", count_Reused));
            }

            if (doors_Created.Count == 0 && refusals.Count == 0)
            {
                notes.Add("No transfer-air doors were required: every route that carries air already has a modelled door.");
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);
        }

        /// <summary>
        /// Creates the single internal door for one unrepresented transfer route: finds the shared internal
        /// wall between the two spaces, places the default door on it and writes the panel back to the
        /// cluster. Null - with the reason in <paramref name="refusal"/> - where no defensible door can be
        /// produced.
        /// </summary>
        private static Aperture AddTransferAirDoor(AdjacencyCluster adjacencyCluster, PartFDoorTransferData partFDoorTransferData, List<string> notes, out string refusal)
        {
            refusal = null;

            Space space_1 = adjacencyCluster.GetObject<Space>(partFDoorTransferData.UpstreamSpaceGuid);
            Space space_2 = adjacencyCluster.GetObject<Space>(partFDoorTransferData.DownstreamSpaceGuid);
            if (space_1 == null || space_2 == null)
            {
                refusal = "one of the two spaces is no longer in the model";
                return null;
            }

            //Identity, never name: the panels related to BOTH spaces by guid. The wall group filter
            //excludes the floor between two stacked rooms, which the transfer network quite rightly treats
            //as an adjacency but no door can be hung in.
            List<Panel> panels = adjacencyCluster.GetPanels(LogicalOperator.And, space_1, space_2)?
                .FindAll(x => x != null && x.PanelType.PanelGroup() == PanelGroup.Wall);
            if (panels == null || panels.Count == 0)
            {
                refusal = "the two spaces share no internal wall panel - the adjacency the transfer network found is not a wall, or the partition is not related to both spaces";
                return null;
            }

            //Deterministic order: the largest wall first, guids as the tie-break, so several candidate
            //panels resolve the same way on every run rather than being refused as ambiguous.
            panels = [.. panels.OrderByDescending(x => x.GetArea()).ThenBy(x => x.Guid)];

            List<string> reasons = [];
            foreach (Panel panel in panels)
            {
                Aperture aperture = AddTransferAirDoor(panel, notes, out string reason);
                if (aperture != null)
                {
                    //The panel returned by GetPanels is the cluster's own object; AddObject puts it back so
                    //the change is persisted, the same path SetPartFDoorTransferData takes.
                    adjacencyCluster.AddObject(panel);
                    return aperture;
                }

                reasons.Add(reason);
            }

            refusal = string.Format("{0} candidate shared wall(s) could not take the door ({1})", panels.Count, string.Join("; ", reasons));
            return null;
        }

        /// <summary>
        /// Places the default door on one wall panel: on the panel's bottom edge, as close to the panel's
        /// horizontal centre as the existing apertures allow. All placement is computed in the panel's own
        /// plane, so a wall of any orientation behaves the same.
        /// </summary>
        private static Aperture AddTransferAirDoor(Panel panel, List<string> notes, out string refusal)
        {
            refusal = null;

            ApertureConstruction apertureConstruction = Query.DefaultApertureConstruction(panel, ApertureType.Door);
            if (apertureConstruction == null)
            {
                //The active library carries no internal-door construction (an unpopulated environment, not
                //a model defect), so a plain construction is substituted and the substitution is recorded.
                apertureConstruction = new ApertureConstruction("Internal Door", ApertureType.Door);
                notes.Add("The default aperture construction library has no internal door construction, so a plain 'Internal Door' construction was used. Review the door construction.");
            }

            Face3D face3D_Panel = panel.GetFace3D();
            Plane plane = face3D_Panel?.GetPlane();
            if (plane == null)
            {
                refusal = "the panel has no valid planar geometry";
                return null;
            }

            Face2D face2D_Panel = plane.Convert(face3D_Panel);
            BoundingBox2D boundingBox2D_Panel = face2D_Panel?.GetBoundingBox();
            if (boundingBox2D_Panel == null)
            {
                refusal = "the panel has no valid planar geometry";
                return null;
            }

            //Which way is up in the panel's plane. A wall has the world vertical lying in its plane; where
            //it does not, the panel is not a wall a door can be hung in.
            Vector2D vector2D_Up = plane.Convert(Vector3D.WorldZ);
            if (vector2D_Up == null || vector2D_Up.Length < 0.5)
            {
                refusal = "the shared panel is not vertical";
                return null;
            }

            bool verticalIsY = System.Math.Abs(vector2D_Up.Y) >= System.Math.Abs(vector2D_Up.X);
            double up = verticalIsY ? vector2D_Up.Y : vector2D_Up.X;

            double hMin = verticalIsY ? boundingBox2D_Panel.Min.X : boundingBox2D_Panel.Min.Y;
            double hMax = verticalIsY ? boundingBox2D_Panel.Max.X : boundingBox2D_Panel.Max.Y;
            double vMin = verticalIsY ? boundingBox2D_Panel.Min.Y : boundingBox2D_Panel.Min.X;
            double vMax = verticalIsY ? boundingBox2D_Panel.Max.Y : boundingBox2D_Panel.Max.X;

            double width = DefaultTransferAirDoorWidth_M;
            double height = DefaultTransferAirDoorHeight_M;

            if (hMax - hMin < width - Core.Tolerance.MacroDistance)
            {
                refusal = string.Format("the wall is {0:0.###} m wide and the {1:0.###} m door does not fit", hMax - hMin, width);
                return null;
            }

            if (vMax - vMin < height - Core.Tolerance.MacroDistance)
            {
                refusal = string.Format("the wall is {0:0.###} m high and the {1:0.###} m door does not fit", vMax - vMin, height);
                return null;
            }

            //The door stands on the bottom edge of the wall. Where the plane's vertical axis points down,
            //the bottom is the axis's maximum, not its minimum.
            double v0 = up >= 0 ? vMin : vMax - height;
            double v1 = v0 + height;

            //Horizontal intervals already taken by the panel's apertures where they cross the door's
            //vertical band, with a millimetre's clearance so the new door never touches an existing
            //opening.
            List<Tuple<double, double>> occupied = [];
            foreach (Aperture aperture_Existing in panel.Apertures ?? [])
            {
                Face3D face3D_Aperture = aperture_Existing?.GetFace3D();
                if (face3D_Aperture == null)
                {
                    continue;
                }

                //Coplanar with the panel by construction - Panel.AddAperture enforces it - so conversion
                //into the panel plane is exact.
                BoundingBox2D boundingBox2D_Aperture = plane.Convert(face3D_Aperture)?.GetBoundingBox();
                if (boundingBox2D_Aperture == null)
                {
                    continue;
                }

                double aVMin = verticalIsY ? boundingBox2D_Aperture.Min.Y : boundingBox2D_Aperture.Min.X;
                double aVMax = verticalIsY ? boundingBox2D_Aperture.Max.Y : boundingBox2D_Aperture.Max.X;
                if (aVMax <= v0 + Core.Tolerance.MacroDistance || aVMin >= v1 - Core.Tolerance.MacroDistance)
                {
                    continue;
                }

                double aHMin = (verticalIsY ? boundingBox2D_Aperture.Min.X : boundingBox2D_Aperture.Min.Y) - Core.Tolerance.MacroDistance;
                double aHMax = (verticalIsY ? boundingBox2D_Aperture.Max.X : boundingBox2D_Aperture.Max.Y) + Core.Tolerance.MacroDistance;

                occupied.Add(new Tuple<double, double>(aHMin, aHMax));
            }

            //The clearest free interval of the wall first - the one whose midpoint is nearest the panel
            //centre - with the door clamped inside it, again as close to the panel centre as fits.
            double centre = (hMin + hMax) / 2;

            double h0 = double.NaN;
            foreach (Tuple<double, double> interval in FreeIntervals(hMin, hMax, occupied).OrderBy(x => System.Math.Abs(((x.Item1 + x.Item2) / 2) - centre)))
            {
                if (interval.Item2 - interval.Item1 < width - Core.Tolerance.MacroDistance)
                {
                    continue;
                }

                double c = System.Math.Max(interval.Item1 + (width / 2), System.Math.Min(centre, interval.Item2 - (width / 2)));

                h0 = c - (width / 2);
                break;
            }

            if (double.IsNaN(h0))
            {
                refusal = "no clear length of the wall remains for the door alongside its existing apertures";
                return null;
            }

            double h1 = h0 + width;

            Point2D Point(double h, double v)
            {
                return verticalIsY ? new Point2D(h, v) : new Point2D(v, h);
            }

            Polygon2D polygon2D = new([Point(h0, v0), Point(h1, v0), Point(h1, v1), Point(h0, v1)]);

            Polygon3D polygon3D = plane.Convert(polygon2D);
            if (polygon3D == null)
            {
                refusal = "the door geometry could not be constructed";
                return null;
            }

            //trimGeometry false: the rectangle was built to fit the wall, and trimming a misfit would
            //silently shrink the door below the width the undercut requirement is read against. A door the
            //panel rejects is refused rather than adjusted.
            List<Aperture> apertures = panel.AddApertures(apertureConstruction, polygon3D, false);
            if (apertures == null || apertures.Count == 0)
            {
                refusal = "the panel rejected the door geometry";
                return null;
            }

            return apertures[0];
        }

        /// <summary>
        /// The complement of the occupied intervals within [min, max], in order, each wide enough to
        /// notice.
        /// </summary>
        private static List<Tuple<double, double>> FreeIntervals(double min, double max, List<Tuple<double, double>> occupied)
        {
            List<Tuple<double, double>> result = [];

            double start = min;
            foreach (Tuple<double, double> interval in (occupied ?? []).OrderBy(x => x.Item1))
            {
                double end = System.Math.Min(System.Math.Max(interval.Item1, min), max);
                if (end - start > Core.Tolerance.MacroDistance)
                {
                    result.Add(new Tuple<double, double>(start, end));
                }

                start = System.Math.Max(start, System.Math.Min(interval.Item2, max));
            }

            if (max - start > Core.Tolerance.MacroDistance)
            {
                result.Add(new Tuple<double, double>(start, max));
            }

            return result;
        }
    }
}
