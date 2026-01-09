using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

// SPLIT GRAPH COMPONENT
// Separates a graph into its connected components (islands), outputting each as an
// independent graph.
//
// PURPOSE:
// When a graph contains multiple disconnected parts (connected components or "islands"),
// this component isolates each part into a separate graph. Useful for:
// - Processing each disconnected network independently
// - Identifying and analyzing isolated subnetworks
// - Filtering out unwanted disconnected fragments
//
// CONNECTED COMPONENTS (ISLANDS):
// A connected component is a maximal subgraph where any two vertices have a path between them.
// In other words, it's a "cluster" of nodes that are all reachable from each other, but
// isolated from other clusters.
//
// EXAMPLE USE CASE:
// Road network with several disconnected neighborhoods:
// - Input: 1 graph with 3 disconnected neighborhoods
// - Output: 3 separate graphs, one for each neighborhood
//
// ALGORITHM:
//
// 1. COMPONENT DETECTION:
//    Uses GraphUtilities.FindConnectedComponents() which performs BFS traversal
//    to identify all connected components in the graph.
//
// 2. NODE INDEX REMAPPING:
//    Each output graph needs its own 0-based node indices.
//    - Original graph: nodes might be [5, 12, 23, 47, 89, ...]
//    - Component 1 contains nodes [5, 12, 23]
//    - New graph 1: these become nodes [0, 1, 2]
//    - Component 2 contains nodes [47, 89]
//    - New graph 2: these become nodes [0, 1]
//
//    This remapping is necessary because:
//    - Each graph expects contiguous indices starting from 0
//    - Preserves correct edge references within each component
//    - Makes each output graph independent and self-contained
//
// 3. EDGE REMAPPING:
//    For each node in a component:
//    - Copy all its outgoing edges
//    - Update edge destination indices to use new node indices
//    - Preserve edge lengths (distances)
//
// 4. GRAPH CONSTRUCTION:
//    Create new GH_Graph objects for each component with:
//    - Remapped node positions
//    - Remapped edge connections
//    - Independent node indexing
//
// OUTPUT:
// List of graphs, one per connected component, in the order they were discovered.
// Each graph is fully functional and can be used independently for pathfinding or analysis.

public class GH_SplitGraphComponent : GH_Component
{
    static readonly string ComponentName = "Split Graph";

    public GH_SplitGraphComponent()
        : base(
            ComponentName,
            GH_JaybirdInfo.ExtractInitials(ComponentName),
            "Split a graph into separate subgraphs, "
                + "one for each connected component (island).",
            GH_JaybirdInfo.TabName,
            "Graph Search"
        ) { }

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, GH_JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("c2f8d4a1-5e9b-4c7a-8f3d-2a6e9b1c5f7d");

    private const int InParam_Graph = 0;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graph",
            "G",
            "Graph to split into connected components (islands)",
            GH_ParamAccess.item
        );
    }

    private const int OutParam_Graphs = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(
            new GH_GraphParameter(),
            "Graphs",
            "G",
            "Separate graphs, one for each connected component (island)",
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

        var components = GraphUtilities.FindConnectedComponents(nodeEdges);

        var outputGraphs = new List<GH_Graph>();

        foreach (var component in components)
        {
            var oldToNewIndex = new Dictionary<int, int>();

            for (int i = 0; i < component.Count; i++)
            {
                oldToNewIndex[component[i]] = i;
            }

            var newNodePositions = new List<Point3d>();
            var newNodeEdges = new List<HashSet<Edge>>();
            foreach (var oldIdx in component)
            {
                newNodePositions.Add(nodePositions[oldIdx]);

                var remappedEdges = new HashSet<Edge>();
                foreach (var edge in nodeEdges[oldIdx])
                {
                    remappedEdges.Add(
                        new Edge
                        {
                            ToNodeIdx = oldToNewIndex[edge.ToNodeIdx],
                            Length = edge.Length,
                            Geometry = edge.Geometry,
                        }
                    );
                }
                newNodeEdges.Add(remappedEdges);
            }

            var newGraph = new GH_Graph(newNodePositions, newNodeEdges);
            outputGraphs.Add(newGraph);
        }

        DA.SetDataList(OutParam_Graphs, outputGraphs);
    }
}
