﻿using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalUpdateLibrary : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0300beb1-0074-4544-8ad1-1b12bff777fd");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalUpdateLibrary()
          : base("SAMAnalytical.UpdateLibrary", "SAMAnalytical.UpdateLibrary",
              "Update Library in analyticalModel",
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            int index = -1;

            index = inputParamManager.AddParameter(new GooAnalyticalModelParam(), "_analyticalModel", "_analyticalModel", "SAM Analytical Model", GH_ParamAccess.item);

            index = inputParamManager.AddParameter(new global::Grasshopper.Kernel.Parameters.Param_GenericObject(), "_library", "_library", "SAM Library (MaterialLibrary or ProfileLibrary)", GH_ParamAccess.item);

            index = inputParamManager.AddBooleanParameter("_missingOnly", "_missingOnly", "Copy only missing objects from library", GH_ParamAccess.item, true);
            inputParamManager[index].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddParameter(new GooAnalyticalModelParam(), "analyticalModel", "analyticalModel", "SAM Analytical Model", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            AnalyticalModel analyticalModel = null;
            if (!dataAccess.GetData(0, ref analyticalModel) || analyticalModel == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ISAMLibrary sAMLibrary = null;
            if (!dataAccess.GetData(1, ref sAMLibrary) || sAMLibrary == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            bool missingOnly = true;
            if (!dataAccess.GetData(2, ref missingOnly))
            {

            }

            if(sAMLibrary is MaterialLibrary)
            {
                IEnumerable<IMaterial> materials = missingOnly ? Analytical.Query.Materials(analyticalModel.AdjacencyCluster, (MaterialLibrary)sAMLibrary) : ((MaterialLibrary)sAMLibrary).GetMaterials();
                if(materials != null)
                {
                    analyticalModel = new AnalyticalModel(analyticalModel);
                    materials?.ToList().ForEach(x => analyticalModel.AddMaterial(x));
                }
            }
            else if(sAMLibrary is ProfileLibrary)
            {

                IEnumerable<Profile> profiles = missingOnly ? Analytical.Query.Profiles(analyticalModel.AdjacencyCluster, (ProfileLibrary)sAMLibrary) : ((ProfileLibrary)sAMLibrary).GetProfiles();
                if (profiles != null)
                {
                    analyticalModel = new AnalyticalModel(analyticalModel);
                    profiles?.ToList().ForEach(x => analyticalModel.AddProfile(x));
                }
            }
            else if (sAMLibrary is InternalConditionLibrary)
            {

                IEnumerable<InternalCondition> internalConditions = ((InternalConditionLibrary)sAMLibrary).GetInternalConditions();
                if (internalConditions != null)
                {
                    analyticalModel = new AnalyticalModel(analyticalModel);
                    internalConditions?.ToList().ForEach(x => analyticalModel.AddInternalCondition(x));
                }
            }

            dataAccess.SetData(0, new GooAnalyticalModel(analyticalModel));
        }
    }
}