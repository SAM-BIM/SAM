// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Core.Grasshopper.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
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

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean;
                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Run Update", Access = GH_ParamAccess.item, Optional = false };
                param_Boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_GenericObject param_Components;
                param_Components = new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_components", NickName = "_components", Description = "Specific components to update (optional). Wire GH_SAMComponent objects, GUID strings, or component names. If empty, scans entire document.", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(param_Components, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_DryRun;
                param_DryRun = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_dryRun", NickName = "_dryRun", Description = "Preview mode — reports changes without applying them", Access = GH_ParamAccess.item, Optional = true };
                param_DryRun.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_DryRun, ParamVisibility.Binding));

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
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

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

            List<GH_SAMComponent> targetComponents = ResolveTargetComponents(dataAccess, gH_Document);

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
                    result = Modify.UpdateComponents(targetComponents, out log);
                }
                else
                {
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
                return null;
            }

            List<object> rawItems = new List<object>();
            if (!dataAccess.GetDataList(index, rawItems) || rawItems.Count == 0)
            {
                return null;
            }

            if (gH_Document == null)
            {
                return null;
            }

            List<GH_SAMComponent> result = new List<GH_SAMComponent>();
            IList<IGH_DocumentObject> allObjects = gH_Document.Objects;

            foreach (object item in rawItems)
            {
                if (item == null)
                {
                    continue;
                }

                if (item is GH_SAMComponent component)
                {
                    result.Add(component);
                    continue;
                }

                Guid guid = Guid.Empty;
                bool hasGuid = false;

                if (item is Guid guidItem)
                {
                    guid = guidItem;
                    hasGuid = true;
                }
                else if (item is string str)
                {
                    if (Guid.TryParse(str, out Guid parsedGuid))
                    {
                        guid = parsedGuid;
                        hasGuid = true;
                    }
                    else if (allObjects != null)
                    {
                        foreach (IGH_DocumentObject obj in allObjects)
                        {
                            if (obj is GH_SAMComponent samComp && samComp.Name == str)
                            {
                                result.Add(samComp);
                                break;
                            }
                        }
                    }
                }
                else if (item is IGH_Goo goo)
                {
                    string value = goo.ToString();
                    if (Guid.TryParse(value, out Guid gooGuid))
                    {
                        guid = gooGuid;
                        hasGuid = true;
                    }
                }

                if (hasGuid && guid != Guid.Empty && allObjects != null)
                {
                    foreach (IGH_DocumentObject obj in allObjects)
                    {
                        if (obj is GH_SAMComponent samComp && samComp.InstanceGuid == guid)
                        {
                            result.Add(samComp);
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
