﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.DesignScript.Runtime;

namespace SAM.Geometry.Dynamo
{
    public static partial class Convert
    {
        [IsVisibleInDynamoLibrary(false)]
        public static Autodesk.DesignScript.Geometry.Point ToDynamo(this Spatial.Point3D point3D)
        {
            return Autodesk.DesignScript.Geometry.Point.ByCoordinates(point3D.X, point3D.Y, point3D.Z);
        }
    }
}
