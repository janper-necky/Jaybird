using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Jaybird;

public class HelloParameter : GH_Param<GH_String>
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

    protected override Bitmap? Icon => IconGenerator.GenerateParameterIcon("Hello Param");

    public override Guid ComponentGuid => new("B2C3D4E5-F607-8901-BCDE-F12345678901");
}

