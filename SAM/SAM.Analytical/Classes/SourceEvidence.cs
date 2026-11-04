// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// What <see cref="Create.PanelsBySolid(Shell, out List{string}, IEnumerable{ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>
    /// could prove about the source element of a mid-surface panel: a symmetric slab about the panel's plane that the
    /// element's material fills over <see cref="Support"/>. One optional record per panel, all-or-nothing: a panel has
    /// either every field here or no record. It is stamped only for a closed, valid solid whose faces are, to within
    /// <see cref="Tolerance.Distance"/> metres over their actual extent, parallel or perpendicular to the panel's plane
    /// (a layered prismatic element), and for no other solid.
    /// <para>
    /// A record is <b>evidence only while it still describes the panel it is on</b>: see
    /// <see cref="Query.ValidSourceEvidence(Panel, double)"/>. Editing a panel on its own plane (trim, extension, merge,
    /// split) keeps its frame, so the frame alone cannot tell that a record has gone stale; the record therefore also keeps
    /// <see cref="BoundFootprint"/>, the footprint the panel had the last time an evidence-aware SAM tool wrote it
    /// (<see cref="Create.PanelsBySolid(Shell, out List{string}, IEnumerable{ISAMGeometry3D}, double, double, bool, string, string, string, double, double, double, int?)"/>
    /// and <see cref="Query.SnapToJunctions(IEnumerable{Panel}, Core.Diagnostics, double, double)"/>). Any other writer leaves
    /// the footprint different from <see cref="BoundFootprint"/>, so the evidence is dropped instead of trusted.
    /// </para>
    /// <para>
    /// All regions are <see cref="Face2D"/>s in the frame <see cref="Plane"/>, inner loops kept. The record is stored on the
    /// panel and saved with it. The record is <b>immutable once constructed</b>: panels and their copies share one instance, so it
    /// has no public way to change a field (the regions and frame it returns are copies, and <see cref="IJSAMObject.FromJsonObject"/>
    /// is implemented explicitly and always refuses); a changed record is a new one, made by <see cref="WithBoundFootprint"/>.
    /// </para>
    /// </summary>
    public class SourceEvidence : IAnalyticalObject
    {
        private Plane plane;
        private List<Face2D> support;
        private List<Face2D> origin;
        private List<Face2D> boundFootprint;

        public SourceEvidence(Plane plane, double guaranteedThickness, IEnumerable<Face2D> support, IEnumerable<Face2D> origin, IEnumerable<Face2D> boundFootprint)
        {
            this.plane = plane == null ? null : new Plane(plane);
            GuaranteedThickness = guaranteedThickness;
            this.support = Clone(support);
            this.origin = Clone(origin);
            this.boundFootprint = Clone(boundFootprint);
        }

        public SourceEvidence(SourceEvidence sourceEvidence)
        {
            if (sourceEvidence == null)
            {
                return;
            }

            plane = sourceEvidence.plane == null ? null : new Plane(sourceEvidence.plane);
            GuaranteedThickness = sourceEvidence.GuaranteedThickness;
            support = Clone(sourceEvidence.support);
            origin = Clone(sourceEvidence.origin);
            boundFootprint = Clone(sourceEvidence.boundFootprint);
        }

        public SourceEvidence(JsonObject jsonObject)
        {
            Read(jsonObject);
        }

        /// <summary>
        /// The panel's frame at extraction (origin, normal and Y axis). Regions are expressed in it.
        /// </summary>
        public Plane Plane
        {
            get
            {
                return plane == null ? null : new Plane(plane);
            }
        }

        /// <summary>
        /// Thickness [m] of the symmetric slab about <see cref="Plane"/> that the source element's material fills over the whole
        /// of <see cref="Support"/>. It is the least of the element's slabs, so it can be below the panel's physical thickness.
        /// </summary>
        public double GuaranteedThickness { get; private set; } = double.NaN;

        /// <summary>
        /// The region where the slab is proved to be material: the intersection, over the layers between the element's
        /// parallel faces, of the material of the section of the solid. Aperture openings and notches are not filled here.
        /// </summary>
        public List<Face2D> Support
        {
            get
            {
                return Clone(support);
            }
        }

        /// <summary>
        /// The panel's own face at extraction (aperture-claimed openings and notches filled, unclaimed holes open).
        /// </summary>
        public List<Face2D> Origin
        {
            get
            {
                return Clone(origin);
            }
        }

        /// <summary>
        /// The panel's footprint the last time an evidence-aware SAM tool wrote it. The record is evidence only while the
        /// panel's current footprint equals it.
        /// </summary>
        public List<Face2D> BoundFootprint
        {
            get
            {
                return Clone(boundFootprint);
            }
        }

        /// <summary>
        /// True when every field is present: a frame, a positive thickness and a non-empty <see cref="Support"/>,
        /// <see cref="Origin"/> and <see cref="BoundFootprint"/>. A record that is not complete is no evidence.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return plane != null && GuaranteedThickness > 0 && !double.IsInfinity(GuaranteedThickness) && support != null && support.Count != 0 && origin != null && origin.Count != 0 && boundFootprint != null && boundFootprint.Count != 0;
            }
        }

        /// <summary>
        /// This record bound to another footprint (the one an evidence-aware tool has just written).
        /// </summary>
        public SourceEvidence WithBoundFootprint(IEnumerable<Face2D> boundFootprint)
        {
            return new SourceEvidence(plane, GuaranteedThickness, support, origin, boundFootprint);
        }

        /// <summary>
        /// Always false, and nothing changes. A record is immutable once constructed: panels and their copies share one instance,
        /// so overwriting it would change the certified thickness or support of every one of them without going through the
        /// rebinding check. Read a record with <see cref="SourceEvidence(JsonObject)"/>.
        /// </summary>
        bool IJSAMObject.FromJsonObject(JsonObject jsonObject)
        {
            return false;
        }

        private void Read(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return;
            }

            plane = jsonObject["Plane"] is JsonObject planeJson ? new Plane(planeJson) : null;
            GuaranteedThickness = jsonObject["GuaranteedThickness"] is JsonValue jsonValue && jsonValue.TryGetValue(out double value) ? value : double.NaN;
            support = ReadRegions(jsonObject["Support"]);
            origin = ReadRegions(jsonObject["Origin"]);
            boundFootprint = ReadRegions(jsonObject["BoundFootprint"]);
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this),
            };

            if (plane?.ToJsonObject() is JsonObject planeJson)
            {
                result["Plane"] = planeJson;
            }

            if (!double.IsNaN(GuaranteedThickness))
            {
                result["GuaranteedThickness"] = GuaranteedThickness;
            }

            result["Support"] = Write(support);
            result["Origin"] = Write(origin);
            result["BoundFootprint"] = Write(boundFootprint);

            return result;
        }

        private static List<Face2D> Clone(IEnumerable<Face2D> face2Ds)
        {
            return face2Ds?.Where(x => x != null).Select(x => new Face2D(x.ToJsonObject())).ToList();
        }

        private static List<Face2D> ReadRegions(JsonNode jsonNode)
        {
            if (jsonNode is not JsonArray jsonArray)
            {
                return null;
            }

            List<Face2D> result = new List<Face2D>();
            foreach (JsonNode item in jsonArray)
            {
                if (item is JsonObject jsonObject)
                {
                    result.Add(new Face2D(jsonObject));
                }
            }

            return result;
        }

        private static JsonArray Write(List<Face2D> face2Ds)
        {
            JsonArray result = new JsonArray();
            if (face2Ds != null)
            {
                foreach (Face2D face2D in face2Ds)
                {
                    if (face2D?.ToJsonObject() is JsonObject jsonObject)
                    {
                        result.Add(jsonObject);
                    }
                }
            }

            return result;
        }
    }
}
