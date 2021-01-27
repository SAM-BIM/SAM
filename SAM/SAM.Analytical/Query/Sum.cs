﻿using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        public static double Sum(this AdjacencyCluster adjacencyCluster, Zone zone, string name)
        {
            if (adjacencyCluster == null || zone == null || string.IsNullOrWhiteSpace(name))
                return double.NaN;

            List<Space> spaces = adjacencyCluster.GetSpaces(zone);
            if (spaces == null)
                return double.NaN;

            double result = 0;
            foreach(Space space in spaces)
            {
                if (!space.TryGetValue(name, out double value) || double.IsNaN(value))
                    continue;

                result += value;
            }
            return result;
        }

        public static double Sum(this AdjacencyCluster adjacencyCluster, Zone zone, SpaceParameter spaceParameter)
        {
            if (adjacencyCluster == null || zone == null)
                return double.NaN;

            List<Space> spaces = adjacencyCluster.GetSpaces(zone);
            if (spaces == null)
                return double.NaN;

            double result = 0;
            foreach (Space space in spaces)
            {
                if (!space.TryGetValue(spaceParameter, out double value) || double.IsNaN(value))
                    continue;

                result += value;
            }
            return result;
        }
    }
}