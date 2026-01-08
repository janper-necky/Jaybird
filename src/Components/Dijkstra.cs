using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

internal struct Edge
{
    internal int ToNodeIdx;
    internal double Weight;
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
        var nodePointToIndex = new Dictionary<Point3d, int>();
        var nodePoints = new List<Point3d>();
        // Which edges lead out of a node
        var nodeIdxToEdges = new Dictionary<int, HashSet<Edge>>();

        foreach (var line in lines)
        {
            var nodeAPointRounded = RoundPoint(line.From, JaybirdInfo.Decimals);
            if (
                !nodePointToIndex.TryGetValue(
                    nodeAPointRounded,
                    out var nodeAIdx
                )
            )
            {
                nodeAIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeAPointRounded, nodeAIdx);
                nodePoints.Add(nodeAPointRounded);
            }

            var nodeBPointRounded = RoundPoint(line.To, JaybirdInfo.Decimals);
            if (
                !nodePointToIndex.TryGetValue(
                    nodeBPointRounded,
                    out var nodeBIdx
                )
            )
            {
                nodeBIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeBPointRounded, nodeBIdx);
                nodePoints.Add(nodeBPointRounded);
            }

            if (nodeAIdx == nodeBIdx)
            {
                continue;
            }

            var length = nodePoints[nodeAIdx].DistanceTo(nodePoints[nodeBIdx]);

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

        var helper_edgeLines = new List<Line>();
        foreach (var (nodeAIdx, edges) in nodeIdxToEdges)
        {
            // TODO: Prevent duplicate lines created due to unidirectional edges
            foreach (var edge in edges)
            {
                var nodeBIdx = edge.ToNodeIdx;
                var line = new Line(nodePoints[nodeAIdx], nodePoints[nodeBIdx]);
                helper_edgeLines.Add(line);
            }
        }

        DA.SetDataList(OutParam_Nodes, nodePoints);
        DA.SetDataList(OutParam_Edges, helper_edgeLines);

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

        var nodePreviousIdx = Enumerable.Repeat(-1, nodePoints.Count).ToArray();

        var resolvedNodeIdxs = new HashSet<int>();

        var stack = new SortedList<double, int>(new CostComparer())
        {
            { nodeCosts[startNodeIdx], startNodeIdx },
        };

        var helper_visitedEdgeLines = new List<Line>();

        while (stack.Count > 0)
        {
            var (currentCost, currentNodeIdx) = stack.ElementAt(0);

            if (currentNodeIdx == endNodeIdx)
            {
                break;
            }

            stack.RemoveAt(0);

            if (
                !nodeIdxToEdges.TryGetValue(
                    currentNodeIdx,
                    out var currentEdges
                )
            )
            {
                // Dead end
                continue;
            }

            foreach (var edge in currentEdges)
            {
                var neighborNodeIdx = edge.ToNodeIdx;
                if (resolvedNodeIdxs.Contains(neighborNodeIdx))
                {
                    continue;
                }

                helper_visitedEdgeLines.Add(
                    new Line(
                        nodePoints[currentNodeIdx],
                        nodePoints[neighborNodeIdx]
                    )
                );

                var neighborCost = nodeCosts[neighborNodeIdx];
                var newNeighborCost = currentCost + edge.Weight;
                if (newNeighborCost >= neighborCost)
                {
                    continue;
                }

                nodeCosts[neighborNodeIdx] = newNeighborCost;
                stack.Add(newNeighborCost, neighborNodeIdx);

                nodePreviousIdx[neighborNodeIdx] = currentNodeIdx;
            }

            resolvedNodeIdxs.Add(currentNodeIdx);
        }

        DA.SetDataList(OutParam_VisitedEdges, helper_visitedEdgeLines);

        if (stack.Count == 0)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                "Emptied stack and didn't find the end. Perhaps the end is not reachable."
            );
            return;
        }

        if (stack.GetValueAtIndex(0) != endNodeIdx)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Error,
                "Stack is not empty yet and didn't find the end. This is strange."
            );
            return;
        }

        AddRuntimeMessage(
            GH_RuntimeMessageLevel.Remark,
            "Yeeehaw, found the path!"
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
