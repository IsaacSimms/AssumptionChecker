///// loads and saves non-secret app settings to a JSON file in AppData /////

using System.IO;
using System.Text.Json;
using AssumptionChecker.WPFApp.Models;

namespace AssumptionChecker.WPFApp.Services
{
    // == service for loading and saving application settings to a JSON file in the user's AppData folder == //
    public class AppSettingsService : IAppSettingsService
    {
        // == settings file path == //
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AssumptionChecker",
            "wpf-settings.json");
        
        // == JSON serializer options == //
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        // == load settings from disk, returning defaults if file is missing or corrupt == //
        public AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            // == try to read and deserialize settings, returning defaults if anything goes wrong == //
            try
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        // == save settings to disk == //
        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
    }
}