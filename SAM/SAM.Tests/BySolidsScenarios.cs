// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Core;
using SAM.Geometry.Spatial;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static SAM.Tests.PanelsBySolidTests;

namespace SAM.Tests
{
    /// <summary>
    /// Reproducible BySolids runs used by the Diagnostics tests: the contemporary house fixture and synthetic elements
    /// through <see cref="Analytical.Create.PanelsBySolid"/>, <see cref="Analytical.Query.SnapToJunctions"/> and
    /// <see cref="Analytical.Create.AperturesBySolids"/>, each reduced to a log and an "engineering fingerprint"
    /// (everything that is not diagnostics text: panel and aperture types, areas, thicknesses, orientation,
    /// extents, source stamps, hosting). The committed golden file <c>Golden/PanelsBySolid/bysolids_baseline_q4.json</c>
    /// was captured from the legacy <c>out List&lt;string&gt;</c> API before the Diagnostics migration.
    /// </summary>
    internal static class BySolidsScenarios
    {
        internal sealed class Settings
        {
            public string Name { get; set; }

            public double MinThickness { get; set; }

            public double MaxThicknessRatio { get; set; }
        }

        /// <summary>The component defaults, a documented working setting, a below-minThickness setting and the documented upper ratio.</summary>
        internal static readonly Settings[] HouseSettings =
        {
            new Settings { Name = "house-default", MinThickness = 0.08, MaxThicknessRatio = 0.2 },
            new Settings { Name = "house-ratio-0.25", MinThickness = 0.08, MaxThicknessRatio = 0.25 },
            new Settings { Name = "house-min-0.25", MinThickness = 0.25, MaxThicknessRatio = 0.25 },
            new Settings { Name = "house-min-0.05-ratio-0.3", MinThickness = 0.05, MaxThicknessRatio = 0.3 },
        };

        internal sealed class Result
        {
            public List<string> Log { get; } = new List<string>();

            public List<string> Engineering { get; } = new List<string>();
        }

        private static string F(double value) => value.ToString("0.000000", CultureInfo.InvariantCulture);

        internal static List<string> Fingerprint(IEnumerable<Panel> panels)
        {
            List<string> result = new List<string>();
            foreach (Panel panel in panels)
            {
                Face3D face3D = panel.GetFace3D();
                Vector3D normal = face3D.GetPlane().Normal;
                BoundingBox3D box = face3D.GetBoundingBox();
                panel.TryGetValue(PanelSourceParameter.PhysicalThickness, out double thickness);
                panel.TryGetValue(PanelSourceParameter.SourceName, out string name);
                panel.TryGetValue(PanelSourceParameter.SourceRole, out string role);
                result.Add(string.Format(CultureInfo.InvariantCulture, "{0}|area {1}|thickness {2}|normal {3:0.0000},{4:0.0000},{5:0.0000}|box {6},{7},{8} - {9},{10},{11}|holes {12}|{13}|{14}|apertures {15}",
                    panel.PanelType, F(panel.GetArea()), F(thickness), normal.X, normal.Y, normal.Z, F(box.Min.X), F(box.Min.Y), F(box.Min.Z), F(box.Max.X), F(box.Max.Y), F(box.Max.Z),
                    face3D.GetInternalEdge3Ds()?.Count ?? 0, name, role, panel.Apertures?.Count ?? 0));
            }

            return result;
        }

        internal static List<string> Fingerprint(IEnumerable<Aperture> apertures)
        {
            List<string> result = new List<string>();
            foreach (Aperture aperture in apertures)
            {
                Face3D face3D = aperture.GetFace3D();
                Vector3D normal = face3D.GetPlane().Normal;
                BoundingBox3D box = face3D.GetBoundingBox();
                aperture.TryGetValue(ApertureSourceParameter.SourceThickness, out double thickness);
                aperture.TryGetValue(ApertureSourceParameter.SourceName, out string name);
                result.Add(string.Format(CultureInfo.InvariantCulture, "{0}|area {1}|depth {2}|normal {3:0.0000},{4:0.0000},{5:0.0000}|box {6},{7},{8} - {9},{10},{11}|{12}",
                    aperture.ApertureType, F(aperture.GetArea()), F(thickness), normal.X, normal.Y, normal.Z, F(box.Min.X), F(box.Min.Y), F(box.Min.Z), F(box.Max.X), F(box.Max.Y), F(box.Max.Z), name));
            }

            return result;
        }

        internal static List<ApertureSolid> HouseApertureSolids()
        {
            return PanelsBySolidComplexHouseTests.Load("Apertures").ConvertAll(x => new ApertureSolid(x.Shell, ApertureType.Window) { SourceName = x.Name, SourceId = x.Id, SourceLayer = x.Layer });
        }

        /// <summary>Synthetic apertures: a window (frame and glass), a door leaf away from it, a missing geometry and a deep, non-planar box.</summary>
        internal static List<ApertureSolid> SyntheticApertureSolids()
        {
            Shell frame = Prism(XZ, Rectangle(1, 0.9, 3, 2.1), new List<IList<(double U, double V)>> { Rectangle(1.06, 0.96, 2.94, 2.04) }, 0.09, 0.16);
            Shell glass = Box(1.06, 0.12, 0.96, 2.94, 0.13, 2.04);
            Shell leaf = Box(5, 0.1, 0, 5.9, 0.15, 2.05);
            Shell cube = Box(8, 0, 0, 10, 2, 2);
            return new List<ApertureSolid>
            {
                new ApertureSolid(frame, ApertureType.Window) { SourceName = "frame" },
                new ApertureSolid(glass, ApertureType.Window) { SourceName = "glass" },
                new ApertureSolid(leaf, ApertureType.Door) { SourceName = "leaf" },
                new ApertureSolid(null, ApertureType.Window) { SourceName = "nothing" },
                new ApertureSolid(cube, ApertureType.Window) { SourceName = "cube" },
            };
        }

        /// <summary>Synthetic panel solids covering Info-only, Air and every skip reason.</summary>
        internal static List<PanelsBySolidComplexHouseTests.SourceSolid> SyntheticSolids()
        {
            List<(string Name, Shell Shell)> solids = new List<(string, Shell)>
            {
                ("wall", Prism(XZ, Rectangle(0, 0, 4, 3), null, 0, 0.25)),
                ("slab-void", Prism(XY, Rectangle(0, 0, 6, 4), new List<IList<(double U, double V)>> { Rectangle(2, 1, 3, 3) }, 2.8, 3.0)),
                ("tread", Box(0, 0, 0, 1, 0.3, 0.25)),
                ("balustrade", Prism(XZ, Rectangle(0, 0, 4, 1), null, 0, 0.05)),
                ("open", new Shell(Prism(XZ, Rectangle(0, 0, 4, 3), null, 0, 0.25).Face3Ds.Take(3).ToList())),
            };

            return solids.ConvertAll(x => new PanelsBySolidComplexHouseTests.SourceSolid { Name = x.Name, Id = x.Name + "-id", Layer = "Synthetic", Shell = x.Shell });
        }

        /// <summary>One run of the three producers: the log lines they returned and the geometry they produced.</summary>
        internal sealed class Producers
        {
            public System.Func<Shell, IEnumerable<ISAMGeometry3D>, Settings, PanelsBySolidComplexHouseTests.SourceSolid, int, (List<Panel> Panels, List<string> Lines)> Panels { get; set; }

            public System.Func<IEnumerable<Panel>, (List<Panel> Panels, List<string> Lines)> Snap { get; set; }

            public System.Func<IEnumerable<ApertureSolid>, (List<Aperture> Apertures, List<string> Lines, List<int> Excluded)> Apertures { get; set; }
        }

        /// <summary>The legacy <c>out List&lt;string&gt;</c> overloads.</summary>
        internal static readonly Producers Legacy = new Producers
        {
            Panels = (shell, apertures, settings, solid, index) =>
            {
                List<Panel> panels = shell.PanelsBySolid(out List<string> lines, apertures, settings.MinThickness, settings.MaxThicknessRatio, true, solid.Name, solid.Id, solid.Layer, sourceIndex: index);
                return (panels, lines);
            },
            Snap = panels =>
            {
                List<Panel> snapped = panels.SnapToJunctions(out List<string> lines, 0.3);
                return (snapped, lines);
            },
            Apertures = apertureSolids =>
            {
                List<Aperture> apertures = apertureSolids.AperturesBySolids(out List<string> lines, out List<int> excluded);
                return (apertures, lines, excluded);
            },
        };

        /// <summary>The structured overloads, collecting every record of the run into <paramref name="sink"/>.</summary>
        internal static Producers Structured(Core.Diagnostics sink)
        {
            List<string> Since(int count) => sink.Skip(count).Select(x => x.ToString()).ToList();

            return new Producers
            {
                Panels = (shell, apertures, settings, solid, index) =>
                {
                    int count = sink.Count;
                    List<Panel> panels = shell.PanelsBySolid(sink, apertures, settings.MinThickness, settings.MaxThicknessRatio, true, solid.Name, solid.Id, solid.Layer, sourceIndex: index);
                    return (panels, Since(count));
                },
                Snap = panels =>
                {
                    int count = sink.Count;
                    List<Panel> snapped = panels.SnapToJunctions(sink, 0.3);
                    return (snapped, Since(count));
                },
                Apertures = apertureSolids =>
                {
                    int count = sink.Count;
                    List<Aperture> apertures = apertureSolids.AperturesBySolids(sink, out List<int> excluded);
                    return (apertures, Since(count), excluded);
                },
            };
        }

        /// <summary>Panels, snapping and (optionally) hosting of the apertures on the snapped panels, as the Grasshopper components chain them.</summary>
        internal static Result RunPanels(Producers producers, List<PanelsBySolidComplexHouseTests.SourceSolid> solids, List<ApertureSolid> apertureSolids, Settings settings)
        {
            Result result = new Result();
            List<ISAMGeometry3D> apertureGeometries = apertureSolids?.ConvertAll(x => x.Geometry).Where(x => x != null).ToList();
            List<Panel> all = new List<Panel>();
            for (int i = 0; i < solids.Count; i++)
            {
                (List<Panel> panels, List<string> lines) = producers.Panels(solids[i].Shell, apertureGeometries, settings, solids[i], i);
                result.Log.AddRange(lines.ConvertAll(x => string.Format("[{0}] {1}", i, x)));
                result.Engineering.AddRange(Fingerprint(panels).ConvertAll(x => string.Format("[{0}] {1}", i, x)));
                all.AddRange(panels);
            }

            if (all.Count != 0)
            {
                (List<Panel> snapped, List<string> lines) = producers.Snap(all);
                result.Log.AddRange(lines.ConvertAll(x => "[snap] " + x));
                result.Engineering.AddRange(Fingerprint(snapped).ConvertAll(x => "[snapped] " + x));

                if (apertureSolids != null && apertureSolids.Count != 0)
                {
                    List<Aperture> apertures = producers.Apertures(apertureSolids).Apertures;
                    AdjacencyCluster adjacencyCluster = new AdjacencyCluster();
                    foreach (Panel panel in snapped)
                    {
                        adjacencyCluster.AddObject(panel);
                    }

                    List<Aperture> hosted = adjacencyCluster.AddAperturesByApertures(apertures, true, Tolerance.MacroDistance, 0.1) ?? new List<Aperture>();
                    result.Engineering.Add(string.Format("[hosted] {0} of {1} apertures", hosted.Count, apertures.Count));
                    result.Engineering.AddRange(Fingerprint(adjacencyCluster.GetPanels()).OrderBy(x => x, System.StringComparer.Ordinal).Select(x => "[hosted] " + x));
                }
            }

            return result;
        }

        internal static Result RunApertures(Producers producers, List<ApertureSolid> apertureSolids)
        {
            Result result = new Result();
            (List<Aperture> apertures, List<string> lines, List<int> excluded) = producers.Apertures(apertureSolids);
            result.Log.AddRange(lines);
            result.Engineering.AddRange(Fingerprint(apertures));
            result.Engineering.Add("excluded " + string.Join(",", excluded));
            return result;
        }
    }
}
