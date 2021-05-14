﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Geometry.Spatial
{
    public static partial class Query
    {
        public static List<Shell> Union(this Shell shell_1, Shell shell_2, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            if (shell_1 == null || shell_2 == null)
            {
                return null;
            }

            Shell shell_1_Temp = new Shell(shell_1);
            Shell shell_2_Temp = new Shell(shell_2);


            if (!shell_1.GetBoundingBox().InRange(shell_2.GetBoundingBox(), tolerance_Distance))
            {
                return new List<Shell>() { shell_1_Temp, shell_2_Temp };
            }

            shell_1_Temp.Split(shell_2);
            shell_2_Temp.Split(shell_1);

            List<Tuple<BoundingBox3D, Face3D>> boundiries_1 = shell_1_Temp.Boundaries;
            List<Tuple<BoundingBox3D, Face3D>> boundiries_2 = shell_2_Temp.Boundaries;

            bool union = false;
            for (int i = boundiries_1.Count - 1; i >= 0; i--)
            {
                Face3D face3D = boundiries_1[i].Item2;
                Point3D point3D = face3D.InternalPoint3D(tolerance_Distance);

                List<Tuple<BoundingBox3D, Face3D>> boundaries_On = boundiries_2.FindAll(x => x.Item1.InRange(point3D, tolerance_Distance) && x.Item2.On(point3D, tolerance_Distance));
                if (boundaries_On != null && boundaries_On.Count != 0)
                {
                    boundaries_On.ForEach(x => boundiries_2.Remove(x));
                    union = true;
                }
                else if (shell_2.Inside(point3D, silverSpacing, tolerance_Distance))
                {
                    boundiries_1.RemoveAt(i);
                    union = true;
                }
            }

            for (int i = boundiries_2.Count - 1; i >= 0; i--)
            {
                Face3D face3D = boundiries_2[i].Item2;
                Point3D point3D = face3D.InternalPoint3D(tolerance_Distance);

                if (shell_1.Inside(point3D, silverSpacing, tolerance_Distance))
                {
                    boundiries_2.RemoveAt(i);
                    union = true;
                }
            }

            if (!union)
            {
                return new List<Shell>() { shell_1_Temp, shell_2_Temp };
            }

            List<Face3D> face3Ds = boundiries_1.ConvertAll(x => x.Item2);
            face3Ds.AddRange(boundiries_2.ConvertAll(x => x.Item2));

            return new List<Shell>() { new Shell(face3Ds) };

        }

        public static List<Shell> Union(this IEnumerable<Shell> shells, double silverSpacing = Core.Tolerance.MacroDistance, double tolerance_Angle = Core.Tolerance.Angle, double tolerance_Distance = Core.Tolerance.Distance)
        {
            if (shells == null)
            {
                return null;
            }

            List<Shell> shells_Temp = new List<Shell>(shells);
            if (shells_Temp.Count == 0)
            {
                return new List<Shell>();
            }

            List<Shell> result = new List<Shell>();

            if (shells_Temp.Count == 1)
            {
                result.Add(new Shell(shells_Temp[0]));
                return result;
            }

            while (shells_Temp.Count > 0)
            {
                Shell shell = shells_Temp[0];
                shells_Temp.RemoveAt(0);

                List<Shell> shells_Union = null;
                foreach (Shell shell_Result in result)
                {
                    shells_Union = shell_Result.Union(shell);
                    if (shells_Union != null && shells_Union.Count == 1)
                    {
                        result.Remove(shell_Result);
                        break;
                    }
                }

                if (shells_Union != null && shells_Union.Count == 1)
                {
                    result.Add(shells_Union[0]);
                }
                else
                {
                    result.Add(new Shell(shell));
                }
            }

            if(result.Count != shells.Count())
            {
                result = Union(result, silverSpacing, tolerance_Angle, tolerance_Distance);
            }

            return result;
        }
    }
}