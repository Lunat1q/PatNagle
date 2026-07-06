using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using PatNagle.User;

namespace PatNagle.UI;

public partial class RegionSelector : Window
{
    private Point _mouseCurPos;
    private Rectangle? _rec;
    private Point _start;
    private bool _isDown;

    public RegionSelector()
    {
        InitializeComponent();
        if (AppSettings.Instance.Region != null)
        {
            var r = AppSettings.Instance.Region;
            DrawNewRectangle(r.LeftTop, r.RightBottom);
        }
    }

    private void DrawNewRectangle(Point p1, Point p2)
    {
        Region.Children.Clear();

        _rec = new Rectangle
        {
            StrokeThickness = 5,
            Stroke = new SolidColorBrush(Colors.Red),
            Width = Math.Max(p2.X - p1.X, 10),
            Height = Math.Max(p2.Y - p1.Y, 10)
        };
        Canvas.SetLeft(_rec, p1.X);
        Canvas.SetTop(_rec, p1.Y);
        Region.Children.Add(_rec);
    }

    private void Window_MouseDown(object? sender, PointerPressedEventArgs e)
    {
        _start = e.GetPosition(this);
        _isDown = true;
        DrawNewRectangle(_start, _mouseCurPos);
    }

    private void Window_MouseUp(object? sender, PointerReleasedEventArgs e)
    {
        AppSettings.Instance.Region = new ScreenRegion
        {
            LeftTop = _start,
            RightBottom = _mouseCurPos
        };
        AppSettings.Instance.Region.CalculatePercentages(Bounds.Width, Bounds.Height);
        AppSettings.Save();
        Close();
    }

    private void Region_MouseMove(object? sender, PointerEventArgs e)
    {
        _mouseCurPos = e.GetPosition(this);
        if (_rec != null && _isDown)
        {
            _rec.Height = Math.Max(_mouseCurPos.Y - _start.Y, 10);
            _rec.Width = Math.Max(_mouseCurPos.X - _start.X, 10);
        }
    }
}
