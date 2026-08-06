// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core;
using SAM.Core.Grasshopper;
using SAM.Weather.Grasshopper.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMWeatherAdaptiveSetpointACCIByTemperature : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e75af232-c217-4748-9243-1eb153e93e31");

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
        public SAMWeatherAdaptiveSetpointACCIByTemperature()
          : base("SAMWeather.AdaptiveSetpointACCIByTemperature", "SAMWeather.AdaptiveSetpointACCIByTemperature",
        "Calculate adaptive thermal comfort setpoints from a single dry bulb temperature value using the ASHRAE 55 adaptive comfort standard (ACCI method). Returns upper and lower limits for a given outdoor temperature.",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "_temperature", NickName = "_temperature", Description = "Outdoor dry bulb temperature [°C] used to calculate the adaptive comfort limits. Clamped internally between 10 °C and 33.5 °C.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "temperature", NickName = "temperature", Description = "The supplied dry bulb temperature [°C] passed through unchanged.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "upper", NickName = "upper", Description = "Upper adaptive comfort temperature limit [°C]. Calculated as (T_out * 0.31) + 17.8 + 3.5, clamped.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number { Name = "lower", NickName = "lower", Description = "Lower adaptive comfort temperature limit [°C]. Calculated as (T_out * 0.31) + 17.8 - 3.5, clamped.", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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

            index = Params.IndexOfInputParam("_temperature");
            double temperature = double.NaN;
            if (index == -1 || !dataAccess.GetData(index, ref temperature) || double.IsNaN(temperature))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            Range<double> range = Weather.Query.DryBulbTemperatureRange(temperature);


            index = Params.IndexOfOutputParam("temperature");
            if (index != -1)
            {
                dataAccess.SetData(index, temperature);
            }

            index = Params.IndexOfOutputParam("upper");
            if (index != -1)
            {
                dataAccess.SetData(index, range.Max);
            }

            index = Params.IndexOfOutputParam("lower");
            if (index != -1)
            {
                dataAccess.SetData(index, range.Min);
            }
        }
    }
}
