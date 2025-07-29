using CSVDiff.Models;
using CSVDiff.ViewModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSVDiff.Managers
{
    internal class SettingsManager
    {
        public UserSettings UserSettings { get; }

        public SettingsManager()
        {
            UserSettings = LoadSettings() ?? new UserSettings();
        }

        private static UserSettings? LoadSettings()
        {
            if (TryGetSettingsPath(out string path))
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        var json = System.IO.File.ReadAllText(path);
                        return JsonConvert.DeserializeObject<UserSettings>(json);
                    }
                    catch { }
                }
            }

            return null;
        }

        private static bool TryGetSettingsPath(out string path)
        {
            var executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var executableDirectory = System.IO.Path.GetDirectoryName(executablePath);
            if (executableDirectory == null)
            {
                path = null;
                return false;
            }

            path = System.IO.Path.Combine(executableDirectory, "settings.txt");
            return true;
        }

        public void SaveUserSettings()
        {
            if (TryGetSettingsPath(out string path)) 
            {
                try
                {
                    var json = JsonConvert.SerializeObject(UserSettings, Formatting.Indented);
                    System.IO.File.WriteAllText(path, json);
                }
                catch { }
            }
        }
    }
}
