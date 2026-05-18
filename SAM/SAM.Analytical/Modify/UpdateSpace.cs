// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Bulk update of multiple spaces. Functionally equivalent to calling <see cref="UpdateSpace"/>
        /// once per input space, but caches the existing-space shells exactly once so the per-input
        /// matching loop avoids rebuilding shell geometry every iteration.
        /// On 1000-space models the per-call version was O(M * N * shell-rebuild) ≈ 35 s; this version
        /// is O(N) cache + O(M * N * bbox-check).
        /// </summary>
        public static int UpdateSpaces(this AdjacencyCluster adjacencyCluster, IEnumerable<Space> spaces, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance = Core.Tolerance.Distance)
        {
            if (adjacencyCluster == null || spaces == null)
                return 0;

            List<Space> existingSpaces = adjacencyCluster.GetSpaces();
            if (existingSpaces == null || existingSpaces.Count == 0)
                return 0;

            // Build the shell cache once. The original UpdateSpace built one Shell per existing space
            // *per call* — i.e. N shells rebuilt for each of M input spaces.
            Dictionary<Space, Shell> shellsByExistingSpace = new Dictionary<Space, Shell>(existingSpaces.Count);
            foreach (Space s in existingSpaces)
            {
                Shell sh = adjacencyCluster.Shell(s);
                if (sh != null)
                    shellsByExistingSpace[s] = sh;
            }

            int updated = 0;
            foreach (Space space in spaces)
            {
                if (space == null)
                    continue;
                Point3D point3D = space.Location;
                if (point3D == null)
                    continue;

                List<Space> candidates = null;
                foreach (KeyValuePair<Space, Shell> kvp in shellsByExistingSpace)
                {
                    Shell sh = kvp.Value;
                    if (sh.InRange(point3D, tolerance) || sh.Inside(point3D, tolerance))
                    {
                        if (candidates == null)
                            candidates = new List<Space>();
                        candidates.Add(kvp.Key);
                    }
                }

                if (candidates == null || candidates.Count == 0)
                    continue;

                Space space_Result = null;
                if (candidates.Count == 1)
                {
                    space_Result = candidates[0];
                }
                else
                {
                    Point3D shifted = point3D.GetMoved(Vector3D.WorldZ * silverSpacing) as Point3D;
                    foreach (Space c in candidates)
                    {
                        Shell sh = shellsByExistingSpace[c];
                        if (sh.InRange(shifted, tolerance) || sh.Inside(shifted, tolerance))
                        {
                            space_Result = c;
                            break;
                        }
                    }
                }

                if (space_Result == null)
                    continue;

                List<Panel> panels = adjacencyCluster.GetPanels(space_Result);
                adjacencyCluster.RemoveObject(typeof(Space), space_Result.Guid);
                // Drop the matched space from the cache so subsequent input spaces can't match it again
                // (mirrors the original per-call behaviour, where the next UpdateSpace re-fetched the spaces list).
                shellsByExistingSpace.Remove(space_Result);

                adjacencyCluster.AddObject(space);
                if (panels != null && panels.Count != 0)
                {
                    foreach (Panel panel in panels)
                        adjacencyCluster.AddRelation(space, panel);
                }

                updated++;
            }

            return updated;
        }

        public static bool UpdateSpace(this AdjacencyCluster adjacencyCluster, Space space, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance = Core.Tolerance.Distance)
        {
            if (adjacencyCluster == null || space == null)
                return false;

            Point3D point3D = space.Location;
            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return false;

            Dictionary<Space, Shell> dictionary = new Dictionary<Space, Shell>();
            foreach (Space space_Temp in spaces)
            {
                Shell shell = adjacencyCluster.Shell(space_Temp);
                if (shell == null)
                    continue;

                if (shell.InRange(point3D, tolerance) || shell.Inside(point3D, tolerance))
                    dictionary[space_Temp] = shell;
            }

            if (dictionary.Count == 0)
                return false;

            Space space_Result = null;

            if (dictionary.Count > 1)
            {
                point3D = point3D.GetMoved(Vector3D.WorldZ * silverSpacing) as Point3D;
                foreach (KeyValuePair<Space, Shell> keyValuePair in dictionary)
                {
                    Shell shell = keyValuePair.Value;
                    if (shell.InRange(point3D, tolerance) || shell.Inside(point3D, tolerance))
                    {
                        space_Result = keyValuePair.Key;
                        break;
                    }
                }
            }
            else
            {
                space_Result = dictionary.Keys.First();
            }


            if (space_Result == null)
                return false;

            List<Panel> panels = adjacencyCluster.GetPanels(space_Result);
            adjacencyCluster.RemoveObject(typeof(Space), space_Result.Guid);

            adjacencyCluster.AddObject(space);
            if (panels != null && panels.Count != 0)
            {
                foreach (Panel panel in panels)
                    adjacencyCluster.AddRelation(space, panel);
            }

            return true;
        }
    }
}
