using System.Collections.Generic;
using Avalonia.Input;

namespace PatNagle.Common;

/// <summary>
///     Maps Avalonia.Input.Key to Windows virtual-key codes. Replaces WPF's
///     KeyInterop.VirtualKeyFromKey (not available in Avalonia).
/// </summary>
public static class KeyCodes
{
    private static readonly Dictionary<Key, uint> Specials = new()
    {
        [Key.Back] = 0x08,
        [Key.Tab] = 0x09,
        [Key.Enter] = 0x0D,
        [Key.Escape] = 0x1B,
        [Key.Space] = 0x20,
        [Key.PageUp] = 0x21,
        [Key.PageDown] = 0x22,
        [Key.End] = 0x23,
        [Key.Home] = 0x24,
        [Key.Left] = 0x25,
        [Key.Up] = 0x26,
        [Key.Right] = 0x27,
        [Key.Down] = 0x28,
        [Key.Insert] = 0x2D,
        [Key.Delete] = 0x2E,
        [Key.OemTilde] = 0xC0,
        [Key.OemMinus] = 0xBD,
        [Key.OemPlus] = 0xBB,
    };

    public static uint VirtualKeyFromKey(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            return (uint)(0x41 + (key - Key.A));
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            return (uint)(0x30 + (key - Key.D0));
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return (uint)(0x60 + (key - Key.NumPad0));
        }

        if (key >= Key.F1 && key <= Key.F24)
        {
            return (uint)(0x70 + (key - Key.F1));
        }

        return Specials.TryGetValue(key, out var vk) ? vk : 0;
    }
}
