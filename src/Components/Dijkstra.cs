using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

internal struct Edge
{
    internal int FromNodeIdx;
    internal int ToNodeIdx;
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

        var nodePoints = new List<Point3d>();
        var edges = new List<Edge>();

        foreach (var line in lines)
        {
            int fromIdx = -1;
            int toIdx = -1;
            for (var nodeIdx = 0; nodeIdx < nodePoints.Count; nodeIdx++)
            {
                if (
                    fromIdx == -1
                    && ArePointsSimilar(
                        line.From,
                        nodePoints[nodeIdx],
                        JaybirdInfo.Epsilon
                    )
                )
                {
                    fromIdx = nodeIdx;
                }
                if (
                    toIdx == -1
                    && ArePointsSimilar(
                        line.To,
                        nodePoints[nodeIdx],
                        JaybirdInfo.Epsilon
                    )
                )
                {
                    toIdx = nodeIdx;
                }

                if (fromIdx != -1 && toIdx != -1)
                {
                    break;
                }
            }

            if (fromIdx != -1 && toIdx != -1)
            {
                bool edgeExists = false;
                foreach (var edge in edges)
                {
                    if (
                        edge.FromNodeIdx == fromIdx && edge.ToNodeIdx == toIdx
                        || edge.FromNodeIdx == toIdx && edge.ToNodeIdx == toIdx
                    )
                    {
                        edgeExists = true;
                        break;
                    }
                }

                if (edgeExists)
                {
                    continue;
                }
            }

            if (fromIdx == -1)
            {
                fromIdx = nodePoints.Count;
                nodePoints.Add(line.From);
            }

            if (toIdx == -1)
            {
                toIdx = nodePoints.Count;
                nodePoints.Add(line.To);
            }

            if (fromIdx == toIdx)
            {
                continue;
            }

            edges.Add(new Edge { FromNodeIdx = fromIdx, ToNodeIdx = toIdx });
            edges.Add(new Edge { FromNodeIdx = toIdx, ToNodeIdx = fromIdx });
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

    private static bool ArePointsSimilar(
        Point3d lhs,
        Point3d rhs,
        double epsilon
    )
    {
        return Math.Abs(lhs.X - rhs.X) < epsilon
            && Math.Abs(lhs.Y - rhs.Y) < epsilon
            && Math.Abs(lhs.Z - rhs.Z) < epsilon;
    }
}
