// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace SAM.Tests
{
    /// <summary>
    /// <see cref="Analytical.Create.PanelsBySolid"/> on the contemporary two-storey house (SAM_daily 2026-10-13,
    /// <c>contemporary1.3dm</c>): the 19 solids and 18 glazing solids that <c>import-test.gh</c> feeds to
    /// SAMAnalytical.CreatePanelsBySolids, converted exactly as the component converts them and stored as SAM JSON in
    /// <c>Golden/PanelsBySolid/contemporary1_solids.json</c>. Each external wall storey is one ring-shaped solid with
    /// window niches, each roof solid holds two pitches, and some internal walls are L-shaped.
    /// <para>
    /// Coverage is checked geometrically, not by face count: every large pair of opposite faces with material between
    /// them (an element's two sides) must be covered by a panel of the same solid.
    /// </para>
    /// </summary>
    public class PanelsBySolidComplexHouseTests
    {
        private sealed class SourceSolid
        {
            public string Name { get; set; }

            public string Layer { get; set; }

            public string Id { get; set; }

            public Shell Shell { get; set; }
        }

        private static List<SourceSolid> Load(string key)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Golden", "PanelsBySolid", "contemporary1_solids.json");
            JsonObject root = (JsonObject)JsonNode.Parse(File.ReadAllText(path));
            return ((JsonArray)root[key]).Select(x => new SourceSolid
            {
                Name = (string)x["Name"],
                Layer = (string)x["Layer"],
                Id = (string)x["Id"],
                Shell = new Shell((JsonObject)x["Shell"]),
            }).ToList();
        }

        private static readonly Lazy<(List<SourceSolid> Solids, Dictionary<string, List<Panel>> Panels, Dictionary<string, List<string>> Diagnostics)> House = new Lazy<(List<SourceSolid>, Dictionary<string, List<Panel>>, Dictionary<string, List<string>>)>(() =>
        {
            List<SourceSolid> solids = Load("Solids");
            List<ISAMGeometry3D> apertures = Load("Apertures").ConvertAll(x => (ISAMGeometry3D)x.Shell);
            Dictionary<string, List<Panel>> panels = new Dictionary<string, List<Panel>>();
            Dictionary<string, List<string>> diagnostics = new Dictionary<string, List<string>>();
            foreach (SourceSolid solid in solids)
            {
                // The component's defaults (minThickness 0.08, Air for unclaimed openings) with maxThicknessRatio set to
                // 0.25 on the definition, as this house needs for its 6 x 1.4 m gable (0.214; the default is 0.2).
                panels[solid.Id] = solid.Shell.PanelsBySolid(out List<string> diagnostics_Solid, apertures, maxThicknessRatio: 0.25, sourceName: solid.Name, sourceId: solid.Id, sourceLayer: solid.Layer);
                diagnostics[solid.Id] = diagnostics_Solid;
            }

            return (solids, panels, diagnostics);
        });

        [Fact]
        public void ComplexHouse_Fixture_IsTheGrasshopperInput()
        {
            Assert.Equal(19, House.Value.Solids.Count);
            Assert.Equal(18, Load("Apertures").Count);
            Assert.All(House.Value.Solids, x => Assert.True(x.Shell.IsClosed()));
        }

        [Theory]
        [InlineData("GF_Wall_External", 10)]
        [InlineData("UF_Wall_External", 10)]
        [InlineData("RF_Roof_Main_Ridge_X", 2)]
        [InlineData("RF_Roof_Cross_Ridge_Y", 2)]
        [InlineData("UF_Gable_Cross_South", 1)]
        [InlineData("UF_Gable_Cross_North", 1)]
        [InlineData("UF_Gable_East", 1)]
        [InlineData("UF_Gable_West", 1)]
        [InlineData("GF_Slab_Ground", 1)]
        [InlineData("IF_Slab_Intermediate", 1)]
        public void ComplexHouse_Element_PanelCount(string name, int count)
        {
            SourceSolid solid = Assert.Single(House.Value.Solids, x => x.Name == name);
            Assert.Equal(count, House.Value.Panels[solid.Id].Count(x => x.PanelType != PanelType.Air));
        }

        [Fact]
        public void ComplexHouse_DefaultRatio_SkipsOnlyTheStairAndTheLowGableWithTheRatioNeeded()
        {
            // At the default maxThicknessRatio (0.2) the gable is skipped and reported, never dropped silently.
            List<ISAMGeometry3D> apertures = Load("Apertures").ConvertAll(x => (ISAMGeometry3D)x.Shell);
            List<string> skipped = new List<string>();
            foreach (SourceSolid solid in House.Value.Solids)
            {
                if (solid.Shell.PanelsBySolid(out List<string> diagnostics, apertures, sourceName: solid.Name).Count == 0)
                {
                    Assert.Contains(diagnostics, x => x.Contains("not panel-like"));
                    skipped.Add(solid.Name);
                    if (solid.Name == "UF_Gable_Cross_South")
                    {
                        Assert.Contains(diagnostics, x => x.Contains("0.214"));
                    }
                }
            }

            Assert.Equal(new List<string> { "ST_Stair_GF_to_FF", "UF_Gable_Cross_South" }, skipped.OrderBy(x => x).ToList());
        }

        [Fact]
        public void ComplexHouse_InternalWalls_LShapedWallGivesBothLegs()
        {
            List<int> counts = House.Value.Solids.Where(x => x.Name.EndsWith("_Wall_Internal")).Select(x => House.Value.Panels[x.Id].Count).OrderBy(x => x).ToList();
            Assert.Equal(new List<int> { 1, 1, 1, 1, 1, 1, 1, 2 }, counts);
        }

        [Fact]
        public void ComplexHouse_Stair_IsTheOnlyExclusionAndIsReported()
        {
            List<SourceSolid> excluded = House.Value.Solids.Where(x => House.Value.Panels[x.Id].Count == 0).ToList();
            SourceSolid stair = Assert.Single(excluded);
            Assert.Equal("ST_Stair_GF_to_FF", stair.Name);
            Assert.Contains(House.Value.Diagnostics[stair.Id], x => x.Contains("not panel-like"));
        }

        [Fact]
        public void ComplexHouse_EveryElementSide_IsCoveredByItsPanels()
        {
            List<string> uncovered = new List<string>();
            foreach (SourceSolid solid in House.Value.Solids.Where(x => x.Name != "ST_Stair_GF_to_FF"))
            {
                List<Panel> panels = House.Value.Panels[solid.Id].FindAll(x => x.PanelType != PanelType.Air);
                foreach ((Face3D Face, Face3D Overlap, double Thickness) side in ElementSides(solid.Shell))
                {
                    double area = side.Overlap.GetArea();
                    double covered = Covered(side.Overlap, panels, side.Thickness);
                    if (covered < 0.95 * area)
                    {
                        uncovered.Add(string.Format("{0}: {1:0.###} of {2:0.###} m2 at {3}", solid.Name, covered, area, side.Face.GetInternalPoint3D()));
                    }
                }
            }

            Assert.True(uncovered.Count == 0, string.Join(Environment.NewLine, uncovered));
        }

        [Fact]
        public void ComplexHouse_NoDuplicatePanels()
        {
            foreach (SourceSolid solid in House.Value.Solids)
            {
                List<Panel> panels = House.Value.Panels[solid.Id].FindAll(x => x.PanelType != PanelType.Air);
                for (int i = 0; i < panels.Count; i++)
                {
                    Plane plane_i = panels[i].GetFace3D().GetPlane();
                    for (int j = i + 1; j < panels.Count; j++)
                    {
                        Plane plane_j = panels[j].GetFace3D().GetPlane();
                        if (System.Math.Abs(plane_i.Normal.DotProduct(plane_j.Normal)) < 0.999 || System.Math.Abs(plane_i.Normal.DotProduct(new Vector3D(plane_i.Origin, plane_j.Origin))) > 0.31)
                        {
                            continue;
                        }

                        double overlap = plane_i.Convert(panels[i].GetFace3D()).Intersection(plane_i.Convert(panels[j].GetFace3D()), 1e-6)?.Sum(x => x.GetArea()) ?? 0;
                        Assert.True(overlap < 0.01, string.Format("{0}: panels {1} and {2} overlap by {3:0.###} m2", solid.Name, i, j, overlap));
                    }
                }
            }
        }

        [Fact]
        public void ComplexHouse_PanelTypesAndPhysicalThickness()
        {
            foreach (SourceSolid solid in House.Value.Solids.Where(x => x.Name != "ST_Stair_GF_to_FF"))
            {
                (PanelType panelType, double thickness) = solid.Name switch
                {
                    "RF_Roof_Main_Ridge_X" or "RF_Roof_Cross_Ridge_Y" => (PanelType.Roof, 0.25),
                    "IF_Slab_Intermediate" => (PanelType.Floor, 0.25),
                    "GF_Slab_Ground" => (PanelType.Floor, 0.2),
                    "GF_Wall_Internal" or "UF_Wall_Internal" => (PanelType.Wall, 0.15),
                    _ => (PanelType.Wall, 0.3),
                };

                foreach (Panel panel in House.Value.Panels[solid.Id].Where(x => x.PanelType != PanelType.Air))
                {
                    Assert.Equal(panelType, panel.PanelType);
                    Assert.True(panel.TryGetValue(PanelSourceParameter.PhysicalThickness, out double physicalThickness));
                    Assert.Equal(thickness, physicalThickness, 3);
                    Assert.True(panel.TryGetValue(PanelSourceParameter.SourceId, out string sourceId) && sourceId == solid.Id);

                    double z = panel.GetFace3D().GetPlane().Normal.Z;
                    if (panelType == PanelType.Roof)
                    {
                        Assert.True(z > 0.5, solid.Name);
                    }
                    else if (panelType == PanelType.Floor)
                    {
                        Assert.True(z < -0.99, solid.Name);
                    }
                }
            }
        }

        [Fact]
        public void ComplexHouse_SnapKeepsEveryPanel()
        {
            List<Panel> panels = House.Value.Panels.Values.SelectMany(x => x).ToList();
            List<Panel> snapped = panels.SnapToJunctions(out _, 0.3);
            Assert.Equal(panels.Count, snapped.Count);
            Assert.All(snapped, x => Assert.True(x.GetFace3D().IsValid()));
            Assert.Equal(panels.Select(x => x.Guid).OrderBy(x => x), snapped.Select(x => x.Guid).OrderBy(x => x));
        }

        /// <summary>
        /// The sides of the solid's elements, found independently of the implementation: each pair of opposite parallel
        /// faces 0.08 to 0.6 m apart with at least 1 m2 of projected overlap and material half-way between them. The
        /// thickness returned is the largest such separation of the face: the element it bounds (a wall face behind a
        /// window niche also pairs with the niche back, but belongs to the full wall).
        /// </summary>
        private static List<(Face3D Face, Face3D Overlap, double Thickness)> ElementSides(Shell shell)
        {
            List<(int Index, Face3D Overlap, double Thickness)> sides = new List<(int, Face3D, double)>();
            List<Face3D> face3Ds = shell.Face3Ds;
            for (int i = 0; i < face3Ds.Count; i++)
            {
                Plane plane_i = face3Ds[i].GetPlane();
                for (int j = 0; j < face3Ds.Count; j++)
                {
                    Plane plane_j = face3Ds[j].GetPlane();
                    if (i == j || plane_i.Normal.DotProduct(plane_j.Normal) > -0.9998)
                    {
                        continue;
                    }

                    double offset = plane_i.Normal.DotProduct(new Vector3D(plane_i.Origin, plane_j.Origin));
                    if (System.Math.Abs(offset) < 0.08 || System.Math.Abs(offset) > 0.6)
                    {
                        continue;
                    }

                    foreach (Face2D overlap in plane_i.Convert(face3Ds[i]).Intersection(plane_i.Convert(face3Ds[j]), 1e-6) ?? new List<Face2D>())
                    {
                        if (overlap.GetArea() < 1.0)
                        {
                            continue;
                        }

                        Face3D overlap3D = plane_i.Convert(overlap);
                        Point3D point3D = overlap3D.GetInternalPoint3D();
                        Vector3D half = plane_i.Normal * (offset / 2);
                        if (shell.Inside(new Point3D(point3D.X + half.X, point3D.Y + half.Y, point3D.Z + half.Z)))
                        {
                            sides.Add((i, overlap3D, System.Math.Abs(offset)));
                        }
                    }
                }
            }

            return sides.ConvertAll(x => (face3Ds[x.Index], x.Overlap, sides.Where(y => y.Index == x.Index).Max(y => y.Thickness)));
        }

        /// <summary>
        /// Area of <paramref name="face3D"/> covered by parallel panels within its element's thickness.
        /// </summary>
        private static double Covered(Face3D face3D, List<Panel> panels, double thickness)
        {
            Plane plane = face3D.GetPlane();
            double result = 0;
            foreach (Panel panel in panels)
            {
                Face3D face3D_Panel = panel.GetFace3D();
                Plane plane_Panel = face3D_Panel.GetPlane();
                if (System.Math.Abs(plane_Panel.Normal.DotProduct(plane.Normal)) < 0.9998 || System.Math.Abs(plane_Panel.Normal.DotProduct(new Vector3D(plane_Panel.Origin, plane.Origin))) > thickness)
                {
                    continue;
                }

                result += plane_Panel.Convert(face3D).Intersection(plane_Panel.Convert(face3D_Panel), 1e-6)?.Sum(x => x.GetArea()) ?? 0;
            }

            return result;
        }
    }
}
