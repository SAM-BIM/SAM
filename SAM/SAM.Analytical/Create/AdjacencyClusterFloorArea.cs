// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Create
    {
        /// <summary>
        /// List-specific creation overload that preserves the existing shell/space
        /// workflow and then standardizes SpaceParameter.Area from a mid-height section.
        /// </summary>
        public static AdjacencyCluster AdjacencyCluster(this List<Shell> shells, List<Space> spaces, double elevation_Ground = 0, double silverSpacing = Tolerance.MacroDistance, double tolerance_Angle = Tolerance.Angle, double tolerance_Distance = Tolerance.Distance)
        {
            AdjacencyCluster result = AdjacencyCluster(
                (IEnumerable<Shell>)shells,
                (IEnumerable<Space>)spaces,
                elevation_Ground,
                silverSpacing,
                tolerance_Angle,
                tolerance_Distance);

            result?.UpdateSpaceAreas(tolerance_Angle, tolerance_Distance, silverSpacing);
            return result;
        }

        /// <summary>
        /// List-specific shell creation overload used by
        /// SAMAnalytical.CreateAdjacencyClusterByBreps.
        /// </summary>
        public static AdjacencyCluster AdjacencyCluster(this List<Shell> shells, double elevationGround = 0, double thinnessRatio = 0.001, double minArea = 0.1, double maxDistance = 0.1, double maxAngle = 0.0872664626, double silverSpacing = Tolerance.MacroDistance, double tolerance_Distance = Tolerance.Distance, double tolerance_Angle = Tolerance.Angle)
        {
            AdjacencyCluster result = AdjacencyCluster(
                (IEnumerable<Shell>)shells,
                elevationGround,
                thinnessRatio,
                minArea,
                maxDistance,
                maxAngle,
                silverSpacing,
                tolerance_Distance,
                tolerance_Angle);

            result?.UpdateSpaceAreas(tolerance_Angle, tolerance_Distance, silverSpacing);
            return result;
        }

        /// <summary>
        /// List-specific face creation overload used by the horizontal-level
        /// CreateAdjacencyCluster workflow, ensuring generated spaces also carry area.
        /// </summary>
        public static AdjacencyCluster AdjacencyCluster(this List<Face3D> face3Ds, double elevation_Ground = 0, double tolerance = Tolerance.Distance)
        {
            AdjacencyCluster result = AdjacencyCluster((IEnumerable<Face3D>)face3Ds, elevation_Ground, tolerance);
            result?.UpdateSpaceAreas(Tolerance.Angle, tolerance, Tolerance.MacroDistance);
            return result;
        }

        /// <summary>
        /// List-specific panel reconstruction overload for model creation paths that
        /// begin with existing spaces and panels.
        /// </summary>
        public static AdjacencyCluster AdjacencyCluster(this List<Space> spaces, List<Panel> panels, double offset = 0.1, bool addMissingSpaces = false, bool addMissingPanels = false, double thinnessRatio = 0.01, double minArea = Tolerance.MacroDistance, double maxDistance = 0.1, double maxAngle = 0.0872664626, double silverSpacing = Tolerance.MacroDistance, double tolerance_Distance = Tolerance.Distance, double tolerance_Angle = Tolerance.Angle)
        {
            AdjacencyCluster result = AdjacencyCluster(
                (IEnumerable<Space>)spaces,
                (IEnumerable<Panel>)panels,
                offset,
                addMissingSpaces,
                addMissingPanels,
                thinnessRatio,
                minArea,
                maxDistance,
                maxAngle,
                silverSpacing,
                tolerance_Distance,
                tolerance_Angle);

            result?.UpdateSpaceAreas(tolerance_Angle, tolerance_Distance, silverSpacing);
            return result;
        }
    }
}
