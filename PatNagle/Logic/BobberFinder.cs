using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using PatNagle.Logic.Control;
using PatNagle.Logic.Image;
using PatNagle.Logic.Utils;
using PatNagle.User;

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
    
    //debug section
    private const bool DisableCatch = false;

    public BobberFinder(AppSettings settings, BobberActions actions, MainFormContext context)
    {
        _settings = settings;
        _actions = actions;
        _context = context;
        this.Done = true;
    }

    public bool Done { get; private set; }

    public event PictureUpdated? PictureChanged;
    public event NewBobberLocation? BobberFound;

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
        _context.Running = false;
        _cts?.Cancel();
    }

    private void Finder(AppSettings settings,
                        BobberActions actions,
                        MainFormContext context,
                        CancellationToken ctsToken)
    {
        try
        {
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
            context.Running = true;
            var iteration = 0;


            while (!ctsToken.IsCancellationRequested)
            {
                using (var db = AppScreen.GetSelectedRegion(settings.Region))
                {
                    OnPictureChanged(db.Bitmap);
                    var dot = found
                        ? GetAverageRedColorPosition(db, offset, pos.x, pos.y)
                        : FindFirstRedDot(db, offset);
                    if (dot.found)
                    {
                        if (!found)
                        {
                            found = true;
                            pos = dot.pos;
                            actions.FoundDelegate(pos.x, pos.y);
                            firstStamp = DateTime.Now;
                            this.OnBobberFound($"x: {pos.x} y: {pos.y}");
                            context.AddDistance(0);
                        }
                        else
                        {
                            var dist = CalculateDistance(dot, pos);
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

                                Thread.Sleep(MouseControl.GetRandomDelay(300));
                                actions.HookDelegate();
                                context.FishingStatus = "Hooked!";
                                hooks++;
                                UpdateStats(context, casts, hooks, fails);
                                Thread.Sleep(MouseControl.GetRandomDelay(2500));
                                if (ctsToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                found = false;
                                context.ClearAllDistance();
                                actions.CastDelegate();
                                context.FishingStatus = "Casting...";
                                this.OnBobberFound("???");
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
                        actions.HookDelegate();
                        Thread.Sleep(MouseControl.GetRandomDelay(1500));
                        actions.CastDelegate();
                        context.FishingStatus = "Re-Casting...";
                        this.OnBobberFound("???");
                        fails++;
                        casts++;
                        iteration = 0;
                    }

                    iteration++;
                }

                Thread.Sleep(settings.ThreadSleepTime);
            }

            context.Running = false;
        }
        catch (Exception e)
        {
            Done = false;
            context.Running = false;
            Debug.WriteLine(e);
            throw;
        }
    }

    private static double CalculateDistance((bool found, (int x, int y) pos) dot, (int x, int y) pos)
    {
        //var distX = dot.pos.x - pos.x;
        var distY = Math.Abs(dot.pos.y - pos.y);
        //var dist = Math.Sqrt(distX * distX + distY * distY);
        return distY;
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
                    if (DebugIsOn)
                    {
                        db.SetPixel(i, j, Color.FromArgb(255, 255, 0, 0));
                    }
                }
            }
        }

        if (dotsFound == 0)
        {
            return (false, (0, 0));
        }

        var xAvg = xSum / dotsFound;
        var yAvg = ySum / dotsFound;

        if (DebugIsOn)
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
        }

        return (true, (xAvg, yAvg));
    }

    private static bool CheckRangeOfCoords(DirectBitmap db, int x, int y)
    {
        return y >= 0 && y < db.Height && x >= 0 && x < db.Width;
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