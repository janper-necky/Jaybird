using Grasshopper.Kernel;

namespace Jaybird;

public class HelloComponent : GH_Component
{
    // Input parameter indices
    private const int InputName = 0;

    // Output parameter indices
    private const int OutputMessage = 0;

    public HelloComponent()
        : base("Hello", "Hello", "Hello World component", JaybirdInfo.TabName, "Main") { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Name",
            "N",
            "Name to greet",
            GH_ParamAccess.item,
            "World"
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Message", "M", "Greeting message", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        if (!DA.GetData(InputName, ref name))
            return;

        DA.SetData(OutputMessage, $"Hello, {name}!");
    }

    protected override Bitmap? Icon => IconGenerator.GenerateComponentIcon("Hello");

    public override Guid ComponentGuid => new("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
}

