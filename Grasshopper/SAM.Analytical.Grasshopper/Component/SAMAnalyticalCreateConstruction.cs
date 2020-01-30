﻿using System;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using SAM.Analytical.Grasshopper.Properties;
using SAM.Geometry;
using SAM.Geometry.Grasshopper;
using SAM.Geometry.Spatial;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalCreateConstruction : GH_Component
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("75ebc676-3401-481c-90e5-8f767b8b215b");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCreateConstruction()
          : base("SAMAnalytical.CreateConstruction", "SAMAnalyticalCreate.Construction",
              "Create Construction, if nothing connect default values: _name = Basic Roof: SIM_EXT_SLD_Roof DA01 ",
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            inputParamManager.AddTextParameter("_name", "_name", "Contruction Name", GH_ParamAccess.item, "Basic Roof: SIM_EXT_SLD_Roof DA01");

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddGenericParameter("Construction", "Construction", "SAM Analytical Construction", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            string name = null;
            if (!dataAccess.GetData<string>(0, ref name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            dataAccess.SetData(0, new Construction(name));
        }
    }
}