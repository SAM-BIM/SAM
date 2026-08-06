// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    /// <summary>
    /// Grasshopper component that duplicates <see cref="Construction"/> definitions per
    /// panel type (e.g., Internal, External), ensuring the same construction name
    /// is not shared across different <c>PanelType</c> contexts.
    ///
    /// <para>
    /// Why? In some models a single construction instance can be referenced by
    /// both internal and external panels. Splitting/duplicating constructions by
    /// panel type prevents accidental cross-use and clarifies downstream analysis
    /// and scheduling.
    /// </para>
    ///
    /// <para>
    /// Input accepts either an <see cref="AdjacencyCluster"/> or an
    /// <see cref="AnalyticalModel"/>. The component deep-copies the input object,
    /// performs the duplication on the copy, and outputs the updated object plus
    /// the list of affected constructions.
    /// </para>
    /// </summary>
    public class SAMAnalyticalDuplicateConstructionByPanelType : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("26bb4c4f-4de5-43d8-966c-0b1d479905b7");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_analytical", NickName = "_analytical", Description = "SAM AdjacencyCluster or AnalyticalModel whose constructions will be duplicated by panel type", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "analytical", NickName = "analytical", Description = "Updated SAM object (AdjacencyCluster or AnalyticalModel) with duplicated constructions", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooConstructionParam() { Name = "constructions", NickName = "constructions", Description = "List of SAM constructions that were created or modified by the duplication", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                return result.ToArray();
            }
        }

        /// <summary>
        /// Core solve step. Reads the input object, duplicates constructions by
        /// panel type on a deep copy, then outputs the updated object along with
        /// the list of constructions that were created/modified.
        /// </summary>
        public SAMAnalyticalDuplicateConstructionByPanelType()
          : base("SAMAdjacencyCluster.DuplicateConstructionByPanelType", "SAMAdjacencyCluster.DuplicateConstructionByPanelType",
              "Allows an engineer to duplicate constructions by PanelType, ensuring a single construction name is not shared across different panel type contexts (e.g. separating an 'Internal' construction from an 'External' one).\n\n* Split construction in model by panel type.",
              "SAM", "Analytical01")
        {
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            SAMObject sAMObject = null;
            index = Params.IndexOfInputParam("_analytical");
            if (index == -1 || !dataAccess.GetData(0, ref sAMObject) || sAMObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Construction> constructions = new List<Construction>();
            if (sAMObject is AdjacencyCluster)
            {
                AdjacencyCluster adjacencyCluster = new AdjacencyCluster((AdjacencyCluster)sAMObject);
                constructions = adjacencyCluster.SetConstructionsDefaultPanelType(true);
                sAMObject = adjacencyCluster;
            }
            else if (sAMObject is AnalyticalModel)
            {
                AnalyticalModel analyticalModel = new AnalyticalModel((AnalyticalModel)sAMObject);
                AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
                if (adjacencyCluster != null)
                {
                    adjacencyCluster = new AdjacencyCluster(adjacencyCluster);
                    constructions = adjacencyCluster.SetConstructionsDefaultPanelType(true);
                    analyticalModel = new AnalyticalModel(analyticalModel, adjacencyCluster);
                }

                sAMObject = analyticalModel;
            }

            index = Params.IndexOfOutputParam("analytical");
            if (index != -1)
                dataAccess.SetData(index, sAMObject);

            index = Params.IndexOfOutputParam("constructions");
            if (index != -1)
                dataAccess.SetDataList(index, constructions?.ConvertAll(x => new GooConstruction(x)));
        }
    }
}
