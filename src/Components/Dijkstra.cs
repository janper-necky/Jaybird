using System.Drawing;
using Grasshopper.Kernel;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;

namespace Jaybird;

internal struct Edge
{
    internal int FromNodeIdx;
    internal int ToNodeIdx;
    internal double Weight;
}

public class Component_Dijkstra : GH_Component
{
    static string ComponentName = "Dijkstra";

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

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddLineParameter(
            "Lines",
            "L",
            "Lines representing roads",
            GH_ParamAccess.list
        );
    }

    private const int OutParam_Nodes = 0;
    private const int OutParam_Edges = 1;

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
            "Edges in the graph",
            GH_ParamAccess.list
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
            if (!nodePointsToIndex.TryGetValue(line.From, out var fromNodeIdx))
            {
                fromNodeIdx = nodePointsToIndex.Count;
                nodePointsToIndex.Add(line.From, fromNodeIdx);
                nodePoints.Add(line.From);
            }
            if (!nodePointsToIndex.TryGetValue(line.To, out var toNodeIdx))
            {
                toNodeIdx = nodePointsToIndex.Count;
                nodePointsToIndex.Add(line.To, toNodeIdx);
                nodePoints.Add(line.To);
            }

            if (
                !nodeIdxToEdgeIndices.TryGetValue(
                    fromNodeIdx,
                    out var edgeIndices
                )
            )
            {
                edgeIndices = new List<int>();
                nodeIdxToEdgeIndices.Add(fromNodeIdx, edgeIndices);
            }
            edgeIndices.Add(edges.Count);

            var length = nodePoints[fromNodeIdx]
                .DistanceTo(nodePoints[toNodeIdx]);

            edges.Add(
                new Edge
                {
                    FromNodeIdx = fromNodeIdx,
                    ToNodeIdx = toNodeIdx,
                    Weight = length,
                }
            );
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
    }
}
