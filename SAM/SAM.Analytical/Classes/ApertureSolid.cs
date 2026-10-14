// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Geometry.Spatial;

namespace SAM.Analytical
{
    /// <summary>
    /// One member of a window or door assembly (a frame, a frame member, a glass pane, a door leaf) given to
    /// <see cref="Create.AperturesBySolids"/>, with its explicit aperture type and its source for traceability.
    /// </summary>
    public class ApertureSolid
    {
        /// <param name="geometry">Closed <see cref="Shell"/> (or a planar <see cref="Face3D"/>, e.g. glass modelled as a surface), in metres.</param>
        /// <param name="apertureType">Window or Door. An assembly with any Door member becomes a Door.</param>
        public ApertureSolid(ISAMGeometry3D geometry, ApertureType apertureType)
        {
            Geometry = geometry;
            ApertureType = apertureType;
        }

        public ISAMGeometry3D Geometry { get; }

        public ApertureType ApertureType { get; }

        /// <summary>Source name (e.g. Rhino object name).</summary>
        public string SourceName { get; set; }

        /// <summary>Source identifier (e.g. Rhino object Guid).</summary>
        public string SourceId { get; set; }

        /// <summary>Source layer (full path).</summary>
        public string SourceLayer { get; set; }

        /// <summary>
        /// Optional explicit assembly key (e.g. the Rhino group). Members sharing a key form one assembly; members
        /// with different keys are never merged, even when they touch. Members without a key are grouped by proximity.
        /// </summary>
        public string GroupKey { get; set; }
    }
}
