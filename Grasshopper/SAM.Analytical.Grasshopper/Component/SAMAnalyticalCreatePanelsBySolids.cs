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
    /// Converts thin closed Rhino solids (walls, slabs, ceilings, roofs, gables) into analytical mid-surface panels
    /// with <see cref="Create.PanelsBySolid"/>: true section outlines with notches and holes, aperture-claimed openings
    /// filled, unclaimed openings (e.g. stair voids) as Air panels, and source traceability on every panel - then snaps outer
    /// edges onto neighbouring mid-planes (<see cref="Analytical.Query.SnapToJunctions"/>).
    /// </summary>
    public class SAMAnalyticalCreatePanelsBySolids : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("8b0f5f2c-6a8e-4f6d-9d1c-3e2a7b5c4d10");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_GH_PanelCreate);

        public SAMAnalyticalCreatePanelsBySolids()
          : base("SAMAnalytical.CreatePanelsBySolids", "SAMAnalytical.CreatePanelsBySolids",
              "Create SAM Analytical mid-surface Panels from thin closed solids (walls, slabs, ceilings, roofs, gables).\nOpenings claimed by aperture geometry are filled (add the apertures afterwards); unclaimed openings such as stair voids become Air panels.\nOuter edges are snapped onto neighbouring mid-planes at real junctions (snap_). Rhino document units must be metres. Feed the panels to SAMOCCT.CreateAdjacencyCluster.",
              "SAM", "Analytical01")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];

                Param_Brep solids = new() { Name = "_solids", NickName = "_solids", Description = "Closed solids, one per building element (wall, slab, ceiling, roof, gable). Referenced Rhino objects keep their name, id and layer on the panels.", Access = GH_ParamAccess.list };
                solids.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(solids, ParamVisibility.Binding));

                Param_Geometry apertures = new() { Name = "apertureGeometries_", NickName = "apertureGeometries_", Description = "Window and door geometry (frames, glass, leaves). Openings they occupy are filled in the host panel so the apertures can be added later.", Access = GH_ParamAccess.list, Optional = true };
                apertures.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(apertures, ParamVisibility.Binding));

                Param_Number param_Number = new() { Name = "minThickness_", NickName = "minThickness_", Description = "Elements thinner than this are skipped (e.g. balustrades) [m]", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.08);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new() { Name = "snap_", NickName = "snap_", Description = "Maximum distance [m] an outer panel edge moves to meet a neighbouring panel's mid-plane at a real junction (0 disables)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.3);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new() { Name = "maxThicknessRatio_", NickName = "maxThicknessRatio_", Description = "Elements whose thickness exceeds this fraction of their smallest in-plane extent are not panel-like and are skipped (e.g. stair treads)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.2);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                Param_Boolean param_Boolean = new() { Name = "airForOpenings_", NickName = "airForOpenings_", Description = "Fill openings no aperture claims (e.g. stair voids) with Air panels so the spaces on either side stay separate", Access = GH_ParamAccess.item, Optional = true };
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
                    new GH_SAMParam(new GooPanelParam() { Name = "panels", NickName = "panels", Description = "SAM Analytical Panels: mid-surface panels and Air panels", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new GooPanelParam() { Name = "airPanels", NickName = "airPanels", Description = "The Air panels among panels (unclaimed openings)", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_Integer() { Name = "excluded", NickName = "excluded", Description = "Indexes of solids that produced no panel", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_String() { Name = "log", NickName = "log", Description = "Per-solid conversion report", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                ];
                return [.. result];
            }
        }

        protected override void SolveInstance(IGH_DataAccess dataAccess)
        {
            // SAM analytical geometry is in metres and Rhino geometry is converted without scaling: refuse rather
            // than produce a model a thousand times too large.
            string unitsWarning = Geometry.Rhino.Query.ModelUnitsWarning();
            if (unitsWarning != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, unitsWarning);
                return;
            }

            int index = Params.IndexOfInputParam("_solids");
            List<GH_Brep> solids = [];
            if (index == -1 || !dataAccess.GetDataList(index, solids) || solids.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid solids");
                return;
            }

            List<ISAMGeometry3D> apertureGeometries = [];
            index = Params.IndexOfInputParam("apertureGeometries_");
            List<IGH_GeometricGoo> apertureGoos = [];
            if (index != -1 && dataAccess.GetDataList(index, apertureGoos))
            {
                foreach (IGH_GeometricGoo goo in apertureGoos)
                {
                    List<ISAMGeometry3D> geometry3Ds = goo?.ToSAM(true);
                    if (geometry3Ds != null)
                    {
                        apertureGeometries.AddRange(geometry3Ds.Where(x => x != null));
                    }
                }
            }

            double minThickness = 0.08;
            index = Params.IndexOfInputParam("minThickness_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref minThickness);
            }

            double snap = 0.3;
            index = Params.IndexOfInputParam("snap_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref snap);
            }

            double maxThicknessRatio = 0.2;
            index = Params.IndexOfInputParam("maxThicknessRatio_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref maxThicknessRatio);
            }

            bool airForOpenings = true;
            index = Params.IndexOfInputParam("airForOpenings_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref airForOpenings);
            }

            global::Rhino.RhinoDoc rhinoDoc = global::Rhino.RhinoDoc.ActiveDoc;
            List<Panel> panels = [];
            List<int> excluded = [];
            List<string> log = [];
            for (int i = 0; i < solids.Count; i++)
            {
                GH_Brep solid = solids[i];
                Shell shell = solid?.ToSAM(true)?.OfType<Shell>().FirstOrDefault();

                string sourceName = null, sourceId = null, sourceLayer = null;
                if (solid != null && solid.IsReferencedGeometry)
                {
                    sourceId = solid.ReferenceID.ToString();
                    global::Rhino.DocObjects.RhinoObject rhinoObject = rhinoDoc?.Objects.FindId(solid.ReferenceID);
                    if (rhinoObject != null)
                    {
                        sourceName = rhinoObject.Attributes.Name;
                        sourceLayer = rhinoDoc.Layers.FindIndex(rhinoObject.Attributes.LayerIndex)?.FullPath;
                    }
                }

                string name = string.IsNullOrWhiteSpace(sourceName) ? string.Format("#{0}", i) : string.Format("#{0} {1}", i, sourceName);
                if (shell == null)
                {
                    excluded.Add(i);
                    log.Add(string.Format("{0}: skipped (not a closed solid).", name));
                    continue;
                }

                List<Panel> panels_Solid = shell.PanelsBySolid(out List<string> diagnostics, apertureGeometries, minThickness, maxThicknessRatio, airForOpenings, string.IsNullOrWhiteSpace(sourceName) ? name : sourceName, sourceId, sourceLayer);
                log.AddRange(diagnostics.Select(x => string.Format("#{0} {1}", i, x)));
                if (panels_Solid == null || panels_Solid.Count == 0)
                {
                    excluded.Add(i);
                    continue;
                }

                panels.AddRange(panels_Solid);
            }

            if (snap > 0 && panels.Count != 0)
            {
                panels = panels.SnapToJunctions(out List<string> snapDiagnostics, snap);
                log.AddRange(snapDiagnostics);
            }

            List<Panel> airPanels = panels.FindAll(x => x.PanelType == PanelType.Air);
            log.Insert(0, string.Format("{0} panel(s) ({1} Air) from {2} solid(s); {3} solid(s) produced no panel.", panels.Count, airPanels.Count, solids.Count, excluded.Count));

            index = Params.IndexOfOutputParam("panels");
            if (index != -1)
            {
                dataAccess.SetDataList(index, panels.ConvertAll(x => new GooPanel(x)));
            }

            index = Params.IndexOfOutputParam("airPanels");
            if (index != -1)
            {
                dataAccess.SetDataList(index, airPanels.ConvertAll(x => new GooPanel(x)));
            }

            index = Params.IndexOfOutputParam("excluded");
            if (index != -1)
            {
                dataAccess.SetDataList(index, excluded);
            }

            index = Params.IndexOfOutputParam("log");
            if (index != -1)
            {
                dataAccess.SetDataList(index, log);
            }
        }
    }
}
