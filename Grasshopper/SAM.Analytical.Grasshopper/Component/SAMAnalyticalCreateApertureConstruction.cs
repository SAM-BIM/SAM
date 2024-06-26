﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalCreateApertureConstruction : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("92dadf21-4b38-4f76-8e24-3de677eaeb1a");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.2";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCreateApertureConstruction()
          : base("SAMAnalytical.CreateApertureConstruction", "SAMAnalytical.CreateApertureConstruction",
              "Create Aperture Construction \n*The layers should be ordered from inside to outside",
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            ApertureConstruction apertureConstruction = Analytical.Query.DefaultApertureConstruction(PanelType.WallExternal, ApertureType.Window);

            inputParamManager.AddTextParameter("_name_", "_name_", "Name", GH_ParamAccess.item, apertureConstruction.Name);
            inputParamManager.AddTextParameter("_apertureType", "_apertureType", "Aperture Type", GH_ParamAccess.item, apertureConstruction.ApertureType.ToString());

            GooConstructionLayerParam gooConstructionLayerParam = null;

            gooConstructionLayerParam = new GooConstructionLayerParam();
            gooConstructionLayerParam.PersistentData.AppendRange(apertureConstruction.PaneConstructionLayers.ConvertAll(x => new GooConstructionLayer(x)));
            inputParamManager.AddParameter(gooConstructionLayerParam, "paneConstructionLayers_", "paneConstructionLayers_", "SAM Pane Contruction Layers \n* order from Inside to Outside", GH_ParamAccess.list);

            gooConstructionLayerParam = new GooConstructionLayerParam();
            gooConstructionLayerParam.Optional = true;
            //gooConstructionLayerParam.PersistentData.AppendRange(apertureConstruction.FrameConstructionLayers.ConvertAll(x => new GooConstructionLayer(x)));
            inputParamManager.AddParameter(gooConstructionLayerParam, "frameConstructionLayers_", "frameConstructionLayers_", "SAM Frame Contruction Layers", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooApertureConstructionParam(), "ApertureConstruction", "ApertureConstruction", "SAM Analytical Aperture Construction", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            string name = null;
            if (!dataAccess.GetData(0, ref name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ApertureType apertureType = ApertureType.Undefined;

            GH_ObjectWrapper objectWrapper = null;
            dataAccess.GetData(1, ref objectWrapper);
            if (objectWrapper != null)
            {
                if (objectWrapper.Value is GH_String)
                    apertureType = Analytical.Query.ApertureType(((GH_String)objectWrapper.Value).Value);
                else
                    apertureType = Analytical.Query.ApertureType(objectWrapper.Value);
            }

            List<GH_ObjectWrapper> objectWrappers = null;

            objectWrappers = new List<GH_ObjectWrapper>();
            if(!dataAccess.GetDataList(2, objectWrappers))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<ConstructionLayer> paneConstructionLayers = new List<ConstructionLayer>();
            foreach (GH_ObjectWrapper objectWrapper_ConstructionLayer in objectWrappers)
            {
                ConstructionLayer constructionLayer = null;
                if (objectWrapper_ConstructionLayer.Value is ConstructionLayer)
                    constructionLayer = (ConstructionLayer)objectWrapper_ConstructionLayer.Value;
                else if (objectWrapper_ConstructionLayer.Value is GooConstructionLayer)
                    constructionLayer = ((GooConstructionLayer)objectWrapper_ConstructionLayer.Value).Value;

                if (constructionLayer == null)
                    continue;

                paneConstructionLayers.Add(constructionLayer);
            }

            objectWrappers = new List<GH_ObjectWrapper>();
            dataAccess.GetDataList(3, objectWrappers);

            List<ConstructionLayer> frameConstructionLayers = null;

            if (objectWrappers != null && objectWrappers.Count != 0)
            {
                foreach (GH_ObjectWrapper objectWrapper_ConstructionLayer in objectWrappers)
                {
                    ConstructionLayer constructionLayer = null;
                    if (objectWrapper_ConstructionLayer.Value is ConstructionLayer)
                        constructionLayer = (ConstructionLayer)objectWrapper_ConstructionLayer.Value;
                    else if (objectWrapper_ConstructionLayer.Value is GooConstructionLayer)
                       constructionLayer = ((GooConstructionLayer)objectWrapper_ConstructionLayer.Value).Value;

                    if (constructionLayer == null)
                        continue;

                    if(frameConstructionLayers == null)
                    {
                        frameConstructionLayers = new List<ConstructionLayer>();
                    }

                    frameConstructionLayers.Add(constructionLayer); 
                }
            }

            dataAccess.SetData(0, new GooApertureConstruction(new ApertureConstruction(Guid.NewGuid(), name, apertureType, paneConstructionLayers, frameConstructionLayers)));
        }
    }
}