using System.Drawing;
using Grasshopper.Kernel;

namespace Jaybird;

public class HelloComponent : GH_Component
{
    public HelloComponent()
        : base(
            "Hello",
            "Hello",
            "Hello World component",
            JaybirdInfo.TabName,
            "Main"
        ) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(
            Name,
            JaybirdInfo.ComponentBackgroundColor
        );

    public override Guid ComponentGuid =>
        new("c12bdcaf-adcb-49df-bba9-deecf765f794");

    private const int InParam_Name = 0;

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

    private const int OutParam_Message = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter(
            "Message",
            "M",
            "Greeting message",
            GH_ParamAccess.item
        );
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string name = string.Empty;
        if (!DA.GetData(InParam_Name, ref name))
        {
            return;
        }

        DA.SetData(OutParam_Message, $"Hello, {name}!");
    }
}
