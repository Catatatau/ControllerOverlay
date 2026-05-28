using System;
using System.IO;
using Newtonsoft.Json;

namespace ControllerOverlay.Settings
{
    public class SettingsManager
    {
        private static readonly string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControllerOverlay");
        private static readonly string SettingsFilePath = Path.Combine(AppDataFolder, "settings.json");

        public AppSettings CurrentSettings { get; private set; }

        public SettingsManager()
        {
            CurrentSettings = new AppSettings();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                    if (settings != null)
                    {
                        CurrentSettings = settings;
                    }
                }
            }
            catch (Exception)
            {
                // Fallback to default
                CurrentSettings = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder))
                {
                    Directory.CreateDirectory(AppDataFolder);
                }

                string json = JsonConvert.SerializeObject(CurrentSettings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}
