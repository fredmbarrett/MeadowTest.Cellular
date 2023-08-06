using Meadow;
using System;
using System.Collections.Generic;

namespace MeadowTest.Cellular
{
    public class TestAppSettings
    {
        // device settings
        public string DeviceName { get; set; } = "MeadowTest";

        // network settings
        public string WifiSsid { get; set; }
        public string WifiPassword { get; set; }
        public int WifiWakeUpDelaySeconds { get; set; } = 15;
        public int WifiMaxRetryCount { get; set; } = 3;
        public int WifiTimeoutSeconds { get; set; } = 30;

        // cell settings
        public string CellApnName { get; set; } = "";
        public int CellWakeUpDelaySeconds { get; set; } = 15;
        public int CellTimeoutSeconds { get; set; } = 60;
        public int CellRetryDelaySeconds { get; set; } = 900;
        public int CellMaxRetryCount { get; set; } = 3;

        private Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public TestAppSettings() { }

        public TestAppSettings(Dictionary<string, string> settings)
        {
            Settings = settings;

            foreach (var s in settings)
            {
                Resolver.Log.Trace($"{s.Key} = {s.Value}");
            }

            DeviceName = ParseStringSetting("TestApp.DeviceName", "MeadowTest");

            WifiSsid = ParseStringSetting("TestApp.WifiSsid", "");
            WifiPassword = ParseStringSetting("TestApp.WifiPassword", "");
            WifiWakeUpDelaySeconds = ParseIntSetting("TestApp.WifiWakeUpDelaySeconds", 15);
            WifiMaxRetryCount = ParseIntSetting("TestApp.WifiMaxRetryCount", 3);
            WifiTimeoutSeconds = ParseIntSetting("TestApp.WifiTimeoutSeconds", 30);

            CellApnName = ParseStringSetting("TestApp.CellApnName", "TestApp.vzwentp");
            CellWakeUpDelaySeconds = ParseIntSetting("TestApp.CellWakeUpDelaySeconds", 15);
            CellTimeoutSeconds = ParseIntSetting("TestApp.CellTimeoutSeconds", 60);
            CellRetryDelaySeconds = ParseIntSetting("TestApp.CellRetryDelaySeconds", 900);
            CellMaxRetryCount = ParseIntSetting("TestApp.CellMaxRetryCount", 3);
        }

        private string ParseStringSetting(string hive, string defaultValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hive)) { return defaultValue; }
                var result = Settings[hive] ?? defaultValue;
                return result.Replace("\"", "");
            }
            catch
            {
                return defaultValue;
            }
        }

        private int ParseIntSetting(string passedValue, int? defaultValue = 0)
        {
            string value = ParseStringSetting(passedValue, defaultValue.ToString());
            return value.ToInt(defaultValue);
        }

        private bool ParseBoolSetting(string passedValue)
        {
            string value = ParseStringSetting(passedValue, "false");
            return value.ToLower() == "true";
        }
    }

    public static class SettingsExtensions
    {
        /// <summary>
        /// Converts the string to an INT32 if a valid int value.
        /// Returns either a default value or zero if string is missing not valid int.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int ToInt(this string value, int? defaultValue)
        {
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out int result))
            {
                result = defaultValue ?? 0;
            }

            return result;
        }
    }
}
