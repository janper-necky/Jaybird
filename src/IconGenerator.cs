using System.Drawing.Drawing2D;

namespace Jaybird;

public static class IconGenerator
{
    private const int IconSize = 24;

    /// <summary>
    /// Generates a square icon for a component with the first letter(s) of the name
    /// </summary>
    public static Bitmap GenerateComponentIcon(string name, Color? backgroundColor = null)
    {
        backgroundColor ??= JaybirdInfo.PrimaryColor;
        var text = GetIconText(name);
        return GenerateIcon(text, backgroundColor.Value, IconShape.Square);
    }

    /// <summary>
    /// Generates a hexagonal icon for a parameter with the first letter(s) of the name
    /// </summary>
    public static Bitmap GenerateParameterIcon(string name, Color? backgroundColor = null)
    {
        backgroundColor ??= JaybirdInfo.SecondaryColor;
        var text = GetIconText(name);
        return GenerateIcon(text, backgroundColor.Value, IconShape.Hexagon);
    }

    private static string GetIconText(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        // Get first letters of each word (up to 2 characters)
        var words = name.Split([' ', '_'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return words[0][0].ToString().ToUpper();

        return string.Join("", words.Take(2).Select(w => w[0])).ToUpper();
    }

    private static Bitmap GenerateIcon(
        string text,
        Color backgroundColor,
        IconShape shape
    )
    {
        var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Draw background shape
        using (var brush = new SolidBrush(backgroundColor))
        {
            var path = GetShapePath(shape, IconSize);
            graphics.FillPath(brush, path);
        }

        // Draw text
        using (var textBrush = new SolidBrush(JaybirdInfo.TextColor))
        {
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };

            graphics.DrawString(
                text,
                JaybirdInfo.IconFont,
                textBrush,
                new RectangleF(0, 0, IconSize, IconSize),
                format
            );
        }

        return bitmap;
    }

    private static GraphicsPath GetShapePath(IconShape shape, int size)
    {
        var path = new GraphicsPath();

        switch (shape)
        {
            case IconShape.Square:
                path.AddRectangle(new Rectangle(0, 0, size, size));
                break;

            case IconShape.Hexagon:
                var points = new PointF[6];
                var centerX = size / 2f;
                var centerY = size / 2f;
                var radius = size / 2f;

                for (int i = 0; i < 6; i++)
                {
                    var angle = Math.PI / 3 * i - Math.PI / 2;
                    points[i] = new PointF(
                        centerX + radius * (float)Math.Cos(angle),
                        centerY + radius * (float)Math.Sin(angle)
                    );
                }

                path.AddPolygon(points);
                break;
        }

        return path;
    }

    private enum IconShape
    {
        Square,
        Hexagon,
    }
}
