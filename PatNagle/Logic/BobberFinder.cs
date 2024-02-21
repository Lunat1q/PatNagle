using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using PatNagle.Logic.Image;
using PatNagle.Logic.Utils;
using PatNagle.User;

namespace PatNagle.Logic;

internal class BobberFinder
{
    private readonly AppSettings _settings;
    private readonly BobberActions _actions;
    private readonly MainFormContext _context;
    private Thread? _runner;
    private CancellationTokenSource? _cts;
    private readonly Color _targetColor = Color.FromArgb(255, 125, 64, 31);

    public BobberFinder(AppSettings settings, BobberActions actions, MainFormContext context)
    {
        _settings = settings;
        _actions = actions;
        _context = context;
    }

    public bool Done { get; private set; }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var ts = new ThreadStart(() => Finder(_settings, _actions, _context, _cts.Token));
        _runner = new Thread(ts);
        _runner.Start();
    }

    public void Stop()
    {
        Done = true;
        _cts?.Cancel();
    }

    private void Finder(AppSettings settings,
                        BobberActions actions,
                        MainFormContext context,
                        CancellationToken ctsToken)
    {
        try
        {
            var sleep = 33;
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

            AppScreen.SaveSelectedRegion(settings.Region);
            var found = false;
            (int x, int y) pos = (0, 0);
            var firstStamp = DateTime.Now;
            var offset = settings.BobberZoneRange;
            var casts = 0;
            var hooks = 0;
            var fails = 0;
            while (!ctsToken.IsCancellationRequested)
            {
                using (var db = AppScreen.GetSelectedRegion(settings.Region))
                {
                    var dot = found ?
                        GetAverageRedColorPosition(db, offset, pos.x, pos.y) :
                        FindFirstRedDot(db, offset);
                    if (dot.found)
                    {
                        if (!found)
                        {
                            found = true;
                            pos = dot.pos;
                            actions.FoundDelegate(pos.x, pos.y);
                            firstStamp = DateTime.Now;
                            context.BobberLocation = $"x: {pos.x} y: {pos.y}";
                            context.Items.Add(0);
                        }
                        else
                        {
                            var distX = dot.pos.x - pos.x;
                            var distY = dot.pos.y - pos.y;
                            var dist = Math.Sqrt(distX * distX + distY * distY);
                            context.FishingStatus = "Waiting...";
                            context.Items.Add(-(int)dist);
                            if (dist > _settings.BobberDiveThreshold)
                            {
                                actions.CaughtDelegate((int)dist);
                                context.FishingStatus = "Hooked!";
                                hooks++;
                                this.UpdateStats(context, casts, hooks, fails);
                                Thread.Sleep(3000);
                                found = false;
                                actions.CastDelegate();
                                context.FishingStatus = "Casting...";
                                context.BobberLocation = "???";
                                casts++;
                            }
                        }
                    }

                    if (firstStamp.AddSeconds(30) < DateTime.Now)
                    {
                        found = false;
                        firstStamp = DateTime.Now;
                        actions.CastDelegate();
                        context.FishingStatus = "Re-Casting...";
                        context.BobberLocation = "???";
                        fails++;
                        casts++;
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

    private void UpdateStats(MainFormContext context, int casts, int hooks, int fails)
    {
        context.UpdateStats(casts, hooks, fails);
    }

    private (bool found, (int x, int y) pos) FindFirstRedDot(DirectBitmap db, int offset)
    {
        for (var i = 0; i < db.Width; i++)
        {
            for (var j = 0; j < db.Height; j++)
            {
                var color = db.GetPixel(i, j);
                var diff = color.GetDiff(_targetColor);
                if (diff < _settings.ColorMaxDistance)
                {
                    return GetAverageRedColorPosition(db, offset, i, j);
                }
            }
        }

        return (false, (0, 0));
    }

    private (bool found, (int x, int y) pos) GetAverageRedColorPosition(DirectBitmap db, int offset, int x, int y)
    {
        var dotsFound = 0;
        var xSum = 0;
        var ySum = 0;
        for (var i = x - offset; i < x + offset; i++)
        {
            // checking twice the offset to get a better average in case of bobber diving
            for (var j = y - 2 * offset; j < y + offset; j++)
            {
                var color = db.GetPixel(x, y);
                var diff = color.GetDiff(_targetColor);
                if (diff < _settings.ColorMaxDistance)
                {
                    dotsFound++;
                    xSum += i;
                    ySum += j;
                }
            }
        }

        if (dotsFound == 0)
        {
            return (false, (0, 0));
        }
        return (true, (xSum / dotsFound, ySum / dotsFound));
    }

    private (bool found, (int x, int y) pos) FindRedDot(DirectBitmap db,
                                                        int startX,
                                                        int startY,
                                                        int maxX,
                                                        int maxY,
                                                        int offset,
                                                        bool found)
    {
        for (var i = startX; i < maxX; i++)
        {
            for (var j = startY; j < maxY; j++)
            {
                var color = db.GetPixel(i, j);
                var diff = color.GetDiff(_targetColor);
                if (diff < _settings.ColorMaxDistance)
                {
                    return (true, (i, j));
                }
            }
        }

        return (false, (0, 0));
    }


    private (bool found, (int x, int y) pos) FindAverageRedDotPosition(DirectBitmap db,
                                                        int startX,
                                                        int startY,
                                                        int maxX,
                                                        int maxY,
                                                        int offset,
                                                        bool found)
    {
        for (var i = startX; i < maxX; i++)
        {
            for (var j = startY; j < maxY; j++)
            {
                var color = db.GetPixel(i, j);
                var diff = color.GetDiff(_targetColor);
                if (diff < _settings.ColorMaxDistance)
                {
                    return (true, (i, j));
                }
            }
        }

        return (false, (0, 0));
    }


}