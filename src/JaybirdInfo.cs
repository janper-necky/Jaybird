using System.Drawing;
using Grasshopper.Kernel;

namespace Jaybird;

public class JaybirdInfo : GH_AssemblyInfo
{
    public override string Name => PluginName;

    public override Bitmap? Icon =>
        IconGenerator.GenerateComponentIcon(
            PluginName,
            ComponentBackgroundColor
        );

    public override string Description => "Grasshopper plugin for Rhino 8+";

    public override Guid Id => new("85a22e6b-7d3a-4b87-bc4f-33b9f0e4d829");

    public override string AuthorName => "Jan Pernecky";

    public override string AuthorContact => "janper@janper.sk";

    public const string PluginName = "Jaybird";
    public const string TabName = "Jaybird";

    public static readonly Color ComponentBackgroundColor = Color.FromArgb(
        180,
        50,
        80
    );
    public static readonly Color ParameterBackgroundColor = Color.FromArgb(
        16,
        16,
        24
    );
    public static readonly Color TextColor = Color.White;

    public static readonly Font IconFont = new("Arial", 10, FontStyle.Bold);
}
