// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core.Grasshopper;
using SAM.Weather.Grasshopper.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Weather.Grasshopper
{
    public class SAMWeatherPrevailingMeanOutdoorAirTemperatures : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("37a74368-5add-483a-bc06-8465cbb62de3");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMWeatherPrevailingMeanOutdoorAirTemperatures()
          : base("SAMWeather.PrevailingMeanOutdoorAirTemperatures", "SAMWeather.PrevailingMeanOutdoorAirTemperatures",
              "Calculate the prevailing mean outdoor air temperature for each day of the weather year, as required by CIBSE TM52 for adaptive thermal comfort assessments.",
              "SAM", "Weather")
        {
        }

        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();

                GooWeatherObjectParam weatherObjectParam = new GooWeatherObjectParam() { Name = "weatherObject", NickName = "weatherObject", Description = "SAM WeatherData or WeatherYear containing the outdoor dry bulb temperature time series.", Access = GH_ParamAccess.item, Optional = false };
                result.Add(new GH_SAMParam(weatherObjectParam, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer @integer;

                @integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_sequentialDays_", NickName = "_sequentialDays_", Description = "Number of preceding days used in the running mean calculation. Default: 7, as specified by CIBSE TM52.", Access = GH_ParamAccess.item, Optional = true };
                @integer.SetPersistentData(7);
                result.Add(new GH_SAMParam(@integer, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Number @number;

                @number = new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "alpha_", NickName = "alpha_", Description = "Weighting factor for the exponentially-weighted running mean. When omitted, a simple arithmetic mean is used. When supplied, the weighting method is used with the specified alpha value.", Access = GH_ParamAccess.item, Optional = true };
                result.Add(new GH_SAMParam(@number, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Boolean boolean;

                boolean = new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "_run", NickName = "_run", Description = "Set to True to execute the calculation. Connect a boolean toggle.", Access = GH_ParamAccess.item, Optional = true };
                boolean.SetPersistentData(false);
                result.Add(new GH_SAMParam(boolean, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "tempartures", NickName = "tempartures", Description = "Hourly prevailing mean outdoor air temperatures [°C]. Each daily value is repeated 24 times to produce 8760 hourly values.", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
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

            bool run = false;
            index = Params.IndexOfInputParam("_run");
            if (index == -1 || !dataAccess.GetData(index, ref run))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                dataAccess.SetDataList(0, null);
                return;
            }

            if (!run)
                return;

            IWeatherObject weatherObject = null;

            index = Params.IndexOfInputParam("weatherObject");
            if (index == -1 || !dataAccess.GetData(index, ref weatherObject) || weatherObject == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            if (!(weatherObject is WeatherData) && !(weatherObject is WeatherYear))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            int sequentialDays = int.MinValue;
            index = Params.IndexOfInputParam("_sequentialDays_");
            if (index != -1)
            {
                dataAccess.GetData(index, ref sequentialDays);
            }

            if (sequentialDays == int.MinValue || sequentialDays == int.MaxValue)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            double alpha = double.NaN;
            index = Params.IndexOfInputParam("alpha_");
            if (index == -1 || !dataAccess.GetData(index, ref alpha))
            {
                alpha = double.NaN;
            }

            IPrevailingMeanOutdoorAirTemperatureCalculationMethod prevailingMeanOutdoorAirTemperatureCalculationMethod = double.IsNaN(alpha) ? new SimpleArithmeticMeanCalculationMethod(sequentialDays) : new WeightingCalculationMethod(sequentialDays, alpha);

            PrevailingMeanOutdoorAirTemperatureCalculator prevailingMeanOutdoorAirTemperatureCalculator = new PrevailingMeanOutdoorAirTemperatureCalculator(prevailingMeanOutdoorAirTemperatureCalculationMethod);
            List<double> dryBulbTemperatures = prevailingMeanOutdoorAirTemperatureCalculator.Calculate(weatherObject as dynamic);
            if (dryBulbTemperatures != null)
            {
                List<double> dryBulbTemperatures_Temp = new List<double>();
                foreach (double dryBulbTemperature in dryBulbTemperatures)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        dryBulbTemperatures_Temp.Add(dryBulbTemperature);
                    }
                }

                dryBulbTemperatures = dryBulbTemperatures_Temp;
            }


            index = Params.IndexOfOutputParam("tempartures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, dryBulbTemperatures);
            }

        }
    }
}
