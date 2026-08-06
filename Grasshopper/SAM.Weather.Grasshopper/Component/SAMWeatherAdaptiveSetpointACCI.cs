// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Weather;
using SAM.Weather.Grasshopper;
using SAM.Weather.Grasshopper.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMWeatherAdaptiveSetpointACCI : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("b495c149-2872-4b4d-881b-e1b7c3bc28db");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMWeatherAdaptiveSetpointACCI()
          : base("SAMWeather.AdaptiveSetpointACCI", "SAMWeather.AdaptiveSetpointACCI",
        "Calculate adaptive thermal comfort setpoints using the ASHRAE 55 adaptive comfort standard (ACCI method). Provides upper and lower dry bulb temperature limits from a weather object.",
        "SAM", "Weather")
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
                result.Add(new GH_SAMParam(new GooWeatherObjectParam() { Name = "_weatherObject", NickName = "_weatherObject", Description = "SAM WeatherData, WeatherYear, WeatherDay or WeatherHour containing dry bulb temperature data.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new GooWeatherObjectParam() { Name = "weatherObject", NickName = "weatherObject", Description = "The supplied weather object passed through unchanged.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "upper", NickName = "upper", Description = "Upper adaptive comfort temperature limit [°C], calculated as (T × 0.31) + 17.8 + 3.5 where T is the dry bulb temperature clamped to 10–33.5°C.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "lower", NickName = "lower", Description = "Lower adaptive comfort temperature limit [°C], calculated as (T × 0.31) + 17.8 − 3.5 where T is the dry bulb temperature clamped to 10–33.5°C.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
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

            index = Params.IndexOfInputParam("_weatherObject");
            IWeatherObject weatherObject = null;
            if (index == -1 || !dataAccess.GetData(index, ref weatherObject) || weatherObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            List<Range<double>> ranges = null;
            if (weatherObject is WeatherHour)
            {
                Range<double> range = ((WeatherHour)weatherObject).DryBulbTemperatureRange();
                if (range != null)
                {
                    ranges = new List<Range<double>>() { range };
                }
            }
            else if (weatherObject is WeatherDay)
            {
                ranges = ((WeatherDay)weatherObject).DryBulbTemperatureRanges();
            }
            else if (weatherObject is WeatherYear)
            {
                ranges = ((WeatherYear)weatherObject).DryBulbTemperatureRanges();
            }
            else if (weatherObject is WeatherData)
            {
                ranges = ((WeatherData)weatherObject).DryBulbTemperatureRanges();
            }


            index = Params.IndexOfOutputParam("weatherObject");
            if (index != -1)
            {
                dataAccess.SetData(index, weatherObject);
            }

            index = Params.IndexOfOutputParam("upper");
            if (index != -1)
            {
                dataAccess.SetDataList(index, ranges?.ConvertAll(x => x.Max));
            }

            index = Params.IndexOfOutputParam("lower");
            if (index != -1)
            {
                dataAccess.SetDataList(index, ranges?.ConvertAll(x => x.Min));
            }
        }
    }
}
