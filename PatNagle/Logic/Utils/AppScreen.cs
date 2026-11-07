using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatNagle.Logic.Image;
using PatNagle.User;

namespace PatNagle.Logic.Utils
{
    internal class AppScreen
    {
        public static DirectBitmap GetSelectedRegion(ScreenRegion r, IntPtr windowHandle)
        {
            var img = ScreenCapture.CaptureWindow(windowHandle, r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY);
            var db = new DirectBitmap(img.Width, img.Height);
            using (var graphics = Graphics.FromImage(db.Bitmap))
            {
                graphics.DrawImage(img, Point.Empty);
            }

            return db;
        }
        
        public static DirectBitmap GetSelectedRegion(ScreenRegion r)
        {
            var img = ScreenCapture.CaptureScreenRelatively(r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY, true);
            var db = new DirectBitmap(img.Width, img.Height);
            using (var graphics = Graphics.FromImage(db.Bitmap))
            {
                graphics.DrawImage(img, Point.Empty);
            }

            return db;
        }
        
        public static PictureData GetSelectedRegionWithGraphics(ScreenRegion r)
        {
            var img = ScreenCapture.CaptureScreenRelatively(r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY, true);
            var db = new DirectBitmap(img.Width, img.Height);
            var graphics = Graphics.FromImage(db.Bitmap);
            graphics.DrawImage(img, Point.Empty);

            return new PictureData(db, graphics);
        }
        
        public static void GetSelectedRegion(ScreenRegion r, Graphics graphics)
        {
            var img = ScreenCapture.CaptureScreenRelatively(r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY, true);
            graphics.DrawImage(img, Point.Empty);
        }

        public static void SaveSelectedRegion(ScreenRegion r, IntPtr windowHandle)
        {
            var img = ScreenCapture.CaptureWindow(windowHandle, r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY);
            img.Save("C:\\Temp\\test.png");
        }
        
        public static void SaveSelectedRegion(ScreenRegion r)
        {
            var img = ScreenCapture.CaptureScreenRelatively(r.StartPercentageX,
                r.EndPercentageX, r.StartPercentageY,
                r.EndPercentageY, true);
            img.Save("C:\\Temp\\test.png");
        }

        public static (int x, int y) FromRegionToScreenPosition(int x, int y)
        {
            // LeftTop is in Avalonia DIPs, but the captured bitmap is physical pixels.
            // Derive the origin exactly like ScreenCapture.CaptureWindow does for the BitBlt source.
            var r = AppSettings.Instance.Region!;
            var rect = new ScreenCapture.User32.RECT();
            ScreenCapture.User32.GetWindowRect(ScreenCapture.User32.GetDesktopWindow(), ref rect);
            var originX = (int)(r.StartPercentageX * (rect.right - rect.left) / 100);
            var originY = (int)(r.StartPercentageY * (rect.bottom - rect.top) / 100);
            return (rect.left + originX + x, rect.top + originY + y);
        }
    }

    public sealed class PictureData : IDisposable
    {
        public PictureData(DirectBitmap picture, Graphics graphic)
        {
            Picture = picture;
            Graphic = graphic;
        }

        public DirectBitmap Picture { get; }
        public Graphics Graphic { get; }

        public void Dispose()
        {
            Picture.Dispose();
            Graphic.Dispose();
        }
    }
}
