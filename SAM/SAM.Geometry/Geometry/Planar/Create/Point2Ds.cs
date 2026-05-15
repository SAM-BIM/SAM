// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Geometry.Planar
{
    public static partial class Create
    {
        public static List<Point2D> Point2Ds(this JsonArray jsonArray)
        {
            if (jsonArray == null)
                return null;

            List<Point2D> result = new List<Point2D>();

            foreach (JsonNode jsonNode in jsonArray)
                result.Add(new Point2D(jsonNode as JsonObject));

            return result;
        }

        public static List<Point2D> Point2Ds(this BoundingBox2D boundingBox2D, int count)
        {
            if (count == -1)
                return null;

            return Point2Ds(boundingBox2D.Min.X, boundingBox2D.Min.Y, boundingBox2D.Max.X, boundingBox2D.Max.Y, count);
        }

        public static List<Point2D> Point2Ds(double x_min, double y_min, double x_max, double y_max, int count)
        {
            if (count == -1)
                return null;

            Random random = new Random();

            List<Point2D> result = new List<Point2D>();
            for (int i = 0; i < count; i++)
            {
                double x = Core.Query.NextDouble(random, x_min, x_max);
                double y = Core.Query.NextDouble(random, y_min, y_max);

                result.Add(new Point2D(x, y));
            }

            return result;
        }

        public static List<Point2D> Point2Ds(params double[] values)
        {
            if (values == null)
            {
                return null;
            }

            int length = values.Length;
            if (length == 0 || length % 2 != 0)
            {
                return null;
            }

            List<Point2D> result = new List<Point2D>();
            for (int i = 0; i < length; i = i + 2)
            {
                result.Add(new Point2D(values[i], values[i + 1]));
            }

            return result;
        }
    }
}
