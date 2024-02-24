using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        [SliderLimits(20, 800, 1, 1, 1)]
        public int ColorMaxDistance { get; set; } = 200;
        
        [PropertyMember]
        [SliderLimits(1, 333, 1, 1, 1)]
        public int ThreadSleepTime { get; set; } = 33;
        
        [PropertyMember]
        public Key CastKey { get; set; } = Key.F;

        [PropertyMember]
        [PropertyGroup("Start/Stop")]
        public Key StartStopKey { get; set; } = Key.F;

        [PropertyMember]
        [DisplayName("With Alt")]
        [PropertyGroup("Start/Stop")]
        public bool StartStopWithAlt { get; set; } = true;

        [PropertyMember]
        [DisplayName("With Control")]
        [PropertyGroup("Start/Stop")]
        public bool StartStopWithControl { get; set; } = true;

        [PropertyMember]
        [PropertyGroup("Topmost")]
        public Key TopmostKey { get; set; } = Key.T;

        [PropertyMember]
        [DisplayName("With Alt")]
        [PropertyGroup("Topmost")]
        public bool TopmostWithAlt { get; set; } = true;

        [PropertyMember]
        [DisplayName("With Control")]
        [PropertyGroup("Topmost")]
        public bool TopmostWithControl { get; set; }

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
