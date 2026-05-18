// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        public static void AddResults(this AdjacencyCluster adjacencyCluster, IEnumerable<IResult> results, bool simplify = true)
        {
            if (results == null || adjacencyCluster == null)
            {
                return;
            }

            List<Space> spaces = null;
            List<Panel> panels = null;
            List<Zone> zones = null;

            // Lazy dictionaries keyed by Guid and Name. The original code linear-scanned the spaces / panels / zones
            // list two or three times per result, giving O(results * (spaces + panels + zones)). On a 1000-space model
            // with thousands of results that was tens of millions of comparisons.
            Dictionary<Guid, Space> spacesByGuid = null;
            Dictionary<string, Space> spacesByName = null;
            Dictionary<Guid, Panel> panelsByGuid = null;
            Dictionary<string, Panel> panelsByName = null;
            Dictionary<Guid, Zone> zonesByGuid = null;
            Dictionary<string, Zone> zonesByName = null;

            foreach (IResult result in results)
            {
                IResult result_Temp = result.Clone();
                if (result_Temp == null)
                {
                    result_Temp = result;
                }

                if (simplify)
                {
                    if (result_Temp is TM52ExtendedResult)
                    {
                        result_Temp = ((TM52ExtendedResult)result_Temp).Simplify();
                    }
                }

                adjacencyCluster.AddObject(result_Temp);

                if (result_Temp is SpaceSimulationResult)
                {
                    SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result_Temp;

                    Space space = LookupSpace(adjacencyCluster, spaceSimulationResult.Reference, spaceSimulationResult.Name,
                        ref spaces, ref spacesByGuid, ref spacesByName);

                    if (space != null)
                    {
                        adjacencyCluster.AddRelation(space, spaceSimulationResult);
                    }
                }
                else if (result_Temp is SurfaceSimulationResult)
                {
                    SurfaceSimulationResult surfaceSimulationResult = (SurfaceSimulationResult)result_Temp;

                    Panel panel = LookupPanel(adjacencyCluster, surfaceSimulationResult.Reference, surfaceSimulationResult.Name,
                        ref panels, ref panelsByGuid, ref panelsByName);

                    if (panel != null)
                    {
                        adjacencyCluster.AddRelation(panel, surfaceSimulationResult);
                    }
                }
                else if (result_Temp is ZoneSimulationResult)
                {
                    ZoneSimulationResult zoneSimulationResult = (ZoneSimulationResult)result_Temp;

                    Zone zone = LookupZone(adjacencyCluster, zoneSimulationResult.Reference, zoneSimulationResult.Name,
                        ref zones, ref zonesByGuid, ref zonesByName);

                    if (zone != null)
                    {
                        adjacencyCluster.AddRelation(zone, zoneSimulationResult);
                    }
                }
                else if (result_Temp is TMResult)
                {
                    TMResult tMSpaceResult = (TMResult)result_Temp;

                    Space space = LookupSpace(adjacencyCluster, tMSpaceResult.Reference, tMSpaceResult.Name,
                        ref spaces, ref spacesByGuid, ref spacesByName);

                    if (space != null)
                    {
                        adjacencyCluster.AddRelation(space, tMSpaceResult);
                    }
                }
                else if (result_Temp is TMExtendedResult)
                {
                    TMExtendedResult tMSpaceExtendedResult = (TMExtendedResult)result_Temp;

                    Space space = LookupSpace(adjacencyCluster, tMSpaceExtendedResult.Reference, tMSpaceExtendedResult.Name,
                        ref spaces, ref spacesByGuid, ref spacesByName);

                    if (space != null)
                    {
                        adjacencyCluster.AddRelation(space, tMSpaceExtendedResult);
                    }
                }
            }
        }

        private static Space LookupSpace(AdjacencyCluster adjacencyCluster, string reference, string fallbackName,
            ref List<Space> spaces, ref Dictionary<Guid, Space> spacesByGuid, ref Dictionary<string, Space> spacesByName)
        {
            Space space = null;
            if (Core.Query.TryConvert(reference, out Guid guid))
            {
                EnsureSpaceIndex(adjacencyCluster, ref spaces, ref spacesByGuid, ref spacesByName);
                spacesByGuid?.TryGetValue(guid, out space);
            }

            if (space == null)
            {
                ObjectReference objectReference = Core.Convert.ComplexReference<ObjectReference>(reference);
                if (objectReference != null && objectReference.Reference != null)
                {
                    space = adjacencyCluster?.GetObjects<Space>(objectReference)?.FirstOrDefault();
                }
            }

            if (space == null)
            {
                EnsureSpaceIndex(adjacencyCluster, ref spaces, ref spacesByGuid, ref spacesByName);
                if (!string.IsNullOrEmpty(reference))
                    spacesByName?.TryGetValue(reference, out space);
                if (space == null && !string.IsNullOrEmpty(fallbackName))
                    spacesByName?.TryGetValue(fallbackName, out space);
            }

            return space;
        }

        private static Panel LookupPanel(AdjacencyCluster adjacencyCluster, string reference, string fallbackName,
            ref List<Panel> panels, ref Dictionary<Guid, Panel> panelsByGuid, ref Dictionary<string, Panel> panelsByName)
        {
            Panel panel = null;
            if (Core.Query.TryConvert(reference, out Guid guid))
            {
                EnsurePanelIndex(adjacencyCluster, ref panels, ref panelsByGuid, ref panelsByName);
                panelsByGuid?.TryGetValue(guid, out panel);
            }

            if (panel == null)
            {
                ObjectReference objectReference = Core.Convert.ComplexReference<ObjectReference>(reference);
                if (objectReference != null)
                {
                    panel = adjacencyCluster?.GetObjects<Panel>(objectReference)?.FirstOrDefault();
                }
            }

            if (panel == null)
            {
                EnsurePanelIndex(adjacencyCluster, ref panels, ref panelsByGuid, ref panelsByName);
                if (!string.IsNullOrEmpty(reference))
                    panelsByName?.TryGetValue(reference, out panel);
                if (panel == null && !string.IsNullOrEmpty(fallbackName))
                    panelsByName?.TryGetValue(fallbackName, out panel);
            }

            return panel;
        }

        private static Zone LookupZone(AdjacencyCluster adjacencyCluster, string reference, string fallbackName,
            ref List<Zone> zones, ref Dictionary<Guid, Zone> zonesByGuid, ref Dictionary<string, Zone> zonesByName)
        {
            Zone zone = null;
            if (Core.Query.TryConvert(reference, out Guid guid))
            {
                EnsureZoneIndex(adjacencyCluster, ref zones, ref zonesByGuid, ref zonesByName);
                zonesByGuid?.TryGetValue(guid, out zone);
            }

            if (zone == null)
            {
                ObjectReference objectReference = Core.Convert.ComplexReference<ObjectReference>(reference);
                if (objectReference != null)
                {
                    zone = adjacencyCluster?.GetObjects<Zone>(objectReference)?.FirstOrDefault();
                }
            }

            if (zone == null)
            {
                EnsureZoneIndex(adjacencyCluster, ref zones, ref zonesByGuid, ref zonesByName);
                if (!string.IsNullOrEmpty(reference))
                    zonesByName?.TryGetValue(reference, out zone);
                if (zone == null && !string.IsNullOrEmpty(fallbackName))
                    zonesByName?.TryGetValue(fallbackName, out zone);
            }

            return zone;
        }

        private static void EnsureSpaceIndex(AdjacencyCluster adjacencyCluster, ref List<Space> spaces, ref Dictionary<Guid, Space> spacesByGuid, ref Dictionary<string, Space> spacesByName)
        {
            if (spacesByGuid != null && spacesByName != null) return;
            if (spaces == null) spaces = adjacencyCluster?.GetSpaces();
            spacesByGuid = new Dictionary<Guid, Space>(spaces?.Count ?? 0);
            spacesByName = new Dictionary<string, Space>(spaces?.Count ?? 0);
            if (spaces != null)
            {
                foreach (Space s in spaces)
                {
                    if (s == null) continue;
                    spacesByGuid[s.Guid] = s;
                    if (!string.IsNullOrEmpty(s.Name))
                        spacesByName[s.Name] = s;
                }
            }
        }

        private static void EnsurePanelIndex(AdjacencyCluster adjacencyCluster, ref List<Panel> panels, ref Dictionary<Guid, Panel> panelsByGuid, ref Dictionary<string, Panel> panelsByName)
        {
            if (panelsByGuid != null && panelsByName != null) return;
            if (panels == null) panels = adjacencyCluster?.GetPanels();
            panelsByGuid = new Dictionary<Guid, Panel>(panels?.Count ?? 0);
            panelsByName = new Dictionary<string, Panel>(panels?.Count ?? 0);
            if (panels != null)
            {
                foreach (Panel p in panels)
                {
                    if (p == null) continue;
                    panelsByGuid[p.Guid] = p;
                    if (!string.IsNullOrEmpty(p.Name))
                        panelsByName[p.Name] = p;
                }
            }
        }

        private static void EnsureZoneIndex(AdjacencyCluster adjacencyCluster, ref List<Zone> zones, ref Dictionary<Guid, Zone> zonesByGuid, ref Dictionary<string, Zone> zonesByName)
        {
            if (zonesByGuid != null && zonesByName != null) return;
            if (zones == null) zones = adjacencyCluster?.GetZones();
            zonesByGuid = new Dictionary<Guid, Zone>(zones?.Count ?? 0);
            zonesByName = new Dictionary<string, Zone>(zones?.Count ?? 0);
            if (zones != null)
            {
                foreach (Zone z in zones)
                {
                    if (z == null) continue;
                    zonesByGuid[z.Guid] = z;
                    if (!string.IsNullOrEmpty(z.Name))
                        zonesByName[z.Name] = z;
                }
            }
        }
    }
}
