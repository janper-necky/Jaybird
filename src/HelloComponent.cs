using Grasshopper.Kernel;

namespace Jaybird;

public class HelloComponent : GH_Component
{
    public HelloComponent()
        : base("Hello", "Hello", "Hello World component", "Jaybird", "Main")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Name to greet", GH_ParamAccess.item, "World");
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Message", "M", "Greeting message", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        if (!DA.GetData(0, ref name)) return;

        DA.SetData(0, $"Hello, {name}!");
    }

    protected override Bitmap? Icon => null;
    public override Guid ComponentGuid => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
}
