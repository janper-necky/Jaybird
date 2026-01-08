using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

public class Component_Dijkstra : GH_Component
{
    static readonly string ComponentName = "Dijkstra";

    public Component_Dijkstra()
        : base(
            ComponentName,
            JaybirdInfo.ExtractInitials(ComponentName),
            "Find shortest path from the start point to the end point using the Dijkstra algorithm.",
            JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("1076e0d4-279c-427c-a79c-43abbf0de560");

    private const int InParam_Lines = 0;
    private const int InParam_StartPoint = 1;
    private const int InParam_EndPoint = 2;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddLineParameter("Lines", "L", "Lines representing roads", GH_ParamAccess.list);
        pManager.AddPointParameter(
            "Start Point",
            "S",
            "Starting point for the path",
            GH_ParamAccess.item
        );
        pManager.AddPointParameter("End Point", "E", "End point for the path", GH_ParamAccess.item);
    }

    private const int OutParam_Nodes = 0;
    private const int OutParam_Edges = 1;
    private const int OutParam_VisitedEdges = 2;
    private const int OutParam_PathPolyline = 3;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Nodes", "N", "Nodes in the graph", GH_ParamAccess.list);
        pManager.AddLineParameter("Edges", "E", "All edges in the graph", GH_ParamAccess.list);
        pManager.AddLineParameter(
            "Visited Edges",
            "VE",
            "Edges visited by the algorithm",
            GH_ParamAccess.list
        );
        pManager.AddCurveParameter(
            "Path",
            "P",
            "Path from start to end as polyline",
            GH_ParamAccess.item
        );
    }

    internal struct Edge
    {
        internal int ToNodeIdx;
        internal double Weight;

        public override readonly bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            var other = (Edge)obj;

            return other.ToNodeIdx == ToNodeIdx && other.Weight == Weight;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(ToNodeIdx, Weight);
        }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var lines = new List<Line>();
        DA.GetDataList(InParam_Lines, lines);

        // For fast comparison
        var nodePointToIndex = new Dictionary<Point3d, int>();
        var nodePoints = new List<Point3d>();
        // Which edges lead out of a node
        var nodeIdxToEdges = new Dictionary<int, HashSet<Edge>>();

        foreach (var line in lines)
        {
            var nodeAPointRounded = RoundPoint(line.From, JaybirdInfo.Decimals);
            if (!nodePointToIndex.TryGetValue(nodeAPointRounded, out var nodeAIdx))
            {
                nodeAIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeAPointRounded, nodeAIdx);
                nodePoints.Add(nodeAPointRounded);
            }

            var nodeBPointRounded = RoundPoint(line.To, JaybirdInfo.Decimals);
            if (!nodePointToIndex.TryGetValue(nodeBPointRounded, out var nodeBIdx))
            {
                nodeBIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeBPointRounded, nodeBIdx);
                nodePoints.Add(nodeBPointRounded);
            }

            if (nodeAIdx == nodeBIdx)
            {
                continue;
            }

            var length = line.Length;

            var edgeAB = new Edge { ToNodeIdx = nodeBIdx, Weight = length };
            if (nodeIdxToEdges.TryGetValue(nodeAIdx, out var edgesFromA))
            {
                edgesFromA.Add(edgeAB);
            }
            else
            {
                edgesFromA = new HashSet<Edge>() { edgeAB };
                nodeIdxToEdges.Add(nodeAIdx, edgesFromA);
            }

            var edgeBA = new Edge { ToNodeIdx = nodeAIdx, Weight = length };
            if (nodeIdxToEdges.TryGetValue(nodeBIdx, out var edgesFromB))
            {
                edgesFromB.Add(edgeBA);
            }
            else
            {
                edgesFromB = new HashSet<Edge>() { edgeBA };
                nodeIdxToEdges.Add(nodeBIdx, edgesFromB);
            }
        }

        DA.SetDataList(OutParam_Nodes, nodePoints);

        var edgeLines = new List<Line>();
        var usedEdges = new HashSet<(int, int)>();
        foreach (var (nodeAIdx, edges) in nodeIdxToEdges)
        {
            foreach (var edge in edges)
            {
                var nodeBIdx = edge.ToNodeIdx;
                var orderedEdge = (Math.Min(nodeAIdx, nodeBIdx), Math.Max(nodeAIdx, nodeBIdx));
                if (usedEdges.Contains(orderedEdge))
                {
                    continue;
                }
                usedEdges.Add(orderedEdge);
                var line = new Line(nodePoints[nodeAIdx], nodePoints[nodeBIdx]);
                edgeLines.Add(line);
            }
        }
        DA.SetDataList(OutParam_Edges, edgeLines);

        var startPoint = new Point3d();
        if (!DA.GetData(InParam_StartPoint, ref startPoint))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect start point.");
            return;
        }

        var endPoint = new Point3d();
        if (!DA.GetData(InParam_EndPoint, ref endPoint))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to collect end point.");
            return;
        }

        var startNodeIdx = -1;
        var startPointClosestDistance = double.MaxValue;
        var endNodeIdx = -1;
        var endPointClosestDistance = double.MaxValue;

        for (var nodeIdx = 0; nodeIdx < nodePoints.Count; nodeIdx++)
        {
            var distanceToStartPoint = nodePoints[nodeIdx].DistanceTo(startPoint);
            if (distanceToStartPoint < startPointClosestDistance)
            {
                startPointClosestDistance = distanceToStartPoint;
                startNodeIdx = nodeIdx;
            }

            var distanceToEndPoint = nodePoints[nodeIdx].DistanceTo(endPoint);
            if (distanceToEndPoint < endPointClosestDistance)
            {
                endPointClosestDistance = distanceToEndPoint;
                endNodeIdx = nodeIdx;
            }
        }

        var nodeCosts = new double[nodePoints.Count];
        Array.Fill(nodeCosts, double.MaxValue);
        nodeCosts[startNodeIdx] = 0.0;

        var nodePreviousIdx = new int[nodePoints.Count];
        Array.Fill(nodePreviousIdx, -1);

        var priorityQueue = new PriorityQueue<int, double>();
        priorityQueue.Enqueue(startNodeIdx, nodeCosts[startNodeIdx]);

        var visitedEdgeLines = new List<Line>();

        bool foundTheEnd = false;

        while (priorityQueue.TryDequeue(out var currentNodeIdx, out var currentCost))
        {
            if (currentCost > nodeCosts[currentNodeIdx])
            {
                // This node has been already visited and received a lower cost
                // than the queued. This is a stall queue entry and can be
                // removed.
                continue;
            }

            if (currentNodeIdx == endNodeIdx)
            {
                foundTheEnd = true;
                break;
            }

            if (!nodeIdxToEdges.TryGetValue(currentNodeIdx, out var currentEdges))
            {
                // Dead end
                continue;
            }

            foreach (var edge in currentEdges)
            {
                var neighborNodeIdx = edge.ToNodeIdx;

                visitedEdgeLines.Add(
                    new Line(nodePoints[currentNodeIdx], nodePoints[neighborNodeIdx])
                );

                var neighborCost = nodeCosts[neighborNodeIdx];
                var newNeighborCost = currentCost + edge.Weight;
                if (newNeighborCost >= neighborCost)
                {
                    continue;
                }

                nodeCosts[neighborNodeIdx] = newNeighborCost;
                priorityQueue.Enqueue(neighborNodeIdx, newNeighborCost);

                nodePreviousIdx[neighborNodeIdx] = currentNodeIdx;
            }
        }

        DA.SetDataList(OutParam_VisitedEdges, visitedEdgeLines);

        if (!foundTheEnd)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The end point may not be reachable.");
            return;
        }

        AddRuntimeMessage(
            GH_RuntimeMessageLevel.Remark,
            "Found the shortest path to the end point."
        );

        var path = new List<Point3d>();
        var pathNodeIdx = endNodeIdx;
        path.Add(nodePoints[pathNodeIdx]);

        while (pathNodeIdx != startNodeIdx)
        {
            var previousNodeIdx = nodePreviousIdx[pathNodeIdx];
            pathNodeIdx = previousNodeIdx;
            path.Add(nodePoints[pathNodeIdx]);
        }

        path.Reverse();

        var pathPolyline = new Polyline(path);

        DA.SetData(OutParam_PathPolyline, pathPolyline);
    }

    private static Point3d RoundPoint(Point3d pt, int decimals)
    {
        return new Point3d(
            Math.Round(pt.X, decimals),
            Math.Round(pt.Y, decimals),
            Math.Round(pt.Z, decimals)
        );
    }
}
