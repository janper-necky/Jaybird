using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

public class GH_AStarComponent : GH_Component
{
    static readonly string ComponentName = "A*";

    public GH_AStarComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Find shortest path from the start point to the end point using the A* algorithm.",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.primary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("d8db4b89-1b72-48f2-be99-f8be250ba188");

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
            "Path from start to end as polyline",
            GH_ParamAccess.item
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

        var nodePoints = ghGraph.NodePoints;
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

        if (startNodeIdx < 0 || startNodeIdx >= nodePoints.Length)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Start node index is out of range.");
            return;
        }

        if (endNodeIdx < 0 || endNodeIdx >= nodePoints.Length)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "End node index is out of range.");
            return;
        }

        // A* ALGORITHM IMPLEMENTATION
        // A* is an informed search algorithm that finds the shortest path between two nodes
        // It uses a heuristic (estimated distance to goal) to guide the search more efficiently than Dijkstra
        // Priority = actual distance from start + heuristic distance to goal (f = g + h)
        // This makes A* faster than Dijkstra when a good heuristic is available
        // For spatial graphs, we use straight-line distance as the heuristic (admissible and consistent)
        // Compare to Dijkstra which uses only distance from start: priority = g
        //
        // INITIALIZATION:
        // - nodeDistancesFromStart: tracks best known distance from start to each node (g-score)
        // - nodeDistanceToEnd: cached heuristic distances to goal (h-score), computed lazily
        // - nodePreviousIdx: tracks previous node in shortest path (for reconstruction)
        // - priorityQueue: processes nodes in order of f-score (g + h)
        // - visitedNodeIdxs: tracks processed nodes to avoid revisiting
        // - visitedEdgeLines: optional visualization of algorithm progress

        var nodeDistancesFromStart = new double[nodePoints.Length];
        Array.Fill(nodeDistancesFromStart, double.MaxValue);
        nodeDistancesFromStart[startNodeIdx] = 0.0;

        var endNodePoint = nodePoints[endNodeIdx];
        var nodeDistanceToEnd = new double[nodePoints.Length];
        Array.Fill(nodeDistanceToEnd, -1.0);

        var nodePreviousIdx = new int[nodePoints.Length];
        Array.Fill(nodePreviousIdx, -1);

        var priorityQueue = new PriorityQueue<int, double>();
        priorityQueue.Enqueue(startNodeIdx, 0.0);

        var visitedEdgeLines = new List<Line>();

        var visitedNodeIdxs = new HashSet<int>();

        bool foundTheEnd = false;

        // MAIN SEARCH LOOP
        // Process nodes in order of increasing f-score (g + h) until goal is reached
        // For each node: skip if already visited, check if it's the goal, then explore all neighbors
        // Update neighbor distances if we found a better path through current node
        // Calculate heuristic lazily and enqueue neighbors with priority = g + h
        //
        // VISITED TRACKING:
        // Uses HashSet to prevent revisiting nodes (simpler than Dijkstra's stale-entry approach)
        // Alternative: use stale-entry detection like Dijkstra (trades simpler logic for less memory)

        while (priorityQueue.TryDequeue(out var currentNodeIdx, out var currentPriority))
        {
            if (visitedNodeIdxs.Contains(currentNodeIdx))
            {
                continue;
            }

            if (currentNodeIdx == endNodeIdx)
            {
                foundTheEnd = true;
                break;
            }

            visitedNodeIdxs.Add(currentNodeIdx);

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
                        new Line(nodePoints[currentNodeIdx], nodePoints[neighborNodeIdx])
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

                // KEY DIFFERENCE FROM DIJKSTRA:
                // Calculate heuristic (h-score) lazily and cache it
                if (nodeDistanceToEnd[neighborNodeIdx] < 0.0)
                {
                    nodeDistanceToEnd[neighborNodeIdx] = nodePoints[neighborNodeIdx]
                        .DistanceTo(endNodePoint);
                }

                // Priority = g + h (distance from start + heuristic to goal)
                // Dijkstra uses only g
                priorityQueue.Enqueue(
                    neighborNodeIdx,
                    nodeDistancesFromStart[neighborNodeIdx] + nodeDistanceToEnd[neighborNodeIdx]
                );

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

        // PATH RECONSTRUCTION: walk backwards from end to start, then reverse
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
}
