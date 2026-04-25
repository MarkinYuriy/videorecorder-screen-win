using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace VideoRecorderScreen.Services
{
    public class ScreenCaptureService : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CURSORINFO pci);
        [DllImport("user32.dll")] static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr hIcon, int cx, int cy, int step, IntPtr hbr, int flags);

        private const int CURSOR_SHOWING = 0x1;
        private const int DI_NORMAL = 0x3;
        private CancellationTokenSource? _cts;
        private Task? _task;
        private bool _captureCursor;

        public event Action<Bitmap>? FrameReady;

        public void Start(Rectangle region, int fps, bool captureCursor = true)
        {
            _captureCursor = captureCursor;
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
                var bmp = CaptureFrame(region, _captureCursor);
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

        private static Bitmap CaptureFrame(Rectangle region, bool captureCursor)
        {
            var bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
            if (captureCursor) DrawCursor(g, region);
            return bmp;
        }

        private static void DrawCursor(Graphics g, Rectangle region)
        {
            var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
            if (!GetCursorInfo(ref ci) || (ci.flags & CURSOR_SHOWING) == 0) return;

            int x = ci.ptScreenPos.x - region.X;
            int y = ci.ptScreenPos.y - region.Y;

            var hdc = g.GetHdc();
            try { DrawIconEx(hdc, x, y, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL); }
            finally { g.ReleaseHdc(hdc); }
        }

        public void Dispose() => _cts?.Cancel();
    }
}
