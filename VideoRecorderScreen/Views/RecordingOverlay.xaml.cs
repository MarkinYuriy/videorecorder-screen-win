using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Interop;
using Size = System.Windows.Size;

namespace VideoRecorderScreen.Views
{
    public partial class RecordingOverlay : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        private readonly Rect _region;
        private readonly DispatcherTimer _blinkTimer;
        private readonly DispatcherTimer _clockTimer;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private bool _dotVisible = true;

        public event Action? StopRequested;

        public RecordingOverlay(Rect region)
        {
            InitializeComponent();
            _region = region;

            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _blinkTimer.Tick += (_, _) =>
            {
                _dotVisible = !_dotVisible;
                RecDot.Opacity = _dotVisible ? 1.0 : 0.0;
            };

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => UpdateClock();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetWindowDisplayAffinity(new WindowInteropHelper(this).Handle, WDA_EXCLUDEFROMCAPTURE);

            double ox = -SystemParameters.VirtualScreenLeft;
            double oy = -SystemParameters.VirtualScreenTop;

            // Рамка
            Canvas.SetLeft(RegionBorder, _region.X + ox);
            Canvas.SetTop(RegionBorder, _region.Y + oy);
            RegionBorder.Width = _region.Width;
            RegionBorder.Height = _region.Height;

            // Панель упр��вления ��� над рамкой или внутри если нет места
            ControlPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double panelH = ControlPanel.DesiredSize.Height;
            double px = _region.X + ox;
            double py = _region.Y + oy - panelH - 6;
            if (py < 0) py = _region.Y + oy + 6;
            Canvas.SetLeft(ControlPanel, px);
            Canvas.SetTop(ControlPanel, py);

            UpdateClock();
            _blinkTimer.Start();
            _clockTimer.Start();
        }

        private void UpdateClock()
        {
            var t = _stopwatch.Elapsed;
            TimerLabel.Text = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _blinkTimer.Stop();
            _clockTimer.Stop();
            StopRequested?.Invoke();
            Close();
        }
    }
}
