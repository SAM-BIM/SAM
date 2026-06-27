// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAM.Core.Grasshopper
{
    public class SAMCoreToFile : GH_SAMComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("82599e6b-f17f-4bd9-89bd-e9b85abefe79");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small3;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMCoreToFile()
          : base("ToFile", "ToFile",
              "Writes SAM objects to File ",
              "SAM", "Core")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager inputParamManager)
        {
            string path = null;

            int index = -1;

            index = inputParamManager.AddGenericParameter("_SAMObjects", "_SAMObjects", "any SAM Objects", GH_ParamAccess.list);
            inputParamManager[index].DataMapping = GH_DataMapping.Flatten;

            index = inputParamManager.AddTextParameter("path_", "path_", "File path including extension", GH_ParamAccess.item, path);
            inputParamManager[index].Optional = true;

            inputParamManager.AddBooleanParameter("_run", "_run", "Run, set to True to export to given path", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager outputParamManager)
        {
            outputParamManager.AddBooleanParameter("Successful", "Successful", "Correctly imported?", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            bool run = false;
            if (!dataAccess.GetData(2, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                dataAccess.SetData(0, false);
                return;
            }
            if (!run)
                return;

            string path = null;
            dataAccess.GetData(1, ref path);

            List<GH_ObjectWrapper> objectWrapperList = new List<GH_ObjectWrapper>();

            if (!dataAccess.GetDataList(0, objectWrapperList) || objectWrapperList == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                dataAccess.SetData(0, false);
                return;
            }

            List<IJSAMObject> jSAMObjects = new List<IJSAMObject>();
            foreach (GH_ObjectWrapper gH_ObjectWrapper in objectWrapperList)
            {
                IJSAMObject jSAMObject = ToJSAMObject(gH_ObjectWrapper?.Value);
                if (jSAMObject != null)
                {
                    jSAMObjects.Add(jSAMObject);
                }
            }

            bool result = Core.Convert.ToFile(jSAMObjects, path);
            dataAccess.SetData(0, result);
        }

        private static IJSAMObject ToJSAMObject(object @object)
        {
            if (@object == null)
            {
                return null;
            }

            if (@object is IGooJSAMObject gooJSAMObject)
            {
                return gooJSAMObject.GetJSAMObject();
            }

            object value = @object;
            if (@object is IGH_Goo gH_Goo)
            {
                try
                {
                    value = (gH_Goo as dynamic).Value;
                }
                catch
                {
                    value = @object;
                }
            }

            if (value is IJSAMObject jSAMObject)
            {
                if (ImplementsInterface(value, "SAM.Geometry.Object.ISAMGeometryObject"))
                {
                    return jSAMObject;
                }

                return ToSAMGeometryObject(value) ?? jSAMObject;
            }

            return ToSAMGeometryObject(value);
        }

        private static IJSAMObject ToSAMGeometryObject(object @object)
        {
            if (@object == null || !ImplementsInterface(@object, "SAM.Geometry.ISAMGeometry"))
            {
                return null;
            }

            Type convertType = @object.GetType().Assembly.GetType("SAM.Geometry.Object.Convert") ?? Type.GetType("SAM.Geometry.Object.Convert, SAM.Geometry");
            MethodInfo methodInfo = convertType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "ToSAM_ISAMGeometryObject"
                    && x.GetParameters().Length == 4
                    && x.GetParameters()[0].ParameterType.FullName == "SAM.Geometry.ISAMGeometry");

            if (methodInfo == null)
            {
                return null;
            }

            try
            {
                return methodInfo.Invoke(null, new object[] { @object, null, null, null }) as IJSAMObject;
            }
            catch
            {
                return null;
            }
        }

        private static bool ImplementsInterface(object @object, string fullName)
        {
            return @object?.GetType().GetInterfaces().Any(x => x.FullName == fullName) == true;
        }
    }
}
