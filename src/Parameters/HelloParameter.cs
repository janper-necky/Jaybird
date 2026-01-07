using System.Drawing;
using Grasshopper.Kernel;

namespace Jaybird;

public class HelloParameter : GH_Param<GH_Hello>
{
    public HelloParameter()
        : base(
            "Hello Param",
            "HP",
            "Hello World parameter",
            JaybirdInfo.TabName,
            "Params",
            GH_ParamAccess.item
        ) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateParameterIcon(
            Name,
            JaybirdInfo.ParameterBackgroundColor
        );

    public override Guid ComponentGuid =>
        new("180115b3-0fa1-4710-b3a4-35910f48b8e7");
}
