// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        public static List<Aperture> Apertures(this AdjacencyCluster adjacencyCluster, ApertureConstruction apertureConstruction)
        {
            if (adjacencyCluster == null || apertureConstruction == null)
                return null;

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null)
                return null;

            Guid guid = apertureConstruction.Guid;

            List<Aperture> result = new List<Aperture>();
            foreach (Panel panel in panels)
            {
                List<Aperture> apertures = panel.Apertures;
                if (apertures == null || apertures.Count == 0)
                    continue;

                foreach (Aperture aperture in apertures)
                    if (aperture.TypeGuid.Equals(guid))
                        result.Add(aperture);
            }

            return result;
        }

        public static List<Aperture> Apertures(this AdjacencyCluster adjacencyCluster, Point3D point3D, int maxCount = 1, double tolerance = Core.Tolerance.Distance)
        {
            if (adjacencyCluster == null || point3D == null)
            {
                return null;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null || panels.Count == 0)
            {
                return null;
            }

            List<Aperture> result = new List<Aperture>();
            foreach (Panel panel in panels)
            {
                List<Aperture> apertures = panel?.Apertures;
                if (apertures == null || apertures.Count == 0)
                {
                    continue;
                }

                if (apertures.Count > 1)
                {
                    if (!panel.GetBoundingBox().InRange(point3D, tolerance))
                    {
                        continue;
                    }
                }

                foreach (Aperture aperture in apertures)
                {
                    BoundingBox3D boundingBox3D = aperture?.GetBoundingBox();
                    if (boundingBox3D == null)
                    {
                        continue;
                    }

                    if (aperture.Face3D.InRange(point3D, tolerance))
                    {
                        result.Add(aperture);
                        if (result.Count == maxCount)
                        {
                            return result;
                        }
                    }
                }
            }

            return result;
        }

        // Flat (bounding box, aperture) index built once per AdjacencyCluster. Pair with the Apertures
        // overload below to replace repeated AdjacencyCluster.Apertures(point, ...) calls inside hot loops,
        // where each call would otherwise re-walk every panel.
        public static List<Tuple<BoundingBox3D, Aperture>> AperturesWithBoundingBoxes(this AdjacencyCluster adjacencyCluster)
        {
            if (adjacencyCluster == null)
            {
                return null;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null || panels.Count == 0)
            {
                return null;
            }

            List<Tuple<BoundingBox3D, Aperture>> result = new List<Tuple<BoundingBox3D, Aperture>>();
            foreach (Panel panel in panels)
            {
                List<Aperture> apertures = panel?.Apertures;
                if (apertures == null || apertures.Count == 0)
                {
                    continue;
                }

                foreach (Aperture aperture in apertures)
                {
                    BoundingBox3D boundingBox3D = aperture?.GetBoundingBox();
                    if (boundingBox3D == null)
                    {
                        continue;
                    }

                    result.Add(new Tuple<BoundingBox3D, Aperture>(boundingBox3D, aperture));
                }
            }

            return result;
        }

        public static List<Aperture> Apertures(this List<Tuple<BoundingBox3D, Aperture>> aperturesWithBoundingBoxes, Point3D point3D, int maxCount = 1, double tolerance = Core.Tolerance.Distance)
        {
            if (aperturesWithBoundingBoxes == null || aperturesWithBoundingBoxes.Count == 0 || point3D == null)
            {
                return null;
            }

            List<Aperture> result = new List<Aperture>();
            foreach (Tuple<BoundingBox3D, Aperture> entry in aperturesWithBoundingBoxes)
            {
                if (entry == null)
                {
                    continue;
                }

                if (!entry.Item1.InRange(point3D, tolerance))
                {
                    continue;
                }

                Aperture aperture = entry.Item2;
                if (aperture?.Face3D == null)
                {
                    continue;
                }

                if (!aperture.Face3D.InRange(point3D, tolerance))
                {
                    continue;
                }

                result.Add(aperture);
                if (result.Count == maxCount)
                {
                    return result;
                }
            }

            return result;
        }
    }
}
