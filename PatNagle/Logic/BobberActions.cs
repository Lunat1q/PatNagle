using System;

namespace PatNagle.Logic;

internal class BobberActions
{
    public BobberActions(Action<int, int> foundDelegate, Action<int> caughtDelegate, Action castDelegate)
    {
        this.FoundDelegate = foundDelegate;
        this.CaughtDelegate = caughtDelegate;
        this.CastDelegate = castDelegate;
    }

    internal Action<int, int> FoundDelegate { get; }
    internal Action<int> CaughtDelegate { get; }
    internal Action CastDelegate { get; }
}