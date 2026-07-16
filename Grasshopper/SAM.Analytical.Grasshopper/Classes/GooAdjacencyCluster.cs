// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SAM.Analytical.Grasshopper
{
    public class GooAdjacencyCluster : GooJSAMObject<AdjacencyCluster>, IGH_PreviewData, IGH_BakeAwareData
    {
        private sealed class PreviewSnapshot
        {
            public AdjacencyCluster Source;
            public double UnitScale;
            public BoundingBox ClippingBox;
            public List<Point3d> SpacePoints;
            public List<PanelsWire> PanelWires;
            public List<PanelMesh> PanelMeshes;
        }

        private struct PanelsWire
        {
            public List<Point3d[]> Loops;
        }

        private struct PanelMesh
        {
            public Face3D Face3D;
            public Brep Brep;
        }

        private PreviewSnapshot previewSnapshot;

        public GooAdjacencyCluster()
            : base()
        {
        }

        public GooAdjacencyCluster(AdjacencyCluster adjacencyCluster)
            : base(adjacencyCluster)
        {
        }

        private static PreviewSnapshot BuildPreviewSnapshot(AdjacencyCluster cluster, double unitScale)
        {
            PreviewSnapshot result = new PreviewSnapshot()
            {
                Source = cluster,
                UnitScale = unitScale,
                SpacePoints = new List<Point3d>(),
                PanelWires = new List<PanelsWire>(),
                PanelMeshes = new List<PanelMesh>()
            };

            List<BoundingBox3D> boundingBox3Ds = new List<BoundingBox3D>();

            IEnumerable<IPanel> panels = cluster.GetObjects<IPanel>();
            List<ISpace> spaces = cluster.GetObjects<ISpace>();

            if (spaces != null)
            {
                foreach (ISpace space in spaces)
                {
                    Point3D location = space?.Location;
                    if (location == null) continue;

                    boundingBox3Ds.Add(location.GetBoundingBox(1));
                    Point3d? pt = Geometry.Rhino.Convert.ToRhino(location);
                    if (pt.HasValue)
                        result.SpacePoints.Add(pt.Value);
                }
            }

            if (panels != null)
            {
                foreach (IPanel panel in panels)
                {
                    Face3D face3D = panel.Face3D;
                    if (face3D == null) continue;

                    BoundingBox3D bb = face3D.GetBoundingBox();
                    if (bb != null) boundingBox3Ds.Add(bb);

                    PanelsWire pw = new PanelsWire();
                    pw.Loops = new List<Point3d[]>();

                    IClosedPlanar3D externalEdge3D = face3D.GetExternalEdge3D();
                    if (externalEdge3D is ISegmentable3D segExt)
                    {
                        List<Point3d> pts = segExt.GetPoints().ConvertAll(x => Geometry.Rhino.Convert.ToRhino(x));
                        if (pts.Count != 0) { pts.Add(pts[0]); pw.Loops.Add(pts.ToArray()); }
                    }

                    IEnumerable<IClosedPlanar3D> internalEdge3Ds = face3D.GetInternalEdge3Ds();
                    if (internalEdge3Ds != null)
                    {
                        foreach (IClosedPlanar3D internalEdge3D in internalEdge3Ds)
                        {
                            if (internalEdge3D is ISegmentable3D segInt)
                            {
                                List<Point3d> pts = segInt.GetPoints().ConvertAll(x => Geometry.Rhino.Convert.ToRhino(x));
                                if (pts.Count != 0) { pts.Add(pts[0]); pw.Loops.Add(pts.ToArray()); }
                            }
                        }
                    }

                    result.PanelWires.Add(pw);

                    result.PanelMeshes.Add(new PanelMesh()
                    {
                        Face3D = face3D,
                        Brep = Geometry.Rhino.Convert.ToRhino_Brep(face3D)
                    });
                }
            }

            boundingBox3Ds.RemoveAll(x => x == null);
            if (boundingBox3Ds.Count != 0)
                result.ClippingBox = Geometry.Rhino.Convert.ToRhino(new BoundingBox3D(boundingBox3Ds));
            else
                result.ClippingBox = BoundingBox.Empty;

            return result;
        }

        private PreviewSnapshot EnsurePreviewSnapshot()
        {
            AdjacencyCluster cluster = Value;
            if (cluster == null)
                return null;

            double unitScale = Geometry.Rhino.Query.UnitScale();

            PreviewSnapshot result = previewSnapshot;
            if (result != null && ReferenceEquals(result.Source, cluster) && result.UnitScale.Equals(unitScale))
                return result;

            result = BuildPreviewSnapshot(cluster, unitScale);
            previewSnapshot = result;
            return result;
        }

        public BoundingBox ClippingBox
        {
            get
            {
                return EnsurePreviewSnapshot()?.ClippingBox ?? BoundingBox.Empty;
            }
        }

        public override IGH_Goo Duplicate()
        {
            return new GooAdjacencyCluster(Value);
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            PreviewSnapshot snapshot = EnsurePreviewSnapshot();
            if (snapshot == null) return;

            foreach (Point3d pt in snapshot.SpacePoints)
                args.Pipeline.DrawPoint(pt);

            BoundingBox3D boundingBox3D = null;
            if (args.Viewport.IsValidFrustum)
            {
                BoundingBox boundingBox = args.Viewport.GetFrustumBoundingBox();
                boundingBox3D = new BoundingBox3D(new Point3D[] { Geometry.Rhino.Convert.ToSAM(boundingBox.Min), Geometry.Rhino.Convert.ToSAM(boundingBox.Max) });
            }

            List<IPanel> panels = Value.GetObjects<IPanel>();
            if (panels == null || snapshot.PanelWires == null) return;

            int wireIndex = 0;
            foreach (IPanel panel in panels)
            {
                List<ISpace> spaces_Panel = Value.GetRelatedObjects<ISpace>(panel);
                if (spaces_Panel != null && spaces_Panel.Count > 1)
                {
                    wireIndex++;
                    continue;
                }

                Face3D face3D = panel.Face3D;
                if (face3D == null)
                {
                    wireIndex++;
                    continue;
                }

                if (boundingBox3D != null)
                {
                    BoundingBox3D boundingBox3D_Temp = face3D.GetBoundingBox();
                    if (boundingBox3D_Temp != null)
                    {
                        if (!boundingBox3D.Inside(boundingBox3D_Temp) && !boundingBox3D.Intersect(boundingBox3D_Temp))
                        {
                            wireIndex++;
                            continue;
                        }
                    }
                }

                if (wireIndex < snapshot.PanelWires.Count)
                {
                    PanelsWire pw = snapshot.PanelWires[wireIndex];
                    if (pw.Loops != null)
                    {
                        foreach (Point3d[] loop in pw.Loops)
                            args.Pipeline.DrawPolyline(new List<Point3d>(loop), loop == pw.Loops[0] ? System.Drawing.Color.DarkRed : System.Drawing.Color.BlueViolet);
                    }
                }

                wireIndex++;
            }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            PreviewSnapshot snapshot = EnsurePreviewSnapshot();
            if (snapshot == null) return;

            BoundingBox3D boundingBox3D = null;
            if (args.Viewport.IsValidFrustum)
            {
                BoundingBox boundingBox = args.Viewport.GetFrustumBoundingBox();
                boundingBox3D = new BoundingBox3D(new Point3D[] { Geometry.Rhino.Convert.ToSAM(boundingBox.Min), Geometry.Rhino.Convert.ToSAM(boundingBox.Max) });
            }

            List<IPanel> panels = Value.GetObjects<IPanel>();
            if (panels == null || snapshot.PanelMeshes == null) return;

            int meshIndex = 0;
            foreach (IPanel panel in panels)
            {
                List<ISpace> spaces = Value.GetRelatedObjects<ISpace>(panel);
                if (spaces != null && spaces.Count > 1)
                {
                    meshIndex++;
                    continue;
                }

                if (meshIndex >= snapshot.PanelMeshes.Count) break;

                PanelMesh pm = snapshot.PanelMeshes[meshIndex];
                if (pm.Face3D == null)
                {
                    meshIndex++;
                    continue;
                }

                if (boundingBox3D != null)
                {
                    BoundingBox3D boundingBox3D_Temp = pm.Face3D.GetBoundingBox();
                    if (boundingBox3D_Temp != null)
                    {
                        if (!boundingBox3D.Inside(boundingBox3D_Temp) && !boundingBox3D.Intersect(boundingBox3D_Temp))
                        {
                            meshIndex++;
                            continue;
                        }
                    }
                }

                if (pm.Brep != null)
                    args.Pipeline.DrawBrepShaded(pm.Brep, args.Material);

                meshIndex++;
            }
        }

        public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            obj_guid = Guid.Empty;

            List<IPanel> panels = Value?.GetObjects<IPanel>();
            if (panels == null || panels.Count == 0)
            {
                return false;
            }

            List<Brep> breps = new List<Brep>();
            foreach (IPanel panel in panels)
            {
                List<Brep> breps_Panel = Rhino.Convert.ToRhino_Breps(panel);
                if (breps_Panel == null)
                {
                    continue;
                }

                breps.AddRange(breps_Panel);
            }

            if (breps == null || breps.Count == 0)
            {
                return false;
            }

            Brep result = Brep.MergeBreps(breps, Core.Tolerance.MacroDistance);
            if (result == null)
            {
                return false;
            }

            obj_guid = doc.Objects.AddBrep(result);
            return true;
        }

        public override bool CastFrom(object source)
        {
            if (source is AdjacencyCluster)
            {
                Value = (AdjacencyCluster)source;
                return true;
            }

            if (typeof(IGH_Goo).IsAssignableFrom(source.GetType()))
            {
                try
                {
                    source = (source as dynamic).Value;
                }
                catch
                {
                }

                if (source is AdjacencyCluster)
                {
                    Value = (AdjacencyCluster)source;
                    return true;
                }
            }

            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (Value == null)
                return false;

            if (typeof(Y).IsAssignableFrom(typeof(GH_Mesh)))
            {
                target = (Y)(object)Value.ToGrasshopper_Mesh();
                return true;
            }

            return base.CastTo(ref target);
        }
    }

    //Params Components -> SAM used for internalizing data
    public class GooAdjacencyClusterParam : GH_PersistentParam<GooAdjacencyCluster>, IGH_PreviewObject, IGH_BakeAwareObject
    {
        public override Guid ComponentGuid => new Guid("408ca3f4-0598-4f18-8b25-1f9646c53ef0");

        protected override System.Drawing.Bitmap Icon => Core.Convert.ToBitmap(Resources.SAM_Small);

        //Here we control name, nickname, description, category, sub-category as deafult we use typeofclass name
        public GooAdjacencyClusterParam()
            : base(typeof(AdjacencyCluster).Name, typeof(AdjacencyCluster).Name, typeof(AdjacencyCluster).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooAdjacencyCluster> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooAdjacencyCluster value)
        {
            throw new NotImplementedException();
        }

        #region IGH_PreviewObject

        bool IGH_PreviewObject.Hidden { get; set; }
        bool IGH_PreviewObject.IsPreviewCapable => !VolatileData.IsEmpty;
        BoundingBox IGH_PreviewObject.ClippingBox => Preview_ComputeClippingBox();

        public bool IsBakeCapable => true;

        void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);

        void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);

        public void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc.CreateDefaultAttributes(), obj_ids);
        }

        public void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            foreach (var value in VolatileData.AllData(true))
            {
                Guid uuid = default;
                (value as IGH_BakeAwareData)?.BakeGeometry(doc, att, out uuid);
                obj_ids.Add(uuid);
            }
        }

        public void BakeGeometry_ByPanelType(RhinoDoc doc)
        {
            Modify.BakeGeometry_ByPanelType(doc, VolatileData, true, Core.Tolerance.Distance);
        }

        public void BakeGeometry_ByDischargeCoefficient(RhinoDoc doc)
        {
            Modify.BakeGeometry_ByDischargeCoefficient(doc, VolatileData);
        }

        public void BakeGeometry_ByConstruction(RhinoDoc doc)
        {
            Modify.BakeGeometry_ByConstruction(doc, VolatileData, true, Core.Tolerance.Distance);
        }

        public void BakeGeometry_ByBoundaryType(RhinoDoc doc)
        {
            Modify.BakeGeometry_ByBoundaryType(doc, VolatileData, true, Core.Tolerance.Distance);
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Bake By Type", Menu_BakeByPanelType, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());
            Menu_AppendItem(menu, "Bake By Construction", Menu_BakeByConstruction, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());
            Menu_AppendItem(menu, "Bake By BoundaryType", Menu_BakeByBoundaryType, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());
            Menu_AppendItem(menu, "Bake By Discharge Coefficient", Menu_BakeByDischargeCoefficient, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());
            Menu_AppendItem(menu, "Save As...", Menu_SaveAs, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());

            if (System.IO.File.Exists(Query.AnalyticalUIPath()))
            {
                Menu_AppendItem(menu, "Open in UI", Menu_OpenInUI, Core.Convert.ToBitmap(Resources.SAM3), VolatileData.AllData(true).Any());
            }

            base.AppendAdditionalMenuItems(menu);
        }

        private void Menu_BakeByPanelType(object sender, EventArgs e)
        {
            BakeGeometry_ByPanelType(RhinoDoc.ActiveDoc);
        }

        private void Menu_BakeByDischargeCoefficient(object sender, EventArgs e)
        {
            BakeGeometry_ByDischargeCoefficient(RhinoDoc.ActiveDoc);
        }

        private void Menu_BakeByConstruction(object sender, EventArgs e)
        {
            BakeGeometry_ByConstruction(RhinoDoc.ActiveDoc);
        }

        private void Menu_BakeByBoundaryType(object sender, EventArgs e)
        {
            BakeGeometry_ByBoundaryType(RhinoDoc.ActiveDoc);
        }

        private void Menu_SaveAs(object sender, EventArgs e)
        {
            Core.Grasshopper.Query.SaveAs(VolatileData);
        }

        private void Menu_OpenInUI(object sender, EventArgs e)
        {
            Process process = Convert.ToUI(VolatileData);
        }

        #endregion IGH_PreviewObject
    }
}
