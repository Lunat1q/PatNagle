using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TiqUtils.SettingsController;

namespace PatNagle.Logic.Utils
{
    internal class AppHelper
    {
        public static IntPtr GetProcessByName(string name)
        {
            var process = Process.GetProcessesByName(name).FirstOrDefault();
            return process?.MainWindowHandle ?? IntPtr.Zero;
        }
        
        public static IntPtr FindWowWindow()
        {
            var result = GetProcessByName("Wow");
            if (result != IntPtr.Zero)
            {
                return result;
            }
            var processlist = Process.GetProcesses();
            foreach (var process in processlist)
            {
                if (process.MainWindowTitle.Equals("WORLD OF WARCRAFT", StringComparison.OrdinalIgnoreCase))
                {
                    return process.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }
    }
}
