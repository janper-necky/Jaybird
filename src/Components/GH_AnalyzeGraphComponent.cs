using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

// ANALYZE GRAPH COMPONENT
// This component analyzes the structure of a graph and extracts various metrics.
//
// PURPOSE:
// Provides statistical analysis and structural information about a graph, useful for
// understanding network topology, identifying problematic areas, and validating graph quality.
//
// OUTPUTS:
// 1. NODES & EDGES: Raw graph data as points and lines for visualization
//
// 2. LEAF NODES (dead ends): Nodes with degree = 1
//    - These are terminal points in the network
//    - In road networks: dead-end streets or cul-de-sacs
//    - Calculated by counting unique neighbors (combines incoming + outgoing edges)
//
// 3. JUNCTIONS (crossroads): Nodes with degree >= 3
//    - These are decision points where paths diverge or converge
//    - In road networks: intersections with 3+ connecting streets
//    - Calculated by counting unique neighbors (not just outgoing edges)
//
// 4. ISOLATED VERTICES (orphans): Nodes with degree = 0
//    - These are completely disconnected points
//    - Often indicate data quality issues or incomplete graphs
//    - Useful for validation and cleanup
//
// 5. CONNECTED COMPONENTS (islands): Number of disconnected subgraphs
//    - Each component is a maximal set of nodes where any two have a path between them
//    - Uses BFS traversal to identify components (see GraphUtilities)
//    - Count > 1 indicates a fragmented network
//
// DEGREE CALCULATION:
// Node degree = number of unique neighbors (not edge count)
// For bidirectional graphs: one visual connection = one neighbor (not two)
// For directed graphs: incoming and outgoing edges to same node = one neighbor
// This makes the metrics work correctly regardless of graph directionality.

public class GH_AnalyzeGraphComponent : GH_Component
{
    static readonly string ComponentName = "Analyze Graph";

    public GH_AnalyzeGraphComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Analyze a graph: nodes, edges, leaf nodes (dead ends), "
                + "junctions (crossroads), isolated vertices (orphans), "
                + "and connected components (islands).",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override System.Guid ComponentGuid => new("04ca808c-dafc-4f69-b688-f0ad25f91495");

    private const int InParam_Graph = 0;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph to analyze",
            GH_ParamAccess.item
        );
    }

    private const int OutParam_Nodes = 0;
    private const int OutParam_Edges = 1;
    private const int OutParam_LeafNodeCount = 2;
    private const int OutParam_JunctionCount = 3;
    private const int OutParam_IsolatedVertexCount = 4;
    private const int OutParam_ComponentCount = 5;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter(
            "Nodes",
            "N",
            "All nodes in the graph in their original order (indices preserved for search algorithms)",
            GH_ParamAccess.list
        );
        pManager.AddCurveParameter(
            "Edges",
            "E",
            "All edges in the graph (as stored polyline geometry)",
            GH_ParamAccess.list
        );
        pManager.AddIntegerParameter(
            "Leaf Node Count",
            "L",
            "Number of leaf nodes (dead ends): nodes connected to exactly one neighbor",
            GH_ParamAccess.item
        );
        pManager.AddIntegerParameter(
            "Junction Count",
            "J",
            "Number of junctions (crossroads): nodes connected to 3+ neighbors",
            GH_ParamAccess.item
        );
        pManager.AddIntegerParameter(
            "Isolated Vertex Count",
            "IV",
            "Number of isolated vertices (orphans): nodes with no connections",
            GH_ParamAccess.item
        );
        pManager.AddIntegerParameter(
            "Component Count",
            "CC",
            "Number of connected components (islands): disconnected subgraphs",
            GH_ParamAccess.item
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

        DA.SetDataList(OutParam_Nodes, nodePositions);

        var edgeCurves = new List<Curve>();
        for (int i = 0; i < nodeEdges.Length; i++)
        {
            foreach (var edge in nodeEdges[i])
            {
                foreach (var geom in edge.Geometry)
                {
                    if (geom is Curve curve)
                    {
                        edgeCurves.Add(curve);
                    }
                }
            }
        }
        DA.SetDataList(OutParam_Edges, edgeCurves);

        // Build incoming edges lookup for each node
        var incomingEdges = new List<int>[nodeEdges.Length];
        for (int i = 0; i < nodeEdges.Length; i++)
        {
            incomingEdges[i] = new List<int>();
        }

        for (int i = 0; i < nodeEdges.Length; i++)
        {
            foreach (var edge in nodeEdges[i])
            {
                incomingEdges[edge.ToNodeIdx].Add(i);
            }
        }

        // Count nodes by unique neighbor count (degree)
        // Leaf nodes (dead ends): degree 1 - exactly 1 unique neighbor
        // Junctions (crossroads): degree >= 3 - connected to 3+ neighbors
        // Isolated vertices (orphans): degree 0 - no neighbors at all
        int leafNodes = 0;
        int junctions = 0;
        int isolatedVertices = 0;

        for (int i = 0; i < nodeEdges.Length; i++)
        {
            var uniqueNeighbors = new HashSet<int>();

            // Add all outgoing neighbors
            foreach (var edge in nodeEdges[i])
            {
                uniqueNeighbors.Add(edge.ToNodeIdx);
            }

            // Add all incoming neighbors
            foreach (var incomingNodeIdx in incomingEdges[i])
            {
                uniqueNeighbors.Add(incomingNodeIdx);
            }

            int degree = uniqueNeighbors.Count;

            if (degree == 0)
            {
                isolatedVertices++;
            }
            else if (degree == 1)
            {
                leafNodes++;
            }
            else if (degree >= 3)
            {
                junctions++;
            }
        }

        DA.SetData(OutParam_LeafNodeCount, leafNodes);
        DA.SetData(OutParam_JunctionCount, junctions);
        DA.SetData(OutParam_IsolatedVertexCount, isolatedVertices);

        // Connected components (islands): maximal subgraphs where any two vertices
        // have a path between them (disconnected parts of the graph)
        // Detection uses BFS starting from each unvisited node to find all reachable nodes
        // Traverses both outgoing and incoming edges to handle directed graphs
        // Each BFS traversal identifies one connected component
        int componentCount = GraphUtilities.FindConnectedComponents(nodeEdges).Count;
        DA.SetData(OutParam_ComponentCount, componentCount);
    }
}
