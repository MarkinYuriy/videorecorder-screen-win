using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using VideoRecorderScreen.Models;
using Color = System.Windows.Media.Color;

namespace VideoRecorderScreen.Views
{
    public partial class WizardWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        private readonly Rect _region;
        private readonly TaskCompletionSource<WizardResult?> _tcs;

        private WizardWindow(Rect region, TaskCompletionSource<WizardResult?> tcs)
        {
            InitializeComponent();
            _region = region;
            _tcs = tcs;

            SourceInitialized += (_, _) =>
                SetWindowDisplayAffinity(new WindowInteropHelper(this).Handle, WDA_EXCLUDEFROMCAPTURE);

            var s = App.SettingsService.Settings;
            SetFps(s.DefaultFps);
            MicCheck.IsChecked     = false;
            SysAudioCheck.IsChecked = false;
            CursorCheck.IsChecked  = s.CaptureCursor;

            Closed += (_, _) => _tcs.TrySetResult(null);
        }

        public static Task<WizardResult?> ShowAsync(Rect region)
        {
            var tcs = new TaskCompletionSource<WizardResult?>();
            new WizardWindow(region, tcs).Show();
            return tcs.Task;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (Step2Panel.Visibility == Visibility.Visible)
                GoToStep3();
            else
                StartRecording();
        }

        private void Back_Click(object sender, RoutedEventArgs e) => GoToStep2();

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _tcs.TrySetResult(null);
            Close();
        }

        private static string L(string key) => Services.LocalizationService.Get(key);

        private void GoToStep2()
        {
            Step2Panel.Visibility = Visibility.Visible;
            Step3Panel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            NextButton.Content = L("Btn_Next");
            Dot1.Fill = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
            Dot2.Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        }

        private void GoToStep3()
        {
            SummaryRegion.Text = $"{(int)_region.Width} × {(int)_region.Height}  (x={((int)_region.X)}, y={((int)_region.Y)})";
            SummaryFps.Text = $"{GetFps()} fps";
            SummaryMic.Text    = MicCheck.IsChecked      == true ? L("Value_Enabled") : L("Value_Disabled");
            SummaryAudio.Text  = SysAudioCheck.IsChecked == true ? L("Value_Enabled") : L("Value_Disabled");
            SummaryCursor.Text = CursorCheck.IsChecked   == true ? L("Value_Enabled") : L("Value_Disabled");

            Step2Panel.Visibility = Visibility.Collapsed;
            Step3Panel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
            NextButton.Content = L("Btn_Start");
            Dot2.Fill = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
        }

        private async void StartRecording()
        {
            var fps = GetFps();
            var s = App.SettingsService.Settings;
            s.DefaultFps = fps;
            App.SettingsService.Save();

            var result = new WizardResult
            {
                Region = _region,
                Fps = fps,
                MicEnabled = MicCheck.IsChecked == true,
                SystemAudioEnabled = SysAudioCheck.IsChecked == true,
                CaptureCursor = CursorCheck.IsChecked == true
            };

            int countdown = s.CountdownSeconds;
            if (countdown > 0)
            {
                Step3Panel.Visibility  = Visibility.Collapsed;
                CountdownPanel.Visibility = Visibility.Visible;
                BackButton.Visibility  = Visibility.Collapsed;
                NextButton.IsEnabled   = false;

                for (int i = countdown; i > 0; i--)
                {
                    if (!IsVisible) return; // user closed during countdown
                    CountdownNumber.Text = i.ToString();
                    await Task.Delay(1000);
                }
            }

            Hide();
            await Task.Delay(150); // let compositor remove window before first frame
            _tcs.TrySetResult(result);
            Close();
        }

        private void SetFps(int fps)
        {
            var rb = fps switch { 5 => Fps5, 15 => Fps15, 20 => Fps20, 30 => Fps30, _ => Fps10 };
            rb.IsChecked = true;
        }

        private int GetFps()
        {
            if (Fps5.IsChecked == true)  return 5;
            if (Fps15.IsChecked == true) return 15;
            if (Fps20.IsChecked == true) return 20;
            if (Fps30.IsChecked == true) return 30;
            return 10;
        }
    }
}
