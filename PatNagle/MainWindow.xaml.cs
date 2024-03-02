using LiveChartsCore.SkiaSharpView;
using PatNagle.Logic;
using PatNagle.Logic.Control;
using PatNagle.Logic.Utils;
using PatNagle.UI;
using PatNagle.User;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TiqUtils.Wpf.UIBuilders;

namespace PatNagle;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly MainFormContext _context;
    private BobberFinder _bobberF;
    private IntPtr _wowWindow;
    private HotKeyManager? _hotKeyMgr;

    public MainWindow()
    {
        _context = new MainFormContext(this.Dispatcher);
        DataContext = _context;
        InitializeComponent();
        Chart.XAxes = new[]
        {
            new Axis
            {
                IsVisible = false
            }
        };
        UpdateChart();
        InitBobber();
    }

    private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _hotKeyMgr = new HotKeyManager(this);
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        ModifierKeys modifierToggle = ModifierKeys.None;
        if (AppSettings.Instance.StartStopWithControl)
        {
            modifierToggle |= ModifierKeys.Control;
        }
        if (AppSettings.Instance.StartStopWithAlt)
        {
            modifierToggle |= ModifierKeys.Alt;
        }

        ModifierKeys modifierTopmost = ModifierKeys.None;
        if (AppSettings.Instance.TopmostWithControl)
        {
            modifierTopmost |= ModifierKeys.Control;
        }
        if (AppSettings.Instance.TopmostWithAlt)
        {
            modifierTopmost |= ModifierKeys.Alt;
        }
        _hotKeyMgr?.RegisterHotKey(modifierToggle, AppSettings.Instance.StartStopKey, RunToggle);
        _hotKeyMgr?.RegisterHotKey(modifierTopmost, AppSettings.Instance.TopmostKey, TopMostToggle);
    }

    private void TopMostToggle()
    {
        this.Topmost = !this.Topmost;
    }

    private void RegionSelector_Click(object sender, RoutedEventArgs e)
    {
        var selector = new RegionSelector();
        selector.ShowDialog();
    }

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        RunToggle();
    }

    [MemberNotNull(nameof(_bobberF))]
    private void InitBobber()
    {
        var bobberActions = new BobberActions(FoundDelegate, HookDelegate, CastDelegate);
        _bobberF = new BobberFinder(AppSettings.Instance, bobberActions, _context);
    }

    private void RunToggle()
    {

        if (!_context.Running)
        {
            _bobberF.Start();
            _wowWindow = AppHelper.FindWowWindow();
        }
        else
        {
            _bobberF.Stop();
        }
    }

    private void CastDelegate()
    {
        Dispatcher.Invoke(() => KeyboardControl.SimulatePress(AppSettings.Instance.CastKey));
    }

    private void HookDelegate()
    {
        Dispatcher.Invoke(() =>
        {
            MouseControl.RightClick(_wowWindow, _context.CurMouseX, _context.CurMouseY);
        });
    }


    private void FoundDelegate(int x, int y)
    {
        Dispatcher.Invoke(() =>
        {
            var appPos = AppScreen.FromRegionToScreenPosition(x, y);
            _context.CurMouseX = appPos.x + AppSettings.Instance.MouseHookXOffset;
            _context.CurMouseY = appPos.y + AppSettings.Instance.MouseHookYOffset;
            MouseControl.SetCursorPos(_context.CurMouseX, _context.CurMouseY);
        });
    }

    private void MetroWindow_Closing(object sender, CancelEventArgs e)
    {
        _bobberF.Stop();
        _hotKeyMgr?.Dispose();
    }

    private void UpdateChart()
    {
        _context.UpdateSections();
        var cartesianAxis = Chart.YAxes.First();
        cartesianAxis.MinLimit = -AppSettings.Instance.BobberDiveThreshold - 5;
        cartesianAxis.MaxLimit = 0;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = AppSettings.Instance.CreateAutoUISettingsDialog();
        dialog.ShowDialog();
        UpdateChart();
        AppSettings.Save();
    }

    private void Debug_Click(object sender, RoutedEventArgs e)
    {
        new DebugVision(_bobberF).Show();
    }
}