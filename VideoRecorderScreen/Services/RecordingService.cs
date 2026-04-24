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
            AppLogger.Log($"RecordingService.Start: region={settings.Region} fps={settings.Fps}");
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

        // Returns path to merged temp file (not yet at final destination)
        public async Task<string> StopAsync()
        {
            AppLogger.Log("RecordingService.StopAsync: stopping capture");
            IsRecording = false;
            await _screen.StopAsync();
            _audio.Stop();

            var tempFinal = Path.Combine(_tempDir!, "output.mp4");
            AppLogger.Log($"RecordingService.StopAsync: encoding to {tempFinal}, hasAudio={_audio.HasAudio}");
            await _encoder.StopAsync(_audio.HasAudio ? _audio.TempWavPath : null, tempFinal);
            AppLogger.Log($"RecordingService.StopAsync: done, file exists={File.Exists(tempFinal)}");
            return tempFinal;
        }

        public void Cleanup()
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
