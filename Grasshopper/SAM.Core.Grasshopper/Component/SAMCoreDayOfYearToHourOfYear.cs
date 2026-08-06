// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using SAM.Core.Grasshopper.Properties;
using System;
using System.Collections.Generic;

namespace SAM.Core.Grasshopper
{
    public class SAMCoreDayOfYearToHourOfYear : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("9cc0c531-cfde-4927-87df-61c56a18f4fc");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.0";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.SAM_Get3;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        //private GH_OutputParamManager outputParamManager;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMCoreDayOfYearToHourOfYear()
          : base("SAMCore.DayOfYearToHourOfYear", "SAMCore.DayOfYearToHourOfYear",
              "Convert day-of-year and hour-of-day inputs into a DateTime and its hour-of-year index [0–8759]. Defaults to year 2018 for non-leap-year convention.",
              "SAM", "Core")
        {
        }



        protected override GH_SAMParam[] Inputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_dayOfYear", NickName = "_dayOfYear", Description = "Day index within the year (1 = 1st January) [1–365]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_hour", NickName = "_hour", Description = "Hour index within the day [0–23]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_year_", NickName = "_year_", Description = "Calendar year used to construct the DateTime [default 2018]", Access = GH_ParamAccess.item };
                integer.SetPersistentData(2018);
                result.Add(new GH_SAMParam(integer, ParamVisibility.Binding));
                return result.ToArray();
            }
        }

        protected override GH_SAMParam[] Outputs
        {
            get
            {
                List<GH_SAMParam> result = new List<GH_SAMParam>();
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Time() { Name = "dateTime", NickName = "dateTime", Description = "Resulting DateTime value", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hourOfYear", NickName = "hourOfYear", Description = "Zero-based hour index within the year [0–8759]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
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
            int index;

            index = Params.IndexOfInputParam("_dayOfYear");

            int dayOfYear = -1;
            if (index == -1 || !dataAccess.GetData(index, ref dayOfYear))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_hour");

            int hour = -1;
            if (index == -1 || !dataAccess.GetData(index, ref hour))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_year_");

            int year = -1;
            if (index == -1 || !dataAccess.GetData(index, ref year))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            DateTime dateTime = new DateTime(year, 1, 1).AddDays(dayOfYear - 1).AddHours(hour);

            index = Params.IndexOfOutputParam("dateTime");
            if (index != -1)
            {
                dataAccess.SetData(index, dateTime);
            }

            index = Params.IndexOfOutputParam("hourOfYear");
            if (index != -1)
            {
                dataAccess.SetData(index, dateTime.HourOfYear());
            }
        }
    }
}
