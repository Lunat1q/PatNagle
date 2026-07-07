using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PatNagle.Logic.Control
{
    internal static class MouseControl
    {
        private const uint WmRbuttondown = 516;
        private const uint WmRbuttonup = 517;
        private static readonly Random Rnd = new Random();
        
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MouseEventFMove = 0x0001;

        public static void Move(int xDelta, int yDelta)
        {
            mouse_event(MouseEventFMove, xDelta, yDelta, 0, 0);
        }
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        public static (int x, int y) GetCursorPosition()
        {
            return GetCursorPos(out var p) ? (p.X, p.Y) : (0, 0);
        }


        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SendNotifyMessage(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);
        private static long MakeDWord(int loWord, int hiWord)
        {
            return (hiWord << 16) | (loWord & 0xFFFF);
        }
        
        public static int GetRandomDelay(int baseDelay = 80)
        {
            return baseDelay + Rnd.Next(-1 * baseDelay / 5, baseDelay / 3);
        }

        public static void RightClick(IntPtr window, int x, int y)
        {
            var dWord = MakeDWord(x, y);

            SendNotifyMessage(window, WmRbuttondown, (UIntPtr)1, (IntPtr)dWord);
            Thread.Sleep(GetRandomDelay());
            SendNotifyMessage(window, WmRbuttonup, (UIntPtr)1, (IntPtr)dWord);
        }
    }
}
