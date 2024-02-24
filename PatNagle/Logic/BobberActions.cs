using System;

namespace PatNagle.Logic;

internal class BobberActions
{
    public BobberActions(Action<int, int> foundDelegate, Action hookDelegate, Action castDelegate)
    {
        this.FoundDelegate = foundDelegate;
        this.HookDelegate = hookDelegate;
        this.CastDelegate = castDelegate;
    }

    internal Action<int, int> FoundDelegate { get; }
    internal Action HookDelegate { get; }
    internal Action CastDelegate { get; }
}