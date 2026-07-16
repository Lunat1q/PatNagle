using LiveChartsCore.SkiaSharpView;
using PatNagle.Common;
using PatNagle.Common.AutoUI;
using PatNagle.Logic;
using PatNagle.Logic.Control;
using PatNagle.Logic.Utils;
using PatNagle.UI;
using PatNagle.User;
using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HotKeyManager = PatNagle.Logic.Control.HotKeyManager;

namespace PatNagle;

public partial class MainWindow : Window
{
    private readonly MainFormContext _context;
    private BobberFinder _bobberF;
    private IntPtr _wowWindow;
    private HotKeyManager? _hotKeyMgr;

    public MainWindow()
    {
        _context = new MainFormContext();
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

    private void MetroWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        _hotKeyMgr = new HotKeyManager(this);
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        KeyModifiers modifierToggle = KeyModifiers.None;
        if (AppSettings.Instance.StartStopWithControl)
        {
            modifierToggle |= KeyModifiers.Control;
        }
        if (AppSettings.Instance.StartStopWithAlt)
        {
            modifierToggle |= KeyModifiers.Alt;
        }

        KeyModifiers modifierTopmost = KeyModifiers.None;
        if (AppSettings.Instance.TopmostWithControl)
        {
            modifierTopmost |= KeyModifiers.Control;
        }
        if (AppSettings.Instance.TopmostWithAlt)
        {
            modifierTopmost |= KeyModifiers.Alt;
        }
        _hotKeyMgr?.RegisterHotKey(modifierToggle, AppSettings.Instance.StartStopKey, RunToggle);
        _hotKeyMgr?.RegisterHotKey(modifierTopmost, AppSettings.Instance.TopmostKey, TopMostToggle);
    }

    private void TopMostToggle()
    {
        this.Topmost = !this.Topmost;
    }

    private async void RegionSelector_Click(object? sender, RoutedEventArgs e)
    {
        var selector = new RegionSelector();
        await selector.ShowDialog(this);
    }

    private void Run_Click(object? sender, RoutedEventArgs e)
    {
        RunToggle();
    }

    [MemberNotNull(nameof(_bobberF))]
    private void InitBobber()
    {
        var bobberActions = new BobberActions(FoundDelegate, HookDelegate, CastDelegate, PeriodicDelegate);
        _bobberF = new BobberFinder(AppSettings.Instance, bobberActions, _context);
    }

    private void RunToggle()
    {
        try
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
        catch (Exception ex)
        {
            Dialogs.ShowError($"Type: {ex.GetType().Name}\r\nError: {ex.Message}\r\nStack: {ex.StackTrace}");
        }
    }

    private void CastDelegate()
    {
        Dispatcher.UIThread.Invoke(() => KeyboardControl.SimulatePress(AppSettings.Instance.CastKey));
    }

    private void PeriodicDelegate()
    {
        Dispatcher.UIThread.Invoke(() => KeyboardControl.SimulatePress(AppSettings.Instance.PeriodicKey));
    }

    private void HookDelegate()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            MouseControl.RightClick(_wowWindow, _context.CurMouseX, _context.CurMouseY);
        });
    }


    private void FoundDelegate(int x, int y)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var appPos = AppScreen.FromRegionToScreenPosition(x, y);
            _context.CurMouseX = appPos.x + AppSettings.Instance.MouseHookXOffset;
            _context.CurMouseY = appPos.y + AppSettings.Instance.MouseHookYOffset;
            MouseControl.SetCursorPos(_context.CurMouseX, _context.CurMouseY);
        });
    }

    private void MetroWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        _bobberF.Stop();
        _hotKeyMgr?.Dispose();
    }

    private void UpdateChart()
    {
        _context.UpdateSections();
        Chart.YAxes = new[]
        {
            new Axis
            {
                MinLimit = -AppSettings.Instance.BobberDiveThreshold - 5,
                MaxLimit = 0
            }
        };
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = AppSettings.Instance.CreateAutoUISettingsDialog();
        await dialog.ShowDialog(this);
        UpdateChart();
        AppSettings.Save();
    }

    private void Debug_Click(object? sender, RoutedEventArgs e)
    {
        new DebugVision(_bobberF).Show();
    }
}
