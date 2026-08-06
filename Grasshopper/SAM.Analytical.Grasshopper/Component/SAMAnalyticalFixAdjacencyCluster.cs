// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalFixAdjacencyCluster : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("4b43d355-6ca0-40de-b0df-9da29bede2a7");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalFixAdjacencyCluster()
          : base("SAMAnalytical.FixAdjacencyCluster", "SAMAnalytical.FixAdjacencyCluster",
              "Allows the engineer to validate and fix an AdjacencyCluster by checking each panel's adjacency count, correcting PanelType (e.g. external to internal), duplicating constructions, and assigning the correct prefix (SIM_EXT_SLD/SIM_INT_SLD), as well as fixing aperture assignments",
              "SAM", "Analytical01")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                GooAdjacencyClusterParam adjacencyClusterParam = new GooAdjacencyClusterParam() { Name = "_adjacencyCluster", NickName = "_adjacencyCluster", Description = "SAM Analytical AdjacencyCluster", Access = GH_ParamAccess.item };
                adjacencyClusterParam.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(adjacencyClusterParam, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAdjacencyClusterParam() { Name = "adjacencyCluster", NickName = "adjacencyCluster", Description = "SAM Analytical AdjacencyCluster", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "panelPrefixes", NickName = "panelPrefixes", Description = "Construction prefixes that were reassigned to panels (e.g. SIM_EXT_SLD, SIM_INT_SLD)", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooPanelParam() { Name = "panels", NickName = "panels", Description = "Panels that were updated with corrected PanelType and construction", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "aperturePrefixes", NickName = "aperturePrefixes", Description = "Construction prefixes that were reassigned to apertures", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooPanelParam() { Name = "apertures", NickName = "apertures", Description = "Apertures that were updated", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index = -1;

            index = Params.IndexOfInputParam("_adjacencyCluster");
            AdjacencyCluster adjacencyCluster = null;
            if (index == -1 || !dataAccess.GetData(index, ref adjacencyCluster) || adjacencyCluster == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid Data");
                return;
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster);
            adjacencyCluster.FixAdjacencyCluster(out List<string> panelPrefixes, out List<Panel> panels, out List<string> aperturePrefixes, out List<Aperture> apertures);

            index = Params.IndexOfOutputParam("adjacencyCluster");
            if (index != -1)
                dataAccess.SetData(index, new GooAdjacencyCluster(adjacencyCluster));

            index = Params.IndexOfOutputParam("panelPrefixes");
            if (index != -1)
                dataAccess.SetDataList(index, panelPrefixes);

            index = Params.IndexOfOutputParam("panels");
            if (index != -1)
                dataAccess.SetDataList(index, panels?.ConvertAll(x => new GooPanel(x)));

            index = Params.IndexOfOutputParam("aperturePrefixes");
            if (index != -1)
                dataAccess.SetDataList(index, aperturePrefixes);

            index = Params.IndexOfOutputParam("apertures");
            if (index != -1)
                dataAccess.SetDataList(index, apertures?.ConvertAll(x => new GooAperture(x)));
        }
    }
}
