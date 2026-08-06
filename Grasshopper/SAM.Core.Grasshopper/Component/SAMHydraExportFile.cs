// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public class SAMHydraExportFile : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e728a90f-a7e0-47e1-b45b-6e2a96222d1d");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small3;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        //private GH_OutputParamManager outputParamManager;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMHydraExportFile()
          : base("SAMHydra.ExportFile", "SAMHydra.ExportFile",
              "Export the active Grasshopper document to a SAMHydra repository for sharing with the community.",
              "SAM", "Hydra")
        {
        }



        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_githubUserName", NickName = "_githubUserName", Description = "Your GitHub username for the target SAMHydra repository", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_fileName", NickName = "_fileName", Description = "Name for the exported file (without extension)", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_fileDescription", NickName = "_fileDescription", Description = "Description text for the exported file", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "changeLog_", NickName = "changeLog_", Description = "Optional list of change-log entries documenting revisions", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "fileTags_", NickName = "fileTags_", Description = "Optional list of tags for categorising the export", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "targetFolder_", NickName = "targetFolder_", Description = "Optional subfolder path within the Hydra repository", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "includeRhino_", NickName = "includeRhino_", Description = "Set to True to include the Rhino geometry alongside the definition", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "gHForThumb_", NickName = "gHForThumb_", Description = "Set to True to use the Grasshopper canvas as the thumbnail image", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "additionalFiles_", NickName = "additionalFiles_", Description = "Optional list of additional file paths to include in the export", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_export", NickName = "_export", Description = "Set to True to trigger the export", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "messages", NickName = "messages", Description = "List of status messages returned from the Hydra export process", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="dataAccess">
        /// The DA object is used to retrieve from inputs and store in outputs.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index;

            string githubUserName = null;
            index = Params.IndexOfInputParam("_githubUserName");
            if (index == -1 || !dataAccess.GetData(index, ref githubUserName) || string.IsNullOrWhiteSpace(githubUserName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string fileName = null;
            index = Params.IndexOfInputParam("_fileName");
            if (index == -1 || !dataAccess.GetData(index, ref fileName) || string.IsNullOrWhiteSpace(fileName))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<string> description = new List<string>();
            index = Params.IndexOfInputParam("_fileDescription");
            if (index == -1 || !dataAccess.GetDataList(index, description) || description == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<string> changeLog = new List<string>();
            index = Params.IndexOfInputParam("changeLog_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, changeLog);
            }

            List<string> fileTags = new List<string>();
            index = Params.IndexOfInputParam("fileTags_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, fileTags);
            }

            string targetFolder = null;
            index = Params.IndexOfInputParam("targetFolder_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref targetFolder);
            }

            bool includeRhino = false;
            index = Params.IndexOfInputParam("includeRhino_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref includeRhino);
            }

            bool gHForThumb = true;
            index = Params.IndexOfInputParam("gHForThumb_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref gHForThumb);
            }

            List<string> additionalFiles = new List<string>();
            index = Params.IndexOfInputParam("additionalFiles_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, additionalFiles);
            }

            bool export = false;
            index = Params.IndexOfInputParam("_export");
            if (index != -1)
            {
                dataAccess.GetData(index, ref export);
            }

            List<string> messages = new List<string>() { "Export not activated." };
            if (export)
            {
                GH_Document gH_Document = OnPingDocument();

                messages = Modify.ExportHydra(gH_Document, githubUserName, fileName, description, changeLog, fileTags, targetFolder, includeRhino, gHForThumb, additionalFiles);
            }

            index = Params.IndexOfOutputParam("messages");
            if (index != -1)
            {
                dataAccess.SetDataList(index, messages?.ConvertAll(x => new GH_String(x)));
            }

        }
    }
}
