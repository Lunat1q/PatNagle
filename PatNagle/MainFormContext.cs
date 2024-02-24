using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using PatNagle.User;
using SkiaSharp;
using TiqUtils.Wpf.AbstractClasses;

namespace PatNagle;

internal class MainFormContext : Notified
{
    private readonly Dispatcher _dispatcher;
    private string _fishingStatus = "Unknown";
    private bool _running;
    private RectangularSection[] _sections = Array.Empty<RectangularSection>();
    private string _stats = "Not run";

    public MainFormContext(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        Series = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = Items,
                GeometryStroke = null,
                GeometryFill = null
            }
        };
    }

    public ISeries[] Series { get; set; }

    public RectangularSection[] Sections
    {
        get => _sections;
        set
        {
            if (Equals(value, _sections))
            {
                return;
            }

            _sections = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<int> Items { get; } = new();

    public int CurMouseX { get; set; }
    public int CurMouseY { get; set; }

    public bool Running
    {
        get => _running;
        set
        {
            if (value == _running)
            {
                return;
            }

            _running = value;
            OnPropertyChanged();
        }
    }

    public string FishingStatus
    {
        get => _fishingStatus;
        set
        {
            if (value == _fishingStatus)
            {
                return;
            }

            _fishingStatus = value;
            OnPropertyChanged();
        }
    }

    public string Stats
    {
        get => _stats;
        set
        {
            if (value == _stats)
            {
                return;
            }

            _stats = value;
            OnPropertyChanged();
        }
    }

    public void AddDistance(int distance)
    {
        _dispatcher.Invoke(() =>
        {
            Items.Add(distance);
        });
    }

    public void UpdateSections()
    {
        Sections = new[]
        {
            new RectangularSection
            {
                Yi = -AppSettings.Instance.BobberDiveThreshold,
                Yj = -AppSettings.Instance.BobberDiveThreshold,
                Stroke = new SolidColorPaint
                {
                    Color = SKColors.Red,
                    StrokeThickness = 3,
                    PathEffect = new DashEffect(new float[] { 6, 6 })
                }
            }
        };
    }

    public void UpdateStats(int casts, int hooks, int fails)
    {
        Stats = $"C:{casts}/H:{hooks}/F:{fails}";
    }

    public void ClearAllDistance()
    {
        _dispatcher.Invoke(() =>
        {
            Items.Clear();
            OnPropertyChanged(nameof(Series));
        });
    }
}