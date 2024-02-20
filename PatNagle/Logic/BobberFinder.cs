using System;
using System.Diagnostics;
using System.Drawing;
using System.Security.Policy;
using System.Threading;
using PatNagle.Logic.Image;
using PatNagle.Logic.Utils;
using PatNagle.User;

namespace PatNagle.Logic;

internal class BobberFinder
{
    private readonly ScreenRegion _r;
    private readonly Action<int, int> _foundDelegate;
    private readonly Action<int> _caughtDelegate;
    private readonly Action _castDelegate;
    private Thread _runner;
    private CancellationTokenSource _cts;
    private readonly Color _targetColor = Color.FromArgb(255, 125, 64, 31);

    public BobberFinder(ScreenRegion r, Action<int, int> foundDelegate, Action<int> caughtDelegate, Action castDelegate)
    {
        _r = r;
        _foundDelegate = foundDelegate;
        _caughtDelegate = caughtDelegate;
        _castDelegate = castDelegate;
    }

    public bool Done { get; private set; }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var ts = new ThreadStart(() => Finder(_r, _foundDelegate, _caughtDelegate, _castDelegate, _cts.Token));
        _runner = new Thread(ts);
        _runner.Start();
    }

    public void Stop()
    {
        Done = true;
        _cts.Cancel();
    }

    private void Finder(ScreenRegion region, Action<int, int> foundDelegate, Action<int> caughtDelegate, Action castDelegate, CancellationToken ctsToken)
    {
        try
        {
            int firstPosX = -1;
            int firstPosY = -1;
            int sleep = 33;
            var allProcesses = Process.GetProcesses();
            Process? wowProcess = null;
            foreach (var p in allProcesses)
            {
                if (p.ProcessName.Equals("Wow", StringComparison.OrdinalIgnoreCase))
                {
                    wowProcess = p;
                    break;
                }
            }

            if (wowProcess == null)
            {
                Done = true;
                Debug.WriteLine("No WoW window!");
                return;
            }

            AppScreen.SaveSelectedRegion(region);
            var found = false;
            (int x, int y) pos = (0, 0);
            DateTime firstStamp = DateTime.Now;
            while (!ctsToken.IsCancellationRequested)
            {
                using (var db = AppScreen.GetSelectedRegion(region))
                {
                    var dot = found ?
                        FindRedDot(db, Math.Max(pos.x - 20, 0), Math.Max(pos.y - 20, 0), Math.Min(pos.x + 20, db.Width), Math.Min(pos.y + 20, db.Height)) :
                        FindRedDot(db, 0, 0, db.Width, db.Height);
                    if (dot.found)
                    {
                        if (!found)
                        {
                            found = true;
                            pos = dot.pos;
                            foundDelegate(pos.x, pos.y);
                            firstStamp = DateTime.Now;
                        }
                        else
                        {
                            var distX = dot.pos.x - pos.x;
                            var distY = dot.pos.y - pos.y;
                            var dist = Math.Sqrt(distX * distX + distY * distY);
                            if (dist > 20)
                            {
                                caughtDelegate((int)dist);
                                Thread.Sleep(3000);
                                found = false;
                                castDelegate();
                            }
                        }
                    }

                    if (firstStamp.AddSeconds(30) < DateTime.Now)
                    {
                        found = false;
                        firstStamp = DateTime.Now;
                        castDelegate();
                    }
                }
                Thread.Sleep(sleep);
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
    }

    private (bool found, (int x, int y) pos) FindRedDot(
                           DirectBitmap db,
                           int startX, int startY,
                           int maxX, int maxY)
    {
        for (var i = startX; i < maxX; i++)
        {
            for (int j = startY; j < maxY; j++)
            {
                var color = db.GetPixel(i, j);
                var diff = color.GetDiff(_targetColor);
                if (diff < 100)
                {
                    return (true, (i, j));
                }
            }
        }

        return (false, (0, 0));
    }
}