using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VideoRecorderScreen.Views
{
    public partial class RecordingTimerPopup : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        public event Action? StopRequested;

        private readonly DateTime _startTime;

        public RecordingTimerPopup(DateTime startTime)
        {
            InitializeComponent();
            _startTime = startTime;

            SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
            };
            Loaded += (_, _) => PositionAboveTray();
        }

        // Called by TrayService on each blink tick — keeps popup in sync with tray icon
        public void Tick(bool blinkOn)
        {
            RecDot.Opacity = blinkOn ? 1.0 : 0.15;
            var elapsed = DateTime.Now - _startTime;
            TimerText.Text = $"{(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
            => StopRequested?.Invoke();

        private void PositionAboveTray()
        {
            var work = SystemParameters.WorkArea;
            Left = work.Right - Width - 12;
            Top  = work.Bottom - Height - 8;
        }
    }
}
