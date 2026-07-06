using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Win32;
using PatNagle.Common;

namespace PatNagle.Logic.Control;

public class HotKeyManager : IDisposable
{
    private readonly Window _window;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, Action> _registeredActions = new();
    private readonly List<int> _registeredIds = new();

    public HotKeyManager(Window window)
    {
        _window = window;
        // Register the window message handler for hotkeys (Win32-only Avalonia backend).
        Win32Properties.AddWndProcHookCallback(_window, HwndHook);
    }

    private IntPtr HwndHook(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            int hotKeyId = wParam.ToInt32();
            if (_registeredActions.TryGetValue(hotKeyId, out var action))
            {
                action();
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    private IntPtr Handle => _window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

    public void RegisterHotKey(KeyModifiers modifiers, Key key, Action act)
    {
        uint fsModifiers = 0;

        if ((modifiers & KeyModifiers.Alt) != 0)
            fsModifiers |= ModifiersAlt;

        if ((modifiers & KeyModifiers.Control) != 0)
            fsModifiers |= ModifiersControl;

        if ((modifiers & KeyModifiers.Shift) != 0)
            fsModifiers |= ModifiersShift;

        if ((modifiers & KeyModifiers.Meta) != 0)
            fsModifiers |= ModifiersWin;

        var keyId = GetHotKeyId(modifiers, key);
        if (!_registeredActions.TryAdd(keyId, act))
        {
            throw new ArgumentException("Hotkey already registered");
        }

        if (!RegisterHotKey(Handle, keyId, fsModifiers, KeyCodes.VirtualKeyFromKey(key)))
        {
            // Handle registration failure
        }
        else
        {
            _registeredIds.Add(keyId);
        }
    }

    private static int GetHotKeyId(KeyModifiers modifiers, Key key) => ((int)key << 16) | (int)modifiers;

    private const uint ModifiersAlt = 0x0001;
    private const uint ModifiersControl = 0x0002;
    private const uint ModifiersShift = 0x0004;
    private const uint ModifiersWin = 0x0008;

    public void Dispose()
    {
        var handle = Handle;
        foreach (var id in _registeredIds)
        {
            UnregisterHotKey(handle, id);
        }

        _registeredIds.Clear();
        _registeredActions.Clear();
    }
}
