using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Jaybird;

public class HelloComponent : GH_Component
{
    static readonly string ComponentName = "Hello";

    public HelloComponent()
        : base(
            ComponentName,
            JaybirdInfo.ExtractInitials(ComponentName),
            "Hello World component",
            JaybirdInfo.TabName,
            "Main"
        ) { }

    protected override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(ComponentName, JaybirdInfo.ComponentBackgroundColor);

    public override Guid ComponentGuid => new("ccef7d82-f43a-47d3-afe6-1351caa4b242");

    private const int InParam_Name = 0;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Name", "N", "Name to greet", GH_ParamAccess.item, "World");
    }

    private const int OutParam_Message = 0;

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Message", "M", "Greeting message", GH_ParamAccess.item);
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
