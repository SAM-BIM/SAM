// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Recalculates creation-time floor area for every space from a horizontal
        /// section through the middle of its reconstructed shell.
        /// </summary>
        /// <remarks>
        /// This is the canonical plan-area calculation used while creating an
        /// adjacency cluster. It deliberately differs from CalculateFloorArea,
        /// which sums geometrical floor-panel surface areas and therefore includes
        /// panel slope. A failed, zero, or non-finite section never overwrites an
        /// existing valid SpaceParameter.Area value.
        /// </remarks>
        public static int UpdateSpaceAreas(this AdjacencyCluster adjacencyCluster, double tolerance_Angle = Tolerance.Angle, double tolerance_Distance = Tolerance.Distance, double tolerance_Snap = Tolerance.MacroDistance)
        {
            if (adjacencyCluster == null)
            {
                return 0;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                return 0;
            }

            int result = 0;
            foreach (Space space in spaces)
            {
                if (space == null)
                {
                    continue;
                }

                Shell shell = adjacencyCluster.Shell(space);
                BoundingBox3D boundingBox3D = shell?.GetBoundingBox();
                if (boundingBox3D == null || !boundingBox3D.IsValid())
                {
                    continue;
                }

                double height = boundingBox3D.Max.Z - boundingBox3D.Min.Z;
                if (double.IsNaN(height) || double.IsInfinity(height) || height <= tolerance_Distance)
                {
                    continue;
                }

                double area = shell.Area(height / 2, tolerance_Angle, tolerance_Distance, tolerance_Snap);
                if (double.IsNaN(area) || double.IsInfinity(area) || area <= 0)
                {
                    continue;
                }

                Space space_Temp = new Space(space);
                space_Temp.SetValue(SpaceParameter.Area, area);
                adjacencyCluster.AddObject(space_Temp);
                result++;
            }

            return result;
        }
    }
}
