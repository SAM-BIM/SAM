﻿using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;

namespace SAM.Architectural.Grasshopper
{
    public static partial class Modify
    {
        public static void BakeGeometry_ByCategory(this RhinoDoc rhinoDoc, global::Grasshopper.Kernel.Data.IGH_Structure gH_Structure, bool cutOpening = false, double tolerance = Core.Tolerance.Distance)
        {
            if (rhinoDoc == null)
                return;

            List<HostPartition> hostPartitions = new List<HostPartition>();
            foreach (var variable in gH_Structure.AllData(true))
            {
                if (variable is GoohostPartition)
                {
                    hostPartitions.Add(((GoohostPartition)variable).Value);
                }
                else if (variable is GooArchitecturalModel)
                {
                    ArchitecturalModel architecturalModel = ((GooArchitecturalModel)variable).Value;
                    if (architecturalModel != null)
                    {
                        List<HostPartition> hostPartitions_Temp = architecturalModel.GetObjects<HostPartition>();
                        if (hostPartitions_Temp != null && hostPartitions_Temp.Count > 0)
                        {
                            hostPartitions.AddRange(hostPartitions_Temp);
                        }
                    }
                }
            }

            BakeGeometry_ByCategory(rhinoDoc, hostPartitions, cutOpening, tolerance);
        }

        public static void BakeGeometry_ByCategory(this RhinoDoc rhinoDoc, IEnumerable<HostPartition> hostPartitions, bool cutOpenings = false, double tolerance = Core.Tolerance.Distance)
        {
            Rhino.DocObjects.Tables.LayerTable layerTable = rhinoDoc?.Layers;
            if (layerTable == null)
                return;

            Layer layer_SAM = Core.Grasshopper.Modify.AddSAMLayer(layerTable);
            if (layer_SAM == null)
                return;

            int index = -1;

            index = layerTable.Add();
            Layer layer_Host = layerTable[index];
            layer_Host.Name = "Host";
            layer_Host.ParentLayerId = layer_SAM.Id;

            index = layerTable.Add();
            Layer layer_Opening = layerTable[index];
            layer_Opening.Name = "Opening";
            layer_Opening.ParentLayerId = layer_SAM.Id;

            //int currentIndex = layerTable.CurrentLayerIndex;

            ObjectAttributes objectAttributes = rhinoDoc.CreateDefaultAttributes();

            List<Guid> guids = new List<Guid>();
            foreach (HostPartition hostPartition in hostPartitions)
            {
                if (hostPartition == null)
                    continue;

                Layer layer = Core.Grasshopper.Modify.GetLayer(layerTable, layer_Host.Id, hostPartition.GetType().ToString(), Architectural.Query.Color(hostPartition));

                //layerTable.SetCurrentLayerIndex(layer.Index, true);
                objectAttributes.LayerIndex = layer.Index;

                Guid guid = default;
                if (BakeGeometry(hostPartition, rhinoDoc, objectAttributes, out guid, cutOpenings, tolerance))
                    guids.Add(guid);

                List<Opening> openings = hostPartition.Openings;
                if (openings == null || openings.Count == 0)
                    continue;

                foreach (Opening opening in openings)
                {
                    if (opening == null)
                        continue;

                    layer = Core.Grasshopper.Modify.GetLayer(layerTable, layer_Opening.Id, opening.GetType().ToString(), Architectural.Query.Color(opening));

                    //layerTable.SetCurrentLayerIndex(layer.Index, true);
                    objectAttributes.LayerIndex = layer.Index;

                    guid = default;
                    if (BakeGeometry(opening, rhinoDoc, objectAttributes, out guid))
                        guids.Add(guid);
                }
            }

            //layerTable.SetCurrentLayerIndex(currentIndex, true);
        }
    }
}