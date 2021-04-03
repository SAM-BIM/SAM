﻿using SAM.Geometry.Spatial;
using SAM.Geometry.Planar;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        public static void Align(this List<Panel> panels, double elevation, double referenceElevation, double maxDistance = 0.2, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            if (panels == null || double.IsNaN(elevation) || double.IsNaN(referenceElevation))
                return;

            List<Panel> panels_Temp = new List<Panel>();
            Dictionary<Segment2D, Panel> dictionary_Reference = new Dictionary<Segment2D, Panel>();
            foreach (Panel panel in panels)
            {
                if (panel == null)
                    continue;

                double max = panel.MaxElevation();
                double min = panel.MinElevation();

                Plane plane = new Plane(new Point3D(0, 0, (max + min) / 2), Vector3D.WorldZ);

                PlanarIntersectionResult planarIntersectionResult = null;

                if (System.Math.Abs(min - elevation) < Core.Tolerance.Distance || (min - Core.Tolerance.Distance < elevation && max - Core.Tolerance.Distance > elevation))
                {
                    planarIntersectionResult = PlanarIntersectionResult.Create(plane, panel.GetFace3D());
                    if (planarIntersectionResult != null)
                    {
                        List<Segment2D> segment2Ds = planarIntersectionResult.GetGeometry2Ds<Segment2D>();
                        if (segment2Ds != null && segment2Ds.Count != 0)
                        {
                            Geometry.Planar.Query.ExtremePoints(segment2Ds.Point2Ds(), out Point2D point2D_1, out Point2D point2D_2);
                            if (point2D_1.Distance(point2D_2) > tolerance_Distance)
                                panels_Temp.Add(panel);
                        }
                    }
                }

                if (System.Math.Abs(min - referenceElevation) < Core.Tolerance.Distance || (min - Core.Tolerance.Distance < referenceElevation && max - Core.Tolerance.Distance > referenceElevation))
                {
                    if (planarIntersectionResult == null)
                        planarIntersectionResult = PlanarIntersectionResult.Create(plane, panel.GetFace3D());

                    if (planarIntersectionResult != null)
                    {
                        List<Segment2D> segment2Ds = planarIntersectionResult.GetGeometry2Ds<Segment2D>();
                        if (segment2Ds != null && segment2Ds.Count != 0)
                        {
                            foreach (Segment2D segment2D in segment2Ds)
                            {
                                dictionary_Reference[segment2D] = panel;
                            }
                        }
                    }
                }
            }

            if (panels_Temp.Count == 0 || dictionary_Reference.Count == 0)
                return;

            Align(panels_Temp, dictionary_Reference, maxDistance, tolerance_Angle, tolerance_Distance);

            foreach (Panel panel_Temp in panels_Temp)
            {
                int index = panels.FindIndex(x => x.Guid == panel_Temp.Guid);
                if (index == -1)
                    continue;

                panels[index] = panel_Temp;
            }
        }

        public static void Align(this List<Panel> panels, Dictionary<Segment2D, Panel> dictionary_Reference, double maxDistance = 0.2, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            if (panels == null || dictionary_Reference == null)
                return;

            Plane plane = Plane.WorldXY;

            bool updated = false;
            for (int i = 0; i < panels.Count; i++)
            {
                Panel panel = panels[i];
                if (panel == null)
                    continue;

                double max = panel.MaxElevation();
                double min = panel.MinElevation();

                Plane plane_Temp = Plane.WorldXY.GetMoved(Vector3D.WorldZ * ((max + min) / 2)) as Plane;

                PlanarIntersectionResult planarIntersectionResult = PlanarIntersectionResult.Create(plane_Temp, panel.GetFace3D());
                if (planarIntersectionResult == null || !planarIntersectionResult.Intersecting)
                    continue;

                List<Segment2D> segment2Ds_Intersection = planarIntersectionResult.GetGeometry2Ds<Segment2D>();
                if (segment2Ds_Intersection == null || segment2Ds_Intersection.Count == 0)
                    continue;

                Geometry.Planar.Query.ExtremePoints(segment2Ds_Intersection.Point2Ds(), out Point2D point2D_1, out Point2D point2D_2);
                if (point2D_1.Distance(point2D_2) <= tolerance_Distance)
                    continue;

                Segment2D segment2D = new Segment2D(point2D_1, point2D_2);

                List<Segment2D> segment2Ds_Temp = dictionary_Reference.Keys.ToList().FindAll(x => x.Collinear(segment2D));
                if (segment2Ds_Temp == null || segment2Ds_Temp.Count == 0)
                    continue;

                List<Segment2D> segment2Ds_Result = new List<Segment2D>();
                foreach (Segment2D segment2D_Temp in segment2Ds_Temp)
                {
                    double distance = segment2D_Temp.Distance(segment2D, Core.Tolerance.MacroDistance);
                    if(distance < tolerance_Distance)
                    {
                        segment2Ds_Result = null;
                        break;
                    }

                    if (distance > maxDistance + Core.Tolerance.MacroDistance)
                        continue;

                    segment2Ds_Result.Add(segment2D_Temp);
                }

                if (segment2Ds_Result == null || segment2Ds_Result.Count == 0)
                    continue;

                segment2Ds_Temp = segment2Ds_Result;

                segment2Ds_Temp.Sort((x, y) => segment2D.Distance(x).CompareTo(segment2D.Distance(y)));

                Segment2D segment2D_Reference = null;

                foreach (Segment2D segment2D_Temp in segment2Ds_Temp)
                {
                    Panel panel_Reference = dictionary_Reference[segment2D_Temp];
                    if (panel.Construction == null)
                    {
                        if (panel_Reference.Construction == null)
                        {
                            segment2D_Reference = segment2D_Temp;
                            break;
                        }
                        continue;
                    }

                    if (panel.Construction.Name.Equals(panel_Reference.Construction.Name))
                    {
                        segment2D_Reference = segment2D_Temp;
                        break;
                    }
                }

                if (segment2D_Reference == null)
                {
                    HashSet<PanelType> panelTypes = new HashSet<PanelType>();
                    panelTypes.Add(panel.PanelType);
                    switch (panelTypes.First())
                    {
                        case Analytical.PanelType.CurtainWall:
                            panelTypes.Add(Analytical.PanelType.WallExternal);
                            break;

                        case Analytical.PanelType.UndergroundWall:
                            panelTypes.Add(Analytical.PanelType.WallExternal);
                            break;

                        case Analytical.PanelType.Undefined:
                            panelTypes.Add(Analytical.PanelType.WallInternal);
                            break;
                    }

                    foreach (Segment2D segment2D_Temp in segment2Ds_Temp)
                    {
                        PanelType panelType_Temp = dictionary_Reference[segment2D_Temp].PanelType;
                        if (panelTypes.Contains(panelType_Temp))
                        {
                            segment2D_Reference = segment2D_Temp;
                            break;
                        }
                    }
                }

                if (segment2D_Reference == null)
                    segment2D_Reference = segment2Ds_Temp.First();

                if (segment2D_Reference == null)
                    continue;

                point2D_1 = segment2D.Mid();
                point2D_2 = segment2D_Reference.Project(point2D_1);
                if (point2D_1.Distance(point2D_2) <= tolerance_Distance)
                    continue;

                Vector3D vector3D = Plane.WorldXY.Convert(new Vector2D(point2D_1, point2D_2));

                List<Panel> panels_Connected = panel.ConnectedPanels(panels, tolerance_Distance);

                panel = new Panel(panel); 
                panel.Move(vector3D);
                panels[i] = panel;

                if (panels_Connected != null)
                {
                    Plane plane_Panel = panel.Plane;

                    foreach (Panel panel_Connected in panels_Connected)
                    {
                        Panel panel_New = Extend(panel_Connected, plane_Panel, tolerance_Angle, tolerance_Distance);
                        if (panel_New == null)
                            continue;

                        int index = panels.IndexOf(panel_Connected);
                        if (index != -1)
                            panels[index] = panel_New;
                    }
                }

                updated = true;
                break;
            }

            if (updated)
                Align(panels, dictionary_Reference, maxDistance, tolerance_Angle, tolerance_Distance);

        }
    }
}