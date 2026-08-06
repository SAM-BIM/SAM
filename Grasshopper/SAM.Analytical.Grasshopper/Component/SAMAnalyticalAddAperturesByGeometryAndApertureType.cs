// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalAddAperturesByGeometryAndApertureType : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("14802f03-04a2-4179-8305-6b43f5fc19d4");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.5";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalAddAperturesByGeometryAndApertureType()
          : base("SAMAnalytical.AddAperturesByGeometryAndApertureType", "SAMAnalytical.AddAperturesByGeometryAndApertureType",
              "Create and assign Apertures from geometry (Face3D or surface) using a specified ApertureType to a Panel, AdjacencyCluster or AnalyticalModel. The component resolves the default construction for the chosen type. Supports frame width or frame percentage offsets.",
              "SAM", "Analytical")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_GenericObject() { Name = "_geometries_", NickName = "_geometries_", Description = "Planar geometry (Face3D, surfaces, or Rhino planar Breps/curves) defining aperture outlines. For closed polylines, a default frame thickness of 0.05 m applies to the perimeter.", Access = GH_ParamAccess.list, Optional = true, DataMapping = GH_DataMapping.Flatten }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "_analyticalObject", NickName = "_analyticalObject", Description = "SAM Analytical Object such as AdjacencyCluster, Panel or AnalyticalModel", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "_apertureType_", NickName = "_apertureType_", Description = "Aperture type name (e.g. Window, Door, Rooflight). Defaults to Window if omitted.", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number param_Number;

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "maxDistance_", NickName = "maxDistance_", Description = "Maximum snapping tolerance for matching geometry to host panel faces [m]", Access = GH_ParamAccess.item };
                param_Number.SetPersistentData(0.1);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean;

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "trimGeometry_", NickName = "trimGeometry_", Description = "Trim and/or split aperture geometry that extends beyond host panel boundaries", Access = GH_ParamAccess.item };
                param_Boolean.SetPersistentData(true);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                param_Number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "minArea_", NickName = "minArea_", Description = "Minimum acceptable area for a created aperture; smaller apertures are discarded [m\u00B2]", Access = GH_ParamAccess.item };
                param_Number.SetPersistentData(Tolerance.MacroDistance);
                result.Add(new GH_SAMParam(param_Number, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "frameWidth_", NickName = "frameWidth_", Description = "Frame width offset subtracted from the aperture geometry edge on each side [m]. Minimum is the sum of frame layer thicknesses (default 0.05 m). Use either this or framePercentage_, not both.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "framePercentage_", NickName = "framePercentage_", Description = "Frame inset as a percentage of the aperture pane dimension [0\u2013100%]. Use either this or frameWidth_, not both.", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Binding));

                return result.ToArray();
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "AnalyticalObject", NickName = "AnalyticalObject", Description = "SAM Analytical Object with apertures applied", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new GooApertureParam() { Name = "Apertures", NickName = "Apertures", Description = "Apertures created from the supplied geometry", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
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
            int index = -1;

            index = Params.IndexOfInputParam("_geometries_");
            List<GH_ObjectWrapper> objectWrappers = new List<GH_ObjectWrapper>();
            if (index != -1)
            {
                dataAccess.GetDataList(index, objectWrappers);
            }

            List<Face3D> face3Ds = new List<Face3D>();

            foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
            {
                if (Geometry.Grasshopper.Query.TryGetSAMGeometries(objectWrapper, out List<Face3D> face3Ds_Temp) && face3Ds_Temp != null)
                {
                    face3Ds.AddRange(face3Ds_Temp);
                }
            }

            bool trimGeometry = true;
            index = Params.IndexOfInputParam("trimGeometry_");
            if (index == -1 || !dataAccess.GetData(index, ref trimGeometry))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            ApertureType apertureType = ApertureType.Window;

            string apertureTypeString = null;
            index = Params.IndexOfInputParam("_apertureType_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref apertureTypeString);
            }

            if (!string.IsNullOrWhiteSpace(apertureTypeString))
            {
                if (!Enum.TryParse(apertureTypeString, out apertureType))
                    apertureType = ApertureType.Window;
            }

            double maxDistance = Tolerance.MacroDistance;
            index = Params.IndexOfInputParam("maxDistance_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref maxDistance);
            }

            double minArea = Tolerance.MacroDistance;
            index = Params.IndexOfInputParam("minArea_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref minArea);
            }

            SAMObject sAMObject = null;
            index = Params.IndexOfInputParam("_analyticalObject");
            if (index == -1 || !dataAccess.GetData(index, ref sAMObject))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<double> frameWidths = new List<double>();
            index = Params.IndexOfInputParam("frameWidth_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, frameWidths);
            }

            List<double> framePercentages = new List<double>();
            index = Params.IndexOfInputParam("framePercentage_");
            if (index != -1)
            {
                dataAccess.GetDataList(index, framePercentages);
            }

            if (framePercentages != null && framePercentages.Count > 0 && frameWidths != null && frameWidths.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Both framePercentage and frameWidth are connected. Please use only one. If both provided, frameWidth will take precedence");
            }

            if ((frameWidths != null && frameWidths.Count != 0) || (framePercentages != null && framePercentages.Count != 0))
            {
                bool framePercentage = frameWidths == null || frameWidths.Count == 0;

                face3Ds = Geometry.Spatial.Query.Offset(face3Ds, framePercentage ? framePercentages : frameWidths, framePercentage);
            }

            if (sAMObject is Panel)
            {
                Panel panel = Create.Panel((Panel)sAMObject);

                List<Aperture> apertures = new List<Aperture>();

                if (face3Ds != null)
                {
                    foreach (Face3D face3D in face3Ds)
                    {
                        ApertureConstruction apertureConstruction_Temp = Analytical.Query.DefaultApertureConstruction(panel, apertureType);

                        List<Aperture> apertures_Temp = panel.AddApertures(apertureConstruction_Temp, face3D, trimGeometry, minArea, maxDistance);
                        if (apertures_Temp != null)
                            apertures.AddRange(apertures_Temp);
                    }
                }

                index = Params.IndexOfOutputParam("AnalyticalObject");
                if (index != -1)
                {
                    dataAccess.SetData(index, panel);
                }

                index = Params.IndexOfOutputParam("Apertures");
                if (index != -1)
                {
                    dataAccess.SetDataList(index, apertures.ConvertAll(x => new GooAperture(x)));
                }

                return;
            }

            AdjacencyCluster adjacencyCluster = null;
            AnalyticalModel analyticalModel = null;
            if (sAMObject is AdjacencyCluster)
            {
                adjacencyCluster = new AdjacencyCluster((AdjacencyCluster)sAMObject);
            }
            else if (sAMObject is AnalyticalModel)
            {
                analyticalModel = ((AnalyticalModel)sAMObject);
                adjacencyCluster = analyticalModel.AdjacencyCluster;
            }

            if (adjacencyCluster == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Tuple<Panel, Aperture>> tuples_Result = null;

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels != null)
            {
                List<Tuple<BoundingBox3D, IClosedPlanar3D>> tuples = new List<Tuple<BoundingBox3D, IClosedPlanar3D>>();
                foreach (IClosedPlanar3D closedPlanar3D in face3Ds)
                {
                    if (closedPlanar3D == null)
                        continue;

                    tuples.Add(new Tuple<BoundingBox3D, IClosedPlanar3D>(closedPlanar3D.GetBoundingBox(maxDistance), closedPlanar3D));
                }

                tuples_Result = new List<Tuple<Panel, Aperture>>();
                for (int i = 0; i < panels.Count; i++)
                {
                    Panel panel = panels[i];
                    BoundingBox3D boundingBox3D = panel.GetBoundingBox(maxDistance);

                    Panel panel_Temp = null;

                    foreach (Tuple<BoundingBox3D, IClosedPlanar3D> tuple in tuples)
                    {
                        if (!boundingBox3D.InRange(tuple.Item1))
                            continue;

                        if (!panel.ApertureHost(tuple.Item2, minArea, maxDistance))
                            continue;

                        ApertureConstruction apertureConstruction_Temp = Analytical.Query.DefaultApertureConstruction(panel, apertureType);

                        if (apertureConstruction_Temp == null)
                            continue;

                        if (panel_Temp == null)
                            panel_Temp = Create.Panel(panel);

                        List<Aperture> apertures_Temp = panel_Temp.AddApertures(apertureConstruction_Temp, tuple.Item2, trimGeometry, minArea, maxDistance);
                        if (apertures_Temp == null)
                            continue;

                        apertures_Temp.ForEach(x => tuples_Result.Add(new Tuple<Panel, Aperture>(panel_Temp, x)));
                    }

                    if (panel_Temp != null)
                        adjacencyCluster.AddObject(panel_Temp);
                }
            }

            index = Params.IndexOfOutputParam("AnalyticalObject");
            if (index != -1)
            {
                if (analyticalModel != null)
                {
                    AnalyticalModel analyticalModel_Result = new AnalyticalModel(analyticalModel, adjacencyCluster);
                    dataAccess.SetData(index, analyticalModel_Result);
                }
                else
                {
                    dataAccess.SetData(index, adjacencyCluster);
                }
            }

            index = Params.IndexOfOutputParam("Apertures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, tuples_Result?.ConvertAll(x => new GooAperture(x.Item2)));
            }
        }
    }
}
