using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using PatNagle.Common.AutoUI;
using TiqUtils.Serialize;

namespace PatNagle.User
{
    [DisplayName("Fishing Settings")]
    internal class AppSettings
    {
        public static AppSettings Instance { get; } = Load(Settings.Default.data);

        public ScreenRegion Region { get; set; }

        [PropertyMember]
        [SliderLimits(5, 100, 1, 1, 1)]
        public int BobberZoneRange { get; set; } = 20;

        [PropertyMember]
        [SliderLimits(15, 40, 1, 1, 1)]
        public int BobberDiveThreshold { get; set; } = 16;

        [PropertyMember]
        [SliderLimits(20, 80, 1, 1, 1)]
        public int MouseHookXOffset { get; set; } = 55;

        [PropertyMember]
        [SliderLimits(10, 40, 1, 1, 1)]
        public int MouseHookYOffset { get; set; } = 15;

        [PropertyMember]
        [SliderLimits(20, 5000, 1, 1, 1)]
        public int ColorMaxDistance { get; set; } = 500;

        [PropertyMember]
        [SliderLimits(1, 333, 1, 1, 1)]
        public int ThreadSleepTime { get; set; } = 33;

        [PropertyMember]
        public Key CastKey { get; set; } = Key.E;

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
        [DisplayName("Enabled")]
        [PropertyGroup("Periodic Key")]
        public bool PeriodicKeyEnabled { get; set; }

        [PropertyMember]
        [DisplayName("Key")]
        [PropertyGroup("Periodic Key")]
        public Key PeriodicKey { get; set; } = Key.D1;

        [PropertyMember]
        [DisplayName("Delay (min)")]
        [PropertyGroup("Periodic Key")]
        [SliderLimits(1, 10, 1, 1, 1)]
        public int PeriodicDelayMinutes { get; set; } = 5;

        [PropertyMember]
        [DisplayName("Wait (sec)")]
        [PropertyGroup("Periodic Key")]
        [SliderLimits(1, 10, 1, 1, 1)]
        public int PeriodicWaitSeconds { get; set; } = 3;

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
