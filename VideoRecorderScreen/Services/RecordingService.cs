using System.Drawing;
using VideoRecorderScreen.Models;

namespace VideoRecorderScreen.Services
{
    public class RecordingService : IDisposable
    {
        private readonly ScreenCaptureService _screen = new();
        private readonly AudioCaptureService _audio = new();
        private readonly EncoderService _encoder = new();
        private string? _tempDir;

        public bool IsRecording { get; private set; }

        public void Start(WizardResult settings)
        {
            IsRecording = true;

            _tempDir = Path.Combine(Path.GetTempPath(),
                "ScreenRecorder_" + DateTime.Now.Ticks);
            Directory.CreateDirectory(_tempDir);

            int rw = (int)settings.Region.Width;
            int rh = (int)settings.Region.Height;
            var region = new Rectangle(
                (int)settings.Region.X,
                (int)settings.Region.Y,
                rw % 2 == 0 ? rw : rw - 1,
                rh % 2 == 0 ? rh : rh - 1);

            _audio.Start(settings.MicEnabled, settings.SystemAudioEnabled, _tempDir);

            _encoder.Start(region.Width, region.Height,
                settings.Fps, App.SettingsService.Settings.VideoBitrate, _tempDir);

            _screen.FrameReady += _encoder.AddFrame;
            _screen.Start(region, settings.Fps);
        }

        public async Task<string> StopAsync()
        {
            IsRecording = false;

            await _screen.StopAsync();
            _audio.Stop();

            var finalPath = BuildOutputPath();
            await _encoder.StopAsync(_audio.HasAudio ? _audio.TempWavPath : null, finalPath);

            CleanupTemp();
            return finalPath;
        }

        private string BuildOutputPath()
        {
            var s = App.SettingsService.Settings;
            Directory.CreateDirectory(s.RecordingsFolder);

            if (s.AutoFormatFilename)
                return Path.Combine(s.RecordingsFolder,
                    $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm}.mp4");

            return Path.Combine(s.RecordingsFolder, "recording.mp4");
        }

        private void CleanupTemp()
        {
            try
            {
                if (_tempDir != null && Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch { }
        }

        public void Dispose()
        {
            _screen.Dispose();
            _audio.Dispose();
        }
    }
}
