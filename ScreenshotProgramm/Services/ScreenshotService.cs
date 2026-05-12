using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenshotProgramm.Models;
using Point = System.Windows.Point;

namespace ScreenshotProgramm.Services;

public sealed class ScreenshotService
{
    public BitmapSource CaptureFullscreen()
    {
        var bounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;
        return CaptureRegion(bounds);
    }

    public BitmapSource CaptureActiveWindow()
    {
        var handle = GetForegroundWindow();
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            return CaptureFullscreen();
        }

        var bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return CaptureRegion(bounds);
    }

    public BitmapSource CaptureRegion(Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Ungültiger Aufnahmebereich.");
        }

        using var bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        return ConvertBitmap(bitmap);
    }

    public BitmapSource ApplyEllipseMask(BitmapSource source)
    {
        var visual = new DrawingVisual();
        using var context = visual.RenderOpen();
        var rect = new Rect(0, 0, source.PixelWidth, source.PixelHeight);
        context.PushClip(new EllipseGeometry(rect));
        context.DrawImage(source, rect);
        context.Pop();

        var result = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        return result;
    }

    public BitmapSource ApplyLassoMask(BitmapSource source, IReadOnlyList<Point> points)
    {
        if (points.Count < 3)
        {
            return source;
        }

        var minX = points.Min(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxX = points.Max(p => p.X);
        var maxY = points.Max(p => p.Y);
        var width = Math.Max(1, (int)Math.Ceiling(maxX - minX));
        var height = Math.Max(1, (int)Math.Ceiling(maxY - minY));

        var translated = points.Select(p => new Point(p.X - minX, p.Y - minY)).ToList();

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(translated[0], true, true);
            stream.PolyLineTo(translated.Skip(1).ToList(), true, true);
        }

        var visual = new DrawingVisual();
        using var context = visual.RenderOpen();
        context.PushClip(geometry);
        context.DrawImage(source, new Rect(-minX, -minY, source.PixelWidth, source.PixelHeight));
        context.Pop();

        var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);
        return target;
    }

    public string SaveScreenshot(BitmapSource source, string folder, ScreenshotFormat format)
    {
        Directory.CreateDirectory(folder);
        var extension = format switch
        {
            ScreenshotFormat.Bmp => "bmp",
            ScreenshotFormat.Jpg => "jpg",
            _ => "png"
        };

        var fileName = $"Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}." + extension;
        var path = Path.Combine(folder, fileName);

        BitmapEncoder encoder = format switch
        {
            ScreenshotFormat.Bmp => new BmpBitmapEncoder(),
            ScreenshotFormat.Jpg => new JpegBitmapEncoder { QualityLevel = 95 },
            _ => new PngBitmapEncoder()
        };

        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    public static BitmapSource ConvertBitmap(Bitmap bitmap)
    {
        var hBitmap = bitmap.GetHbitmap();
        try
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNative
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RectNative lpRect);
}
