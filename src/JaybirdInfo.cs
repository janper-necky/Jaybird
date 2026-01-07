using Grasshopper.Kernel;

namespace Jaybird;

public class JaybirdInfo : GH_AssemblyInfo
{
    public override string Name => PluginName;
    public override Bitmap? Icon => IconGenerator.GenerateComponentIcon(PluginName);
    public override string Description => "Grasshopper plugin for Rhino 8+";
    public override Guid Id => new("6B8E9A7C-4D3F-4E2B-9A1C-8F7E6D5C4B3A");
    public override string AuthorName => "Author";
    public override string AuthorContact => "";

    public const string PluginName = "Jaybird";
    public const string TabName = "Jaybird";

    public static readonly Color PrimaryColor = Color.FromArgb(66, 135, 245);
    public static readonly Color SecondaryColor = Color.FromArgb(33, 67, 122);
    public static readonly Color TextColor = Color.White;

    public static readonly Font IconFont = new("Arial", 12, FontStyle.Bold);
    public static readonly Font LabelFont = new("Arial", 8, FontStyle.Regular);
}
