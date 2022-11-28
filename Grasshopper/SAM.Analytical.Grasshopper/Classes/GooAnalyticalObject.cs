﻿using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SAM.Analytical.Grasshopper.Properties;
using SAM.Core.Grasshopper;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Grasshopper
{
    public class GooAnalyticalObject : GooJSAMObject<IAnalyticalObject>
    {
        public GooAnalyticalObject()
            : base()
        {
        }

        public GooAnalyticalObject(IAnalyticalObject analyticalObject)
            : base(analyticalObject)
        {
        }

        public override IGH_Goo Duplicate()
        {
            return new GooAnalyticalObject(Value);
        }

        public override bool CastFrom(object source)
        {
            return base.CastFrom(source);
        }

        public override bool CastTo<Y>(ref Y target)
        {
            if (Value == null)
                return false;

            if (typeof(Y).IsAssignableFrom(typeof(GH_Mesh)))
            {
                if (Value is AdjacencyCluster)
                {
                    target = (Y)(object)((AdjacencyCluster)Value).ToGrasshopper_Mesh();
                    return true;
                }
                else if (Value is AnalyticalModel)
                {
                    target = (Y)(object)((AnalyticalModel)Value).ToGrasshopper_Mesh();
                    return true;
                }
                else if (Value is Panel)
                {
                    target = (Y)(object)((Panel)Value).ToGrasshopper_Mesh();
                    return true;
                }
            }
            else if (typeof(Y).IsAssignableFrom(typeof(GH_Brep)))
            {
                if (Value is Panel)
                {
                    target = (Y)(object)(Geometry.Grasshopper.Convert.ToGrasshopper_Brep(((Panel)Value).GetFace3D()));
                    return true;
                }
            }
            return base.CastTo(ref target);
        }
    }

    public class GooAnalyticalObjectParam : GH_PersistentParam<GooAnalyticalObject>
    {
        public override Guid ComponentGuid => new Guid("e06eb117-d4ad-4f3d-9541-b8d121a13a7d");

        protected override System.Drawing.Bitmap Icon => Resources.SAM_Small;

        public GooAnalyticalObjectParam()
            : base(typeof(IAnalyticalObject).Name, typeof(IAnalyticalObject).Name, typeof(IAnalyticalObject).FullName.Replace(".", " "), "Params", "SAM")
        {
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GooAnalyticalObject> values)
        {
            throw new NotImplementedException();
        }

        protected override GH_GetterResult Prompt_Singular(ref GooAnalyticalObject value)
        {
            throw new NotImplementedException();
        }
    }
}