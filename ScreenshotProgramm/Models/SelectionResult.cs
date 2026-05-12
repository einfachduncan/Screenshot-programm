namespace ScreenshotProgramm.Models;

public sealed class SelectionResult
{
    public System.Windows.Rect Region { get; init; }
    public List<System.Windows.Point> LassoPoints { get; init; } = new();
}
