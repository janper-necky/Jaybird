using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

public class GH_MakeGraphComponent : GH_Component
{
    static readonly string ComponentName = "Make Graph";

    public GH_MakeGraphComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Create a graph from lines representing roads.",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("fa7a5090-a7df-445a-ac1c-2f9bb42eed60");

    private const int InParam_Lines = 0;
    private const int InParam_TwoWay = 1;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddLineParameter("Lines", "L", "Lines representing roads", GH_ParamAccess.list);
        pManager.AddBooleanParameter(
            "Two Way",
            "TW",
            "If true, roads are bidirectional; if false, roads follow line direction only",
            GH_ParamAccess.item,
            true
        );
    }

    private const int OutParam_Graph = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph created from the input lines",
            GH_ParamAccess.item
        );
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var lines = new List<Line>();
        DA.GetDataList(InParam_Lines, lines);

        bool twoWay = true;
        DA.GetData(InParam_TwoWay, ref twoWay);

        var nodePointToIndex = new Dictionary<Point3d, int>();
        var nodePoints = new List<Point3d>();
        var nodeIdxToEdges = new Dictionary<int, HashSet<Edge>>();

        int zeroLengthSkipped = 0;

        foreach (var line in lines)
        {
            var nodeAPointRounded = RoundPoint(line.From, GH_JaybirdInfo.Decimals);
            if (!nodePointToIndex.TryGetValue(nodeAPointRounded, out var nodeAIdx))
            {
                nodeAIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeAPointRounded, nodeAIdx);
                nodePoints.Add(nodeAPointRounded);
            }

            var nodeBPointRounded = RoundPoint(line.To, GH_JaybirdInfo.Decimals);
            if (!nodePointToIndex.TryGetValue(nodeBPointRounded, out var nodeBIdx))
            {
                nodeBIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeBPointRounded, nodeBIdx);
                nodePoints.Add(nodeBPointRounded);
            }

            if (nodeAIdx == nodeBIdx)
            {
                zeroLengthSkipped++;
                continue;
            }

            var length = line.Length;

            var edgeAB = new Edge { ToNodeIdx = nodeBIdx, Length = length };
            if (nodeIdxToEdges.TryGetValue(nodeAIdx, out var edgesFromA))
            {
                edgesFromA.Add(edgeAB);
            }
            else
            {
                edgesFromA = new HashSet<Edge>() { edgeAB };
                nodeIdxToEdges.Add(nodeAIdx, edgesFromA);
            }

            if (twoWay)
            {
                var edgeBA = new Edge { ToNodeIdx = nodeAIdx, Length = length };
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
        }

        if (zeroLengthSkipped > 0)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                $"Skipped {zeroLengthSkipped} zero-length line{(zeroLengthSkipped == 1 ? "" : "s")}"
            );
        }

        int orphanCount = 0;
        for (int i = 0; i < nodePoints.Count; i++)
        {
            if (!nodeIdxToEdges.ContainsKey(i) || nodeIdxToEdges[i].Count == 0)
            {
                orphanCount++;
            }
        }

        if (orphanCount > 0)
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                $"Created {orphanCount} orphan node{(orphanCount == 1 ? "" : "s")} with no connections"
            );
        }

        var edgesArray = new HashSet<Edge>[nodePoints.Count];
        for (int i = 0; i < nodePoints.Count; i++)
        {
            if (nodeIdxToEdges.TryGetValue(i, out var edges))
            {
                edgesArray[i] = edges;
            }
            else
            {
                edgesArray[i] = new HashSet<Edge>();
            }
        }

        var ghGraph = new GH_Graph(nodePoints, edgesArray);

        DA.SetData(OutParam_Graph, ghGraph);
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
