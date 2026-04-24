using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace VideoRecorderScreen.Services
{
    public class AudioCaptureService : IDisposable
    {
        private WasapiLoopbackCapture? _loopback;
        private WasapiCapture? _mic;
        private WaveFileWriter? _writer;

        public bool HasAudio { get; private set; }
        public string? TempWavPath { get; private set; }

        public void Start(bool micEnabled, bool sysAudioEnabled, string tempDir)
        {
            HasAudio = micEnabled || sysAudioEnabled;
            if (!HasAudio) return;

            TempWavPath = Path.Combine(tempDir, "audio.wav");

            if (sysAudioEnabled)
            {
                _loopback = new WasapiLoopbackCapture();
                _writer = new WaveFileWriter(TempWavPath, _loopback.WaveFormat);
                _loopback.DataAvailable += OnData;
                _loopback.StartRecording();
            }
            else
            {
                _mic = new WasapiCapture();
                _writer = new WaveFileWriter(TempWavPath, _mic.WaveFormat);
                _mic.DataAvailable += OnData;
                _mic.StartRecording();
            }
        }

        private void OnData(object? sender, WaveInEventArgs e)
            => _writer?.Write(e.Buffer, 0, e.BytesRecorded);

        public void Stop()
        {
            _loopback?.StopRecording();
            _mic?.StopRecording();
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }

        public void Dispose()
        {
            Stop();
            _loopback?.Dispose();
            _mic?.Dispose();
        }
    }
}
