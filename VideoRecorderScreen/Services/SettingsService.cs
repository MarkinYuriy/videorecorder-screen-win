using System.IO;
using System.Text.Json;
using VideoRecorderScreen.Models;

namespace VideoRecorderScreen.Services
{
    public class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenRecorder",
            "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public RecordingSettings Settings { get; private set; } = new();

        public void Load()
        {
            if (!File.Exists(SettingsPath))
            {
                Settings = new RecordingSettings();
                return;
            }
            try
            {
                var json = File.ReadAllText(SettingsPath);
                Settings = JsonSerializer.Deserialize<RecordingSettings>(json) ?? new RecordingSettings();
            }
            catch
            {
                Settings = new RecordingSettings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, JsonOptions));
        }
    }
}
