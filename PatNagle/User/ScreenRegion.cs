using Avalonia;

namespace PatNagle.User;

internal class ScreenRegion
{
    public Point LeftTop { get; set; }

    public Point RightBottom { get; set; }

    public double StartPercentageX { get; set; }
    public double EndPercentageX { get; set; }
    public double StartPercentageY { get; set; }
    public double EndPercentageY { get; set; }

    public void CalculatePercentages(double width, double height)
    {
        StartPercentageX = LeftTop.X * 100 / width;
        EndPercentageX = RightBottom.X * 100 / width;
        StartPercentageY = LeftTop.Y * 100 / height;
        EndPercentageY = RightBottom.Y * 100 / height;
    }
}
