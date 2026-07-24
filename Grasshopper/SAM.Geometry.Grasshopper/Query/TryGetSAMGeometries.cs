// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel.Types;
using SAM.Geometry.Object.Spatial;
using SAM.Geometry.Spatial;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Query
    {
        /// <summary>
        /// Tries to extract or convert SAM geometries of type <typeparamref name="T"/> from a Grasshopper object wrapper.
        /// </summary>
        /// <typeparam name="T">The requested SAM geometry type.</typeparam>
        /// <param name="objectWrapper">The Grasshopper object wrapper containing the geometry input.</param>
        /// <param name="sAMGeometries">When this method returns, contains the converted SAM geometries if found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if one or more valid SAM geometries were retrieved or converted; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetSAMGeometries<T>(this GH_ObjectWrapper objectWrapper, out List<T> sAMGeometries) where T : ISAMGeometry
        {
            sAMGeometries = null;

            if (objectWrapper == null || objectWrapper.Value == null)
            {
                return false;
            }

            List<T> result = new List<T>();
            ProcessValue(objectWrapper.Value, result);

            if (result.Count > 0)
            {
                sAMGeometries = result;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to extract or convert SAM geometries of type <typeparamref name="T"/> from a collection of Grasshopper object wrappers.
        /// </summary>
        /// <typeparam name="T">The requested SAM geometry type.</typeparam>
        /// <param name="objectWrappers">The collection of Grasshopper object wrappers containing geometry inputs.</param>
        /// <param name="sAMGeometries">When this method returns, contains the converted SAM geometries if found; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if one or more valid SAM geometries were retrieved or converted; otherwise, <see langword="false"/>.</returns>
        public static bool TryGetSAMGeometries<T>(this IEnumerable<GH_ObjectWrapper> objectWrappers, out List<T> sAMGeometries) where T : ISAMGeometry
        {
            sAMGeometries = null;

            if (objectWrappers == null)
            {
                return false;
            }

            List<T> result = new List<T>();
            foreach (GH_ObjectWrapper objectWrapper in objectWrappers)
            {
                if (objectWrapper == null || objectWrapper.Value == null)
                {
                    continue;
                }

                ProcessValue(objectWrapper.Value, result);
            }

            if (result.Count > 0)
            {
                sAMGeometries = result;
                return true;
            }

            return false;
        }

        private static void ProcessValue<T>(object value, List<T> result) where T : ISAMGeometry
        {
            if (value == null)
            {
                return;
            }

            if (value is GooSAMGeometry gooSAMGeometry)
            {
                value = gooSAMGeometry.Value;
                if (value == null)
                {
                    return;
                }
            }

            if (value is T directValue)
            {
                result.Add(directValue);
                return;
            }

            if (value is IGH_GeometricGoo geometricGoo)
            {
                object convertedGoo = ConvertGeometricGooToSAM(geometricGoo);
                if (convertedGoo != null)
                {
                    ProcessValue(convertedGoo, result);
                }
                return;
            }

            if (value is IGH_Goo ghGoo)
            {
                object scriptVar = ghGoo.ScriptVariable();
                if (scriptVar != null && scriptVar != value)
                {
                    ProcessValue(scriptVar, result);
                    return;
                }
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object item in enumerable)
                {
                    ProcessValue(item, result);
                }
                return;
            }

            ProcessSAMGeometryConversion(value, result);
        }

        private static object ConvertGeometricGooToSAM(IGH_GeometricGoo geometricGoo)
        {
            if (geometricGoo == null)
            {
                return null;
            }

            if (geometricGoo is GH_Curve ghCurve)
            {
                return ghCurve.ToSAM(true);
            }

            if (geometricGoo is GH_Surface ghSurface)
            {
                return ghSurface.ToSAM(true);
            }

            if (geometricGoo is GH_Brep ghBrep)
            {
                return ghBrep.ToSAM(true);
            }

            if (geometricGoo is GH_Mesh ghMesh)
            {
                return ghMesh.ToSAM();
            }

            if (geometricGoo is GH_Extrusion ghExtrusion)
            {
                return ghExtrusion.ToSAM_Shell();
            }

            if (geometricGoo is GH_Point ghPoint)
            {
                return ghPoint.ToSAM();
            }

            if (geometricGoo is GH_Plane ghPlane)
            {
                return ghPlane.ToSAM();
            }

            if (geometricGoo is GH_Vector ghVector)
            {
                return ghVector.ToSAM();
            }

            if (geometricGoo is GH_Line ghLine)
            {
                return ghLine.ToSAM();
            }

            if (geometricGoo is GH_Rectangle ghRectangle)
            {
                return ghRectangle.ToSAM();
            }

            if (geometricGoo is GH_Circle ghCircle)
            {
                return ghCircle.ToSAM();
            }

            return geometricGoo.ToSAM(true);
        }

        private static void ProcessSAMGeometryConversion<T>(object value, List<T> result) where T : ISAMGeometry
        {
            if (value == null)
            {
                return;
            }

            System.Type targetType = typeof(T);

            if (targetType == typeof(Face3D) || typeof(Face3D).IsAssignableFrom(targetType))
            {
                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    result.Add((T)(object)face3DObject.Face3D);
                    return;
                }

                if (value is Shell shell && shell.Face3Ds != null)
                {
                    foreach (Face3D face in shell.Face3Ds)
                    {
                        if (face != null)
                        {
                            result.Add((T)(object)face);
                        }
                    }
                    return;
                }

                if (value is Mesh3D mesh3D)
                {
                    List<Triangle3D> triangles = mesh3D.GetTriangles();
                    if (triangles != null)
                    {
                        foreach (Triangle3D triangle in triangles)
                        {
                            result.Add((T)(object)new Face3D(triangle));
                        }
                    }
                    return;
                }

                if (value is IClosedPlanar3D closedPlanar3D)
                {
                    Face3D face3D = Spatial.Create.Face3D(closedPlanar3D);
                    if (face3D != null)
                    {
                        result.Add((T)(object)face3D);
                    }
                    return;
                }

                if (value is Polycurve3D polycurve3D)
                {
                    if (Polycurve3D.TryGetPolyline3D(polycurve3D, out Polyline3D polyline3D) && polyline3D != null && polyline3D.IsClosed())
                    {
                        Face3D face3D = Spatial.Create.Face3D(polyline3D.ToPolygon3D());
                        if (face3D != null)
                        {
                            result.Add((T)(object)face3D);
                        }
                    }
                    return;
                }

                if (value is Polyline3D polyline)
                {
                    if (polyline.IsClosed())
                    {
                        Face3D face3D = Spatial.Create.Face3D(polyline.ToPolygon3D());
                        if (face3D != null)
                        {
                            result.Add((T)(object)face3D);
                        }
                    }
                    return;
                }
            }

            if (targetType == typeof(ISegmentable3D) || typeof(ISegmentable3D).IsAssignableFrom(targetType))
            {
                if (value is Polycurve3D polycurve3D)
                {
                    if (Polycurve3D.TryGetPolyline3D(polycurve3D, out Polyline3D polyline3D) && polyline3D != null)
                    {
                        result.Add((T)(object)polyline3D);
                        return;
                    }
                }

                if (value is Face3D face3D)
                {
                    List<IClosedPlanar3D> edge3Ds = face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is ISegmentable3D segmentable3D)
                            {
                                result.Add((T)(object)segmentable3D);
                            }
                        }
                    }
                    return;
                }

                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    List<IClosedPlanar3D> edge3Ds = face3DObject.Face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is ISegmentable3D segmentable3D)
                            {
                                result.Add((T)(object)segmentable3D);
                            }
                        }
                    }
                    return;
                }

                if (value is Shell shell && shell.Face3Ds != null)
                {
                    foreach (Face3D face in shell.Face3Ds)
                    {
                        if (face == null)
                        {
                            continue;
                        }

                        List<IClosedPlanar3D> edge3Ds = face.GetEdge3Ds();
                        if (edge3Ds != null)
                        {
                            foreach (IClosedPlanar3D edge in edge3Ds)
                            {
                                if (edge is ISegmentable3D segmentable3D)
                                {
                                    result.Add((T)(object)segmentable3D);
                                }
                            }
                        }
                    }
                    return;
                }

                if (value is IClosedPlanar3D closedPlanar3D && closedPlanar3D is ISegmentable3D segmentable)
                {
                    result.Add((T)(object)segmentable);
                    return;
                }
            }

            if (targetType == typeof(Polyline3D) || typeof(Polyline3D).IsAssignableFrom(targetType))
            {
                if (value is Polycurve3D polycurve3D)
                {
                    if (Polycurve3D.TryGetPolyline3D(polycurve3D, out Polyline3D polyline3D) && polyline3D != null)
                    {
                        result.Add((T)(object)polyline3D);
                        return;
                    }
                }

                if (value is Polygon3D polygon3D)
                {
                    List<Point3D> points = polygon3D.GetPoints();
                    if (points != null && points.Count > 0)
                    {
                        result.Add((T)(object)new Polyline3D(points, true));
                    }
                    return;
                }

                if (value is Face3D face3D)
                {
                    List<IClosedPlanar3D> edge3Ds = face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is Polygon3D poly3D)
                            {
                                List<Point3D> points = poly3D.GetPoints();
                                if (points != null && points.Count > 0)
                                {
                                    result.Add((T)(object)new Polyline3D(points, true));
                                }
                            }
                            else if (edge is Polyline3D polyline3D)
                            {
                                result.Add((T)(object)polyline3D);
                            }
                        }
                    }
                    return;
                }

                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    List<IClosedPlanar3D> edge3Ds = face3DObject.Face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is Polygon3D poly3D)
                            {
                                List<Point3D> points = poly3D.GetPoints();
                                if (points != null && points.Count > 0)
                                {
                                    result.Add((T)(object)new Polyline3D(points, true));
                                }
                            }
                            else if (edge is Polyline3D polyline3D)
                            {
                                result.Add((T)(object)polyline3D);
                            }
                        }
                    }
                    return;
                }
            }

            if (targetType == typeof(Polygon3D) || typeof(Polygon3D).IsAssignableFrom(targetType))
            {
                if (value is Polyline3D polyline3D && polyline3D.IsClosed())
                {
                    result.Add((T)(object)polyline3D.ToPolygon3D());
                    return;
                }

                if (value is Polycurve3D polycurve3D)
                {
                    if (Polycurve3D.TryGetPolyline3D(polycurve3D, out Polyline3D polyline) && polyline != null && polyline.IsClosed())
                    {
                        result.Add((T)(object)polyline.ToPolygon3D());
                        return;
                    }
                }

                if (value is Face3D face3D)
                {
                    if (face3D.GetExternalEdge3D() is Polygon3D extPolygon)
                    {
                        result.Add((T)(object)extPolygon);
                        return;
                    }
                }

                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    if (face3DObject.Face3D.GetExternalEdge3D() is Polygon3D extPolygon)
                    {
                        result.Add((T)(object)extPolygon);
                        return;
                    }
                }
            }

            if (targetType == typeof(Segment3D) || typeof(Segment3D).IsAssignableFrom(targetType))
            {
                if (value is ISegmentable3D segmentable3D)
                {
                    List<Segment3D> segments = segmentable3D.GetSegments();
                    if (segments != null)
                    {
                        foreach (Segment3D segment in segments)
                        {
                            if (segment != null)
                            {
                                result.Add((T)(object)segment);
                            }
                        }
                    }
                    return;
                }

                if (value is Face3D face3D)
                {
                    List<IClosedPlanar3D> edge3Ds = face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is ISegmentable3D segable)
                            {
                                List<Segment3D> segments = segable.GetSegments();
                                if (segments != null)
                                {
                                    foreach (Segment3D segment in segments)
                                    {
                                        if (segment != null)
                                        {
                                            result.Add((T)(object)segment);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return;
                }

                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    List<IClosedPlanar3D> edge3Ds = face3DObject.Face3D.GetEdge3Ds();
                    if (edge3Ds != null)
                    {
                        foreach (IClosedPlanar3D edge in edge3Ds)
                        {
                            if (edge is ISegmentable3D segable)
                            {
                                List<Segment3D> segments = segable.GetSegments();
                                if (segments != null)
                                {
                                    foreach (Segment3D segment in segments)
                                    {
                                        if (segment != null)
                                        {
                                            result.Add((T)(object)segment);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return;
                }

                if (value is Shell shell && shell.Face3Ds != null)
                {
                    foreach (Face3D face in shell.Face3Ds)
                    {
                        if (face == null)
                        {
                            continue;
                        }

                        List<IClosedPlanar3D> edge3Ds = face.GetEdge3Ds();
                        if (edge3Ds != null)
                        {
                            foreach (IClosedPlanar3D edge in edge3Ds)
                            {
                                if (edge is ISegmentable3D segable)
                                {
                                    List<Segment3D> segments = segable.GetSegments();
                                    if (segments != null)
                                    {
                                        foreach (Segment3D segment in segments)
                                        {
                                            if (segment != null)
                                            {
                                                result.Add((T)(object)segment);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return;
                }
            }

            if (targetType == typeof(Plane) || typeof(Plane).IsAssignableFrom(targetType))
            {
                if (value is Face3D face3D)
                {
                    Plane plane = face3D.GetPlane();
                    if (plane != null)
                    {
                        result.Add((T)(object)plane);
                        return;
                    }
                }

                if (value is IPlanar3D planar3D)
                {
                    Plane plane = planar3D.GetPlane();
                    if (plane != null)
                    {
                        result.Add((T)(object)plane);
                        return;
                    }
                }

                if (value is IFace3DObject face3DObject && face3DObject.Face3D != null)
                {
                    Plane plane = face3DObject.Face3D.GetPlane();
                    if (plane != null)
                    {
                        result.Add((T)(object)plane);
                        return;
                    }
                }
            }

            if (targetType == typeof(Point3D) || typeof(Point3D).IsAssignableFrom(targetType))
            {
                if (value is Spatial.Plane plane)
                {
                    Point3D origin = plane.Origin;
                    if (origin != null)
                    {
                        result.Add((T)(object)origin);
                        return;
                    }
                }
            }
        }
    }
}
