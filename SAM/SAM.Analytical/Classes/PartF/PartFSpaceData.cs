// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    public class PartFSpaceData : SAMObject
    {
        public PartFSpaceData(
            string name,
            PartFType partFType,
            PartFVentilationType partFVentilationType,
            bool isBedroom,
            double? minFlowRate_Lps,
            bool includeInFloorAreaCheck,
            bool isTerminalSpace,
            bool scaleSupplyWithVolume,
            bool scaleExtractAboveMinimum,
            string defaultFlowWeightBasis,
            double calculatedFlowRate_Lps,
            bool isCookingSpace = false,
            SpaceUse spaceUse = SpaceUse.Undefined,
            double? setbackFlowRate_Lps = null)
            : base(name)
        {
            PartFType = partFType;
            PartFVentilationType = partFVentilationType;
            IsBedroom = isBedroom;
            MinFlowRate_Lps = minFlowRate_Lps;
            IncludeInFloorAreaCheck = includeInFloorAreaCheck;
            IsTerminalSpace = isTerminalSpace;
            ScaleSupplyWithVolume = scaleSupplyWithVolume;
            ScaleExtractAboveMinimum = scaleExtractAboveMinimum;
            DefaultFlowWeightBasis = defaultFlowWeightBasis;
            ContinuousDesignFlowRate_Lps = calculatedFlowRate_Lps;
            IsCookingSpace = isCookingSpace;
            SpaceUse = spaceUse;
            SetbackFlowRate_Lps = setbackFlowRate_Lps;
        }

        public PartFSpaceData(PartFSpaceData partFSpaceData)
            : base(partFSpaceData)
        {
            if (partFSpaceData is not null)
            {
                ContinuousDesignFlowRate_Lps = partFSpaceData.ContinuousDesignFlowRate_Lps;
                SetbackFlowRate_Lps = partFSpaceData.SetbackFlowRate_Lps;
                DefaultFlowWeightBasis = partFSpaceData.DefaultFlowWeightBasis;
                IncludeInFloorAreaCheck = partFSpaceData.IncludeInFloorAreaCheck;
                IsBedroom = partFSpaceData.IsBedroom;
                IsTerminalSpace = partFSpaceData.IsTerminalSpace;
                MinFlowRate_Lps = partFSpaceData.MinFlowRate_Lps;
                PartFType = partFSpaceData.PartFType;
                PartFVentilationType = partFSpaceData.PartFVentilationType;
                ScaleExtractAboveMinimum = partFSpaceData.ScaleExtractAboveMinimum;
                ScaleSupplyWithVolume = partFSpaceData.ScaleSupplyWithVolume;
                IsCookingSpace = partFSpaceData.IsCookingSpace;
                SpaceUse = partFSpaceData.SpaceUse;
            }
        }

        public PartFSpaceData()
        {
        }

        public PartFSpaceData(PartFCategory partFCategory)
            : base(partFCategory?.Name)
        {
            ContinuousDesignFlowRate_Lps = null;
            SetbackFlowRate_Lps = null;

            if (partFCategory is not null)
            {
                DefaultFlowWeightBasis = partFCategory.DefaultFlowWeightBasis;
                IncludeInFloorAreaCheck = partFCategory.IncludeInFloorAreaCheck;
                IsBedroom = partFCategory.IsBedroom;
                IsTerminalSpace = partFCategory.IsTerminalSpace;
                MinFlowRate_Lps = partFCategory.MinFlowRate_Lps;
                PartFType = partFCategory.PartFType;
                PartFVentilationType = partFCategory.PartFVentilationType;
                ScaleExtractAboveMinimum = partFCategory.ScaleExtractAboveMinimum;
                ScaleSupplyWithVolume = partFCategory.ScaleSupplyWithVolume;
                IsCookingSpace = partFCategory.IsCookingSpace;
                SpaceUse = partFCategory.SpaceUse;
            }
        }

        public PartFSpaceData(JsonObject jsonObject)
            : base(jsonObject)
        {
        }

        /// <summary>
        /// Ventilation flow rate [l/s] at the continuous design condition: the Approved Document F
        /// sizing case that every implemented minimum is checked against. Equipment is selected on this
        /// rate.
        /// </summary>
        public double? ContinuousDesignFlowRate_Lps { get; set; }

        /// <summary>
        /// Ventilation flow rate [l/s] at the setback operating condition, i.e.
        /// <see cref="ContinuousDesignFlowRate_Lps"/> scaled by the rule set's setback factor, which is
        /// 30% of the continuous design rate by default.
        /// <para>
        /// A SAM reduced-operation convention, not a regulatory rate. It does not reduce or replace the
        /// continuous design calculation and is not checked against the Table 1.2 minimums, which apply
        /// at the continuous design condition.
        /// </para>
        /// <para>
        /// Deliberately not called a background rate: in Approved Document F a background ventilator is a
        /// trickle ventilator, and "whole dwelling (general) ventilation" is the continuous requirement,
        /// so "background" would be ambiguous against both.
        /// </para>
        /// </summary>
        public double? SetbackFlowRate_Lps { get; set; }

        /// <summary>
        /// The continuous design ventilation flow rate [l/s].
        /// <para>
        /// Retained as an alias of <see cref="ContinuousDesignFlowRate_Lps"/> so that existing scripts,
        /// Grasshopper definitions and serialised models keep their original meaning: this property has
        /// always held the sizing rate and continues to. It is read externally by SAM_Tas_Grasshopper and
        /// SAM_Systems, so its meaning is deliberately unchanged. New work should read
        /// <see cref="ContinuousDesignFlowRate_Lps"/> or <see cref="SetbackFlowRate_Lps"/> explicitly.
        /// </para>
        /// </summary>
        public double? CalculatedFlowRate_Lps
        {
            get
            {
                return ContinuousDesignFlowRate_Lps;
            }

            set
            {
                ContinuousDesignFlowRate_Lps = value;
            }
        }

        public string DefaultFlowWeightBasis { get; private set; }

        public bool IncludeInFloorAreaCheck { get; private set; }

        public bool IsBedroom { get; private set; }

        /// <summary>
        /// True where the room contains the cooking function, so Approved Document F, Volume 1
        /// (2021 edition) paragraph 1.17a and Table 1.2 require kitchen extract from it.
        /// </summary>
        public bool IsCookingSpace { get; private set; }

        public bool IsTerminalSpace { get; private set; }

        public double? MinFlowRate_Lps { get; private set; }

        public PartFType PartFType { get; private set; }

        public PartFVentilationType PartFVentilationType { get; private set; }

        public bool ScaleExtractAboveMinimum { get; private set; }

        public bool ScaleSupplyWithVolume { get; private set; }

        /// <summary>
        /// The shared semantic classification the Approved Document F category was resolved from, kept
        /// so a result can be traced back to how the room was recognised.
        /// </summary>
        public SpaceUse SpaceUse { get; private set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            //The continuous design rate is read from whichever key is present, newest name first. A model
            //serialised before the two conditions were separated carries only CalculatedFlowRate_Lps, and
            //that value has always been the sizing rate. DesignFlowRate_Lps is accepted too, for models
            //written by the interim build that used that name.
            if (jsonObject.ContainsKey("ContinuousDesignFlowRate_Lps"))
            {
                ContinuousDesignFlowRate_Lps = jsonObject["ContinuousDesignFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }
            else if (jsonObject.ContainsKey("DesignFlowRate_Lps"))
            {
                ContinuousDesignFlowRate_Lps = jsonObject["DesignFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }
            else if (jsonObject.ContainsKey("CalculatedFlowRate_Lps"))
            {
                ContinuousDesignFlowRate_Lps = jsonObject["CalculatedFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("SetbackFlowRate_Lps"))
            {
                SetbackFlowRate_Lps = jsonObject["SetbackFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }
            else if (jsonObject.ContainsKey("BackgroundFlowRate_Lps"))
            {
                SetbackFlowRate_Lps = jsonObject["BackgroundFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("DefaultFlowWeightBasis"))
            {
                DefaultFlowWeightBasis = jsonObject["DefaultFlowWeightBasis"]?.GetValue<string>();
            }

            if (jsonObject.ContainsKey("IncludeInFloorAreaCheck"))
            {
                IncludeInFloorAreaCheck = jsonObject["IncludeInFloorAreaCheck"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("IsBedroom"))
            {
                IsBedroom = jsonObject["IsBedroom"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("IsCookingSpace"))
            {
                IsCookingSpace = jsonObject["IsCookingSpace"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("IsTerminalSpace"))
            {
                IsTerminalSpace = jsonObject["IsTerminalSpace"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("MinFlowRate_Lps"))
            {
                MinFlowRate_Lps = jsonObject["MinFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
            }

            if (jsonObject.ContainsKey("PartFType"))
            {
                PartFType = Core.Query.Enum<PartFType>(jsonObject["PartFType"]?.GetValue<string>());
            }

            if (jsonObject.ContainsKey("PartFVentilationType"))
            {
                PartFVentilationType = Core.Query.Enum<PartFVentilationType>(jsonObject["PartFVentilationType"]?.GetValue<string>());
            }

            if (jsonObject.ContainsKey("ScaleExtractAboveMinimum"))
            {
                ScaleExtractAboveMinimum = jsonObject["ScaleExtractAboveMinimum"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("ScaleSupplyWithVolume"))
            {
                ScaleSupplyWithVolume = jsonObject["ScaleSupplyWithVolume"]?.GetValue<bool>() ?? false;
            }

            if (jsonObject.ContainsKey("SpaceUse"))
            {
                SpaceUse = Core.Query.Enum<SpaceUse>(jsonObject["SpaceUse"]?.GetValue<string>());
            }

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (ContinuousDesignFlowRate_Lps is not null && !double.IsNaN(ContinuousDesignFlowRate_Lps.Value))
            {
                result["ContinuousDesignFlowRate_Lps"] = ContinuousDesignFlowRate_Lps.Value;

                //Also written under the original key so a model produced here can still be read by an
                //earlier SAM build, which only knows CalculatedFlowRate_Lps.
                result["CalculatedFlowRate_Lps"] = ContinuousDesignFlowRate_Lps.Value;
            }

            if (SetbackFlowRate_Lps is not null && !double.IsNaN(SetbackFlowRate_Lps.Value))
            {
                result["SetbackFlowRate_Lps"] = SetbackFlowRate_Lps.Value;
            }

            if (DefaultFlowWeightBasis is not null)
            {
                result["DefaultFlowWeightBasis"] = DefaultFlowWeightBasis;
            }

            result["IncludeInFloorAreaCheck"] = IncludeInFloorAreaCheck;

            result["IsBedroom"] = IsBedroom;

            result["IsCookingSpace"] = IsCookingSpace;

            result["IsTerminalSpace"] = IsTerminalSpace;

            if (MinFlowRate_Lps is not null && !double.IsNaN(MinFlowRate_Lps.Value))
            {
                result["MinFlowRate_Lps"] = MinFlowRate_Lps.Value;
            }

            result["PartFType"] = PartFType.ToString();

            result["PartFVentilationType"] = PartFVentilationType.ToString();

            result["ScaleExtractAboveMinimum"] = ScaleExtractAboveMinimum;

            result["ScaleSupplyWithVolume"] = ScaleSupplyWithVolume;

            result["SpaceUse"] = SpaceUse.ToString();

            return result;
        }
    }
}
