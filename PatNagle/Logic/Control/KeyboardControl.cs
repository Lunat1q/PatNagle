using System.Runtime.InteropServices;
using System;

namespace PatNagle.Logic.Control;

public class KeyboardControl
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

    public const int KEYEVENTF_KEYDOWN = 0x0001;
    public const int KEYEVENTF_KEYUP = 0x0002;

    public static void SimulateKeyPress(byte virtualKeyCode)
    {
        keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYDOWN, IntPtr.Zero);
    }

    public static void SimulateKeyRelease(byte virtualKeyCode)
    {
        keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }

    public static void SimulateFPress()
    {
        // Simulate pressing and releasing the 'B' key with a delay in between
        SimulateKeyPress(0x46); // 'F' key
        System.Threading.Thread.Sleep(MouseControl.GetRandomDelay(300)); // 1 second delay
        SimulateKeyRelease(0x46); // Release 'F' key
    }
}
