// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class SAMAnalyticalHourOfYearToDateTime : GH_SAMVariableOutputParameterComponent
    {
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("0906bada-8e37-49f9-aa8f-dc7b4d42ff06");

        /// <summary>
        /// The latest version of this component
        /// </summary>
        public override string LatestComponentVersion => "1.0.4";

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

        /// <summary>
        /// Initializes a new instance of the SAM_point3D class.
        /// </summary>
        public SAMAnalyticalHourOfYearToDateTime()
          : base("SAMAnalytical.HourOfYearToDateTime", "SAMAnalytical.HourOfYearToDateTime",
              "Allows an engineer to convert an hour-of-year index (0\u20138760) to a DateTime with year, month, day, hour and minute components.",
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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_hourOfYear", NickName = "_hourOfYear", Description = "Hour-of-year index [0\u20138760]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

                global::Grasshopper.Kernel.Parameters.Param_Integer param_Integer = new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "_year_", NickName = "_year_", Description = "Optional year [default: 2018]", Access = GH_ParamAccess.item, Optional = true };
                param_Integer.PersistentData.Append(new GH_Integer(2018));
                result.Add(new GH_SAMParam(param_Integer, ParamVisibility.Binding));

                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "timeShift_", NickName = "timeShift_", Description = "Optional time shift offset [min]", Access = GH_ParamAccess.item, Optional = true }, ParamVisibility.Voluntary));


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
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Time() { Name = "dateTime", NickName = "dateTime", Description = "Resolved DateTime from the hour-of-year index", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "year", NickName = "year", Description = "Year component of the resolved DateTime", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "month", NickName = "month", Description = "Month component [1\u201312]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "day", NickName = "day", Description = "Day component [1\u201331]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "hour", NickName = "hour", Description = "Hour component [0\u201323]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "minute", NickName = "minute", Description = "Minute component [0\u201359]", Access = GH_ParamAccess.item }, ParamVisibility.Binding));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_Integer() { Name = "dayOfYear", NickName = "dayOfYear", Description = "Day-of-year component [1\u2013365/366]", Access = GH_ParamAccess.item }, ParamVisibility.Voluntary));
                result.Add(new GH_SAMParam(new global::Grasshopper.Kernel.Parameters.Param_String() { Name = "text", NickName = "text", Description = "DateTime as formatted text string", Access = GH_ParamAccess.item }, ParamVisibility.Binding));

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

            int hourIndex = -1;
            index = Params.IndexOfInputParam("_hourOfYear");
            if (index == -1 || !dataAccess.GetData(index, ref hourIndex) || hourIndex < 0 || hourIndex > 8760)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid data");
                return;
            }

            index = Params.IndexOfInputParam("_year_");
            int year = 2018;
            if (index == -1 || !dataAccess.GetData(index, ref year))
            {
                year = 2018;
            }

            index = Params.IndexOfInputParam("timeShift_");
            int minute = 0;
            if (index != -1)
            {
                int minute_Temp = 0;
                if (dataAccess.GetData(index, ref minute_Temp))
                {
                    minute = minute_Temp;
                }
            }

            DateTime dateTime = Analytical.Convert.ToDateTime(hourIndex, year);
            if (minute != 0)
            {
                dateTime = dateTime.AddMinutes(minute);
            }

            index = Params.IndexOfOutputParam("dateTime");
            if (index != -1)
                dataAccess.SetData(index, dateTime);

            index = Params.IndexOfOutputParam("year");
            if (index != -1)
                dataAccess.SetData(index, dateTime.Year);

            index = Params.IndexOfOutputParam("month");
            if (index != -1)
                dataAccess.SetData(index, dateTime.Month);

            index = Params.IndexOfOutputParam("day");
            if (index != -1)
                dataAccess.SetData(index, dateTime.Day);

            index = Params.IndexOfOutputParam("hour");
            if (index != -1)
                dataAccess.SetData(index, dateTime.Hour);

            index = Params.IndexOfOutputParam("minute");
            if (index != -1)
                dataAccess.SetData(index, dateTime.Minute);

            index = Params.IndexOfOutputParam("dayOfYear");
            if (index != -1)
                dataAccess.SetData(index, dateTime.DayOfYear);

            index = Params.IndexOfOutputParam("text");
            if (index != -1)
                dataAccess.SetData(index, Analytical.Convert.ToString(dateTime));
        }
    }
}
