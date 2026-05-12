using System.IO;
using System.Windows.Input;

namespace ScreenshotProgramm.Models;

public enum CaptureShape
{
    Rectangle,
    Ellipse,
    Lasso,
    Window,
    Fullscreen
}

public enum ScreenshotFormat
{
    Png,
    Jpg,
    Bmp
}

public sealed class AppSettings
{
    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.Control | ModifierKeys.Shift;
    public Key HotkeyKey { get; set; } = Key.S;
    public string SavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screenshots");
    public bool AutoSave { get; set; } = true;
    public ScreenshotFormat ScreenshotFormat { get; set; } = ScreenshotFormat.Png;
    public int DelaySeconds { get; set; } = 0;
    public bool PlaySound { get; set; } = true;
    public string Language { get; set; } = "de";
    public bool DarkMode { get; set; }
}
