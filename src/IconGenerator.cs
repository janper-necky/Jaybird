using System.Drawing;
using System.Drawing.Drawing2D;

namespace Jaybird;

public static class IconGenerator
{
    private const int IconSize = 24;

    public static Bitmap GenerateComponentIcon(
        string name,
        Color backgroundColor
    )
    {
        var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System
            .Drawing
            .Text
            .TextRenderingHint
            .AntiAlias;

        using var brush = new SolidBrush(backgroundColor);
        graphics.FillRectangle(brush, 0, 0, IconSize, IconSize);

        DrawText(graphics, name);

        return bitmap;
    }

    public static Bitmap GenerateParameterIcon(
        string name,
        Color backgroundColor
    )
    {
        var bitmap = new Bitmap(IconSize, IconSize);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System
            .Drawing
            .Text
            .TextRenderingHint
            .AntiAlias;

        using var brush = new SolidBrush(backgroundColor);
        using var path = new GraphicsPath();
        var points = new PointF[6];
        var center = IconSize / 2f;
        var radius = IconSize / 2f;

        for (int i = 0; i < 6; i++)
        {
            var angle = Math.PI / 3 * i;
            points[i] = new PointF(
                center + radius * (float)Math.Cos(angle),
                center + radius * (float)Math.Sin(angle)
            );
        }

        path.AddPolygon(points);
        graphics.FillPath(brush, path);

        DrawText(graphics, name);

        return bitmap;
    }

    private static void DrawText(Graphics graphics, string name)
    {
        var text = JaybirdInfo.ExtractInitials(name);

        using var textBrush = new SolidBrush(JaybirdInfo.TextColor);
        using var format = new StringFormat
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
}
