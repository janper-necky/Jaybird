using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Jaybird;

public class HelloParameter : GH_Param<GH_String>
{
    public HelloParameter()
        : base("Hello Param", "HP", "Hello World parameter", "Jaybird", "Params", GH_ParamAccess.item)
    {
    }

    public override Guid ComponentGuid => new("B2C3D4E5-F607-8901-BCDE-F12345678901");
}
