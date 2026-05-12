using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ScreenshotProgramm.Models;
using Point = System.Windows.Point;

namespace ScreenshotProgramm.Views;

public partial class SelectionOverlayWindow : Window
{
    private readonly CaptureShape _shape;
    private Point _start;
    private bool _isDrawing;
    private readonly List<Point> _lassoPoints = new();

    public SelectionOverlayWindow(CaptureShape shape)
    {
        InitializeComponent();
        _shape = shape;
        KeyDown += OnKeyDown;
    }

    public SelectionResult? Result { get; private set; }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(this);
        _isDrawing = true;
        OverlayCanvas.CaptureMouse();

        if (_shape == CaptureShape.Lasso)
        {
            _lassoPoints.Clear();
            _lassoPoints.Add(_start);
            LassoLine.Visibility = Visibility.Visible;
            LassoLine.Points = new PointCollection(_lassoPoints);
        }
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        var current = e.GetPosition(this);

        if (_shape == CaptureShape.Lasso)
        {
            _lassoPoints.Add(current);
            LassoLine.Points = new PointCollection(_lassoPoints);
            return;
        }

        var left = Math.Min(_start.X, current.X);
        var top = Math.Min(_start.Y, current.Y);
        var width = Math.Abs(current.X - _start.X);
        var height = Math.Abs(current.Y - _start.Y);

        if (_shape == CaptureShape.Ellipse)
        {
            SelectionEllipse.Visibility = Visibility.Visible;
            SelectionRectangle.Visibility = Visibility.Collapsed;
            Canvas.SetLeft(SelectionEllipse, left);
            Canvas.SetTop(SelectionEllipse, top);
            SelectionEllipse.Width = width;
            SelectionEllipse.Height = height;
        }
        else
        {
            SelectionRectangle.Visibility = Visibility.Visible;
            SelectionEllipse.Visibility = Visibility.Collapsed;
            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        OverlayCanvas.ReleaseMouseCapture();
        var end = e.GetPosition(this);

        if (_shape == CaptureShape.Lasso)
        {
            if (_lassoPoints.Count > 2)
            {
                var minX = _lassoPoints.Min(p => p.X);
                var minY = _lassoPoints.Min(p => p.Y);
                var maxX = _lassoPoints.Max(p => p.X);
                var maxY = _lassoPoints.Max(p => p.Y);
                Result = new SelectionResult
                {
                    Region = new Rect(minX, minY, maxX - minX, maxY - minY),
                    LassoPoints = _lassoPoints.ToList()
                };
                DialogResult = true;
            }

            return;
        }

        var left = Math.Min(_start.X, end.X);
        var top = Math.Min(_start.Y, end.Y);
        var width = Math.Abs(end.X - _start.X);
        var height = Math.Abs(end.Y - _start.Y);

        if (width > 2 && height > 2)
        {
            Result = new SelectionResult { Region = new Rect(left, top, width, height) };
            DialogResult = true;
        }
    }

    private void OnKeyDown(object? sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }
}
