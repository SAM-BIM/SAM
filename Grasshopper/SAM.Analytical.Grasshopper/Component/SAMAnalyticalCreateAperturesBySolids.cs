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
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_GH_AperturePluralCreate);

        public SAMAnalyticalCreateAperturesBySolids()
          : base("SAMAnalytical.CreateAperturesBySolids", "SAMAnalytical.CreateAperturesBySolids",
              "Create SAM Analytical Apertures from window and door solids (frames, frame members, glass, door leaves).\nSolids touching each other (within maxGap_) or sharing a Rhino group form one assembly and give one aperture: the full opening including the frame, on the assembly's mid-plane. An assembly with any door member is a Door.\nRhino document units must be metres. Connect apertures to SAMAnalytical.AddAperturesByAperture.",
              "SAM", "Analytical01")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];

                Param_Brep windows = new() { Name = "windows_", NickName = "windows_", Description = "Window solids: frames, frame members and glass. Referenced Rhino objects keep their name, id and layer on the apertures.", Access = GH_ParamAccess.list, Optional = true };
                windows.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(windows, ParamVisibility.Binding));

                Param_Brep doors = new() { Name = "doors_", NickName = "doors_", Description = "Door solids: leaves (and door frames). A window solid touching a door leaf (e.g. a door frame on a window layer) joins the door.", Access = GH_ParamAccess.list, Optional = true };
                doors.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(doors, ParamVisibility.Binding));

                GooApertureConstructionParam apertureConstructions = new() { Name = "apertureConstructions_", NickName = "apertureConstructions_", Description = "Optional SAM Aperture Constructions; each aperture takes the first one of its type (Window or Door). Default: the default external construction of the type.", Access = GH_ParamAccess.list, Optional = true };
                result.Add(new GH_SAMParam(apertureConstructions, ParamVisibility.Binding));

                Param_Number param_Number = new() { Name = "maxGap_", NickName = "maxGap_", Description = "Solids closer than this [m] belong to the same window or door assembly", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.01);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                Param_Boolean param_Boolean = new() { Name = "useRhinoGroups_", NickName = "useRhinoGroups_", Description = "Referenced solids in the same Rhino group form one assembly, and solids in different groups are never merged", Access = GH_ParamAccess.item, Optional = true };
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
                    new GH_SAMParam(new Param_Brep() { Name = "excluded", NickName = "excluded", Description = "Solids that gave no aperture (see log)", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_String() { Name = "log", NickName = "log", Description = "Per-assembly conversion report", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
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
            log.Insert(0, string.Format("{0} aperture(s) ({1} window(s), {2} door(s)) from {3} window and {4} door solid(s); {5} solid(s) gave no aperture. Solids are numbered windows_ first, then doors_.",
                apertures.Count, apertures.Count(x => x.ApertureType == ApertureType.Window), apertures.Count(x => x.ApertureType == ApertureType.Door), windows.Count, doors.Count, excluded.Count));

            if (excluded.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("{0} solid(s) gave no aperture; see log", excluded.Count));
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
