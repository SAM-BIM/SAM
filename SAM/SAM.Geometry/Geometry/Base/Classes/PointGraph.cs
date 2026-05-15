// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Json;
using QuickGraph.Algorithms.Observers;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace SAM.Geometry
{
    public abstract class PointGraph<X, T> : IJSAMObject where T : IJSAMObject where X : IPoint
    {
        private double tolerance = Core.Tolerance.Distance;
        private QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>> bidirectionalGraph;

        public PointGraph()
        {

        }

        public PointGraph(double tolerance)
        {
            this.tolerance = tolerance;
        }

        public PointGraph(PointGraph<X, T> pointGraph)
        {
            if (pointGraph != null)
            {
                tolerance = pointGraph.tolerance;

                if (pointGraph.bidirectionalGraph != null)
                {
                    bidirectionalGraph = new QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>>();
                    pointGraph.bidirectionalGraph.Vertices?.ToList().ForEach(x => bidirectionalGraph.AddVertex(Core.Query.Clone(x)));
                    pointGraph.bidirectionalGraph.Edges?.ToList().ForEach(x => bidirectionalGraph.AddEdge(Core.Query.Clone(x)));
                }
            }
        }
        public PointGraph(System.Text.Json.Nodes.JsonObject jsonObject)

        {

            FromJsonObject(jsonObject);

        }

        private X Update(X point)
        {
            if (point == null)
            {
                return default;
            }

            X result = Find(point);
            if (result != null)
            {
                return result;
            }

            if (bidirectionalGraph == null)
            {
                bidirectionalGraph = new QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>>();
            }

            result = Core.Query.Clone(point);

            bidirectionalGraph.AddVertex(result);

            return result;
        }

        protected abstract double Weight(PointGraphEdge<X, T> PointGraphEdge);

        public bool Add(T jSAMObject, X source, X target)
        {
            if (bidirectionalGraph == null)
            {
                bidirectionalGraph = new QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>>();
            }

            X source_Temp = source;
            if (source == null)
            {
                return false;
            }

            X target_Temp = target;
            if (target == null)
            {
                return false;
            }

            source_Temp = Update(source_Temp);
            target_Temp = Update(target_Temp);


            return bidirectionalGraph.AddEdge(new PointGraphEdge<X, T>(jSAMObject, target_Temp, source_Temp));
        }

        public bool Add(T jSAMObject, Func<T, X> sourceFunc, Func<T, X> targetFunc)
        {
            if (sourceFunc == null || targetFunc == null)
            {
                return false;
            }

            X source = sourceFunc.Invoke(jSAMObject);
            if (source == null)
            {
                return false;
            }

            X target = targetFunc.Invoke(jSAMObject);
            if (target == null)
            {
                return false;
            }

            return Add(jSAMObject, source, target);
        }

        public List<bool> AddRange(IEnumerable<T> jSAMObjects, Func<T, X> sourceFunc, Func<T, X> targetFunc)
        {
            if (jSAMObjects == null)
            {
                return null;
            }

            List<bool> result = new List<bool>();
            foreach (T jSAMObject in jSAMObjects)
            {
                result.Add(Add(jSAMObject, sourceFunc, targetFunc));
            }

            return result;
        }

        public List<Z> GetJSAMObjects<Z>(X point) where Z : T
        {
            X point_Temp = Find(point);
            if (point_Temp == null)
            {
                return null;
            }

            if (!bidirectionalGraph.TryGetOutEdges(point_Temp, out IEnumerable<PointGraphEdge<X, T>> pointGraphEdges) || pointGraphEdges == null)
            {
                return null;
            }

            return pointGraphEdges.ToList().FindAll(x => x.JSAMObject is Z).ConvertAll(x => (Z)x.JSAMObject);
        }

        public List<T> JSAMObjects
        {
            get
            {
                return bidirectionalGraph?.Edges?.ToList().ConvertAll(x => x.JSAMObject);
            }
        }

        public double Tolerance
        {
            get
            {
                return tolerance;
            }
        }

        public List<X> GetPoints()
        {
            return bidirectionalGraph?.Vertices?.ToList().ConvertAll(x => Core.Query.Clone(x));
        }

        public List<X> GetPoints(int connectionsCount)
        {
            if (bidirectionalGraph == null || bidirectionalGraph.Vertices == null || bidirectionalGraph.Edges == null)
            {
                return null;
            }

            List<X> result = new List<X>();
            foreach (X point in bidirectionalGraph.Vertices)
            {
                if (!TryGetPointGraphEdges(point, out List<PointGraphEdge<X, T>> pointGraphEdges) || pointGraphEdges == null)
                {
                    continue;
                }

                if (pointGraphEdges.Count == connectionsCount)
                {
                    result.Add(Core.Query.Clone(point));
                }
            }

            return result;
        }

        private bool TryGetPointGraphEdges(X point, out List<PointGraphEdge<X, T>> pointGraphEdges)
        {
            pointGraphEdges = null;
            if (point == null || bidirectionalGraph == null || bidirectionalGraph.Edges == null)
            {
                return false;
            }

            pointGraphEdges = new List<PointGraphEdge<X, T>>();

            IEnumerable<PointGraphEdge<X, T>> pointGraphEdges_Temp = null;
            if (bidirectionalGraph.TryGetInEdges(point, out pointGraphEdges_Temp) && pointGraphEdges_Temp != null)
            {
                pointGraphEdges.AddRange(pointGraphEdges_Temp);
            }

            if (bidirectionalGraph.TryGetOutEdges(point, out pointGraphEdges_Temp) && pointGraphEdges_Temp != null)
            {
                pointGraphEdges.AddRange(pointGraphEdges_Temp);
            }

            return pointGraphEdges.Count > 0;
        }

        public List<T> GetConnectedJSAMObjects(X point)
        {
            X point_Temp = Find(point);
            if (point_Temp == null)
            {
                return null;
            }

            List<PointGraph<X, T>> pointGraphs = Split<PointGraph<X, T>>();
            if (pointGraphs == null || pointGraphs.Count == 0)
            {
                return null;
            }

            PointGraph<X, T> pointGraph = pointGraphs.Find(x => x.Find(point_Temp) != null);
            if (pointGraph == null)
            {
                return null;
            }

            return pointGraph.JSAMObjects;
        }

        public List<T> GetConnectedJSAMObjects(X start, X end)
        {
            if (bidirectionalGraph == null || bidirectionalGraph.Edges == null || bidirectionalGraph.Vertices == null || bidirectionalGraph.Vertices.Count() == 0 || bidirectionalGraph.Edges.Count() == 0)
            {
                return null;
            }

            X start_Temp = Find(start);
            if (start_Temp == null)
            {
                return null;
            }

            X end_Temp = Find(end);
            if (end_Temp == null)
            {
                return null;
            }

            QuickGraph.AdjacencyGraph<X, PointGraphEdge<X, T>> adjacencyGraph = GetAdjacencyGraph(true);
            if (adjacencyGraph == null)
            {
                return null;
            }

            QuickGraph.Algorithms.ShortestPath.FloydWarshallAllShortestPathAlgorithm<X, PointGraphEdge<X, T>> floydWarshallAllShortestPathAlgorithm = new QuickGraph.Algorithms.ShortestPath.FloydWarshallAllShortestPathAlgorithm<X, PointGraphEdge<X, T>>(adjacencyGraph, x => Weight(x));
            floydWarshallAllShortestPathAlgorithm.Compute();
            if (!floydWarshallAllShortestPathAlgorithm.TryGetPath(start, end, out IEnumerable<PointGraphEdge<X, T>> pointGraphEdges) || pointGraphEdges == null)
            {
                return null;
            }

            return pointGraphEdges.ToList().ConvertAll(x => x.JSAMObject);
        }

        public List<T> GetConnectedJSAMObjectsShortestPath(X point)
        {
            if (bidirectionalGraph == null || bidirectionalGraph.Edges == null || bidirectionalGraph.Vertices == null || bidirectionalGraph.Vertices.Count() == 0 || bidirectionalGraph.Edges.Count() == 0)
            {
                return null;
            }

            X point_Temp = Find(point);
            if (point_Temp == null)
            {
                return null;
            }

            QuickGraph.AdjacencyGraph<X, PointGraphEdge<X, T>> adjacencyGraph = GetAdjacencyGraph(true);
            if (adjacencyGraph == null)
            {
                return null;
            }

            QuickGraph.Algorithms.ShortestPath.DijkstraShortestPathAlgorithm<X, PointGraphEdge<X, T>> dijkstraShortestPathAlgorithm = new QuickGraph.Algorithms.ShortestPath.DijkstraShortestPathAlgorithm<X, PointGraphEdge<X, T>>(adjacencyGraph, x => Weight(x));

            //VertexDistanceRecorderObserver<Point3D, point3DGraphEdge<T>> vertexDistanceRecorderObserver = new VertexDistanceRecorderObserver<Point3D, point3DGraphEdge<T>>(x => x.Source.Distance(x.Target));
            //vertexDistanceRecorderObserver.Attach(dijkstraShortestPathAlgorithm);

            //VertexPredecessorRecorderObserver<Point3D, point3DGraphEdge<T>> vertexPredecessorRecorderObserver = new VertexPredecessorRecorderObserver<Point3D, point3DGraphEdge<T>>();
            //vertexPredecessorRecorderObserver.Attach(dijkstraShortestPathAlgorithm);

            VertexPredecessorPathRecorderObserver<X, PointGraphEdge<X, T>> vertexPredecessorPathRecorderObserver = new VertexPredecessorPathRecorderObserver<X, PointGraphEdge<X, T>>();
            vertexPredecessorPathRecorderObserver.Attach(dijkstraShortestPathAlgorithm);

            dijkstraShortestPathAlgorithm.Compute(point_Temp);

            if (vertexPredecessorPathRecorderObserver.VertexPredecessors == null || vertexPredecessorPathRecorderObserver.VertexPredecessors.Count == 0)
            {
                return null;
            }

            return vertexPredecessorPathRecorderObserver.VertexPredecessors.Values.ToList().ConvertAll(x => x.JSAMObject);
        }

        public QuickGraph.UndirectedGraph<X, PointGraphEdge<X, T>> GetUndirectedGraph()
        {
            if (bidirectionalGraph == null)
            {
                return null;
            }

            QuickGraph.UndirectedGraph<X, PointGraphEdge<X, T>> result = new QuickGraph.UndirectedGraph<X, PointGraphEdge<X, T>>();
            if (bidirectionalGraph.Edges != null && bidirectionalGraph.Edges.Count() != 0)
            {
                result.AddVerticesAndEdgeRange(bidirectionalGraph.Edges);
            }

            return result;
        }

        public QuickGraph.AdjacencyGraph<X, PointGraphEdge<X, T>> GetAdjacencyGraph(bool parallelEdges = true)
        {
            if (bidirectionalGraph == null || bidirectionalGraph.Edges == null)
            {
                return null;
            }

            QuickGraph.AdjacencyGraph<X, PointGraphEdge<X, T>> result = new QuickGraph.AdjacencyGraph<X, PointGraphEdge<X, T>>();
            foreach (PointGraphEdge<X, T> point3DGraphEdge in bidirectionalGraph.Edges)
            {
                result.AddVerticesAndEdge(point3DGraphEdge);
                if (parallelEdges)
                {
                    result.AddVerticesAndEdge(new PointGraphEdge<X, T>(point3DGraphEdge.JSAMObject, point3DGraphEdge.Target, point3DGraphEdge.Source));
                }
            }

            return result;
        }

        public List<Z> Split<Z>() where Z : PointGraph<X, T>
        {
            if (bidirectionalGraph == null || bidirectionalGraph.Edges == null || bidirectionalGraph.Edges.Count() <= 1 || bidirectionalGraph.Vertices == null || bidirectionalGraph.Vertices.Count() <= 2)
            {
                return new List<Z> { Core.Query.Clone(this) as Z };
            }

            QuickGraph.UndirectedGraph<X, PointGraphEdge<X, T>> undirectedGraph = GetUndirectedGraph();
            if (undirectedGraph == null)
            {
                return new List<Z> { Core.Query.Clone(this) as Z };
            }

            QuickGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<X, PointGraphEdge<X, T>> connectedComponentsAlgorithm = new QuickGraph.Algorithms.ConnectedComponents.ConnectedComponentsAlgorithm<X, PointGraphEdge<X, T>>(undirectedGraph);
            connectedComponentsAlgorithm.Compute();

            int count = connectedComponentsAlgorithm.ComponentCount;
            if (count <= 1)
            {
                return new List<Z> { Core.Query.Clone(this) as Z };
            }

            IDictionary<X, int> dictionary_Temp = connectedComponentsAlgorithm.Components;
            if (dictionary_Temp == null || dictionary_Temp.Count == 0)
            {
                return new List<Z> { Core.Query.Clone(this) as Z };
            }


            Dictionary<int, List<PointGraphEdge<X, T>>> dictionary = new Dictionary<int, List<PointGraphEdge<X, T>>>();
            foreach (KeyValuePair<X, int> keyValuePair in dictionary_Temp)
            {
                if (!dictionary.TryGetValue(keyValuePair.Value, out List<PointGraphEdge<X, T>> pointGraphEdges) || pointGraphEdges == null)
                {
                    pointGraphEdges = new List<PointGraphEdge<X, T>>();
                    dictionary[keyValuePair.Value] = pointGraphEdges;
                }

                if (TryGetPointGraphEdges(keyValuePair.Key, out List<PointGraphEdge<X, T>> pointGraphEdges_Point) && pointGraphEdges != null)
                {
                    foreach (PointGraphEdge<X, T> pointGraphEdge in pointGraphEdges_Point)
                    {
                        if (!pointGraphEdges.Contains(pointGraphEdge))
                        {
                            pointGraphEdges.Add(pointGraphEdge);
                        }
                    }
                }
            }

            List<Z> result = new List<Z>();
            foreach (List<PointGraphEdge<X, T>> point3DGraphEdges_Temp in dictionary.Values)
            {
                Z pointGraph = Core.Query.Clone(this) as Z;
                pointGraph.bidirectionalGraph = new QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>>();
                pointGraph.bidirectionalGraph.AddVerticesAndEdgeRange(point3DGraphEdges_Temp);

                result.Add(pointGraph);
            }

            return result;
        }

        public abstract X Find(X point);

        public bool Contains(X point)
        {
            return Find(point) != null;
        }
        public virtual bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject == null)
            {
                return false;
            }

            if (jsonObject.ContainsKey("Tolerance"))
            {
                tolerance = jsonObject["Tolerance"]?.GetValue<double>() ?? 0;
            }

            if (jsonObject["BidirectionalGraph"] is JsonObject bidirectionalGraphJson)
            {
                bidirectionalGraph = new QuickGraph.BidirectionalGraph<X, PointGraphEdge<X, T>>();

                if (bidirectionalGraphJson["Vertices"] is JsonArray verticesArray)
                {
                    foreach (JsonNode node in verticesArray)
                    {
                        if (node is JsonObject vertexJson)
                        {
                            X point = Core.Query.IJSAMObject<X>(vertexJson as JsonObject);
                            bidirectionalGraph.AddVertex(point);
                        }
                    }
                }

                if (bidirectionalGraphJson["PointGraphEdges"] is JsonArray edgesArray)
                {
                    foreach (JsonNode node in edgesArray)
                    {
                        if (node is JsonObject edgeJson)
                        {
                            PointGraphEdge<X, T> pointGraphEdge = new PointGraphEdge<X, T>((JsonObject)edgeJson.DeepClone());
                            if (pointGraphEdge != null)
                            {
                                bidirectionalGraph.AddEdge(pointGraphEdge);
                            }
                        }
                    }
                }
            }

            return true;
        }
        public virtual JsonObject ToJsonObject()
        {
            JsonObject result = new JsonObject
            {
                ["_type"] = Core.Query.FullTypeName(this),
                ["Tolerance"] = tolerance
            };

            if (bidirectionalGraph != null)
            {
                JsonObject bidirectionalGraphJson = new JsonObject();

                if (bidirectionalGraph.Vertices != null)
                {
                    JsonArray verticesArray = new JsonArray();
                    foreach (X point in bidirectionalGraph.Vertices)
                    {
                        if (point?.ToJsonObject() is JsonObject vertexJson)
                        {
                            verticesArray.Add(vertexJson.DeepClone());
                        }
                    }

                    bidirectionalGraphJson["Vertices"] = verticesArray;
                }

                if (bidirectionalGraph.Edges != null)
                {
                    JsonArray edgesArray = new JsonArray();
                    foreach (PointGraphEdge<X, T> pointGraphEdge in bidirectionalGraph.Edges)
                    {
                        if (pointGraphEdge?.ToJsonObject() is JsonObject edgeJson)
                        {
                            edgesArray.Add(edgeJson.DeepClone());
                        }
                    }

                    bidirectionalGraphJson["PointGraphEdges"] = edgesArray;
                }

                result["BidirectionalGraph"] = bidirectionalGraphJson;
            }

            return result;
        }
    }
}
