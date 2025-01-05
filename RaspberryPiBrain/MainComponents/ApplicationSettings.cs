using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MainComponents
{
    public static class ApplicationSettings
    {
        public const string DateFormat = "yyyy.MM.dd HH:mm:ss.fff";
        private const string SettingsFileName = "settingsRPIBrain.json";

        #region OrganizationalFunctions
#pragma warning disable CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ zadeklarowanie pola jako dopuszczającego wartość null.
#pragma warning disable IDE1006 // Style nazewnictwa
        private static ApplicationSettingsModel applicationSettings {  get; set; }
#pragma warning restore IDE1006 // Style nazewnictwa
#pragma warning restore CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ zadeklarowanie pola jako dopuszczającego wartość null.

        private static void LoadData()
        {
            applicationSettings = FileManagement.LoadModelFromFile<ApplicationSettingsModel>(SettingsFileName) ?? ApplicationSettingsModel.Default;
        }

        private static void SaveData()
        {
            FileManagement.SaveModelToFile(applicationSettings, SettingsFileName);
        }

        private static T GetSetting<T>(string propertyName)
        {
            LoadData();
            PropertyInfo? property = typeof(ApplicationSettingsModel).GetProperty(propertyName);
#pragma warning disable CS8600 // Konwertowanie literału null lub możliwej wartości null na nienullowalny typ.
#pragma warning disable CS8603 // Możliwe zwrócenie odwołania o wartości null.
            return property != null ? (T)property.GetValue(applicationSettings) : default;
#pragma warning restore CS8603 // Możliwe zwrócenie odwołania o wartości null
#pragma warning restore CS8600 // Konwertowanie literału null lub możliwej wartości null na nienullowalny typ.
        }

        private static void SetSetting<T>(string propertyName, T value)
        {
            PropertyInfo? property = typeof(ApplicationSettingsModel).GetProperty(propertyName);
            if (property != null)
            {
                var currentValue = property.GetValue(applicationSettings);

                if (!Equals(currentValue, value))
                {
                    property.SetValue(applicationSettings, value);
                    SaveData();
                }
            }
        }

        public static void CheckRefresh()
        {
            if (RefreshSettings) RefreshSettings = false;
        }

        #endregion

        public static bool RefreshSettings
        {
            get => GetSetting<bool>(nameof(RefreshSettings));
            set => SetSetting(nameof(RefreshSettings), value);
        }

        public static bool Debug
        {
            get => GetSetting<bool>(nameof(Debug));
            set => SetSetting(nameof(Debug), value);
        }
        
        public static bool FrameLogs
        {
            get => GetSetting<bool>(nameof(FrameLogs));
            set => SetSetting(nameof(FrameLogs), value);
        }

        public static string GniazdkaSerial
        {
            get => GetSetting<string>(nameof(GniazdkaSerial));
            set => SetSetting(nameof(GniazdkaSerial), value);
        }

        public static string OswietlenieSerial
        {
            get => GetSetting<string>(nameof(OswietlenieSerial));
            set => SetSetting(nameof(OswietlenieSerial), value);
        }

        public static int LoopDelay
        {
            get => GetSetting<int>(nameof(LoopDelay));
            set => SetSetting(nameof(LoopDelay), value);
        }

        public static string KeyID
        {
            get => GetSetting<string>(nameof(KeyID));
            set => SetSetting(nameof(KeyID), value);
        }

        public static string MyWebsite
        {
            get => GetSetting<string>(nameof(MyWebsite));
            set => SetSetting(nameof(MyWebsite), value);
        }
    }
}