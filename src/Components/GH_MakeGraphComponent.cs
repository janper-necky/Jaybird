using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

// MAKE GRAPH FROM ROAD MAP COMPONENT
// Converts a collection of polylines (representing roads or paths) into a graph data structure
// suitable for pathfinding algorithms like A* and Dijkstra.
//
// PURPOSE:
// Transforms geometric polyline data into a mathematical graph structure by converting:
// - Polyline endpoints → Graph nodes (intersection points)
// - Polylines → Graph edges (connections between nodes)
//
// CONVERSION PROCESS:
//
// 1. NODE CREATION (Deduplication):
//    - Extract all polyline endpoints (first and last points)
//    - Round coordinates to specified decimal precision (default 3 decimals)
//    - Merge duplicate points within tolerance into single nodes
//    - Assign each unique point a node index (0, 1, 2, ...)
//    - Why rounding? Handles floating-point imprecision where "identical" points
//      may differ by tiny amounts (e.g., 10.000001 vs 10.000000)
//
// 2. EDGE CREATION (Directional vs Bidirectional):
//    - For each polyline, create edge(s) based on "Two Way" parameter:
//      * Two Way = true: Creates TWO edges (A→B and B→A) for bidirectional travel
//        Example: Two-way streets, pedestrian paths, hallways
//      * Two Way = false: Creates ONE edge (A→B) following polyline direction
//        Example: One-way streets, conveyor belts, directed flows
//    - Edge length = Total length of the polyline
//    - Edges stored in adjacency list: nodeEdges[sourceIdx] = set of outgoing edges
//
// OUTPUT:
// A GH_Graph object with adjacency list representation, ready for:
// - Pathfinding algorithms (A*, Dijkstra)
// - Graph analysis (Analyze Graph component)
// - Graph manipulation (Split Graph component)
//
// EXAMPLE:
// Input: Two polylines forming an L-shape
//   Polyline1: (0,0,0) → (1,0,0)
//   Polyline2: (1,0,0) → (1,1,0)
// Two Way = true
// Output: Graph with 3 nodes [0,1,2] and 4 edges:
//   Node 0 → Node 1 (length 1.0)
//   Node 1 → Node 0 (length 1.0)
//   Node 1 → Node 2 (length 1.0)
//   Node 2 → Node 1 (length 1.0)

public class GH_MakeGraphComponent : GH_Component
{
    static readonly string ComponentName = "Make Graph from Road Map";

    public GH_MakeGraphComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Create a graph from polylines representing a road network, "
                + "where polyline endpoints become nodes and polylines become edges.",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("fa7a5090-a7df-445a-ac1c-2f9bb42eed60");

    private const int InParam_Polylines = 0;
    private const int InParam_TwoWay = 1;
    private const int InParam_Decimals = 2;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter(
            "Polylines",
            "P",
            "Road network as polylines: first and last points become intersection nodes, "
                + "polylines become connections (edges) between nodes",
            GH_ParamAccess.list
        );
        pManager.AddBooleanParameter(
            "Two Way",
            "TW",
            "If true, roads are bidirectional; if false, roads follow polyline direction only",
            GH_ParamAccess.item,
            true
        );
        pManager.AddIntegerParameter(
            "Decimals",
            "D",
            "Number of decimal places to round node positions (for deduplication tolerance)",
            GH_ParamAccess.item,
            3
        );
    }

    private const int OutParam_Graph = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph created from the input polylines",
            GH_ParamAccess.item
        );
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var polylines = new List<Curve>();
        DA.GetDataList(InParam_Polylines, polylines);

        bool twoWay = true;
        DA.GetData(InParam_TwoWay, ref twoWay);

        int decimals = default;
        DA.GetData(InParam_Decimals, ref decimals);

        var nodePointToIndex = new Dictionary<Point3d, int>();
        var nodePoints = new List<Point3d>();
        var nodeIdxToEdges = new Dictionary<int, HashSet<Edge>>();

        int zeroLengthSkipped = 0;

        foreach (var polyline in polylines)
        {
            var nodeAPointRounded = RoundPoint(polyline.PointAtStart, decimals);
            if (!nodePointToIndex.TryGetValue(nodeAPointRounded, out var nodeAIdx))
            {
                nodeAIdx = nodePointToIndex.Count;
                nodePointToIndex.Add(nodeAPointRounded, nodeAIdx);
                nodePoints.Add(nodeAPointRounded);
            }

            var nodeBPointRounded = RoundPoint(polyline.PointAtEnd, decimals);
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

            var length = polyline.GetLength();

            var edgeAB = new Edge
            {
                ToNodeIdx = nodeBIdx,
                Length = length,
                Geometry = new List<GeometryBase> { polyline.Duplicate() },
            };
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
                var reversedPolyline = (Curve)polyline.Duplicate();
                reversedPolyline.Reverse();
                var edgeBA = new Edge
                {
                    ToNodeIdx = nodeAIdx,
                    Length = length,
                    Geometry = new List<GeometryBase> { reversedPolyline },
                };
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
                $"Skipped {zeroLengthSkipped} zero-length polyline{(zeroLengthSkipped == 1 ? "" : "s")}"
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
