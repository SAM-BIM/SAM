// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalHourlyValues : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("e8e29842-bb9a-4f90-9d67-488fc4b321fa");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalHourlyValues()
          : base("SAMAnalytical.HourlyValues", "SAMAnalytical.HourlyValues",
              "Allows an engineer to extract hourly TM52/TM59 comfort band values for a given day from an extended thermal result, including acceptable temperature ranges and exceedance data.",
              "SAM", "Analytical02")
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
                result.Add(new GH_SAMParam(new GooResultParam() { Name = "_tMExtendedResult", NickName = "_tMExtendedResult", Description = "SAM TMExtendedResult such as TM52ExtendedResult or TM59ExtendedResult", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_dayOfYear", NickName = "_dayOfYear", Description = "Day-of-year index [1\u2013365/366] to extract hourly data for", Access = GH_ParamAccess.item };
                result.Add(new GH_SAMParam(integer, ParamVisibility.Binding));

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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "name", NickName = "name", Description = "Space name from the extended result", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "minAcceptableTemperatures", NickName = "minAcceptableTemperatures", Description = "Min acceptable comfort temperatures [\u00B0C]", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "maxAcceptableTemperatures", NickName = "maxAcceptableTemperatures", Description = "Max acceptable comfort temperatures [\u00B0C]", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "operativeTemperatures", NickName = "operativeTemperatures", Description = "Operative temperatures for each hour [\u00B0C]", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Number() { Name = "temperatureDifferences", NickName = "temperatureDifferences", Description = "Temperature differences from comfort band [\u00B0C]", Access = GH_ParamAccess.list }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Boolean() { Name = "occupancies", NickName = "occupancies", Description = "Boolean occupancy status for each hour", Access = GH_ParamAccess.list }, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "occupiedHourIndices", NickName = "occupiedHourIndices", Description = "List of occupied hour-of-year indices", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "occupiedHours", NickName = "occupiedHours", Description = "Total count of occupied hours in the year", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "maxExceedableHours", NickName = "maxExceedableHours", Description = "Maximum number of exceedable hours allowed by the standard", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "occupiedHourIndicesExceedingComfortRange", NickName = "occupiedHourIndicesExceedingComfortRange", Description = "List of occupied hour indices that exceed the comfort range", Access = GH_ParamAccess.list }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "occupiedHoursExceedingComfortRange", NickName = "occupiedHoursExceedingComfortRange", Description = "Total count of occupied hours exceeding the comfort range", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));

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

            index = Params.IndexOfInputParam("_tMExtendedResult");
            Result result = null;
            if (index == -1 || !dataAccess.GetData(index, ref result) || result == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            TMExtendedResult tMExtendedResult = result as TMExtendedResult;
            if (tMExtendedResult == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_dayOfYear");
            int dayOfYear = -1;
            if (index == -1 || !dataAccess.GetData(index, ref dayOfYear))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            string name = tMExtendedResult.Name;

            List<int> occupiedHourIndices = new List<int>(tMExtendedResult.OccupiedHourIndices);
            int occupiedHours = tMExtendedResult.OccupiedHours;
            int maxExceedableHours = tMExtendedResult.MaxExceedableHours;

            HashSet<int> occupiedHourIndicesExceedingComfortRange_Temp = tMExtendedResult.GetOccupiedHourIndicesExceedingComfortRange();

            List<int> occupiedHourIndicesExceedingComfortRange = occupiedHourIndicesExceedingComfortRange_Temp == null ? new List<int>() : new List<int>(occupiedHourIndicesExceedingComfortRange_Temp);
            int occupiedHoursExceedingComfortRange = tMExtendedResult.GetOccupiedHoursExceedingComfortRange();

            HashSet<int> hoursOfYear = Core.Query.HoursOfYear(dayOfYear);

            IndexedDoubles indexedDoubles_minAcceptableTemperatures = tMExtendedResult.MinAcceptableTemperatures;
            IndexedDoubles indexedDoubles_maxAcceptableTemperatures = tMExtendedResult.MaxAcceptableTemperatures;
            IndexedDoubles indexedDoubles_operativeTemperatures = tMExtendedResult.OperativeTemperatures;

            List<double> minAcceptableTemperatures = new List<double>();
            List<double> maxAcceptableTemperatures = new List<double>();
            List<double> operativeTemperatures = new List<double>();
            List<double> temperatureDifferences = new List<double>();
            List<bool> occupancies = new List<bool>();

            foreach (int hourOfYear in hoursOfYear)
            {
                double minAcceptableTemperature = indexedDoubles_minAcceptableTemperatures[hourOfYear];
                double maxAcceptableTemperature = indexedDoubles_maxAcceptableTemperatures[hourOfYear];
                double operativeTemperature = indexedDoubles_operativeTemperatures[hourOfYear];

                double temperatureDifference = tMExtendedResult.GetTemperatureDifference(hourOfYear);

                bool occupancy = occupiedHourIndices.Contains(hourOfYear);

                minAcceptableTemperatures.Add(minAcceptableTemperature);
                maxAcceptableTemperatures.Add(maxAcceptableTemperature);
                operativeTemperatures.Add(operativeTemperature);
                temperatureDifferences.Add(temperatureDifference);
                occupancies.Add(occupancy);
            }

            index = Params.IndexOfOutputParam("name");
            if (index != -1)
            {
                dataAccess.SetData(index, name);
            }

            index = Params.IndexOfOutputParam("minAcceptableTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, minAcceptableTemperatures);
            }

            index = Params.IndexOfOutputParam("maxAcceptableTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, maxAcceptableTemperatures);
            }

            index = Params.IndexOfOutputParam("operativeTemperatures");
            if (index != -1)
            {
                dataAccess.SetDataList(index, operativeTemperatures);
            }

            index = Params.IndexOfOutputParam("temperatureDifferences");
            if (index != -1)
            {
                dataAccess.SetDataList(index, temperatureDifferences);
            }

            index = Params.IndexOfOutputParam("occupancies");
            if (index != -1)
            {
                dataAccess.SetDataList(index, occupancies);
            }

            index = Params.IndexOfOutputParam("occupiedHourIndices");
            if (index != -1)
            {
                dataAccess.SetDataList(index, occupiedHourIndices);
            }

            index = Params.IndexOfOutputParam("occupiedHours");
            if (index != -1)
            {
                dataAccess.SetData(index, occupiedHours);
            }

            index = Params.IndexOfOutputParam("maxExceedableHours");
            if (index != -1)
            {
                dataAccess.SetData(index, maxExceedableHours);
            }

            index = Params.IndexOfOutputParam("occupiedHourIndicesExceedingComfortRange");
            if (index != -1)
            {
                dataAccess.SetDataList(index, occupiedHourIndicesExceedingComfortRange);
            }

            index = Params.IndexOfOutputParam("occupiedHoursExceedingComfortRange");
            if (index != -1)
            {
                dataAccess.SetData(index, occupiedHoursExceedingComfortRange);
            }
        }
    }
}
