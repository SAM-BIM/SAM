// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// The one canonical floor area of a space: the actual geometrical floor surface associated with it.
        /// Every creation path and the SAMAnalytical.CalculateFloorArea component go through here, so the
        /// stored <see cref="SpaceParameter.Area"/> means the same thing whichever route built the model.
        /// </summary>
        /// <remarks>
        /// Calculation hierarchy, first valid result wins:
        /// <list type="number">
        /// <item><b>Geometrical floor panels</b> - the sum of <see cref="Panel.GetArea"/> over the space's
        /// downward-facing panels (<see cref="GeomericalFloorPanels"/>), deduplicated by
        /// <see cref="Core.SAMObject.Guid"/> and filtered by panel type. This is the actual surface area, so a
        /// ramped or tilted floor contributes its sloped walking surface, NOT its horizontal projection.</item>
        /// <item><b>Horizontal section</b> - a section through the middle of the space shell. A fallback only,
        /// for incomplete adjacency data or spaces whose panels are virtual/untyped. For a ramp this returns a
        /// plan area, which is why it is never preferred over step 1.</item>
        /// <item><b>Existing value</b> - a valid stored <see cref="SpaceParameter.Area"/> is preserved rather
        /// than replaced by a failure.</item>
        /// </list>
        /// A zero, negative, NaN or infinite result is never returned as a value; the method falls through to
        /// the next step instead, and <see cref="FloorAreaCalculationMethod.Undefined"/> with
        /// <see cref="double.NaN"/> is the honest answer when nothing is knowable.
        /// </remarks>
        /// <param name="adjacencyCluster">Adjacency cluster the space belongs to. Null restricts the result to the space's existing stored value.</param>
        /// <param name="space">Space to calculate.</param>
        /// <param name="floorAreaCalculationMethod">Which step of the hierarchy produced the returned value.</param>
        /// <param name="maxTiltDifference">
        /// Maximum deviation, in degrees, from straight down that still counts as floor. A panel qualifies when
        /// its space-outward normal tilt lies in [180 - maxTiltDifference, 180 + maxTiltDifference], i.e. the
        /// default 20 accepts floors ramped up to 20 degrees from horizontal and rejects anything steeper as a
        /// wall rather than an occupied surface.
        /// </param>
        /// <param name="silverSpacing">Snap/sliver tolerance.</param>
        /// <param name="tolerance_Angle">Angle tolerance.</param>
        /// <param name="tolerance_Distance">Distance tolerance.</param>
        /// <returns>Floor area in m2, or <see cref="double.NaN"/> when no valid area could be established.</returns>
        public static double FloorArea(this AdjacencyCluster adjacencyCluster, Space space, out FloorAreaCalculationMethod floorAreaCalculationMethod, double maxTiltDifference = 20, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            return FloorArea(adjacencyCluster, space, out floorAreaCalculationMethod, out List<Panel> panels, maxTiltDifference, silverSpacing, tolerance_Angle, tolerance_Distance);
        }

        /// <summary>
        /// <inheritdoc cref="FloorArea(AdjacencyCluster, Space, out FloorAreaCalculationMethod, double, double, double, double)" path="/summary"/>
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="FloorArea(AdjacencyCluster, Space, out FloorAreaCalculationMethod, double, double, double, double)" path="/remarks"/>
        /// </remarks>
        /// <param name="adjacencyCluster">Adjacency cluster the space belongs to. Null restricts the result to the space's existing stored value.</param>
        /// <param name="space">Space to calculate.</param>
        /// <param name="floorAreaCalculationMethod">Which step of the hierarchy produced the returned value.</param>
        /// <param name="panels">
        /// The floor panels actually summed, when <paramref name="floorAreaCalculationMethod"/> is
        /// <see cref="FloorAreaCalculationMethod.GeometricalFloorPanels"/>; null otherwise. Reported so a
        /// diagnostic caller can show exactly which surfaces the area came from.
        /// </param>
        /// <param name="maxTiltDifference">
        /// Maximum deviation, in degrees, from straight down that still counts as floor. A panel qualifies when
        /// its space-outward normal tilt lies in [180 - maxTiltDifference, 180 + maxTiltDifference], i.e. the
        /// default 20 accepts floors ramped up to 20 degrees from horizontal and rejects anything steeper as a
        /// wall rather than an occupied surface.
        /// </param>
        /// <param name="silverSpacing">Snap/sliver tolerance.</param>
        /// <param name="tolerance_Angle">Angle tolerance.</param>
        /// <param name="tolerance_Distance">Distance tolerance.</param>
        /// <returns>Floor area in m2, or <see cref="double.NaN"/> when no valid area could be established.</returns>
        public static double FloorArea(this AdjacencyCluster adjacencyCluster, Space space, out FloorAreaCalculationMethod floorAreaCalculationMethod, out List<Panel> panels, double maxTiltDifference = 20, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            floorAreaCalculationMethod = FloorAreaCalculationMethod.Undefined;
            panels = null;

            if (space == null)
            {
                return double.NaN;
            }

            // Parameters live on the cluster's own copy of the space once it has been added, so preservation
            // must read that copy rather than a possibly stale caller-supplied instance.
            Space space_Cluster = adjacencyCluster?.GetObject<Space>(space.Guid) ?? space;

            if (adjacencyCluster != null)
            {
                double area = FloorAreaByPanels(adjacencyCluster, space, maxTiltDifference, silverSpacing, tolerance_Distance, out List<Panel> panels_Floor);
                if (IsValidFloorArea(area))
                {
                    floorAreaCalculationMethod = FloorAreaCalculationMethod.GeometricalFloorPanels;
                    panels = panels_Floor;
                    return area;
                }

                area = FloorAreaBySection(adjacencyCluster.Shell(space), tolerance_Angle, tolerance_Distance, silverSpacing);
                if (IsValidFloorArea(area))
                {
                    floorAreaCalculationMethod = FloorAreaCalculationMethod.HorizontalSection;
                    return area;
                }
            }

            if (space_Cluster.TryGetValue(SpaceParameter.Area, out double area_Existing) && IsValidFloorArea(area_Existing))
            {
                floorAreaCalculationMethod = FloorAreaCalculationMethod.Existing;
                return area_Existing;
            }

            return double.NaN;
        }

        /// <summary>
        /// Step 1: the sum of the actual surface areas of the space's geometrical floor panels.
        /// </summary>
        private static double FloorAreaByPanels(AdjacencyCluster adjacencyCluster, Space space, double maxTiltDifference, double silverSpacing, double tolerance_Distance, out List<Panel> panels)
        {
            panels = null;

            List<Panel> panels_Geometrical = GeomericalFloorPanels(adjacencyCluster, space, maxTiltDifference, silverSpacing, tolerance_Distance);
            if (panels_Geometrical == null || panels_Geometrical.Count == 0)
            {
                return double.NaN;
            }

            HashSet<Guid> guids = new HashSet<Guid>();
            List<Panel> panels_Temp = new List<Panel>();
            double result = 0;

            foreach (Panel panel in panels_Geometrical)
            {
                // Deduplicate by stable identity: one panel can be reached more than once (and a panel bounding
                // the space twice must still be counted once).
                if (panel == null || !guids.Add(panel.Guid))
                {
                    continue;
                }

                if (!IsFloorAreaPanelType(panel.PanelType))
                {
                    continue;
                }

                double area = panel.GetArea();
                if (!IsValidFloorArea(area))
                {
                    continue;
                }

                panels_Temp.Add(panel);
                result += area;
            }

            if (panels_Temp.Count == 0 || !IsValidFloorArea(result))
            {
                return double.NaN;
            }

            panels = panels_Temp;
            return result;
        }

        /// <summary>
        /// Step 2: a horizontal section through the middle of the space shell.
        /// </summary>
        private static double FloorAreaBySection(Shell shell, double tolerance_Angle, double tolerance_Distance, double tolerance_Snap)
        {
            BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
            if (boundingBox3D == null || !boundingBox3D.IsValid())
            {
                return double.NaN;
            }

            double height = boundingBox3D.Max.Z - boundingBox3D.Min.Z;
            if (double.IsNaN(height) || double.IsInfinity(height) || height <= tolerance_Distance)
            {
                return double.NaN;
            }

            double result;
            try
            {
                result = shell.Area(height / 2, tolerance_Angle, tolerance_Distance, tolerance_Snap);
            }
            catch
            {
                // Sectioning runs planar booleans over the shell's faces; an open, degenerate or
                // self-intersecting shell can make those throw. A fallback that takes a whole creation path
                // down is worse than no fallback, so an unusable section simply means "no area".
                return double.NaN;
            }

            return IsValidFloorArea(result) ? result : double.NaN;
        }

        /// <summary>
        /// Panel-type protection for the geometrical calculation. Geometry stays the primary classification
        /// because imported or generated panels routinely arrive with incomplete metadata - which is why
        /// <see cref="PanelType.Undefined"/> is accepted when the geometry already identifies the panel as
        /// floor - but an explicitly incompatible type (a wall, roof, ceiling, shade, air or glazing panel)
        /// overrides the normal test. <see cref="PanelType(Geometry.Spatial.Vector3D, double)"/> alone is not
        /// enough here: it calls any downward-facing surface a floor, including steep ones.
        /// </summary>
        private static bool IsFloorAreaPanelType(PanelType panelType)
        {
            // Deliberately an accept list, not a reject list: an unrecognised or newly added type must not be
            // silently counted as occupied floor surface.
            switch (panelType)
            {
                // Fully qualified: inside Query the bare name PanelType binds to Query.PanelType(...).
                case Analytical.PanelType.Floor:
                case Analytical.PanelType.FloorInternal:
                case Analytical.PanelType.FloorExposed:
                case Analytical.PanelType.FloorRaised:
                case Analytical.PanelType.SlabOnGrade:
                case Analytical.PanelType.UndergroundSlab:
                case Analytical.PanelType.Undefined:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>A floor area is usable only when it is finite and strictly positive.</summary>
        private static bool IsValidFloorArea(double area)
        {
            return !double.IsNaN(area) && !double.IsInfinity(area) && area > 0;
        }
    }
}
