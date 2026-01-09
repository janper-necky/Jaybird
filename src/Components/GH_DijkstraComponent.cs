using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

public class GH_DijkstraComponent : GH_Component
{
    static readonly string ComponentName = "Dijkstra";

    public GH_DijkstraComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Find shortest path from the start point to the end point using the Dijkstra algorithm.",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("1076e0d4-279c-427c-a79c-43abbf0de560");

    private const int InParam_Graph = 0;
    private const int InParam_StartNodeIdx = 1;
    private const int InParam_EndNodeIdx = 2;
    private const int InParam_ShowVisitedEdges = 3;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph to search",
            GH_ParamAccess.item
        );
        pManager.AddIntegerParameter(
            "Start Node",
            "S",
            "Index of the starting node",
            GH_ParamAccess.item
        );
        pManager.AddIntegerParameter("End Node", "E", "Index of the end node", GH_ParamAccess.item);
        pManager.AddBooleanParameter(
            "Show Visited Edges",
            "SVE",
            "Show edges visited by the algorithm",
            GH_ParamAccess.item,
            false
        );
    }

    private const int OutParam_PathPolyline = 0;
    private const int OutParam_VisitedEdges = 1;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter(
            "Path",
            "P",
            "Path from start to end as geometry",
            GH_ParamAccess.list
        );
        pManager.AddLineParameter(
            "Visited Edges",
            "VE",
            "Edges visited by the algorithm",
            GH_ParamAccess.list
        );
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Graph ghGraph = null!;
        if (!DA.GetData(InParam_Graph, ref ghGraph))
        {
            return;
        }

        if (!ghGraph.IsValid)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ghGraph.IsValidWhyNot);
            return;
        }

        var nodePositions = ghGraph.NodePositions;
        var nodeEdges = ghGraph.NodeEdges;

        int startNodeIdx = 0;
        if (!DA.GetData(InParam_StartNodeIdx, ref startNodeIdx))
        {
            return;
        }

        int endNodeIdx = 0;
        if (!DA.GetData(InParam_EndNodeIdx, ref endNodeIdx))
        {
            return;
        }

        bool showVisitedEdges = false;
        DA.GetData(InParam_ShowVisitedEdges, ref showVisitedEdges);

        if (startNodeIdx < 0 || startNodeIdx >= nodeEdges.Length)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Start node index is out of range.");
            return;
        }

        if (endNodeIdx < 0 || endNodeIdx >= nodeEdges.Length)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "End node index is out of range.");
            return;
        }

        // DIJKSTRA'S ALGORITHM IMPLEMENTATION
        // Dijkstra's algorithm finds the shortest path between nodes in a graph
        // It's an uninformed search that explores nodes in order of their distance from start
        // Priority = actual distance from start only (f = g)
        // Guarantees shortest path but may explore more nodes than A*
        // Best used when no good heuristic is available or when finding shortest paths to all nodes
        // Compare to A* which adds a heuristic: priority = g + h
        //
        // INITIALIZATION:
        // - nodeDistancesFromStart: tracks best known distance from start to each node (g-score)
        // - nodePreviousIdx: tracks previous node in shortest path (for reconstruction)
        // - priorityQueue: processes nodes in order of distance from start (g only)
        // - visitedEdgeLines: optional visualization of algorithm progress

        var nodeDistancesFromStart = new double[nodeEdges.Length];
        Array.Fill(nodeDistancesFromStart, double.MaxValue);
        nodeDistancesFromStart[startNodeIdx] = 0.0;

        var nodePreviousIdx = new int[nodeEdges.Length];
        Array.Fill(nodePreviousIdx, -1);

        var priorityQueue = new PriorityQueue<int, double>();
        priorityQueue.Enqueue(startNodeIdx, nodeDistancesFromStart[startNodeIdx]);

        var visitedEdgeLines = new List<Line>();

        bool foundTheEnd = false;

        // MAIN SEARCH LOOP
        // Process nodes in order of increasing distance from start until goal is reached
        // For each node: skip if stale, check if it's the goal, then explore all neighbors
        // Update neighbor distances if we found a better path through current node
        // Enqueue neighbors with priority = g (distance from start only)
        //
        // STALE ENTRY HANDLING:
        // A node can be enqueued multiple times with different priorities
        // Skip entries where currentPriority > best known distance (stale entries)
        // Alternative: use visited HashSet like A* (trades memory for simpler logic)

        while (priorityQueue.TryDequeue(out var currentNodeIdx, out var currentPriority))
        {
            if (currentPriority > nodeDistancesFromStart[currentNodeIdx])
            {
                continue;
            }

            if (currentNodeIdx == endNodeIdx)
            {
                foundTheEnd = true;
                break;
            }

            var currentEdges = nodeEdges[currentNodeIdx];
            if (currentEdges.Count == 0)
            {
                continue;
            }

            foreach (var edge in currentEdges)
            {
                var neighborNodeIdx = edge.ToNodeIdx;

                if (showVisitedEdges)
                {
                    visitedEdgeLines.Add(
                        new Line(nodePositions[currentNodeIdx], nodePositions[neighborNodeIdx])
                    );
                }

                var currentDistanceFromStart = nodeDistancesFromStart[currentNodeIdx];
                var neighborDistanceFromStart = nodeDistancesFromStart[neighborNodeIdx];
                var newDistanceFromStart = currentDistanceFromStart + edge.Length;

                if (newDistanceFromStart >= neighborDistanceFromStart)
                {
                    continue;
                }

                nodeDistancesFromStart[neighborNodeIdx] = newDistanceFromStart;

                // KEY DIFFERENCE FROM A*:
                // Priority = g only (distance from start)
                // A* calculates heuristic and uses: priority = g + h
                priorityQueue.Enqueue(neighborNodeIdx, newDistanceFromStart);

                nodePreviousIdx[neighborNodeIdx] = currentNodeIdx;
            }
        }

        if (showVisitedEdges)
        {
            DA.SetDataList(OutParam_VisitedEdges, visitedEdgeLines);
        }
        else
        {
            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                "Visited edges not generated. Enable 'Show Visited Edges' to see them."
            );
        }

        if (!foundTheEnd)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The end point may not be reachable.");
            return;
        }

        AddRuntimeMessage(
            GH_RuntimeMessageLevel.Remark,
            "Found the shortest path to the end point."
        );

        // PATH RECONSTRUCTION: walk backwards from end to start to get node sequence
        var pathNodeIndices = new List<int>();
        var pathNodeIdx = endNodeIdx;
        pathNodeIndices.Add(pathNodeIdx);

        while (pathNodeIdx != startNodeIdx)
        {
            var previousNodeIdx = nodePreviousIdx[pathNodeIdx];
            pathNodeIdx = previousNodeIdx;
            pathNodeIndices.Add(pathNodeIdx);
        }

        pathNodeIndices.Reverse();

        // Reconstruct path geometry from edge geometries
        var pathGeometries = new List<GeometryBase>();
        for (int i = 0; i < pathNodeIndices.Count - 1; i++)
        {
            var fromNodeIdx = pathNodeIndices[i];
            var toNodeIdx = pathNodeIndices[i + 1];

            // Find the edge from fromNodeIdx to toNodeIdx
            foreach (var edge in nodeEdges[fromNodeIdx])
            {
                if (edge.ToNodeIdx == toNodeIdx)
                {
                    pathGeometries.AddRange(edge.Geometry);
                    break;
                }
            }
        }

        DA.SetDataList(OutParam_PathPolyline, pathGeometries);
    }
}
