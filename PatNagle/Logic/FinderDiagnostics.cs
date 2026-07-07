using System;
using System.Collections.Generic;
using System.Linq;
using PatNagle.User;

namespace PatNagle.Logic;

/// <summary>
///     Collects runtime detection/hook stats from the finder and turns them into
///     human-readable tuning suggestions shown in the debug window. Thread-safe:
///     the finder writes from its worker thread, the UI reads from the dispatcher.
/// </summary>
internal sealed class FinderDiagnostics
{
    private const int Window = 40;

    // ponytail: heuristic cut-offs, tune here. Frame ratio is fraction of the whole
    // capture that matches the target colour; a real feather is well under 1%.
    private const double TooLooseFrameRatio = 0.05;
    private const double NoMatchFrameRatio = 0.001;
    private const int WeakTrackDots = 3;
    private const int MinAttempts = 2;
    private const double HighFailRate = 0.4;

    private readonly object _lock = new();
    private readonly Queue<int> _trackDots = new();               // matched pixels while locked on (positive only)
    private readonly Queue<double> _frameRatios = new();          // matched / sampled across the whole frame
    private readonly Queue<(double MaxDive, bool Hooked)> _attempts = new();

    private double _currentMaxDive;

    public void Reset()
    {
        lock (_lock)
        {
            _trackDots.Clear();
            _frameRatios.Clear();
            _attempts.Clear();
            _currentMaxDive = 0;
        }
    }

    public void OnTrackScan(int dotsFound)
    {
        // Only record real detections. A zero here means the feather was lost (dived out of
        // the box, or disappeared) - counting it would look like weak colour matching.
        if (dotsFound <= 0)
        {
            return;
        }

        lock (_lock)
        {
            Push(_trackDots, dotsFound);
        }
    }

    public void OnFrameSample(int matched, int total)
    {
        if (total <= 0)
        {
            return;
        }

        lock (_lock)
        {
            Push(_frameRatios, (double)matched / total);
        }
    }

    public void OnDive(double dist)
    {
        lock (_lock)
        {
            if (dist > _currentMaxDive)
            {
                _currentMaxDive = dist;
            }
        }
    }

    public void OnAttemptEnd(bool hooked)
    {
        lock (_lock)
        {
            Push(_attempts, (_currentMaxDive, hooked));
            _currentMaxDive = 0;
        }
    }

    public IReadOnlyList<string> BuildSuggestions(AppSettings settings)
    {
        int[] dots;
        double[] frameRatios;
        (double MaxDive, bool Hooked)[] attempts;

        lock (_lock)
        {
            dots = _trackDots.ToArray();
            frameRatios = _frameRatios.ToArray();
            attempts = _attempts.ToArray();
        }

        var lines = new List<string>();

        if (dots.Length == 0 && frameRatios.Length == 0 && attempts.Length == 0)
        {
            lines.Add("No data yet - start fishing to gather tuning stats.");
            return lines;
        }

        AddColorSuggestion(lines, settings, dots, frameRatios);
        AddDiveSuggestion(lines, settings, attempts);
        return lines;
    }

    private static void AddColorSuggestion(List<string> lines, AppSettings settings, int[] dots, double[] frameRatios)
    {
        var color = settings.ColorMaxDistance;

        // Too loose: a large fraction of the whole screen matches the target colour
        // (many clusters / everything lights up).
        if (frameRatios.Length >= 5)
        {
            var ratio = Median(frameRatios);
            if (ratio >= TooLooseFrameRatio)
            {
                lines.Add($"Too much of the screen matches ({ratio:P1}) - many false clusters. " +
                          $"Reduce Color Max Distance (now {color}, try ~{Suggest(color * 0.5, 20, 5000)}).");
                return;
            }

            // Nothing at all matches AND we never once locked on -> threshold too tight
            // (or the target colour is off). Distinct from a bobber that was tracked then lost.
            if (ratio < NoMatchFrameRatio && dots.Length == 0)
            {
                lines.Add($"No feather color detected anywhere on screen. Color Max Distance may be too low - " +
                          $"increase it (now {color}, try ~{Suggest(color * 2, 20, 5000)}).");
                return;
            }
        }

        // Too tight: when locked on, only a couple of pixels match the feather.
        if (dots.Length >= 5)
        {
            var median = Median(dots.Select(d => (double)d).ToArray());
            if (median <= WeakTrackDots)
            {
                lines.Add($"Feather detection weak (~{median:0} matched pixels when tracking). " +
                          $"Increase Color Max Distance (now {color}, try ~{Suggest(color * 1.5, 20, 5000)}).");
            }
            else
            {
                lines.Add("Color detection looks OK.");
            }
        }
    }

    private static void AddDiveSuggestion(List<string> lines, AppSettings settings, (double MaxDive, bool Hooked)[] attempts)
    {
        if (attempts.Length < MinAttempts)
        {
            return;
        }

        var threshold = settings.BobberDiveThreshold;
        var failRate = 1.0 - (double)attempts.Count(a => a.Hooked) / attempts.Length;
        var observedMax = attempts.Max(a => a.MaxDive);

        if (failRate >= HighFailRate)
        {
            if (observedMax < 5)
            {
                lines.Add("Hooks are failing and almost no dive is measured - the bobber is being lost " +
                          "(check the capture region / color) rather than diving too little.");
            }
            else if (observedMax < threshold)
            {
                lines.Add($"Low hook rate ({failRate:P0} missed): bobber only dives to ~{observedMax:0}px but the " +
                          $"hook threshold is {threshold}. Lower Bobber Dive Threshold to ~{Suggest(observedMax - 2, 15, 40)}.");
            }
            else
            {
                lines.Add($"Dives reach the threshold ({threshold}) but hooks still miss ({failRate:P0}) - " +
                          "likely a cast/hook timing or mouse-offset issue, not the threshold.");
            }
        }
        else if (observedMax > threshold * 2)
        {
            lines.Add($"Dives greatly exceed the threshold (peak ~{observedMax:0}px vs {threshold}). " +
                      $"You can raise Bobber Dive Threshold toward ~{Suggest(observedMax * 0.6, 15, 40)} to reject false dives.");
        }
        else
        {
            lines.Add("Dive threshold looks OK.");
        }
    }

    private static int Suggest(double value, int min, int max)
        => Math.Clamp((int)Math.Round(value), min, max);

    private static double Median(double[] values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static void Push<T>(Queue<T> queue, T item)
    {
        queue.Enqueue(item);
        while (queue.Count > Window)
        {
            queue.Dequeue();
        }
    }
}
