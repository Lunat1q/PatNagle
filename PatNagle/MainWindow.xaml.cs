using System;
using System.Drawing.Imaging;
using MahApps.Metro.Controls;
using PatNagle.Logic;
using PatNagle.Logic.Control;
using PatNagle.Logic.Image;
using PatNagle.Logic.Utils;
using PatNagle.UI;
using PatNagle.User;

namespace PatNagle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private BobberFinder? _bobberF;
        private IntPtr _wowWindow;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var selector = new RegionSelector();
            selector.ShowDialog();
        }

        private void Button2_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_bobberF == null || _bobberF.Done)
            {
                _bobberF = new BobberFinder(AppSettings.Instance.Region!, FoundDelegate, CaughtDelegate, CastDelegate);
                _bobberF.Start();
                _wowWindow = AppHelper.FindWowWindow();
            }
            else
            {
                _bobberF.Stop();
                this.Dispatcher.Invoke(() =>
                {
                    Caught.Text = "-";
                    Found.Text = "-";
                });
            }
        }

        private void CastDelegate()
        {
            KeyboardControl.SimulateFPress();
            this.Dispatcher.Invoke(() =>
            {
                Casting.Text = $"CASTING!";
                Caught.Text = "-";
                Found.Text = "-";
            });
        }

        private void CaughtDelegate(int dist)
        {
            this.Dispatcher.Invoke(() =>
            {
                Caught.Text = $"CAUGHT! {dist}";
                MouseControl.RightClick(_wowWindow, _curMouseX, _curMouseY);
            });
        }

        private int _curMouseX;
        private int _curMouseY;
        

        private void FoundDelegate(int x, int y)
        {
            this.Dispatcher.Invoke(() =>
            {
                Found.Text = $"FOUND AT X:{x} Y:{y}!";

                var appPos = AppScreen.FromRegionToScreenPosition(x, y);
                _curMouseX = appPos.x + 55;
                _curMouseY = appPos.y + 15;
                MouseControl.SetCursorPos(_curMouseX, _curMouseY);
            });
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _bobberF?.Stop();
        }
    }
}
