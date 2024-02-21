using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using SkiaSharp;
using System.Collections.ObjectModel;
using TiqUtils.Wpf.AbstractClasses;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using PatNagle.User;

namespace PatNagle;

internal class MainFormContext : Notified
{
    private string _bobberLocation = "???";
    private string _fishingStatus = "Unknown";
    private string _stats = "Not run";
    private ObservableCollection<int> _items = new();
    private RectangularSection[] _sections;

    public ISeries[] Series { get; set; }
    
    public MainFormContext()
    {
        Series = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = _items,
                GeometryStroke = null,
            }
        };

        
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


    public ObservableCollection<int> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged();
        }
    }

    public int CurMouseX { get; set; }
    public int CurMouseY { get; set; }

    public string BobberLocation
    {
        get => _bobberLocation;
        set
        {
            if (value == _bobberLocation)
            {
                return;
            }

            _bobberLocation = value;
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

    public void UpdateStats(int casts, int hooks, int fails)
    {
        this.Stats = $"C:{casts}/H:{hooks}/F:{fails}";
    }
}