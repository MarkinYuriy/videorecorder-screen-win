using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace VideoRecorderScreen.Services
{
    public class ScreenCaptureService : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _task;

        public event Action<Bitmap>? FrameReady;

        public void Start(Rectangle region, int fps)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _task = Task.Run(() => CaptureLoop(region, fps, token), token);
        }

        public async Task StopAsync()
        {
            _cts?.Cancel();
            if (_task != null)
                try { await _task; } catch (OperationCanceledException) { }
        }

        private void CaptureLoop(Rectangle region, int fps, CancellationToken ct)
        {
            var sw = new Stopwatch();
            long ticksPerFrame = Stopwatch.Frequency / fps;

            while (!ct.IsCancellationRequested)
            {
                sw.Restart();
                var bmp = CaptureFrame(region);
                FrameReady?.Invoke(bmp);

                long remaining = ticksPerFrame - sw.ElapsedTicks;
                if (remaining > 0)
                {
                    int sleepMs = (int)(remaining * 1000 / Stopwatch.Frequency) - 1;
                    if (sleepMs > 0) Thread.Sleep(sleepMs);
                    while (sw.ElapsedTicks < ticksPerFrame)
                        Thread.SpinWait(50);
                }
            }
        }

        private static Bitmap CaptureFrame(Rectangle region)
        {
            var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
            return bmp;
        }

        public void Dispose() => _cts?.Cancel();
    }
}
