using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Jaybird
{
    public class JaybirdInfo : GH_AssemblyInfo
    {
        public override string Name => "Jaybird";
        public override Bitmap Icon => null;
        public override string Description => "Grasshopper plugin";
        public override Guid Id => new Guid("6B8E9A7C-4D3F-4E2B-9A1C-8F7E6D5C4B3A");
        public override string AuthorName => "Author";
        public override string AuthorContact => "";
    }
}
