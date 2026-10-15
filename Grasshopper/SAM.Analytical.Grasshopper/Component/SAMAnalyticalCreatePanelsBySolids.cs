// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
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
        public override string LatestComponentVersion => "1.0.3";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_GH_PanelCreate);

        public SAMAnalyticalCreatePanelsBySolids()
          : base("SAMAnalytical.CreatePanelsBySolids", "SAMAnalytical.CreatePanelsBySolids",
              "Create SAM Analytical Panels from closed Rhino solids of planar building elements: walls, floors, slabs, ceilings, roofs and gables.\n" +
              "Each pair of opposite parallel faces with material between them is one element side pair: its distance is the element's thickness and its mid-plane carries the Panel. A solid may hold several elements (a ring of walls, a roof with two pitches); each element plane gives its own Panels. Panel type follows the mid-plane: vertical = Wall, horizontal = Floor, sloped = Roof. The measured thickness is stored as PhysicalThickness, independent of the construction.\n" +
              "Openings occupied by apertureGeometries_ (windows, doors) are filled so Apertures can be added later; other holes (e.g. stair voids) become Air Panels. Nothing is invented: missing walls, floors or ceilings stay missing.\n" +
              "A Solid is skipped, with a Warning, when it is thinner than minThickness_ (a thickness in metres) or not panel-like by maxThicknessRatio_ (e.g. a stair). log gives the reason and the value that would keep it. Warnings are always given in full; diagnosticDetail_ sets how much Info log adds (0 Summary, 1 Standard: one line per Solid, 2 Detailed).\n" +
              "Outer edges are snapped onto neighbouring mid-planes (snap_). Rhino units must be metres. Feed the Panels to SAMOCCT.CreateAdjacencyCluster.",
              "SAM", "Analytical01")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = [];

                Param_Brep solids = new() { Name = "_solids", NickName = "_solids", Description = "Closed solids (Breps or extrusions) of planar building elements: walls, floors, slabs, ceilings, roofs, gables. One element or several per Solid (e.g. a ring of walls, a two-pitch roof). Solids are numbered #0, #1, ... in input order in log. Referenced Rhino objects keep their name, id and layer on the Panels.", Access = GH_ParamAccess.list };
                solids.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(solids, ParamVisibility.Binding));

                Param_Geometry apertures = new() { Name = "apertureGeometries_", NickName = "apertureGeometries_", Description = "Window and door geometry (frames, glass, leaves), the same solids given to SAMAnalytical.CreateAperturesBySolids. Openings they occupy are filled in the host Panel so the Apertures can be added later; without it every opening becomes a hole (and an Air Panel).", Access = GH_ParamAccess.list, Optional = true };
                apertures.DataMapping = GH_DataMapping.Flatten;
                result.Add(new GH_SAMParam(apertures, ParamVisibility.Binding));

                Param_Number param_Number = new() { Name = "minThickness_", NickName = "minThickness_", Description = "Minimum element thickness [m], default 0.08 m. Elements thinner than this are skipped (e.g. balustrades, glass). This is a thickness, not a ratio: set it below your thinnest wall, floor or roof (e.g. 0.05 m for 0.1 m partitions). To keep thick or low elements, change maxThicknessRatio_ instead.", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.08);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new() { Name = "snap_", NickName = "snap_", Description = "Maximum distance [m] an outer Panel edge moves to meet a neighbouring Panel's mid-plane at a real junction, default 0.3 m (0 disables)", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.3);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                param_Number = new() { Name = "maxThicknessRatio_", NickName = "maxThicknessRatio_", Description = "Maximum thickness over smallest in-plane extent [-], default 0.2. Element planes above it are not panel-like and are skipped (e.g. stairs, treads). Raise it (e.g. to 0.25) to keep thick or low elements such as a 0.3 m gable 1.4 m high; log gives the ratio that would keep each skipped Solid. Hidden by default: zoom in on the component and click + to show it.", Access = GH_ParamAccess.item, Optional = true };
                param_Number.SetPersistentData(0.2);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Voluntary));

                Param_Boolean param_Boolean = new() { Name = "airForOpenings_", NickName = "airForOpenings_", Description = "Fill holes no aperture claims (e.g. stair voids) with Air Panels so the spaces on either side stay separate, default true", Access = GH_ParamAccess.item, Optional = true };
                param_Boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Voluntary));

                Param_Integer param_Integer = new() { Name = "diagnosticDetail_", NickName = "diagnosticDetail_", Description = "How much Info log gives: 0 = Summary (default), 1 = Standard, 2 = Detailed. Summary is the totals, Standard adds one line per Solid (and element plane), Detailed adds the other face-pair planes that were ignored. Warnings and Errors are always given in full, and the Panels do not depend on this setting. Any other value is reported and Summary is used. Hidden by default: zoom in on the component and click + to show it.", Access = GH_ParamAccess.item, Optional = true };
                param_Integer.SetPersistentData(0);
                result.Add(new GH_SAMParam(param_Integer, ParamVisibility.Voluntary));

                return [.. result];
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result =
                [
                    new GH_SAMParam(new GooPanelParam() { Name = "panels", NickName = "panels", Description = "SAM Analytical Panels: mid-surface Panels and Air Panels", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
                    new GH_SAMParam(new GooPanelParam() { Name = "airPanels", NickName = "airPanels", Description = "The Air Panels among panels (holes no aperture claims)", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_Integer() { Name = "excluded", NickName = "excluded", Description = "Indexes (#) of the Solids that gave no Panel; see log for the reason", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary),
                    new GH_SAMParam(new Param_String() { Name = "log", NickName = "log", Description = "Diagnostics as 'Severity: Solid #i 'Name': Message'. Severity is Info, Warning or Error. A summary first, then every Warning, then the Info that diagnosticDetail_ asks for: Standard adds one line per Solid (and per element plane), Detailed also the ignored face-pair planes.", Access = GH_ParamAccess.list }, ParamVisibility.Binding),
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

            DiagnosticDetail diagnosticDetail = DiagnosticDetail.Summary;
            string invalidDetail = null;
            int index = Params.IndexOfInputParam("diagnosticDetail_");
            if (index != -1)
            {
                int value = 0;
                if (dataAccess.GetData(index, ref value) && !Core.Query.TryGetDiagnosticDetail(value, out diagnosticDetail))
                {
                    invalidDetail = string.Format("Input diagnosticDetail_ is {0}, which is not 0 (Summary), 1 (Standard) or 2 (Detailed). Summary is used.", value);
                }
            }

            index = Params.IndexOfInputParam("_solids");
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
            Core.Diagnostics diagnostics = new(diagnosticDetail);
            List<string> notPanelLike = [];
            List<string> tooThin = [];
            List<string> otherSkipped = [];
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

                // Deterministic identification: the input position, then the Rhino name when there is one.
                string name = string.IsNullOrWhiteSpace(sourceName) ? string.Format("#{0}", i) : string.Format("#{0} '{1}'", i, sourceName);
                if (shell == null)
                {
                    excluded.Add(i);
                    otherSkipped.Add(name);
                    diagnostics.Add(new Core.Diagnostic(Core.DiagnosticSeverity.Warning, DiagnosticDetail.Summary, "Solid " + name, "Skipped. It is not a closed solid.", BySolidsDiagnosticCode.PanelsNotClosed, i));
                    continue;
                }

                int recordCount = diagnostics.Count;
                List<Panel> panels_Solid = shell.PanelsBySolid(diagnostics, apertureGeometries, minThickness, maxThicknessRatio, airForOpenings, string.IsNullOrWhiteSpace(sourceName) ? string.Format("#{0}", i) : sourceName, sourceId, sourceLayer, sourceIndex: i);
                if (panels_Solid == null || panels_Solid.Count == 0)
                {
                    excluded.Add(i);

                    // By code, not by message text and not by Info: the reason is a Warning and is kept at every detail level.
                    List<Core.Diagnostic> records = diagnostics.Skip(recordCount).ToList();
                    if (records.Exists(x => x.Code == BySolidsDiagnosticCode.PanelsBelowMinThickness))
                    {
                        tooThin.Add(name);
                    }
                    else if (records.Exists(x => x.Code == BySolidsDiagnosticCode.PanelsNotPanelLike))
                    {
                        notPanelLike.Add(name);
                    }
                    else
                    {
                        otherSkipped.Add(name);
                    }

                    continue;
                }

                panels.AddRange(panels_Solid);
            }

            if (snap > 0 && panels.Count != 0)
            {
                panels = panels.SnapToJunctions(diagnostics, snap);
            }

            List<Panel> airPanels = panels.FindAll(x => x.PanelType == PanelType.Air);

            // The totals come first, then an invalid diagnosticDetail_, then everything the solids and the snapping recorded.
            Core.Diagnostics log = new(diagnosticDetail);
            log.Add(DiagnosticSeverity.Info, DiagnosticDetail.Summary, null, () => string.Format("Created {0} ({1}) from {2}. {3}",
                Core.Query.Plural(panels.Count, "panel"), Core.Query.Plural(airPanels.Count, "Air Panel"), Core.Query.Plural(solids.Count, "solid"),
                excluded.Count == 0 ? "No solids were skipped." : string.Format("Skipped {0}.", Core.Query.Plural(excluded.Count, "solid"))), BySolidsDiagnosticCode.PanelsSummary);
            if (invalidDetail != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, invalidDetail);
                log.Add(new Core.Diagnostic(DiagnosticSeverity.Warning, DiagnosticDetail.Summary, null, invalidDetail, BySolidsDiagnosticCode.InvalidDetail));
            }

            foreach (Core.Diagnostic diagnostic in diagnostics)
            {
                log.Add(diagnostic);
            }

            // One Warning per reason, naming the solids; the per-solid detail is in log. Never skip silently.
            if (tooThin.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format(System.Globalization.CultureInfo.InvariantCulture, "Skipped {0} thinner than minThickness_ ({1:0.###} m): {2}. minThickness_ is a thickness in metres, not a ratio. If any is a wall, floor or roof, lower minThickness_ below its thickness (given in log).", Core.Query.Plural(tooThin.Count, "solid"), minThickness, string.Join(", ", tooThin)));
            }

            // The ratio is a proportion test, not a thickness limit: a thick or low wall can fail it.
            if (notPanelLike.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format(System.Globalization.CultureInfo.InvariantCulture, "Skipped {0} as not panel-like by maxThicknessRatio_ ({1:0.###}): {2}. If any is a wall, floor or roof (e.g. a thick or low wall), raise maxThicknessRatio_ to the ratio given in log.", Core.Query.Plural(notPanelLike.Count, "solid"), maxThicknessRatio, string.Join(", ", notPanelLike)));
            }

            if (otherSkipped.Count != 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, string.Format("Skipped {0} (not closed, or no usable pair of opposite faces): {1}. See log.", Core.Query.Plural(otherSkipped.Count, "solid"), string.Join(", ", otherSkipped)));
            }

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
                dataAccess.SetDataList(index, log.Format());
            }
        }
    }
}
