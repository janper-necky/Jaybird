using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

// SIMPLIFY GRAPH COMPONENT
// Optimizes a graph by merging linear road segments between junctions into single edges.
//
// PURPOSE:
// Reduces graph complexity for faster pathfinding by collapsing degree-2 nodes (waypoints)
// that only serve to define the geometric path between actual decision points (junctions).
//
// OPTIMIZATION STRATEGY:
//
// 1. IDENTIFY JUNCTIONS vs WAYPOINTS:
//    - Junction: node with degree ≠ 2 (intersections, dead-ends, origins/destinations)
//    - Waypoint: node with degree = 2 (just defines the path shape between junctions)
//
// 2. MERGE WAYPOINT CHAINS:
//    For each junction, trace along degree-2 waypoints until hitting another junction:
//      Junction A → waypoint → waypoint → waypoint → Junction B
//    Collapses to:
//      Junction A → [merged edge with collected geometries] → Junction B
//
// 3. PRESERVE GEOMETRY:
//    Collect all geometries from the original edges into the merged edge's Geometry list
//    so the visual representation is maintained even though the graph structure is simplified.
//
// OUTPUT:
// A new simplified GH_Graph with:
// - Only junction nodes (degree ≠ 2)
// - Merged edges with polyline geometry
// - Same logical connectivity, but much smaller size

public class GH_SimplifyGraphComponent : GH_Component
{
    static readonly string ComponentName = "Simplify Graph";

    public GH_SimplifyGraphComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Optimize graph by merging linear road segments between junctions into single edges, "
                + "reducing graph size and speeding up pathfinding algorithms.",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.tertiary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("8c3f2b91-d5e4-4a29-b8c9-1f6a3e7d9c2a");

    private const int InParam_Graph = 0;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph to simplify by merging linear road segments",
            GH_ParamAccess.item
        );
    }

    private const int OutParam_SimplifiedGraph = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Simplified Graph",
            "SG",
            "Optimized graph with merged edges between junctions",
            GH_ParamAccess.item
        );
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Graph? ghGraph = null;
        if (!DA.GetData(InParam_Graph, ref ghGraph) || ghGraph == null || !ghGraph.IsValid)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid graph input");
            return;
        }

        // ALGORITHM OVERVIEW:
        //
        // This algorithm simplifies a graph by collapsing linear chains of degree-2 nodes
        // (waypoints) into single edges between junctions (decision points).
        //
        // STEPS:
        // 1. Classify nodes based on connectivity (using node indices only)
        // 2. Build index mapping from old nodes to new junction indices
        // 3. Trace chains junction-to-junction and build merged edges directly
        //
        // EXAMPLE:
        // Before: Junction A → waypoint1 → waypoint2 → Junction B (4 nodes, 3 edges)
        // After:  Junction A → Junction B (2 nodes, 1 edge with polyline geometry)
        //
        // PRELIMINARY SETUP:
        // Extract the original graph data and count the total number of edges across all nodes.
        // This information is used later for reporting the optimization statistics.

        var originalPositions = ghGraph.NodePositions;
        var originalEdges = ghGraph.NodeEdges;
        var originalNodeCount = originalEdges.Length;
        var originalEdgeCount = 0;
        for (int i = 0; i < originalEdges.Length; i++)
        {
            originalEdgeCount += originalEdges[i].Count;
        }

        // STEP 1: CLASSIFY NODES BASED ON CONNECTIVITY
        //
        // GRAPH SEMANTICS: This algorithm assumes a DIRECTED graph where edges represent
        // one-way connections. Bidirectional roads are represented as TWO edges (A→B, B→A).
        // Waypoint detection treats the graph as undirected for neighbor counting (counts both
        // incoming and outgoing), but chain-following strictly follows outgoing edges (directed).
        // This hybrid approach correctly identifies waypoints in bidirectional road networks
        // where degree-2 nodes lie between junctions.
        //
        // A node is a WAYPOINT if it has exactly 2 unique neighbors AND is not a
        // terminal node with asymmetric edges (start fork or sink merge).
        //
        // This correctly identifies waypoints in:
        // - Unidirectional graphs: 1 incoming + 1 outgoing to different nodes
        // - Bidirectional graphs: 2 incoming + 2 outgoing to the same 2 nodes
        //
        // All other nodes are JUNCTIONS and will be preserved:
        // - Dead ends (1 neighbor, or 0 neighbors)
        // - Sources with 1 outgoing (1 neighbor)
        // - Start forks: no incoming, 2+ outgoing (would have 2 neighbors but is a junction)
        // - Sink merges: no outgoing, 2+ incoming (would have 2 neighbors but is a junction)
        // - Intersections (3+ neighbors)
        // - Isolated nodes (0 neighbors)
        // - Self-loops are excluded from neighbor counting

        // Build incoming edges lookup
        var incomingEdges = new List<int>[originalNodeCount];
        for (int i = 0; i < originalNodeCount; i++)
        {
            incomingEdges[i] = new List<int>();
        }
        for (int i = 0; i < originalNodeCount; i++)
        {
            foreach (var edge in originalEdges[i])
            {
                incomingEdges[edge.ToNodeIdx].Add(i);
            }
        }

        // Identify waypoints: exactly 2 unique neighbors, but NOT a start/sink with asymmetric edges
        var isWaypoint = new bool[originalNodeCount];
        for (int i = 0; i < originalNodeCount; i++)
        {
            var outDegree = originalEdges[i].Count;
            var inDegree = incomingEdges[i].Count;

            var uniqueNeighbors = new HashSet<int>();

            // Add outgoing neighbors
            foreach (var edge in originalEdges[i])
            {
                // Exclude self-loops from neighbor count
                if (edge.ToNodeIdx != i)
                {
                    uniqueNeighbors.Add(edge.ToNodeIdx);
                }
            }

            // Add incoming neighbors
            foreach (var incomingNodeIdx in incomingEdges[i])
            {
                // Exclude self-loops from neighbor count
                if (incomingNodeIdx != i)
                {
                    uniqueNeighbors.Add(incomingNodeIdx);
                }
            }

            // A node is a waypoint if:
            // - It has exactly 2 unique neighbors (excluding self), AND
            // - It's not a start node (no incoming) with multiple outgoing edges (that's a fork), AND
            // - It's not a sink node (no outgoing) with multiple incoming edges (that's a merge point)
            var isStartFork = inDegree == 0 && outDegree > 1;
            var isSinkMerge = outDegree == 0 && inDegree > 1;
            isWaypoint[i] = uniqueNeighbors.Count == 2 && !isStartFork && !isSinkMerge;
        }

        // STEP 2: BUILD INDEX MAPPING FROM ORIGINAL TO SIMPLIFIED GRAPH
        //
        // Create a mapping array that translates old node indices to new junction indices.
        // This is necessary because the simplified graph will only contain junctions, so we need
        // to know which new index corresponds to each old junction node.
        //
        // Mapping strategy:
        // - Junctions (not waypoints): Assigned sequential indices in the simplified graph (0, 1, 2, ...)
        //   in the order they appear in the original graph
        // - Waypoints: Marked with -1 to indicate they will not exist as nodes in the
        //   simplified graph (they will be absorbed into edge geometry instead)
        //
        // The junction count represents the total number of nodes in the simplified graph.

        var oldToNewNodeIdx = new int[originalNodeCount];
        var newNodePositions = new List<Point3d>();
        int junctionCount = 0;
        for (int i = 0; i < originalNodeCount; i++)
        {
            if (!isWaypoint[i])
            {
                oldToNewNodeIdx[i] = junctionCount;
                newNodePositions.Add(originalPositions[i]);
                junctionCount++;
            }
            else
            {
                oldToNewNodeIdx[i] = -1;
            }
        }

        // STEP 3: BUILD SIMPLIFIED GRAPH BY TRACING CHAINS
        //
        // For each junction in the original graph, follow its outgoing edges through chains of
        // waypoint nodes until reaching another junction. This process "walks" along the degree-2
        // waypoints, accumulating their geometric points and summing their edge lengths.
        //
        // Chain tracing process:
        // 1. Start from a junction node
        // 2. Follow an outgoing edge to its destination
        // 3. If destination is a waypoint (degree 2), continue following the outgoing edge that
        //    doesn't lead back to the previous node (handles bidirectional graphs)
        // 4. Repeat step 3 until reaching a junction node
        // 5. Create a merged edge from start junction to end junction with the accumulated geometry
        //
        // SAFETY: If a waypoint chain is broken (cycle or missing forward edge), the edge is
        // dropped and a warning is logged. This should not occur with proper graph structure.
        //
        // The result is a simplified graph where each edge directly connects junctions, with
        // geometry lists that capture the full path through all intermediate waypoints.

        var newNodeEdges = new List<HashSet<Edge>>();
        for (int i = 0; i < junctionCount; i++)
        {
            newNodeEdges.Add(new HashSet<Edge>());
        }

        for (int startNodeIdx = 0; startNodeIdx < originalNodeCount; startNodeIdx++)
        {
            // Only process junctions; skip waypoints as they're absorbed into merged edges
            if (oldToNewNodeIdx[startNodeIdx] == -1)
            {
                continue;
            }

            var startJunctionIdx = oldToNewNodeIdx[startNodeIdx];

            foreach (var firstEdge in originalEdges[startNodeIdx])
            {
                // Safety: verify the immediate destination before tracing
                if (oldToNewNodeIdx[firstEdge.ToNodeIdx] != -1)
                {
                    // Direct edge to another junction; no chain to trace
                    var mergedEdge = new Edge
                    {
                        ToNodeIdx = oldToNewNodeIdx[firstEdge.ToNodeIdx],
                        Length = firstEdge.Length,
                        Geometry =
                            firstEdge.Geometry != null
                                ? new List<GeometryBase>(firstEdge.Geometry)
                                : new List<GeometryBase>(),
                    };
                    newNodeEdges[startJunctionIdx].Add(mergedEdge);
                    continue;
                }

                // TRACE WAYPOINT CHAIN:
                // Follow the waypoint chain from this edge, accumulating geometry and length,
                // until reaching another junction. Handles bidirectional graphs by tracking
                // the previous node and only following forward edges to avoid infinite loops.
                var geometries = new List<GeometryBase>();
                var currentNodeIdx = firstEdge.ToNodeIdx;
                var totalLength = firstEdge.Length;

                if (firstEdge.Geometry != null)
                {
                    geometries.AddRange(firstEdge.Geometry);
                }

                var prevNodeIdx = startNodeIdx;
                var visitedInTrace = new HashSet<int>();

                while (oldToNewNodeIdx[currentNodeIdx] == -1)
                {
                    // Cycle detection: if we've visited this node before, chain is broken
                    if (visitedInTrace.Contains(currentNodeIdx))
                    {
                        break;
                    }
                    visitedInTrace.Add(currentNodeIdx);

                    var nextEdges = originalEdges[currentNodeIdx];

                    // Find the outgoing edge that doesn't go back to where we came from
                    // (prevents infinite loops in bidirectional graphs)
                    Edge nextEdge = default;
                    bool foundNext = false;
                    foreach (var edge in nextEdges)
                    {
                        if (edge.ToNodeIdx != prevNodeIdx)
                        {
                            nextEdge = edge;
                            foundNext = true;
                            break;
                        }
                    }

                    if (!foundNext)
                    {
                        // Waypoint has no forward edge; chain terminates abnormally
                        break;
                    }

                    if (nextEdge.Geometry != null)
                    {
                        geometries.AddRange(nextEdge.Geometry);
                    }

                    totalLength += nextEdge.Length;
                    prevNodeIdx = currentNodeIdx;
                    currentNodeIdx = nextEdge.ToNodeIdx;
                }

                var endJunctionIdx = oldToNewNodeIdx[currentNodeIdx];

                // Only create edge if we reached a valid junction (not broken mid-waypoint)
                if (endJunctionIdx != -1)
                {
                    var mergedEdge = new Edge
                    {
                        ToNodeIdx = endJunctionIdx,
                        Length = totalLength,
                        Geometry = geometries,
                    };

                    newNodeEdges[startJunctionIdx].Add(mergedEdge);
                }
                else
                {
                    // Chain was broken: either cycle detected or no forward path found
                    // This indicates a graph structure issue
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        $"Waypoint chain from junction {startNodeIdx} was broken and discarded. "
                            + "This may indicate a cycle or malformed graph structure."
                    );
                }
            }
        }

        // FINALIZATION:
        //
        // Create the simplified graph from the merged edges and calculate statistics to report
        // the optimization results. Count the new edge total and compare to the original graph
        // to show how much the graph was reduced.

        var simplifiedGraph = new GH_Graph(newNodePositions, newNodeEdges);

        var newNodeCount = junctionCount;
        var newEdgeCount = 0;
        for (int i = 0; i < newNodeEdges.Count; i++)
        {
            newEdgeCount += newNodeEdges[i].Count;
        }

        DA.SetData(OutParam_SimplifiedGraph, simplifiedGraph);

        if (newNodeCount < originalNodeCount || newEdgeCount < originalEdgeCount)
        {
            var nodeReduction =
                originalNodeCount > 0
                    ? (originalNodeCount - newNodeCount) * 100.0 / originalNodeCount
                    : 0;
            var edgeReduction =
                originalEdgeCount > 0
                    ? (originalEdgeCount - newEdgeCount) * 100.0 / originalEdgeCount
                    : 0;

            AddRuntimeMessage(
                GH_RuntimeMessageLevel.Remark,
                $"Simplified: {originalNodeCount} → {newNodeCount} nodes ({nodeReduction:F0}% reduction), "
                    + $"{originalEdgeCount} → {newEdgeCount} edges ({edgeReduction:F0}% reduction)"
            );
        }
    }
}
