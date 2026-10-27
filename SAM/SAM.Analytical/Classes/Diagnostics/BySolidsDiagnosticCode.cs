// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical
{
    /// <summary>
    /// Stable <see cref="Core.Diagnostic.Code"/> values of <see cref="Create.PanelsBySolid(Geometry.Spatial.Shell, Core.Diagnostics, System.Collections.Generic.IEnumerable{Geometry.Spatial.ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>,
    /// <see cref="Query.SnapToJunctions(System.Collections.Generic.IEnumerable{Panel}, Core.Diagnostics, double, double)"/> and
    /// <see cref="Create.AperturesBySolids(System.Collections.Generic.IEnumerable{ApertureSolid}, Core.Diagnostics, out System.Collections.Generic.List{int}, System.Collections.Generic.IEnumerable{ApertureConstruction}, double, double, double, double)"/>.
    /// Codes starting <c>PBS</c> belong to panels, <c>SNP</c> to snapping and <c>AGS</c> to apertures; <c>1xx</c> are
    /// Standard Info (except <see cref="PanelsContinuation"/>, Summary), <c>2xx</c> Detailed Info, and the rest Warnings. Callers should branch on the code rather than on message text.
    /// </summary>
    public static class BySolidsDiagnosticCode
    {
        /// <summary>Warning: the solid is not a closed solid.</summary>
        public const string PanelsNotClosed = "PBS001";

        /// <summary>Warning: skipped as below minThickness. Value is the thickness [m], Limit is minThickness [m].</summary>
        public const string PanelsBelowMinThickness = "PBS002";

        /// <summary>Warning: skipped as not panel-like. Value is thickness over smallest extent [-], Limit is maxThicknessRatio [-].</summary>
        public const string PanelsNotPanelLike = "PBS003";

        /// <summary>Warning: no pair of opposite parallel faces with material between them.</summary>
        public const string PanelsNoFacePair = "PBS004";

        /// <summary>Warning: the mid-plane of an element plane does not cut the solid.</summary>
        public const string PanelsMidPlaneMisses = "PBS005";

        /// <summary>Warning: one or more section regions of an element plane could not be turned into panels. Value is the count.</summary>
        public const string PanelsRegionFailed = "PBS006";

        /// <summary>Warning: the solid gave no panel and no other warning explains it. Only in the structured overload (the legacy log never had this line).</summary>
        public const string PanelsNone = "PBS007";

        /// <summary>Standard Info: variable thickness. Value is the stored PhysicalThickness [m].</summary>
        public const string PanelsVariableThickness = "PBS101";

        /// <summary>Standard Info: panels created from an element plane. Value is its thickness [m].</summary>
        public const string PanelsCreated = "PBS102";

        /// <summary>
        /// Summary Info (the one 1xx code at Summary: it keeps input the threshold alone would skip): a skipped solid is kept
        /// by <see cref="Create.PanelsBySolids"/> because one of its element planes, not panel-like on its own, is a piece of
        /// a panel-like element or closes a gap between other elements. Value is that plane's thickness over smallest extent
        /// [-], Limit is maxThicknessRatio [-].
        /// </summary>
        public const string PanelsContinuation = "PBS103";

        /// <summary>Detailed Info: other face-pair planes ignored as not panel-like.</summary>
        public const string PanelsIgnoredNotPanelLike = "PBS201";

        /// <summary>Detailed Info: other face-pair planes ignored as below minThickness. Limit is minThickness [m].</summary>
        public const string PanelsIgnoredBelowMinThickness = "PBS202";

        /// <summary>Warning: the <c>diagnosticDetail_</c> input is not a defined level, so Summary was used. Emitted by the Grasshopper components.</summary>
        public const string InvalidDetail = "BYS001";

        /// <summary>Summary Info: the component's totals for panels. Emitted by the Grasshopper component.</summary>
        public const string PanelsSummary = "PBS100";

        /// <summary>Summary Info: the component's totals for apertures. Emitted by the Grasshopper component.</summary>
        public const string AperturesSummary = "AGS100";

        /// <summary>Summary Info: junction snapping. Value is the snap distance [m].</summary>
        public const string SnapChecked = "SNP001";

        /// <summary>Warning: snapping trimmed a panel to nothing (opposite edges onto one junction), so it was dropped. Value is its area before snapping [m2].</summary>
        public const string SnapCollapsed = "SNP002";

        /// <summary>
        /// Warning: the set of panels dropped by snapping did not settle within the pass limit, so the last pass is returned.
        /// Value is the number of panels still changing between the last two passes, Limit is the pass limit.
        /// </summary>
        public const string SnapNotSettled = "SNP003";

        /// <summary>Warning: a member is not a closed solid or a planar face.</summary>
        public const string AperturesNotClosed = "AGS001";

        /// <summary>Warning: an assembly has no planar faces.</summary>
        public const string AperturesNoPlanarFaces = "AGS002";

        /// <summary>Warning: an assembly has no opening area on its plane.</summary>
        public const string AperturesNoOpening = "AGS003";

        /// <summary>Warning: an assembly is not planar. Value is depth over smallest extent [-], Limit is maxDepthRatio [-].</summary>
        public const string AperturesNotPlanar = "AGS004";

        /// <summary>Warning: no aperture construction is available for the assembly type.</summary>
        public const string AperturesNoConstruction = "AGS005";

        /// <summary>Standard Info: apertures created from an assembly. Value is its depth [m].</summary>
        public const string AperturesCreated = "AGS101";
    }
}
