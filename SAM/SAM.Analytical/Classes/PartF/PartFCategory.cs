// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public class PartFCategory
    {
        public string Name { get; private set; }

        public PartFType PartFType { get; private set; }

        public PartFVentilationType PartFVentilationType { get; private set; }

        public bool IsBedroom { get; private set; }

        /// <summary>
        /// Minimal flw rate [l/s]
        /// </summary>
        public double? MinFlowRate_Lps { get; private set; }

        public bool IncludeInFloorAreaCheck { get; private set; }

        public bool IsTerminalSpace { get; private set; }

        public bool ScaleSupplyWithVolume { get; private set; }

        public bool ScaleExtractAboveMinimum { get; private set; }

        public string DefaultFlowWeightBasis { get; private set; }

        public List<string> Synonyms { get; private set; }

        /// <summary>
        /// True where the room contains the cooking function, so Approved Document F, Volume 1
        /// (2021 edition) paragraph 1.17 and Table 1.2 require kitchen extract from it. A studio and
        /// an open plan living kitchen are both cooking spaces.
        /// </summary>
        public bool IsCookingSpace { get; private set; }

        /// <summary>
        /// The shared semantic classification this Approved Document F category applies to. This is
        /// what links the Part F rule set to the vocabulary that Approved Document O, CIBSE TM59 and
        /// the SAM_UI internal condition mapping also use, so a space is recognised once and every
        /// standard agrees what it is.
        /// <para>
        /// <see cref="Analytical.SpaceUse.Undefined"/> where a rule set predates the shared vocabulary;
        /// such a category is matched by its <see cref="Synonyms"/> alone.
        /// </para>
        /// </summary>
        public SpaceUse SpaceUse { get; private set; }

        public PartFCategory(
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
            List<string> synonyms,
            bool isCookingSpace = false,
            SpaceUse spaceUse = SpaceUse.Undefined)
        {
            IsCookingSpace = isCookingSpace;
            SpaceUse = spaceUse;
            Name = name;
            PartFType = partFType;
            PartFVentilationType = partFVentilationType;
            IsBedroom = isBedroom;
            MinFlowRate_Lps = minFlowRate_Lps;
            IncludeInFloorAreaCheck = includeInFloorAreaCheck;
            IsTerminalSpace = isTerminalSpace;
            ScaleSupplyWithVolume = scaleSupplyWithVolume;
            ScaleExtractAboveMinimum = scaleExtractAboveMinimum;
            DefaultFlowWeightBasis = defaultFlowWeightBasis;
            Synonyms = synonyms;
        }
    }
}

