﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.Geometry.Planar
{
    public interface ISegmentable2D
    {
        List<Segment2D> GetSegments();
        List<Point2D> GetPoints();
    }
}
