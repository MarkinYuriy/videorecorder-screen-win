using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace VideoRecorderScreen.Services
{
    public class EncoderService
    {
        private LiveFrameSource? _frameSource;
        private Task? _encodeTask;
        private string? _tempVideoPath;

        public void Start(int width, int height, int fps, int bitrate, string tempDir)
        {
            // x264 requires even dimensions
            int w = width  % 2 == 0 ? width  : width  - 1;
            int h = height % 2 == 0 ? height : height - 1;

            _tempVideoPath = Path.Combine(tempDir, "video.mp4");
            _frameSource = new LiveFrameSource();

            var src = new RawVideoPipeSource(_frameSource) { FrameRate = fps };

            var ffmpegExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            if (!File.Exists(ffmpegExe))
                throw new FileNotFoundException($"ffmpeg.exe не найден в:\n{AppDomain.CurrentDomain.BaseDirectory}");

            // Task.Run prevents FFMpegCore pipe setup from blocking the UI thread
            _encodeTask = Task.Run(async () => await FFMpegArguments
                .FromPipeInput(src, o => o
                    .ForceFormat("rawvideo")
                    .WithCustomArgument($"-pix_fmt bgr24")
                    .WithCustomArgument($"-video_size {w}x{h}"))
                .OutputToFile(_tempVideoPath, overwrite: true, o => o
                    .WithVideoCodec("libx264")
                    .WithVideoBitrate(bitrate)
                    .WithFramerate(fps)
                    .WithCustomArgument("-pix_fmt yuv420p"))
                .ProcessAsynchronously());
        }

        public void AddFrame(Bitmap bmp) => _frameSource?.Enqueue(bmp);

        public async Task<string> StopAsync(string? audioWavPath, string finalPath)
        {
            _frameSource?.Complete();
            if (_encodeTask != null)
                await _encodeTask;

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            if (audioWavPath != null && File.Exists(audioWavPath) && _tempVideoPath != null)
            {
                await FFMpegArguments
                    .FromFileInput(_tempVideoPath)
                    .AddFileInput(audioWavPath)
                    .OutputToFile(finalPath, overwrite: true, o => o
                        .CopyChannel(FFMpegCore.Enums.Channel.Video)
                        .WithAudioCodec("aac")
                        .WithAudioBitrate(192))
                    .ProcessAsynchronously();

                File.Delete(_tempVideoPath);
                File.Delete(audioWavPath);
            }
            else if (_tempVideoPath != null)
            {
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(_tempVideoPath, finalPath);
            }

            return finalPath;
        }
    }

    internal class BitmapVideoFrame : IVideoFrame, IDisposable
    {
        private readonly Bitmap _bmp;
        public int Width => _bmp.Width;
        public int Height => _bmp.Height;
        public string Format => "bgr24";

        public BitmapVideoFrame(Bitmap bmp) => _bmp = bmp;

        private byte[] GetRawBgr24()
        {
            var data = _bmp.LockBits(
                new Rectangle(0, 0, Width, Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            try
            {
                int rowBytes = Width * 3;
                var buf = new byte[rowBytes * Height];
                for (int y = 0; y < Height; y++)
                    Marshal.Copy(data.Scan0 + y * data.Stride, buf, y * rowBytes, rowBytes);
                return buf;
            }
            finally { _bmp.UnlockBits(data); }
        }

        public void Serialize(Stream pipe)
        {
            var buf = GetRawBgr24();
            pipe.Write(buf, 0, buf.Length);
        }

        public Task SerializeAsync(Stream pipe, CancellationToken ct)
        {
            var buf = GetRawBgr24();
            return pipe.WriteAsync(buf, 0, buf.Length, ct);
        }

        public void Dispose() => _bmp.Dispose();
    }

    internal class LiveFrameSource : IEnumerable<IVideoFrame>
    {
        private readonly BlockingCollection<BitmapVideoFrame> _queue = new(boundedCapacity: 60);

        public void Enqueue(Bitmap bmp)
        {
            if (_queue.IsAddingCompleted) { bmp.Dispose(); return; }
            if (!_queue.TryAdd(new BitmapVideoFrame(bmp), millisecondsTimeout: 200))
                bmp.Dispose();
        }

        public void Complete() => _queue.CompleteAdding();

        public IEnumerator<IVideoFrame> GetEnumerator()
            => _queue.GetConsumingEnumerable().GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
