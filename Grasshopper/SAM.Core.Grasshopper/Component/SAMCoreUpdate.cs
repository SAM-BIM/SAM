// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SAM.Core.Grasshopper
{
    public class SAMCoreUpdate : GH_SAMVariableOutputParameterComponent
    {
        public override Guid ComponentGuid => new Guid("a89bfee3-3a3c-4d29-9c7a-64073724eddc");

        public override string LatestComponentVersion => "1.0.0";

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                global::Grasshopper.Kernel.Parameters.Param_String param_Components;
                param_Components = new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_components", NickName = "_components", Description = "Component names, nicknames, or GUID strings (optional). If empty, scans entire document.", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(param_Components, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_DryRun;
                param_DryRun = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_dryRun", NickName = "_dryRun", Description = "Preview mode — reports changes without applying them", Access = GH_ParamAccess.item, Optional = true };
                param_DryRun.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_DryRun, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean;
                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Run Update", Access = GH_ParamAccess.item, Optional = false };
                param_Boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooLogParam() { Name = "log", NickName = "log", Description = "Log", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "succeeded", NickName = "succeeded", Description = "Succeeded", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        public SAMCoreUpdate()
          : base("SAMCore.Update", "SAMCore.Update",
              "Updates Grasshopper components to the latest version",
              "SAM", "SAM")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown toolStripDropDown)
        {
            Menu_AppendItem(toolStripDropDown, "Update", Menu_Update);
        }

        private void Menu_Update(object sender, EventArgs e)
        {
            Modify.UpdateComponents(OnPingDocument(), out Log log);
        }

        private GH_Document pendingDocument;
        private List<GH_SAMComponent> pendingTargets;

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            try
            {
                int index = -1;
                bool run = false;

                index = Params.IndexOfInputParam("_run");
                if (index == -1 || !dataAccess.GetData(index, ref run) || !run)
                {
                    return;
                }

                GH_Document gH_Document = OnPingDocument();
                if (gH_Document == null)
                {
                    return;
                }

                bool dryRun = false;
                index = Params.IndexOfInputParam("_dryRun");
                if (index != -1)
                {
                    dataAccess.GetData(index, ref dryRun);
                }

                if (dryRun)
                {
                    List<GH_SAMComponent> targetComponents = ResolveTargetComponents(dataAccess, gH_Document);
                    Log log;
                    if (targetComponents != null && targetComponents.Count > 0)
                    {
                        Modify.PreviewUpdateComponents(targetComponents, out log);
                    }
                    else
                    {
                        Modify.PreviewUpdateComponents(gH_Document, out log);
                    }

                    index = Params.IndexOfOutputParam("log");
                    if (index != -1) dataAccess.SetData(index, log);

                    index = Params.IndexOfOutputParam("succeeded");
                    if (index != -1) dataAccess.SetData(index, false);
                    return;
                }

                pendingDocument = gH_Document;
                pendingTargets = ResolveTargetComponents(dataAccess, gH_Document);
                pendingDocument.SolutionEnd += GH_Document_SolutionEnd;

                index = Params.IndexOfOutputParam("succeeded");
                if (index != -1) dataAccess.SetData(index, true);

                ResetRunInput();
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Crash: " + ex.Message);
            }
        }

        private void GH_Document_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            GH_Document doc = sender as GH_Document;
            if (doc != null)
            {
                doc.SolutionEnd -= GH_Document_SolutionEnd;
            }

            if (pendingDocument == null)
            {
                return;
            }

            List<GH_SAMComponent> result;

            Log log;
            if (pendingTargets != null && pendingTargets.Count > 0)
            {
                result = Modify.UpdateComponents(pendingTargets, out log);
            }
            else
            {
                result = Modify.UpdateComponents(pendingDocument, out log);
            }

            int logIndex = Params.IndexOfOutputParam("log");
            if (logIndex != -1)
            {
                Params.Output[logIndex].ClearData();
                Params.Output[logIndex].AddVolatileData(new global::Grasshopper.Kernel.Data.GH_Path(0), 0, log);
            }

            int succeededIndex = Params.IndexOfOutputParam("succeeded");
            if (succeededIndex != -1)
            {
                Params.Output[succeededIndex].ClearData();
                Params.Output[succeededIndex].AddVolatileData(new global::Grasshopper.Kernel.Data.GH_Path(0), 0, result != null && result.Count != 0);
            }

            pendingDocument = null;
            pendingTargets = null;
        }

        private List<GH_SAMComponent> ResolveTargetComponents(IGH_DataAccess dataAccess, GH_Document gH_Document)
        {
            int index = Params.IndexOfInputParam("_components");
            if (index == -1)
            {
                return null;
            }

            List<string> names = new List<string>();
            if (!dataAccess.GetDataList(index, names) || names.Count == 0)
            {
                return null;
            }

            if (gH_Document == null)
            {
                return null;
            }

            List<GH_SAMComponent> result = new List<GH_SAMComponent>();
            IList<IGH_DocumentObject> allObjects = gH_Document.Objects;
            if (allObjects == null)
            {
                return null;
            }

            foreach (string search in names)
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    continue;
                }

                string trimmed = search.Trim();

                if (Guid.TryParse(trimmed, out Guid guid))
                {
                    foreach (IGH_DocumentObject obj in allObjects)
                    {
                        if (obj is GH_SAMComponent samComp && samComp.InstanceGuid == guid)
                        {
                            if (!result.Contains(samComp))
                            {
                                result.Add(samComp);
                            }
                            break;
                        }
                    }
                    continue;
                }

                foreach (IGH_DocumentObject obj in allObjects)
                {
                    if (obj is GH_SAMComponent samComp)
                    {
                        if (string.Equals(samComp.Name, trimmed, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(samComp.NickName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                            samComp.Name.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!result.Contains(samComp))
                            {
                                result.Add(samComp);
                            }
                            break;
                        }
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        private void ResetRunInput()
        {
            int index = Params.IndexOfInputParam("_run");
            if (index != -1)
            {
                IGH_Param runParam = Params.Input[index];
                runParam.ClearData();
                if (runParam is global::Grasshopper.Kernel.Parameters.Param_Boolean boolParam)
                {
                    boolParam.SetPersistentData(false);
                }
            }
        }
    }
}
