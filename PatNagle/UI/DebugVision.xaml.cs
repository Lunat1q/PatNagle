using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PatNagle.Logic;
using PatNagle.Logic.Utils;
using Image = System.Drawing.Image;

namespace PatNagle.UI;

/// <summary>
///     Interaction logic for DebugVision.xaml
/// </summary>
public partial class DebugVision : INotifyPropertyChanged
{
    private readonly BobberFinder _finder;
    private WriteableBitmap? _bi;
    private ImageSource _debugImage;
    private string? _bobberLocation;

    internal DebugVision(BobberFinder finder)
    {
        _finder = finder;
        _debugImage = new WriteableBitmap(1, 1, 96.0, 96.0, PixelFormats.Pbgra32, null);
        InitializeComponent();
        finder.DebugIsOn = true;
        _finder.PictureChanged += FinderOnPictureChanged;
        _finder.BobberFound += FinderOnBobberFound;
    }

    public ImageSource DebugImage
    {
        get => _debugImage;
        private set
        {
            _debugImage = value;
            OnPropertyChanged();
        }
    }

    public string? BobberLocation
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void FinderOnBobberFound(object sender, string coords)
    {
        BobberLocation = coords;
    }

    private void FinderOnPictureChanged(object sender, Bitmap i)
    {
        Dispatcher.Invoke(() => UpdateBitmapImage(i));
    }

    private WriteableBitmap InitBitmap(Image i)
    {
        _bi ??= new WriteableBitmap(i.Width, i.Height, 96.0, 96.0, PixelFormats.Pbgra32, null);

        return _bi!;
    }

    private void UpdateBitmapImage(Bitmap bitmap)
    {
        var writableBitmap = InitBitmap(bitmap);

        var format = bitmap.PixelFormat;
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        // Lock the bitmap bits. 
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, format);
        try
        {
            writableBitmap.Lock();
            Win32.CopyMemory(writableBitmap.BackBuffer, bmpData.Scan0, (uint)(bitmap.Height * bmpData.Stride));
            writableBitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.Width, bitmap.Height));
            writableBitmap.Unlock();
            DebugImage = writableBitmap;
        }
        finally
        {
            // Unlock the bits.
            bitmap.UnlockBits(bmpData);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _finder.PictureChanged -= FinderOnPictureChanged;
        _finder.BobberFound -= FinderOnBobberFound;
        _finder.DebugIsOn = false;
    }
}