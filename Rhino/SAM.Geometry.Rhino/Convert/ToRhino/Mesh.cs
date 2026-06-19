// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Rhino.Geometry;
using System.Collections.Generic;

namespace SAM.Geometry.Rhino
{
    public static partial class Convert
    {
        public static Mesh ToRhino(this Spatial.Mesh3D mesh3D)
        {
            if (mesh3D == null)
            {
                return null;
            }

            Mesh result = new Mesh();

            // Build the Rhino mesh directly from the SAM mesh's shared vertices and face
            // indices. The previous approach rebuilt each triangle from its three edge lines
            // with Mesh.CreateFromLines at a distance tolerance and silently dropped any
            // triangle it could not reconstruct (slivers, near-coincident edges), which lost
            // faces on bake/preview. Indexing the stored topology directly is exact - every
            // triangle becomes one face and no face is dropped.
            List<Spatial.Point3D> point3Ds = mesh3D.GetPoints();
            if (point3Ds == null || point3Ds.Count == 0)
            {
                return result;
            }

            foreach (Spatial.Point3D point3D in point3Ds)
            {
                if (point3D == null)
                {
                    return result;
                }

                result.Vertices.Add(point3D.X, point3D.Y, point3D.Z);
            }

            int trianglesCount = mesh3D.TrianglesCount;
            for (int i = 0; i < trianglesCount; i++)
            {
                System.Tuple<int, int, int> indexes = mesh3D.GetTriangleIndexes(i);
                if (indexes == null)
                {
                    continue;
                }

                result.Faces.AddFace(indexes.Item1, indexes.Item2, indexes.Item3);
            }

            result.Normals.ComputeNormals();
            result.Compact();

            return result;
        }

        public static Mesh ToRhino_Mesh(this Spatial.Face3D face3D)
        {
            Brep brep = face3D?.ToRhino_Brep();
            if (brep == null)
            {
                return null;
            }

            Mesh[] meshes = Mesh.CreateFromBrep(brep, ActiveSetting.GetMeshingParameters());
            if (meshes == null || meshes.Length == 0)
            {
                return null;
            }

            Mesh mesh = null;
            if (meshes.Length == 1)
            {
                mesh = meshes[0];
            }
            else
            {
                mesh = new Mesh();
                mesh.Append(meshes);
                mesh.Normals.ComputeNormals();
            }

            return mesh;
        }

        public static Mesh ToRhino_Mesh(this Spatial.Shell shell)
        {
            Brep brep = shell?.ToRhino();
            if (brep == null)
            {
                return null;
            }

            Mesh[] meshes = Mesh.CreateFromBrep(brep, ActiveSetting.GetMeshingParameters());
            if (meshes == null || meshes.Length == 0)
            {
                return null;
            }

            Mesh mesh = null;
            if (meshes.Length == 1)
            {
                mesh = meshes[0];
            }
            else
            {
                mesh = new Mesh();
                mesh.Append(meshes);
                mesh.Normals.ComputeNormals();
            }

            return mesh;
        }
    }
}
