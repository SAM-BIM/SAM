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

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            int index = -1;

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "DEBUG: _run param not found");
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: _run index={0}", index));

            if (!dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "DEBUG: GetData for _run failed");
                return;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: _run={0}", run));

            if (!run)
            {
                return;
            }

            bool dryRun = false;
            index = Params.IndexOfInputParam("_dryRun");
            if (index != -1)
            {
                dataAccess.GetData(index, ref dryRun);
            }

            GH_Document gH_Document = OnPingDocument();
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: doc={0}", gH_Document != null ? "ok" : "null"));

            List<GH_SAMComponent> targetComponents = ResolveTargetComponents(dataAccess, gH_Document);
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: targets={0}", targetComponents != null ? targetComponents.Count.ToString() : "null"));

            List<GH_SAMComponent> result;

            if (dryRun)
            {
                Log log;

                if (targetComponents != null && targetComponents.Count > 0)
                {
                    result = Modify.PreviewUpdateComponents(targetComponents, out log);
                }
                else
                {
                    result = Modify.PreviewUpdateComponents(gH_Document, out log);
                }

                index = Params.IndexOfOutputParam("log");
                if (index != -1)
                {
                    dataAccess.SetData(index, log);
                }

                index = Params.IndexOfOutputParam("succeeded");
                if (index != -1)
                {
                    dataAccess.SetData(index, false);
                }

                ResetRunInput();
                return;
            }

            {
                Log log;

                if (targetComponents != null && targetComponents.Count > 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "DEBUG: updating specific targets");
                    result = Modify.UpdateComponents(targetComponents, out log);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "DEBUG: scanning whole document");
                    result = Modify.UpdateComponents(gH_Document, out log);
                }

                index = Params.IndexOfOutputParam("log");
                if (index != -1)
                {
                    dataAccess.SetData(index, log);
                }

                index = Params.IndexOfOutputParam("succeeded");
                if (index != -1)
                {
                    dataAccess.SetData(index, result != null && result.Count != 0);
                }
            }

            ResetRunInput();
        }

        private List<GH_SAMComponent> ResolveTargetComponents(IGH_DataAccess dataAccess, GH_Document gH_Document)
        {
            int index = Params.IndexOfInputParam("_components");
            if (index == -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "DEBUG: _components param not found");
                return null;
            }

            List<string> names = new List<string>();
            if (!dataAccess.GetDataList(index, names))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "DEBUG: GetDataList for _components failed");
                return null;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: _components count={0}", names.Count));

            if (names.Count == 0)
            {
                return null;
            }

            foreach (string s in names)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: component query='{0}'", s ?? "(null)"));
            }

            if (gH_Document == null)
            {
                return null;
            }

            List<GH_SAMComponent> result = new List<GH_SAMComponent>();
            IList<IGH_DocumentObject> allObjects = gH_Document.Objects;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: doc objects={0}", allObjects != null ? allObjects.Count.ToString() : "null"));

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

                bool found = false;
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
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("DEBUG: '{0}' not found", trimmed));
                }
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, string.Format("DEBUG: resolved {0} component(s)", result.Count));

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
