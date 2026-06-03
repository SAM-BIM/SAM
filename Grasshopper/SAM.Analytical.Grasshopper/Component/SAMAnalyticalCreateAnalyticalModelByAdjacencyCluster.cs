// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Weather;
using SAM.Weather.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalCreateAnalyticalModelByAdjacencyCluster : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new ("2a22123a-84c5-48dc-b207-c63b430cbca0");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.9";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalCreateAnalyticalModelByAdjacencyCluster()
          : base("SAMAnalytical.CreateAnalyticalModelByAdjacencyCluster", "SAMAnalytical.CreateAnalyticalModelByAdjacencyCluster",
              "Create Analytical Model",
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
                List<GH_SAMParam> result = [];

                global::Grasshopper.Kernel.Parameters.Param_String param_String;

                param_String = new() { Name = "_name_", NickName = "_name_", Description = "Analytical Model Name", Access = GH_ParamAccess.item, Optional = false };
                param_String.SetPersistentData("000000_SAM_AnalyticalModel");
                result.Add(new GH_SAMParam(param_String, ParamVisibility.Binding));

                param_String = new() { Name = "_description_", NickName = "_description_", Description = "SAM Description", Access = GH_ParamAccess.item, Optional = true };
                param_String.SetPersistentData(string.Format("Delivered by SAM https://github.com/HoareLea/SAM [{0}]", DateTime.Now.ToString("yyyy/MM/dd")));
                result.Add(new GH_SAMParam(param_String, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooWeatherDataParam() { Name = "weatherData_", NickName = "weatherData_", Description = "SAM WeatherData", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "coolingDesignDays_", NickName = "coolingDesignDays_", Description = "The SAM Analytical Design Days for Cooling", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new GooAnalyticalObjectParam() { Name = "heatingDesignDays_", NickName = "heatingDesignDays_", Description = "The SAM Analytical Design Days for Heating", Access = GH_ParamAccess.list, Optional = true }, ParamVisibility.Voluntary));

                global::Grasshopper.Kernel.Parameters.Param_Boolean param_Boolean;

                param_Boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_saveWeatherData_", NickName = "_saveWeatherData_", Description = "Save WeatherData, Design Days for Cooling and Design Days for Heating", Access = GH_ParamAccess.item, Optional = true };
                param_Boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(param_Boolean, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooAdjacencyClusterParam() { Name = "_adjacencyCluster", NickName = "_adjacencyCluster", Description = "SAM Adjacency Cluster", Access = GH_ParamAccess.item, Optional = false }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooPanelParam() { Name = "panels_", NickName = "panels_", Description = "SAM Analytical Panels \n*Connect your Shade (PanelType) panels", Access = GH_ParamAccess.list, Optional = true, DataMapping = GH_DataMapping.Flatten }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooMaterialLibraryParam() { Name = "materialLibrary_", NickName = "materialLibrary_", Description = "SAM Material Library", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new GooProfileLibraryParam() { Name = "profileLibrary_", NickName = "profileLibrary_", Description = "SAM Profile Library", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Binding));

                return [.. result];
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = [];
                result.Add(new GH_SAMParam(new GooAnalyticalModelParam() { Name = "AnalyticalModel", NickName = "AnalyticalModel", Description = "SAM Analytical Model", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                return [.. result];
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

            string name = null;
            index = Params.IndexOfInputParam("_name_");
            if (!dataAccess.GetData(index, ref name) || name == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string description = null;
            index = Params.IndexOfInputParam("_description_");
            if (!dataAccess.GetData(index, ref description) || description == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            WeatherData weatherData = null;
            index = Params.IndexOfInputParam("weatherData_");
            if (!dataAccess.GetData(index, ref weatherData))
            {
                weatherData = null;
            }

            List<DesignDay> heatingDesignDays = [];
            index = Params.IndexOfInputParam("heatingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, heatingDesignDays) || heatingDesignDays == null || heatingDesignDays.Count == 0)
            {
                heatingDesignDays = null;
            }

            List<DesignDay> coolingDesignDays = [];
            index = Params.IndexOfInputParam("coolingDesignDays_");
            if (index == -1 || !dataAccess.GetDataList(index, coolingDesignDays) || coolingDesignDays == null || coolingDesignDays.Count == 0)
            {
                coolingDesignDays = null;
            }

            Location location = weatherData?.Location;
            if (location == null)
            {
                location = Core.Query.DefaultLocation();
            }

            bool saveWeatherData = false;
            index = Params.IndexOfInputParam("_saveWeatherData_");
            if (!dataAccess.GetData(index, ref saveWeatherData))
            {
                saveWeatherData = false;
            }

            AdjacencyCluster adjacencyCluster = null;
            index = Params.IndexOfInputParam("_adjacencyCluster");
            dataAccess.GetData(index, ref adjacencyCluster);
            adjacencyCluster = adjacencyCluster == null ? new AdjacencyCluster() : new AdjacencyCluster(adjacencyCluster);

            List<Panel> panels = [];
            index = Params.IndexOfInputParam("panels_");
            dataAccess.GetDataList(index, panels);
            if (panels != null && panels.Count > 0)
            {
                foreach (Panel panel in panels)
                {
                    adjacencyCluster.AddObject(panel);
                }
            }

            MaterialLibrary materialLibrary = null;
            index = Params.IndexOfInputParam("materialLibrary_");
            dataAccess.GetData(index, ref materialLibrary);

            if (materialLibrary == null)
            {
                materialLibrary = ActiveSetting.Setting.GetValue<MaterialLibrary>(AnalyticalSettingParameter.DefaultMaterialLibrary);
            }

            IEnumerable<IMaterial> materials = Analytical.Query.Materials(adjacencyCluster, materialLibrary);
            materialLibrary = Core.Create.MaterialLibrary("Default Material Library", materials);

            ProfileLibrary profileLibrary = null;
            index = Params.IndexOfInputParam("profileLibrary_");
            dataAccess.GetData(index, ref profileLibrary);

            if (profileLibrary == null)
            {
                profileLibrary = ActiveSetting.Setting.GetValue<ProfileLibrary>(AnalyticalSettingParameter.DefaultProfileLibrary);
            }

            IEnumerable<Profile> profiles = Analytical.Query.Profiles(adjacencyCluster, profileLibrary);
            profileLibrary = new ProfileLibrary("Default Profile Library", profiles);

            AnalyticalModel analyticalModel = new (name, description, location, null, adjacencyCluster, materialLibrary, profileLibrary);

            if (saveWeatherData)
            {
                Analytical.Modify.UpdateWeather(analyticalModel, weatherData, coolingDesignDays, heatingDesignDays);
            }

            index = Params.IndexOfOutputParam("AnalyticalModel");
            if(index != -1)
            {
                dataAccess.SetData(index, new GooAnalyticalModel(analyticalModel));
            }
        }
    }
}
