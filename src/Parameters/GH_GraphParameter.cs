using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Jaybird;

public class GH_GraphParameter : GH_Param<GH_Graph>
{
    public GH_GraphParameter()
        : base("Graph", "G", "Graph", GH_JaybirdInfo.TabName, "Params", GH_ParamAccess.item) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateParameterIcon(Name, GH_JaybirdInfo.ParameterBackgroundColor);

    public override Guid ComponentGuid => new("4af0036d-8747-47be-a69d-02221c04741d");
}
