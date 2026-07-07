using System;

namespace PatNagle.Logic.Control;

/// <summary>
///     Keeps the cursor parked on the bobber while fishing, but yields to the user:
///     if the mouse is actively moved it's left alone, and only returned to the bobber
///     after it has been still for <see cref="IdleBeforeReturn" />. Also used to force
///     the cursor onto the bobber right before hooking.
/// </summary>
internal sealed class MouseKeeper
{
    private const int Tolerance = 4; // px; SetCursorPos rounding + tiny jitter
    private static readonly TimeSpan IdleBeforeReturn = TimeSpan.FromSeconds(3);

    private bool _active;
    private (int x, int y) _lastSeen;
    private DateTime _lastMovedAt;

    /// <summary>Record that we just placed the cursor on the bobber ourselves.</summary>
    public void NotifyPlaced(int x, int y)
    {
        _active = true;
        _lastSeen = (x, y);
        _lastMovedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Poll once per finder loop. Leaves the cursor where the user puts it; once it's
    ///     been still for the idle window and isn't on the bobber, moves it back.
    /// </summary>
    public void Tick(int targetX, int targetY)
    {
        if (!_active)
        {
            return;
        }

        var cursor = MouseControl.GetCursorPosition();
        if (Dist(cursor, _lastSeen) > Tolerance)
        {
            // Moved since we last looked - the user is driving; back off.
            _lastSeen = cursor;
            _lastMovedAt = DateTime.UtcNow;
            return;
        }

        if (DateTime.UtcNow - _lastMovedAt >= IdleBeforeReturn && Dist(cursor, (targetX, targetY)) > Tolerance)
        {
            MouseControl.SetCursorPos(targetX, targetY);
            _lastSeen = (targetX, targetY);
            _lastMovedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Put the cursor on the bobber now, unless it's already there (pre-hook).</summary>
    public void EnsureAt(int targetX, int targetY)
    {
        var cursor = MouseControl.GetCursorPosition();
        if (Dist(cursor, (targetX, targetY)) > Tolerance)
        {
            MouseControl.SetCursorPos(targetX, targetY);
        }

        _active = true;
        _lastSeen = (targetX, targetY);
        _lastMovedAt = DateTime.UtcNow;
    }

    /// <summary>Stop managing the cursor (between casts / not tracking a bobber).</summary>
    public void Reset() => _active = false;

    private static int Dist((int x, int y) a, (int x, int y) b)
        => Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
}
