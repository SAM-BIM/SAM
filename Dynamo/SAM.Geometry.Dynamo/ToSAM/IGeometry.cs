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
        public static IGeometry ToSAM(this Autodesk.DesignScript.Geometry.Geometry geometry)
        {
            return Convert.ToSAM(geometry as dynamic);
        }
    }
}
