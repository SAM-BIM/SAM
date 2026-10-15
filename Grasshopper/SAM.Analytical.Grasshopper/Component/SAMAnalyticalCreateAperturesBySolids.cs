// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using SAM.Geometry.Grasshopper;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Grasshopper
{
    /// <summary>
    /// Converts Rhino window and door solids (frames, frame members, glass, door leaves) into one SAM Aperture per
    /// window or door assembly with <see cref="Create.AperturesBySolids"/>. The apertures are not hosted: connect them
    /// to SAMAnalytical.AddAperturesByAperture.
    /// </summary>
    public class SAMAnalyticalCreateAperturesBySolids : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("b115fc0d-c40f-4a98-938b-9b829a28cb95");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.1";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_GH_AperturePluralCreate);

        public SAMAnalyticalCreateAperturesBySolids()
          : base("SAMAnalytical.CreateAperturesBySolids", "SAMAnalytical.CreateAperturesBySolids",
              "Create SAM Analytical Apertures (Windows and Doors) from closed Rhino solids of window and door members: frames, frame members, glass and door leaves.\n" +
              "Solids that touch (within maxGap_) or share a Rhino group form one assembly, which gives one Aperture: the full opening including the frame, on the assembly's mid-plane. An assembly with any door member is a Door. The assembly depth is stored as SourceThickness.\n" +
              "A Solid or assembly is skipped, with a Warning, when it is not closed or not planar (depth over smallest extent above 0.5). log gives one line per assembly.\n" +
              "The Apertures are not hosted: give the same solids to SAMAnalytical.CreatePanelsBySolids (apertureGeometries_) so their openings are filled, then connect them to SAMAnalytical.AddAperturesByAperture. Rhino units must be metres.",
              "SAM", "Analytical01")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];

                Param_Brep windows = new() { Name = "windows_", NickName = "windows_", Description = "Window solids: frames, frame members and glass. Solids are numbered #0, #1, ... in log, windows_ first. Referenced Rhino objects keep their name, id and layer on the Apertures.", Access = GH_ParamAccess.list, Optional = true };
                windows.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(windows, ParamVisibility.Binding));

                Param_Brep doors = new() { Name = "doors_", NickName = "doors_", Description = "Door solids: leaves (and door frames), numbered after windows_ in log. A window solid touching a door leaf (e.g. a door frame on a window layer) joins the Door.", Access = GH_ParamAccess.list, Optional = true };
                doors.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(doors, ParamVisibility.Binding));

                GooApertureConstructionParam apertureConstructions = new() { Name = "apertureConstructions_", NickName = "apertureConstructions_", Description = "SAM Aperture Constructions (optional); each Aperture takes the first one of its type (Window or Door). Default: the default external construction of the type.", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(apertureConstructions, ParamVisibility.Binding));

                Param_Number param_Number = new() { Name = "maxGap_", NickName = "maxGap_", Description = "Solids closer than this [m] belong to the same window or door assembly, default 0.01 m", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.01);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                Param_Boolean param_Boolean = new() { Name = "useRhinoGroups_", NickName = "useRhinoGroups_", Description = "Referenced solids in the same Rhino group form one assembly, and solids in different groups are never merged, default true", Access = GH_ParamAccess.item, Optional = true };
                param_Boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Voluntary));

                return [.. result];
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result =
                [
                    new GH_SAMParam(new GooApertureParam() { Name = "apertures", NickName = "apertures", Description = "SAM Analytical Apertures, one per window or door assembly (not hosted)", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new Param_Brep() { Name = "excluded", NickName = "excluded", Description = "Solids that gave no Aperture; see log for the reason", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_String() { Name = "log", NickName = "log", Description = "Diagnostics: a summary, then one line per assembly, as 'Severity: Solid #i 'Name': Message'. Severity is Info, Warning or Error.", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                ];
                return [.. result];
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            // SAM analytical geometry is in metres and Rhino geometry is converted without scaling: refuse rather
            // than produce apertures a thousand times too large.
            string unitsWarning = Geometry.Rhino.Query.ModelUnitsWarning();
            if (unitsWarning != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, unitsWarning);
                return;
            }

            List<GH_Brep> windows = [];
            int index = Params.IndexOfInputParam("windows_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, windows);
            }

            List<GH_Brep> doors = [];
            index = Params.IndexOfInputParam("doors_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, doors);
            }

            if (windows.Count == 0 && doors.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Connect window and/or door solids");
                return;
            }

            List<ApertureConstruction> apertureConstructions = [];
            index = Params.IndexOfInputParam("apertureConstructions_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, apertureConstructions);
            }

            double maxGap = 0.01;
            index = Params.IndexOfInputParam("maxGap_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref maxGap);
            }

            bool useRhinoGroups = true;
            index = Params.IndexOfInputParam("useRhinoGroups_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref useRhinoGroups);
            }

            global::Rhino.RhinoDoc rhinoDoc = global::Rhino.RhinoDoc.ActiveDoc;
            List<GH_Brep> breps = [.. windows, .. doors];
            List<ApertureSolid> apertureSolids = [];
            for (int i = 0; i < breps.Count; i++)
            {
                GH_Brep brep = breps[i];
                ISAMGeometry3D geometry3D = null;
                List<ISAMGeometry3D> geometry3Ds = brep?.ToSAM(true);
                if (geometry3Ds != null)
                {
                    geometry3D = (ISAMGeometry3D)geometry3Ds.OfType<Shell>().FirstOrDefault() ?? (geometry3Ds.Count == 1 ? geometry3Ds.OfType<Face3D>().FirstOrDefault() : null);
                }

                ApertureSolid apertureSolid = new(geometry3D, i < windows.Count ? ApertureType.Window : ApertureType.Door);
                if (brep != null && brep.IsReferencedGeometry)
                {
                    apertureSolid.SourceId = brep.ReferenceID.ToString();
                    global::Rhino.DocObjects.RhinoObject rhinoObject = rhinoDoc?.Objects.FindId(brep.ReferenceID);
                    if (rhinoObject != null)
                    {
                        apertureSolid.SourceName = string.IsNullOrWhiteSpace(rhinoObject.Attributes.Name) ? null : rhinoObject.Attributes.Name;
                        apertureSolid.SourceLayer = rhinoDoc.Layers.FindIndex(rhinoObject.Attributes.LayerIndex)?.FullPath;

                        int[] groups = rhinoObject.Attributes.GetGroupList();
                        if (useRhinoGroups && groups != null && groups.Length != 0)
                        {
                            apertureSolid.GroupKey = string.Join(",", groups.OrderBy(x => x));
                        }
                    }
                }

                apertureSolids.Add(apertureSolid);
            }

            List<Aperture> apertures = apertureSolids.AperturesBySolids(out List<string> log, out List<int> excluded, apertureConstructions, maxGap);
            log.Insert(0, Core.Query.DiagnosticMessage(Core.LogRecordType.Message, null, string.Format("Created {0} ({1}, {2}) from {3} ({4}, {5}; numbered windows_ first). {6}",
                Core.Query.Plural(apertures.Count, "aperture"), Core.Query.Plural(apertures.Count(x => x.ApertureType == ApertureType.Window), "window"), Core.Query.Plural(apertures.Count(x => x.ApertureType == ApertureType.Door), "door"),
                Core.Query.Plural(breps.Count, "solid"), Core.Query.Plural(windows.Count, "window solid"), Core.Query.Plural(doors.Count, "door solid"),
                excluded.Count == 0 ? "No solids were skipped." : string.Format("Skipped {0}.", Core.Query.Plural(excluded.Count, "solid")))));

            if (excluded.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("Skipped {0} that gave no aperture: {1}. See log.", Core.Query.Plural(excluded.Count, "solid"), string.Join(", ", excluded.ConvertAll(x => string.Format("#{0}", x)))));
            }

            index = Params.IndexOfOutputParam("apertures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, apertures.ConvertAll(x => new GooAperture(x)));
            }

            index = Params.IndexOfOutputParam("excluded");
            if (index != -1)
            {
                dataAccess.SetDataList(index, excluded.ConvertAll(x => breps[x]));
            }

            index = Params.IndexOfOutputParam("log");
            if (index != -1)
            {
                dataAccess.SetDataList(index, log);
            }
        }
    }
}
