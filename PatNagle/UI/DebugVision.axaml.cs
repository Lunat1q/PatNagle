using System;
using System.Drawing;
using System.Drawing.Imaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using PatNagle.Logic;
using PatNagle.Logic.Utils;
using PatNagle.User;
using PixelFormat = Avalonia.Platform.PixelFormat;
using Bitmap = System.Drawing.Bitmap;

namespace PatNagle.UI;

public partial class DebugVision : Window
{
    private readonly BobberFinder _finder;
    private readonly DispatcherTimer _liveTimer;
    private DateTime _lastFrameUtc = DateTime.MinValue;

    internal DebugVision(BobberFinder finder)
    {
        _finder = finder;
        InitializeComponent();
        finder.DebugIsOn = true;
        _finder.PictureChanged += FinderOnPictureChanged;
        _finder.BobberFound += FinderOnBobberFound;

        // Keep showing the captured zone even when the finder isn't running.
        // The finder pushes annotated frames while active; when it goes quiet the
        // timer self-captures so the window is never blank.
        _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _liveTimer.Tick += LiveTimerOnTick;
        _liveTimer.Start();
    }

    private void LiveTimerOnTick(object? sender, EventArgs e)
    {
        SuggestionsText.Text = string.Join(Environment.NewLine, _finder.Diagnostics.BuildSuggestions(AppSettings.Instance));

        // Finder frames are fresher than 250ms while it runs; only self-capture when stale.
        if ((DateTime.UtcNow - _lastFrameUtc).TotalMilliseconds < 250)
        {
            return;
        }

        var region = AppSettings.Instance.Region;
        if (region == null)
        {
            return;
        }

        try
        {
            // ponytail: capture on the UI thread. Debug window, ~10fps; move to a worker if it janks.
            using var db = AppScreen.GetSelectedRegion(region);
            UpdateBitmapImage(db.Bitmap);
        }
        catch (Exception ex)
        {
            ResolutionText.Text = $"Capture error: {ex.Message}";
        }
    }

    private void FinderOnBobberFound(object sender, string coords)
    {
        Dispatcher.UIThread.Post(() => LocationText.Text = $"Bobber position: {coords}");
    }

    private void FinderOnPictureChanged(object sender, Bitmap i)
    {
        // Blocking Invoke: the finder disposes the source bitmap right after this returns,
        // so the pixel copy must complete before we yield the worker thread.
        Dispatcher.UIThread.Invoke(() => UpdateBitmapImage(i));
    }

    private void UpdateBitmapImage(Bitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            // ponytail: recreate the bitmap each frame so the Image re-renders. Debug window; per-frame alloc is fine.
            var wb = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Opaque);

            using (var fb = wb.Lock())
            {
                var srcStride = bmpData.Stride;
                var dstStride = fb.RowBytes;
                var rowBytes = (uint)Math.Min(srcStride, dstStride);
                for (var y = 0; y < height; y++)
                {
                    Win32.CopyMemory(fb.Address + y * dstStride, bmpData.Scan0 + y * srcStride, rowBytes);
                }
            }

            ImageView.Source = wb;
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }

        _lastFrameUtc = DateTime.UtcNow;
        ResolutionText.Text = $"Zone resolution: {width} x {height}";
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        _liveTimer.Stop();
        _finder.PictureChanged -= FinderOnPictureChanged;
        _finder.BobberFound -= FinderOnBobberFound;
        _finder.DebugIsOn = false;
    }
}
