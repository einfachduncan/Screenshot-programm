using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ScreenshotProgramm.Services;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace ScreenshotProgramm.Views;

public partial class EditorWindow : Window
{
    private enum Tool
    {
        Text,
        Rectangle,
        Ellipse,
        Arrow,
        Blur
    }

    private Tool _activeTool = Tool.Text;
    private readonly LocalizationService _localizationService;
    private readonly WriteableBitmap _baseImage;
    private Point _start;
    private Shape? _activeShape;
    private bool _isDrawing;

    public EditorWindow(BitmapSource source, LocalizationService localizationService, bool darkMode)
    {
        InitializeComponent();
        _localizationService = localizationService;

        Title = _localizationService["editor_title"];
        SaveButton.Content = _localizationService["save"];
        CancelButton.Content = _localizationService["cancel"];
        TextToolButton.Content = _localizationService["text"];
        RectangleToolButton.Content = _localizationService["rectangle"];
        EllipseToolButton.Content = _localizationService["ellipse"];
        ArrowToolButton.Content = _localizationService["arrow"];
        BlurToolButton.Content = _localizationService["blur"];

        if (darkMode)
        {
            Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF111827"));
            Foreground = System.Windows.Media.Brushes.White;
        }

        _baseImage = new WriteableBitmap(source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0));
        ScreenshotImage.Source = _baseImage;
        DrawingCanvas.Width = _baseImage.PixelWidth;
        DrawingCanvas.Height = _baseImage.PixelHeight;
    }

    public BitmapSource? EditedImage { get; private set; }

    private void SelectTextTool(object sender, RoutedEventArgs e) => _activeTool = Tool.Text;
    private void SelectRectangleTool(object sender, RoutedEventArgs e) => _activeTool = Tool.Rectangle;
    private void SelectEllipseTool(object sender, RoutedEventArgs e) => _activeTool = Tool.Ellipse;
    private void SelectArrowTool(object sender, RoutedEventArgs e) => _activeTool = Tool.Arrow;
    private void SelectBlurTool(object sender, RoutedEventArgs e) => _activeTool = Tool.Blur;

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(DrawingCanvas);

        if (_activeTool == Tool.Text)
        {
            var prompt = new TextInputWindow();
            if (prompt.ShowDialog() == true)
            {
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = prompt.TextValue,
                    Foreground = new SolidColorBrush(GetSelectedColor()),
                    FontSize = 24,
                    FontWeight = FontWeights.SemiBold
                };
                Canvas.SetLeft(textBlock, _start.X);
                Canvas.SetTop(textBlock, _start.Y);
                DrawingCanvas.Children.Add(textBlock);
            }

            return;
        }

        _isDrawing = true;
        DrawingCanvas.CaptureMouse();

        switch (_activeTool)
        {
            case Tool.Rectangle:
                _activeShape = new System.Windows.Shapes.Rectangle { Stroke = new SolidColorBrush(GetSelectedColor()), StrokeThickness = 3 };
                break;
            case Tool.Ellipse:
                _activeShape = new Ellipse { Stroke = new SolidColorBrush(GetSelectedColor()), StrokeThickness = 3 };
                break;
            case Tool.Arrow:
                _activeShape = new Line { Stroke = new SolidColorBrush(GetSelectedColor()), StrokeThickness = 4, X1 = _start.X, Y1 = _start.Y, X2 = _start.X, Y2 = _start.Y };
                break;
            default:
                _activeShape = null;
                break;
        }

        if (_activeShape is not null)
        {
            DrawingCanvas.Children.Add(_activeShape);
        }
    }

    private void OnCanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        var current = e.GetPosition(DrawingCanvas);

        if (_activeTool == Tool.Blur)
        {
            return;
        }

        if (_activeShape is Line line)
        {
            line.X2 = current.X;
            line.Y2 = current.Y;
            return;
        }

        if (_activeShape is null)
        {
            return;
        }

        var left = Math.Min(_start.X, current.X);
        var top = Math.Min(_start.Y, current.Y);
        var width = Math.Abs(current.X - _start.X);
        var height = Math.Abs(current.Y - _start.Y);
        Canvas.SetLeft(_activeShape, left);
        Canvas.SetTop(_activeShape, top);
        _activeShape.Width = width;
        _activeShape.Height = height;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        DrawingCanvas.ReleaseMouseCapture();

        if (_activeTool == Tool.Blur)
        {
            var end = e.GetPosition(DrawingCanvas);
            var left = (int)Math.Max(0, Math.Min(_start.X, end.X));
            var top = (int)Math.Max(0, Math.Min(_start.Y, end.Y));
            var width = (int)Math.Min(_baseImage.PixelWidth - left, Math.Abs(end.X - _start.X));
            var height = (int)Math.Min(_baseImage.PixelHeight - top, Math.Abs(end.Y - _start.Y));

            if (width > 2 && height > 2)
            {
                ApplyBlur(left, top, width, height);
            }
        }

        _activeShape = null;
    }

    private void ApplyBlur(int x, int y, int width, int height)
    {
        var stride = _baseImage.BackBufferStride;
        var pixels = new byte[stride * _baseImage.PixelHeight];
        _baseImage.CopyPixels(pixels, stride, 0);

        var sourceCopy = (byte[])pixels.Clone();
        const int radius = 4;

        for (var yy = y; yy < y + height; yy++)
        {
            for (var xx = x; xx < x + width; xx++)
            {
                int count = 0;
                int b = 0;
                int g = 0;
                int r = 0;
                int a = 0;

                for (var ky = -radius; ky <= radius; ky++)
                {
                    var py = yy + ky;
                    if (py < 0 || py >= _baseImage.PixelHeight) continue;

                    for (var kx = -radius; kx <= radius; kx++)
                    {
                        var px = xx + kx;
                        if (px < 0 || px >= _baseImage.PixelWidth) continue;

                        var index = py * stride + px * 4;
                        b += sourceCopy[index];
                        g += sourceCopy[index + 1];
                        r += sourceCopy[index + 2];
                        a += sourceCopy[index + 3];
                        count++;
                    }
                }

                if (count == 0) continue;
                var target = yy * stride + xx * 4;
                pixels[target] = (byte)(b / count);
                pixels[target + 1] = (byte)(g / count);
                pixels[target + 2] = (byte)(r / count);
                pixels[target + 3] = (byte)(a / count);
            }
        }

        _baseImage.WritePixels(new Int32Rect(0, 0, _baseImage.PixelWidth, _baseImage.PixelHeight), pixels, stride, 0);
    }

    private Color GetSelectedColor()
    {
        var item = (System.Windows.Controls.ComboBoxItem?)ColorPicker.SelectedItem;
        return item?.Content?.ToString() switch
        {
            "Blue" => Colors.DeepSkyBlue,
            "Green" => Colors.LimeGreen,
            "Yellow" => Colors.Gold,
            "White" => Colors.White,
            _ => Colors.Red
        };
    }

    private void SaveClicked(object sender, RoutedEventArgs e)
    {
        EditedImage = MergeImage();
        DialogResult = true;
    }

    private void CancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private BitmapSource MergeImage()
    {
        var visual = new DrawingVisual();
        using var context = visual.RenderOpen();
        context.DrawImage(_baseImage, new Rect(0, 0, _baseImage.PixelWidth, _baseImage.PixelHeight));

        var brush = new VisualBrush(DrawingCanvas);
        context.DrawRectangle(brush, null, new Rect(0, 0, DrawingCanvas.Width, DrawingCanvas.Height));

        var merged = new RenderTargetBitmap(_baseImage.PixelWidth, _baseImage.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        merged.Render(visual);
        return merged;
    }

    private void OnWindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            return;
        }

        if (e.Key == Key.Enter)
        {
            EditedImage = MergeImage();
            DialogResult = true;
        }
    }
}
