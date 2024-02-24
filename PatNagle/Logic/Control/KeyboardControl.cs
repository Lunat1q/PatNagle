using System.Runtime.InteropServices;
using System;
using System.Windows.Input;

namespace PatNagle.Logic.Control;

public class KeyboardControl
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    private const int KeyEventKeydown = 0x0001;
    private const int KeyEventKeyup = 0x0002;

    public static void SimulateKeyPress(byte virtualKeyCode)
    {
        keybd_event(virtualKeyCode, 0, KeyEventKeydown, IntPtr.Zero);
    }

    public static void SimulateKeyRelease(byte virtualKeyCode)
    {
        keybd_event(virtualKeyCode, 0, KeyEventKeyup, IntPtr.Zero);
    }

    public static void SimulatePress(Key instanceCastKey)
    {
        var vk = (byte)KeyInterop.VirtualKeyFromKey(instanceCastKey);
        SimulateKeyPress(vk); // Press key
        System.Threading.Thread.Sleep(MouseControl.GetRandomDelay(300)); // some delay
        SimulateKeyRelease(vk); // Release key
    }
}
