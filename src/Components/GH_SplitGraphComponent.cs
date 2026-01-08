using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Jaybird;

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

        var nodePoints = ghGraph.NodePoints;
        var nodeEdges = ghGraph.NodeEdges;

        var components = GraphUtilities.FindConnectedComponents(nodeEdges);

        var outputGraphs = new List<GH_Graph>();

        foreach (var component in components)
        {
            var oldToNewIndex = new Dictionary<int, int>();
            var newNodePoints = new List<Point3d>();

            for (int i = 0; i < component.Count; i++)
            {
                oldToNewIndex[component[i]] = i;
                newNodePoints.Add(nodePoints[component[i]]);
            }

            var newNodeEdges = new List<HashSet<Edge>>();
            foreach (var oldIdx in component)
            {
                var remappedEdges = new HashSet<Edge>();
                foreach (var edge in nodeEdges[oldIdx])
                {
                    remappedEdges.Add(
                        new Edge { ToNodeIdx = oldToNewIndex[edge.ToNodeIdx], Length = edge.Length }
                    );
                }
                newNodeEdges.Add(remappedEdges);
            }

            var newGraph = new GH_Graph(newNodePoints, newNodeEdges.ToArray());
            outputGraphs.Add(newGraph);
        }

        DA.SetDataList(OutParam_Graphs, outputGraphs);
    }
}
