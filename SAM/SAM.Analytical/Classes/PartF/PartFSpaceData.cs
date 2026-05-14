// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
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
            double calculatedFlowRate_Lps)
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
            CalculatedFlowRate_Lps = CalculatedFlowRate_Lps;
        }

        public PartFSpaceData(PartFSpaceData partFSpaceData)
            : base(partFSpaceData)
        {
            if (partFSpaceData is not null)
            {
                CalculatedFlowRate_Lps = partFSpaceData.CalculatedFlowRate_Lps;
                DefaultFlowWeightBasis = partFSpaceData.DefaultFlowWeightBasis;
                IncludeInFloorAreaCheck = partFSpaceData.IncludeInFloorAreaCheck;
                IsBedroom = partFSpaceData.IsBedroom;
                IsTerminalSpace = partFSpaceData.IsTerminalSpace;
                MinFlowRate_Lps = partFSpaceData.MinFlowRate_Lps;
                PartFType = partFSpaceData.PartFType;
                PartFVentilationType = partFSpaceData.PartFVentilationType;
                ScaleExtractAboveMinimum = partFSpaceData.ScaleExtractAboveMinimum;
                ScaleSupplyWithVolume = partFSpaceData.ScaleSupplyWithVolume;
            }

        }

        public PartFSpaceData()
        {
        }

        public PartFSpaceData(PartFCategory partFCategory)
            : base(partFCategory?.Name)
        {
            CalculatedFlowRate_Lps = null;

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
            }
        }


        public PartFSpaceData(JObject jObject)
            : base(jObject)
        {
        }

        public double? CalculatedFlowRate_Lps { get; set; }

        public string DefaultFlowWeightBasis { get; private set; }

        public bool IncludeInFloorAreaCheck { get; private set; }

        public bool IsBedroom { get; private set; }

        public bool IsTerminalSpace { get; private set; }

        public double? MinFlowRate_Lps { get; private set; }

        public PartFType PartFType { get; private set; }

        public PartFVentilationType PartFVentilationType { get; private set; }

        public bool ScaleExtractAboveMinimum { get; private set; }

        public bool ScaleSupplyWithVolume { get; private set; }

        public override bool FromJsonObject(JsonObject jsonObject)
        {
            bool result = base.FromJsonObject(jsonObject);
            if (!result)
            {
                return result;
            }

            if (jsonObject.ContainsKey("CalculatedFlowRate_Lps"))
            {
                CalculatedFlowRate_Lps = jsonObject["CalculatedFlowRate_Lps"]?.GetValue<double>() ?? double.NaN;
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

            return result;
        }

        public override JsonObject ToJsonObject()
        {
            JsonObject result = base.ToJsonObject();
            if (result is null)
            {
                return result;
            }

            if (CalculatedFlowRate_Lps is not null && !double.IsNaN(CalculatedFlowRate_Lps.Value))
            {
                result["CalculatedFlowRate_Lps"] = CalculatedFlowRate_Lps.Value;
            }

            if (DefaultFlowWeightBasis is not null)
            {
                result["DefaultFlowWeightBasis"] = DefaultFlowWeightBasis;
            }

            result["IncludeInFloorAreaCheck"] = IncludeInFloorAreaCheck;

            result["IsBedroom"] = IsBedroom;

            result["IsTerminalSpace"] = IsTerminalSpace;

            if (MinFlowRate_Lps is not null && !double.IsNaN(MinFlowRate_Lps.Value))
            {
                result["MinFlowRate_Lps"] = MinFlowRate_Lps.Value;
            }

            result["PartFType"] = PartFType.ToString();

            result["PartFVentilationType"] = PartFVentilationType.ToString();

            result["ScaleExtractAboveMinimum"] = ScaleExtractAboveMinimum;

            result["ScaleSupplyWithVolume"] = ScaleSupplyWithVolume;

            return result;
        }
    }
}