using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace PatNagle.Common;

/// <summary>Minimal replacement for WPF MessageBox. Error popup, callable from any thread.</summary>
public static class Dialogs
{
    public static void ShowError(string message, string title = "Error")
    {
        Dispatcher.UIThread.Post(() =>
        {
            var text = new SelectableTextBlock
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(16)
            };

            var ok = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Avalonia.Thickness(16, 0, 16, 16)
            };

            var panel = new StackPanel();
            panel.Children.Add(new ScrollViewer { Content = text });
            panel.Children.Add(ok);

            var window = new Window
            {
                Title = title,
                Width = 480,
                SizeToContent = SizeToContent.Height,
                MaxHeight = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = panel
            };
            ok.Click += (_, _) => window.Close();
            window.Show();
        });
    }
}
