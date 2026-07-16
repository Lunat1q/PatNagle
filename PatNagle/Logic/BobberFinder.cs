using PatNagle.Common;
using PatNagle.Logic.Control;
using PatNagle.Logic.Image;
using PatNagle.Logic.Utils;
using PatNagle.User;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace PatNagle.Logic;

internal class BobberFinder
{
    public delegate void PictureUpdated(object sender, Bitmap i);
    public delegate void NewBobberLocation(object sender, string coords);

    private readonly BobberActions _actions;
    private readonly MainFormContext _context;
    private readonly AppSettings _settings;
    private readonly Color _targetColor = Color.FromArgb(255, 130, 64, 18);
    private CancellationTokenSource? _cts;
    private Thread? _runner;

    public bool DebugIsOn = false;

    private readonly MouseKeeper _mouse = new();

    //debug section
    private const bool DisableCatch = false;

    public BobberFinder(AppSettings settings, BobberActions actions, MainFormContext context)
    {
        _settings = settings;
        _actions = actions;
        _context = context;
    }

    public event PictureUpdated? PictureChanged;
    public event NewBobberLocation? BobberFound;

    public FinderDiagnostics Diagnostics { get; } = new();

    public void Start()
    {
        Diagnostics.Reset();
        _cts = new CancellationTokenSource();
        var ts = new ThreadStart(() => Finder(_settings, _actions, _context, _cts.Token));
        _runner = new Thread(ts);
        _runner.Start();
    }

    public void Stop()
    {
        _context.Running = false;
        _cts?.Cancel();
    }

    private void Finder(AppSettings settings,
                        BobberActions actions,
                        MainFormContext context,
                        CancellationToken cancellationToken)
    {
        try
        {

            var allProcesses = Process.GetProcesses();
            var wowProcess = AppHelper.FindWowWindow();

            if (wowProcess == IntPtr.Zero)
            {
                Debug.WriteLine("No WoW window!");
                return;
            }

            //AppScreen.SaveSelectedRegion(settings.Region);
            var found = false;
            (int x, int y) pos = (0, 0);
            var firstStamp = DateTime.Now;
            var offset = settings.BobberZoneRange;
            var casts = 0;
            var hooks = 0;
            var fails = 0;
            context.Running = true;
            _mouse.Reset();
            var iteration = 0;
            var lastPeriodic = DateTime.Now;

            // Press the configured key between attempts once the delay elapses, then wait so the
            // in-game action finishes before the next cast. Never called mid-cast/mid-track.
            void TryPeriodicPress()
            {
                if (!settings.PeriodicKeyEnabled ||
                    lastPeriodic.AddMinutes(settings.PeriodicDelayMinutes) > DateTime.Now)
                {
                    return;
                }

                context.FishingStatus = "Periodic key...";
                actions.PeriodicDelegate();
                Thread.Sleep(settings.PeriodicWaitSeconds * 1000);
                lastPeriodic = DateTime.Now;
            }


            while (!cancellationToken.IsCancellationRequested)
            {
                using (var db = AppScreen.GetSelectedRegion(settings.Region))
                {
                    OnPictureChanged(db.Bitmap);
                    if (DebugIsOn)
                    {
                        // Sample how much of the whole frame matches the target colour (downsampled).
                        var (matched, total) = CountFrameMatches(db, 4);
                        Diagnostics.OnFrameSample(matched, total);
                    }
                    var dot = found
                        ? GetAverageRedColorPosition(db, offset, pos.x, pos.y, DebugIsOn)
                        : FindFirstRedDot(db, offset);
                    if (dot.found)
                    {
                        if (!found)
                        {
                            found = true;
                            pos = dot.pos;
                            actions.FoundDelegate(pos.x, pos.y);
                            _mouse.NotifyPlaced(context.CurMouseX, context.CurMouseY);
                            firstStamp = DateTime.Now;
                            this.OnBobberFound($"x: {pos.x} y: {pos.y}");
                            context.AddDistance(0);
                        }
                        else
                        {
                            var dist = CalculateYDistance(dot, pos);
                            Diagnostics.OnDive(dist);
                            context.FishingStatus = "Waiting...";
                            var posRecorder = 5 + 50 / settings.ThreadSleepTime;
                            if (iteration % posRecorder == 0)
                            {
                                context.AddDistance(-(int)dist);
                            }

                            if (dist > _settings.BobberDiveThreshold && !DisableCatch)
                            {
                                if (iteration % posRecorder != 0)
                                {
                                    context.AddDistance(-(int)dist);
                                }

                                Thread.Sleep(MouseControl.GetRandomDelay(500));
                                _mouse.EnsureAt(context.CurMouseX, context.CurMouseY);
                                actions.HookDelegate();
                                context.FishingStatus = "Hooked!";
                                hooks++;
                                Diagnostics.OnAttemptEnd(true);
                                UpdateStats(context, casts, hooks, fails);
                                Thread.Sleep(MouseControl.GetRandomDelay(2500));
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                found = false;
                                _mouse.Reset();
                                context.ClearAllDistance();
                                TryPeriodicPress();
                                actions.CastDelegate();
                                context.FishingStatus = "Casting...";
                                this.OnBobberFound("???");
                                Thread.Sleep(MouseControl.GetRandomDelay(2500));
                                casts++;
                                iteration = 0;
                            }
                        }
                    }

                    OnPictureChanged(db.Bitmap);

                    if (firstStamp.AddSeconds(30) < DateTime.Now && !DisableCatch)
                    {
                        found = false;
                        firstStamp = DateTime.Now;
                        Diagnostics.OnAttemptEnd(false);
                        _mouse.EnsureAt(context.CurMouseX, context.CurMouseY);
                        actions.HookDelegate();
                        _mouse.Reset();
                        Thread.Sleep(MouseControl.GetRandomDelay(1500));
                        TryPeriodicPress();
                        actions.CastDelegate();
                        context.FishingStatus = "Re-Casting...";
                        this.OnBobberFound("???");
                        Thread.Sleep(MouseControl.GetRandomDelay(2500));
                        fails++;
                        casts++;
                        iteration = 0;
                    }

                    // Keep the cursor parked on the bobber, yielding to active user movement.
                    if (found)
                    {
                        _mouse.Tick(context.CurMouseX, context.CurMouseY);
                    }

                    iteration++;
                }

                Thread.Sleep(settings.ThreadSleepTime);
            }

            context.Running = false;
        }
        catch (Exception e)
        {
            context.Running = false;
            Dialogs.ShowError($"Type: {e.GetType().Name}\r\nError: {e.Message}\r\nStack: {e.StackTrace}");
            Debug.WriteLine(e);
            throw;
        }
    }

    private static double CalculateYDistance((bool found, (int x, int y) pos) dot, (int x, int y) pos)
    {
        //var distX = dot.pos.x - pos.x;
        var distY = Math.Abs(dot.pos.y - pos.y);
        //var dist = Math.Sqrt(distX * distX + distY * distY);
        return distY;
    }
    
    private static double CalculateFullDistance((int x, int y) pos1, (int x, int y) pos2)
    {
        var distX = pos1.x - pos2.x;
        var distY = pos1.y - pos2.y;
        var dist = Math.Sqrt(distX * distX + distY * distY);
        return dist;
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

                    return GetAverageRedColorPosition(db, offset, i, j, DebugIsOn);
                    //return GetAverageRedColorPosition(db, offset, prevPos.x, prevPos.y, DebugIsOn);
                }
            }
        }

        return (false, (0, 0));
    }

    private (bool found, (int x, int y) pos) GetAverageRedColorPosition(DirectBitmap db, int offset, int x, int y, bool debug)
    {
        var dotsFound = 0;
        var xSum = 0;
        var ySum = 0;
        for (var i = x - offset; i < x + offset; i++)
        {
            // checking twice the offset to get a better average in case of bobber diving
            for (var j = y - offset; j < y + offset + offset; j++)
            {
                if (!CheckRangeOfCoords(db, i, j))
                {
                    continue;
                }
                
                var color = db.GetPixel(i, j);
                var diff = color.GetDiff(_targetColor);
                if (diff < _settings.ColorMaxDistance)
                {
                    dotsFound++;
                    xSum += i;
                    ySum += j;
                    if (debug)
                    {
                        db.SetPixel(i, j, Color.FromArgb(255, 255, 0, 0));
                    }
                }
            }
        }

        Diagnostics.OnTrackScan(dotsFound);

        if (dotsFound == 0)
        {
            return (false, (0, 0));
        }

        var xAvg = xSum / dotsFound;
        var yAvg = ySum / dotsFound;

        DrawDebugBoxes(debug, db, offset, x, y, yAvg, xAvg);

        return (true, (xAvg, yAvg));
    }

    private void DrawDebugBoxes(bool debugIsOn, DirectBitmap db, int offset, int x, int y, int yAvg, int xAvg)
    {
        if (debugIsOn)
        {
            for (int i = 0; i < db.Width; i++)
            {
                db.SetPixel(i, yAvg, Color.FromArgb(255, 0, 255, 0));
                db.SetPixel(i, y, Color.FromArgb(255, 0, 0, 255));
            }

            for (int i = 0; i < db.Height; i++)
            {
                db.SetPixel(xAvg, i, Color.FromArgb(255, 0, 255, 0));
                db.SetPixel(x, i, Color.FromArgb(255, 0, 0, 255));
            }

            //drawing bobber zone region
            for (var i = x - offset; i < x + offset; i++)
            {
                if (CheckRangeOfCoords(db, i, y - offset))
                {
                    db.SetPixel(i, y - offset, Color.FromArgb(255, 255, 0, 255));
                }
                if (CheckRangeOfCoords(db, i, y + offset + offset))
                {
                    db.SetPixel(i, y + offset + offset, Color.FromArgb(255, 255, 0, 255));
                }
            }

            // checking twice the offset to get a better average in case of bobber diving
            for (var j = y - offset; j < y + offset + offset; j++)
            {
                if (CheckRangeOfCoords(db, x - offset, j))
                {
                    db.SetPixel(x - offset, j, Color.FromArgb(255, 255, 0, 255));
                }

                if (CheckRangeOfCoords(db, x + offset, j))
                {
                    db.SetPixel(x + offset, j, Color.FromArgb(255, 255, 0, 255));
                }
            }
        }
    }

    private static bool CheckRangeOfCoords(DirectBitmap db, int x, int y)
    {
        return y >= 0 && y < db.Height && x >= 0 && x < db.Width;
    }

    private (int matched, int total) CountFrameMatches(DirectBitmap db, int step)
    {
        var matched = 0;
        var total = 0;
        for (var i = 0; i < db.Width; i += step)
        {
            for (var j = 0; j < db.Height; j += step)
            {
                total++;
                if (db.GetPixel(i, j).GetDiff(_targetColor) < _settings.ColorMaxDistance)
                {
                    matched++;
                }
            }
        }

        return (matched, total);
    }

    protected virtual void OnPictureChanged(Bitmap i)
    {
        PictureChanged?.Invoke(this, i);
    }

    protected virtual void OnBobberFound(string coords)
    {
        BobberFound?.Invoke(this, coords);
    }
}