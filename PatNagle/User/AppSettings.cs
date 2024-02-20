using System.Diagnostics;
using TiqUtils.Serialize;

namespace PatNagle.User
{
    internal class AppSettings
    {
        public static AppSettings Instance { get; } = Load(Settings.Default.data);
        public ScreenRegion? Region { get; set; }

        private static AppSettings Load(string data)
        {
            var settings = (data.DeserializeDataFromString<AppSettings>() ?? new AppSettings());
            return settings;
        }

        public static void Save()
        {
            Settings.Default.data = Instance.SerializeDataToString();
            Settings.Default.Save();
        }
    }
}
