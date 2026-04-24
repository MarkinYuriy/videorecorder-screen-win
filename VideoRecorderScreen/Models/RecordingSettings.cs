using System.IO;

namespace VideoRecorderScreen.Models
{
    public class RecordingSettings
    {
        public bool AutoFormatFilename { get; set; } = true;
        public string RecordingsFolder { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Recordings");
        public string Hotkey { get; set; } = "Ctrl+Shift+R";
        public int DefaultFps { get; set; } = 10;
        public int VideoBitrate { get; set; } = 4000;
        public bool LaunchWithWindows { get; set; } = false;
        public string Language { get; set; } = "";
        public bool LanguageUserSelected { get; set; } = false;

        // Last selected capture region (pixels, screen coordinates)
        public int RegionX { get; set; } = 0;
        public int RegionY { get; set; } = 0;
        public int RegionWidth { get; set; } = 1280;
        public int RegionHeight { get; set; } = 720;
    }
}
