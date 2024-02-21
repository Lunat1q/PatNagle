using System.ComponentModel;
using System.Windows;
using TiqUtils.Serialize;
using TiqUtils.Wpf.UIBuilders;

namespace PatNagle.User
{
    [DisplayName("Fishing Settings")]
    internal class AppSettings
    {
        public static AppSettings Instance { get; } = Load(Settings.Default.data);
        
        public ScreenRegion Region { get; set; }

        [PropertyMember]
        [SliderLimits(5, 50, 1, 1, 1)]
        public int BobberZoneRange { get; set; } = 20;

        [PropertyMember]
        [SliderLimits(15, 40, 1, 1, 1)]
        public int BobberDiveThreshold { get; set; } = 20;

        [PropertyMember]
        [SliderLimits(20, 80, 1, 1, 1)]
        public int MouseHookXOffset { get; set; } = 55;

        [PropertyMember]
        [SliderLimits(10, 40, 1, 1, 1)]
        public int MouseHookYOffset { get; set; } = 15;
        
        [PropertyMember]
        [SliderLimits(20, 200, 1, 1, 1)]
        public int ColorMaxDistance { get; set; } = 100;

        public AppSettings()
        {
            Region = new ScreenRegion
            {
                LeftTop = new Point(300, 300),
                RightBottom = new Point(1100, 600),
                StartPercentageX = 30,
                EndPercentageX = 70,
                StartPercentageY = 30,
                EndPercentageY = 50
            };
        }

        private static AppSettings Load(string data)
        {
            var settings = data.DeserializeDataFromString<AppSettings>() ?? new AppSettings();
            return settings;
        }

        public static void Save()
        {
            Settings.Default.data = Instance.SerializeDataToString();
            Settings.Default.Save();
        }
    }
}
