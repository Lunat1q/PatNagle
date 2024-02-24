using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace PatNagle.Logic.Control;

public class HotKeyManager : IDisposable
{
    private readonly Window _window;
    private const int WmHotkey = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private Dictionary<int, Action> _registeredActions = new Dictionary<int, Action>();

    public HotKeyManager(Window window)
    {
        _window = window;
        // Register the window message handler for hotkeys
        HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle)!;
        source.AddHook(HwndHook);
    }


    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey)
        {
            int hotKeyId = wParam.ToInt32();
            if (_registeredActions.TryGetValue(hotKeyId, out var action))
            {
                action();
            }
        }

        return IntPtr.Zero;
    }

    public void RegisterHotKey(ModifierKeys modifiers, Key key, Action act)
    {
        IntPtr handle = new WindowInteropHelper(_window).Handle;

        uint fsModifiers = 0;

        if ((modifiers & ModifierKeys.Alt) != 0)
            fsModifiers |= (uint)ModifiersAlt;

        if ((modifiers & ModifierKeys.Control) != 0)
            fsModifiers |= (uint)ModifiersControl;

        if ((modifiers & ModifierKeys.Shift) != 0)
            fsModifiers |= (uint)ModifiersShift;

        if ((modifiers & ModifierKeys.Windows) != 0)
            fsModifiers |= (uint)ModifiersWin;

        var keyId = GetHotKeyId(modifiers, key);
        if (!_registeredActions.TryAdd(keyId, act))
        {
            throw new ArgumentException("Hotkey already registered");
        }

        if (!RegisterHotKey(handle, keyId, fsModifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
        {
            // Handle registration failure
        }
    }
    private int GetHotKeyId(ModifierKeys modifiers, Key key) { return ((int)key << 16) | (int)modifiers; }

    private void UnregisterHotKey()
    {
        int id = GetHashCode();
        IntPtr handle = new WindowInteropHelper(_window).Handle;

        UnregisterHotKey(handle, id);
    }

    private const uint ModifiersAlt = 0x0001;
    private const uint ModifiersControl = 0x0002;
    private const uint ModifiersShift = 0x0004;
    private const uint ModifiersWin = 0x0008;

    public void Dispose()
    {
        UnregisterHotKey();
    }
}