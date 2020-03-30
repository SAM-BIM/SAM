﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using SAM.Core.Grasshopper.Properties;

namespace SAM.Core.Grasshopper
{
    public class Filter : GH_Component
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("215ed8be-b96c-4fc7-a806-36fddccbb735");

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Get;

        private GH_OutputParamManager outputParamManager;
        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public Filter()
          : base("GetValue", "GetValue",
              "Get Value of object property",
              "SAM", "Core")
        {

        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            inputParamManager.AddGenericParameter("_objects", "_objects", "Objects", GH_ParamAccess.list);
            inputParamManager.AddTextParameter("_name", "name", "Name", GH_ParamAccess.item, "Name");
            inputParamManager.AddGenericParameter("_value", "_value", "Value", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            this.outputParamManager = outputParamManager;
            outputParamManager.AddGenericParameter("Objects", "Objects", "Objects", GH_ParamAccess.item);
            //outputParamManager.AddGenericParameter("Points", "Pts", "Snap points", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            string name = null;
            if (!dataAccess.GetData(1, ref name) || string.IsNullOrWhiteSpace(name))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
            if (!dataAccess.GetDataList(0, objectWrappers))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<object> objects = new List<object>();
            foreach(GH_ObjectWrapper gH_ObjectWrapper in objectWrappers)
            {
                object @object = gH_ObjectWrapper.Value;

                if (@object is IGH_Goo)
                {
                    try
                    {
                        @object = (@object as dynamic).Value;
                    }
                    catch (Exception exception)
                    {
                        @object = null;
                    }
                }

                if (@object != null)
                    objects.Add(@object);
            }

            if (@objects == null || @objects.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            GH_ObjectWrapper objectWrapper = null;
            if (!dataAccess.GetData(2, ref objectWrapper) || objectWrapper == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            object value = objectWrapper.Value;


            List<object> result = new List<object>();
            foreach(object @object in objects)
            {
                object value_Temp;
                if (Query.TryGetValue(@object, name, out value_Temp))
                {
                    if (value == value_Temp || (value != null && value.Equals(value_Temp)))
                        result.Add(@object);
                }
            }

            dataAccess.SetDataList(0, result);
        }
    }
}