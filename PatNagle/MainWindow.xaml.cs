using System;
using System.Linq;
using System.Windows;
using LiveChartsCore.SkiaSharpView;
using MahApps.Metro.Controls;
using PatNagle.Logic;
using PatNagle.Logic.Control;
using PatNagle.Logic.Utils;
using PatNagle.UI;
using PatNagle.User;
using TiqUtils.Wpf.UIBuilders;

namespace PatNagle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private BobberFinder? _bobberF;
        private IntPtr _wowWindow;
        private readonly MainFormContext _context;

        public MainWindow()
        {
            _context = new MainFormContext();
            this.DataContext = _context;
            InitializeComponent();
            Chart.XAxes = new[]
            {
                new Axis
                {
                    IsVisible = false
                }
            };
            this.UpdateChart();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selector = new RegionSelector();
            selector.ShowDialog();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            if (_bobberF == null || _bobberF.Done)
            {
                var bobberActions = new BobberActions(FoundDelegate, CaughtDelegate, CastDelegate);
                _bobberF = new BobberFinder(AppSettings.Instance, bobberActions, _context);
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
            KeyboardControl.SimulateFPress();
        }

        private void CaughtDelegate(int dist)
        {
            this.Dispatcher.Invoke(() =>
            {
                MouseControl.RightClick(_wowWindow, _context.CurMouseX, _context.CurMouseY);
            });
        }
        

        private void FoundDelegate(int x, int y)
        {
            this.Dispatcher.Invoke(() =>
            {
                var appPos = AppScreen.FromRegionToScreenPosition(x, y);
                _context.CurMouseX = appPos.x + AppSettings.Instance.MouseHookXOffset;
                _context.CurMouseY = appPos.y + AppSettings.Instance.MouseHookYOffset;
                MouseControl.SetCursorPos(_context.CurMouseX, _context.CurMouseY);
            });
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _bobberF?.Stop();
        }

        private void UpdateChart()
        {
            _context.UpdateSections();
            var cartesianAxis = Chart.YAxes.First();
            cartesianAxis.MinLimit = -AppSettings.Instance.BobberDiveThreshold - 5;
            cartesianAxis.MaxLimit = 0;
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            var dialog = AppSettings.Instance.CreateAutoUISettingsDialog();
            dialog.ShowDialog();
            this.UpdateChart();
            AppSettings.Save();
        }
    }
}
