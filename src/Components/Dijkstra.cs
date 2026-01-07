using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

internal struct Edge
{
    internal int FromNodeIdx;
    internal int ToNodeIdx;
    internal double Weight;
}

internal struct State
{
    internal int NodeIdx;
    internal int CameFromNodeIdx;
}

public class Component_Dijkstra : GH_Component
{
    static readonly string ComponentName = "Dijkstra";

    public Component_Dijkstra()
        : base(
            ComponentName,
            JaybirdInfo.ExtractInitials(ComponentName),
            "Dijkstra algorithm component",
            JaybirdInfo.TabName,
            "Main"
        ) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(
            ComponentName,
            JaybirdInfo.ComponentBackgroundColor
        );

    public override Guid ComponentGuid =>
        new("1076e0d4-279c-427c-a79c-43abbf0de560");

    private const int InParam_Lines = 0;
    private const int InParam_StartPoint = 1;
    private const int InParam_EndPoint = 2;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddLineParameter(
            "Lines",
            "L",
            "Lines representing roads",
            GH_ParamAccess.list
        );
        pManager.AddPointParameter(
            "Start Point",
            "S",
            "Starting point for the path",
            GH_ParamAccess.item
        );
        pManager.AddPointParameter(
            "End Point",
            "E",
            "End point for the path",
            GH_ParamAccess.item
        );
    }

    private const int OutParam_Nodes = 0;
    private const int OutParam_Edges = 1;
    private const int OutParam_VisitedEdges = 2;
    private const int OutParam_PathPolyline = 3;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter(
            "Nodes",
            "N",
            "Nodes in the graph",
            GH_ParamAccess.list
        );
        pManager.AddLineParameter(
            "Edges",
            "E",
            "All edges in the graph",
            GH_ParamAccess.list
        );
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

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var lines = new List<Line>();
        DA.GetDataList(InParam_Lines, lines);

        // For fast comparison
        var nodePointsToIndex = new Dictionary<Point3d, int>();
        var nodePoints = new List<Point3d>();
        // Which edges lead out of a node
        var nodeIdxToEdgeIndices = new Dictionary<int, List<int>>();
        var edges = new List<Edge>();

        foreach (var line in lines)
        {
            var roundedFromPoint = RoundPoint(line.From, JaybirdInfo.Decimals);
            if (
                !nodePointsToIndex.TryGetValue(
                    roundedFromPoint,
                    out var fromNodeIdx
                )
            )
            {
                fromNodeIdx = nodePointsToIndex.Count;
                nodePointsToIndex.Add(roundedFromPoint, fromNodeIdx);
                nodePoints.Add(roundedFromPoint);
            }

            var roundedPointTo = RoundPoint(line.To, JaybirdInfo.Decimals);
            if (
                !nodePointsToIndex.TryGetValue(
                    roundedPointTo,
                    out var toNodeIdx
                )
            )
            {
                toNodeIdx = nodePointsToIndex.Count;
                nodePointsToIndex.Add(roundedPointTo, toNodeIdx);
                nodePoints.Add(roundedPointTo);
            }

            var length = nodePoints[fromNodeIdx]
                .DistanceTo(nodePoints[toNodeIdx]);

            if (
                !nodeIdxToEdgeIndices.TryGetValue(
                    fromNodeIdx,
                    out var edgeIndicesFrom
                )
            )
            {
                edgeIndicesFrom = new List<int>();
                nodeIdxToEdgeIndices.Add(fromNodeIdx, edgeIndicesFrom);
            }
            edgeIndicesFrom.Add(edges.Count);
            edges.Add(
                new Edge
                {
                    FromNodeIdx = fromNodeIdx,
                    ToNodeIdx = toNodeIdx,
                    Weight = length,
                }
            );

            if (
                !nodeIdxToEdgeIndices.TryGetValue(
                    toNodeIdx,
                    out var edgeIndicesTo
                )
            )
            {
                edgeIndicesTo = new List<int>();
                nodeIdxToEdgeIndices.Add(toNodeIdx, edgeIndicesTo);
            }
            edgeIndicesTo.Add(edges.Count);
            edges.Add(
                new Edge
                {
                    FromNodeIdx = toNodeIdx,
                    ToNodeIdx = fromNodeIdx,
                    Weight = length,
                }
            );
        }

        var edgeLines = new List<Line>();
        foreach (var edge in edges)
        {
            // TODO: Prevent duplicate lines created due to unidirectional edges
            var line = new Line(
                nodePoints[edge.FromNodeIdx],
                nodePoints[edge.ToNodeIdx]
            );
            edgeLines.Add(line);
        }

        DA.SetDataList(OutParam_Nodes, nodePoints);
        DA.SetDataList(OutParam_Edges, edgeLines);

        var startPoint = new Point3d();
        if (!DA.GetData(InParam_StartPoint, ref startPoint))
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Warning,
                "Failed to collect start point."
            );
            return;
        }

        var endPoint = new Point3d();
        if (!DA.GetData(InParam_EndPoint, ref endPoint))
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Warning,
                "Failed to collect end point."
            );
            return;
        }

        var startNodeIdx = -1;
        var startPointClosestDistance = double.MaxValue;
        var endNodeIdx = -1;
        var endPointClosestDistance = double.MaxValue;

        for (var nodeIdx = 0; nodeIdx < nodePoints.Count; nodeIdx++)
        {
            var distanceToStartPoint = nodePoints[nodeIdx]
                .DistanceTo(startPoint);
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

        var nodeCosts = Enumerable
            .Repeat(double.MaxValue, nodePoints.Count)
            .ToArray();
        nodeCosts[startNodeIdx] = 0;

        var stack = new SortedList<double, State>(new CostComparer());

        stack.Add(
            nodeCosts[startNodeIdx],
            new State { NodeIdx = startNodeIdx, CameFromNodeIdx = -1 }
        );

        var backTrace = new Dictionary<int, int>();

        var visitedEdges = new List<Edge>();

        while (stack.Count > 0)
        {
            var currentCost = stack.GetKeyAtIndex(0);
            var currentState = stack.GetValueAtIndex(0);

            var currentNodeIdx = currentState.NodeIdx;

            if (currentNodeIdx == endNodeIdx)
            {
                // TODO: Maybe we have to notify we were successful
                break;
            }

            stack.RemoveAt(0);

            if (
                !nodeIdxToEdgeIndices.TryGetValue(
                    currentNodeIdx,
                    out var currentEdges
                )
            )
            {
                // Dead end
                continue;
            }

            foreach (var edgeIdx in currentEdges)
            {
                var edge = edges[edgeIdx];
                visitedEdges.Add(edge);

                var neighborNodeIdx = edge.ToNodeIdx;
                var neighborCost = nodeCosts[neighborNodeIdx];
                var newNeighborCost = currentCost + edge.Weight;
                if (newNeighborCost >= neighborCost)
                {
                    continue;
                }

                nodeCosts[neighborNodeIdx] = newNeighborCost;
                // TODO: Some nodes may have the same key. Find a better structure
                stack.Add(
                    newNeighborCost,
                    new State
                    {
                        NodeIdx = neighborNodeIdx,
                        CameFromNodeIdx = currentNodeIdx,
                    }
                );

                if (!backTrace.TryAdd(neighborNodeIdx, currentNodeIdx))
                {
                    backTrace[neighborNodeIdx] = currentNodeIdx;
                }
            }
        }

        var visitedEdgeLines = new List<Line>();
        foreach (var edge in visitedEdges)
        {
            // TODO: Prevent duplicate lines created due to unidirectional edges
            var line = new Line(
                nodePoints[edge.FromNodeIdx],
                nodePoints[edge.ToNodeIdx]
            );
            visitedEdgeLines.Add(line);
        }

        DA.SetDataList(OutParam_VisitedEdges, visitedEdgeLines);

        if (stack.Count == 0)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                "Emptied stack and didn't fine end"
            );
            return;
        }

        if (stack.GetValueAtIndex(0).NodeIdx == endNodeIdx)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                "Yeeehaw, found it!"
            );
        }

        var path = new List<Point3d>();
        var pathNodeIdx = endNodeIdx;
        path.Add(nodePoints[pathNodeIdx]);

        while (pathNodeIdx != startNodeIdx)
        {
            var previousNodeIdx = backTrace[pathNodeIdx];
            pathNodeIdx = previousNodeIdx;
            path.Add(nodePoints[pathNodeIdx]);
        }

        path.Reverse();

        var pathPolyline = new Polyline(path);

        DA.SetData(OutParam_PathPolyline, pathPolyline);
    }

    private static Point3d RoundPoint(Point3d pt, int decimals)
    {
        decimals = Math.Min(5, decimals);
        return new Point3d(
            Math.Round(pt.X, decimals),
            Math.Round(pt.Y, decimals),
            Math.Round(pt.Z, decimals)
        );
    }
}

public class CostComparer : IComparer<double>
{
    public int Compare(double lhs, double rhs)
    {
        int result = lhs.CompareTo(rhs);
        if (result == 0)
        {
            return 1;
        }

        return result;
    }
}
