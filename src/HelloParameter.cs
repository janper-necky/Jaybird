using Grasshopper.Kernel;
using System;

namespace Jaybird
{
    public class HelloParameter : GH_Param<GH_String>
    {
        public HelloParameter()
            : base("Hello Param", "HP", "Hello World parameter", "Jaybird", "Params", GH_ParamAccess.item)
        {
        }

        public override Guid ComponentGuid => new Guid("B2C3D4E5-F6G7-8901-BCDE-F12345678901");
    }
}
