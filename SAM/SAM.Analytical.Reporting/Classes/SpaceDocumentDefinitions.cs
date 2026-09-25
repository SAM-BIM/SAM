// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Reporting;

namespace SAM.Analytical.Reporting
{
    /// <summary>
    /// The space document types.
    /// </summary>
    public static class SpaceDocumentDefinitions
    {
        /// <summary>
        /// Phase 1 Space Assumptions: identity, geometry, internal condition, design criteria, ventilation, systems,
        /// fabric and persisted Tas design loads, then the provenance footer. Built from the saved model only.
        /// </summary>
        public static DocumentDefinition<SpaceDocumentData> SpaceAssumptions { get; } = new DocumentDefinition<SpaceDocumentData>(
            "space-assumptions",
            "Space Assumptions",
            DocumentScope.Space,
            new ISectionBuilder<SpaceDocumentData>[]
            {
                new SpaceIdentitySectionBuilder(),
                new SpaceGeometrySectionBuilder(),
                new SpaceInternalConditionSectionBuilder(),
                new SpaceDesignCriteriaSectionBuilder(),
                new SpaceVentilationSectionBuilder(),
                new SpaceSystemsSectionBuilder(),
                new SpaceFabricSectionBuilder(),
                new SpaceSizingSectionBuilder(),
            },
            new SpaceFooterBuilder(),
            data => data.Identity.Name.TryGetValue(out string name) ? name : data.Identity.Guid.ToString("D"));
    }
}
