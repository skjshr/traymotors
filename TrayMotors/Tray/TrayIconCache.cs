using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TrayMotors;

public sealed class TrayIconCache : IDisposable
{
    public const int FrameCount = 12;
    private readonly Dictionary<(ResourceKind Kind, int Frame, IconColorBucket Bucket), Icon> icons = new();

    public TrayIconCache()
    {
        foreach (var kind in Enum.GetValues<ResourceKind>())
        {
            foreach (var bucket in Enum.GetValues<IconColorBucket>())
            {
                for (var frame = 0; frame < FrameCount; frame++)
                {
                    icons[(kind, frame, bucket)] = CreateIcon(kind, frame, bucket);
                }
            }
        }
    }

    public Icon Get(ResourceKind kind, int frame, IconColorBucket bucket) =>
        icons[(kind, Math.Abs(frame) % FrameCount, bucket)];

    private static Icon CreateIcon(ResourceKind kind, int frame, IconColorBucket bucket)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;

        var palette = Palette(bucket);
        using var main = new SolidBrush(palette.Main);
        using var accent = new SolidBrush(palette.Accent);
        using var dark = new SolidBrush(palette.Dark);
        using var pen = new Pen(palette.Dark, 2);

        switch (kind)
        {
            case ResourceKind.Cpu:
                DrawPiston(graphics, frame, main, accent, dark, pen);
                break;
            case ResourceKind.Memory:
                DrawTurbine(graphics, frame, main, accent, dark, pen);
                break;
            case ResourceKind.Gpu:
                DrawRotary(graphics, frame, main, accent, dark, pen);
                break;
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            _ = DestroyIcon(handle);
        }
    }

    private static void DrawPiston(Graphics graphics, int frame, Brush main, Brush accent, Brush dark, Pen pen)
    {
        var travel = frame % 6;
        var offset = travel <= 3 ? travel * 2 : (6 - travel) * 2;
        using var rodPen = new Pen(((SolidBrush)dark).Color, 3)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        graphics.FillRoundedRectangle(dark, new Rectangle(8, 2, 16, 21), new Size(4, 4));
        graphics.FillRoundedRectangle(Brushes.Black, new Rectangle(10, 4, 12, 17), new Size(3, 3));
        graphics.FillRoundedRectangle(main, new Rectangle(10, 5 + offset, 12, 8), new Size(2, 2));
        graphics.FillRectangle(accent, 10, 7 + offset, 12, 3);
        graphics.DrawLine(rodPen, 16, 13 + offset, 16, 24);
        graphics.FillEllipse(dark, 9, 21, 14, 9);
        graphics.FillEllipse(accent, 13, 23, 6, 5);
        graphics.FillRoundedRectangle(dark, new Rectangle(5, 27, 22, 3), new Size(2, 2));
    }

    private static void DrawTurbine(Graphics graphics, int frame, Brush main, Brush accent, Brush dark, Pen pen)
    {
        var angle = frame * 30.0 * Math.PI / 180.0;
        var center = new PointF(16, 16);

        graphics.FillEllipse(dark, 3, 3, 26, 26);
        graphics.FillEllipse(Brushes.Black, 6, 6, 20, 20);

        for (var blade = 0; blade < 3; blade++)
        {
            var a = angle + blade * Math.PI * 2 / 3;
            using var bladePath = new GraphicsPath();
            bladePath.AddClosedCurve(
                [
                    PointOnCircle(center, a - 0.55, 4),
                    PointOnCircle(center, a - 0.10, 13),
                    PointOnCircle(center, a + 0.48, 8)
                ],
                0.45f);
            graphics.FillPath(main, bladePath);
        }

        graphics.FillEllipse(accent, 11, 11, 10, 10);
        graphics.DrawEllipse(pen, 4, 4, 24, 24);
    }

    private static void DrawRotary(Graphics graphics, int frame, Brush main, Brush accent, Brush dark, Pen pen)
    {
        var angle = frame * 30.0 * Math.PI / 180.0;
        var center = new PointF(16, 16);
        using var rotor = CreateRoundedRotorPath(center, 10, angle);

        graphics.FillEllipse(dark, 3, 3, 26, 26);
        graphics.FillEllipse(Brushes.Black, 6, 6, 20, 20);
        graphics.FillPath(main, rotor);
        graphics.DrawPath(pen, rotor);
        graphics.FillEllipse(accent, 13, 13, 6, 6);
        graphics.FillRoundedRectangle(dark, new Rectangle(25, 14, 5, 4), new Size(2, 2));
        graphics.DrawEllipse(pen, 4, 4, 24, 24);
    }

    private static GraphicsPath CreateRoundedRotorPath(PointF center, float radius, double angle)
    {
        var points = Enumerable.Range(0, 3)
            .Select(index => PointOnCircle(center, angle + index * Math.PI * 2 / 3, radius))
            .ToArray();
        var path = new GraphicsPath();
        path.AddClosedCurve(points, 0.78f);
        return path;
    }

    private static PointF PointOnCircle(PointF center, double angle, float radius) =>
        new(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius);

    private static (Color Main, Color Accent, Color Dark) Palette(IconColorBucket bucket) =>
        bucket switch
        {
            IconColorBucket.Warm => (Color.FromArgb(238, 191, 65), Color.FromArgb(255, 229, 142), Color.FromArgb(91, 68, 15)),
            IconColorBucket.Hot => (Color.FromArgb(235, 105, 50), Color.FromArgb(255, 171, 99), Color.FromArgb(95, 36, 18)),
            IconColorBucket.Critical => (Color.FromArgb(228, 46, 59), Color.FromArgb(255, 115, 124), Color.FromArgb(92, 18, 25)),
            IconColorBucket.Unknown => (Color.FromArgb(132, 142, 150), Color.FromArgb(185, 193, 199), Color.FromArgb(58, 65, 70)),
            _ => (Color.FromArgb(73, 184, 130), Color.FromArgb(138, 224, 177), Color.FromArgb(27, 82, 58))
        };

    public void Dispose()
    {
        foreach (var icon in icons.Values)
        {
            icon.Dispose();
        }

        icons.Clear();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(nint handle);
}
