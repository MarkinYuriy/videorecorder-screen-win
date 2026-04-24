using System.Windows;
using System.Windows.Media;
using VideoRecorderScreen.Models;
using Color = System.Windows.Media.Color;

namespace VideoRecorderScreen.Views
{
    public partial class WizardWindow : Window
    {
        private readonly Rect _region;
        private readonly TaskCompletionSource<WizardResult?> _tcs;

        private WizardWindow(Rect region, TaskCompletionSource<WizardResult?> tcs)
        {
            InitializeComponent();
            _region = region;
            _tcs = tcs;

            var s = App.SettingsService.Settings;
            SetFps(s.DefaultFps);
            MicCheck.IsChecked = false;
            SysAudioCheck.IsChecked = false;

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

        private void GoToStep2()
        {
            Step2Panel.Visibility = Visibility.Visible;
            Step3Panel.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;
            NextButton.Content = "Далее →";
            Dot1.Fill = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
            Dot2.Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
        }

        private void GoToStep3()
        {
            SummaryRegion.Text = $"{(int)_region.Width} × {(int)_region.Height}  (x={((int)_region.X)}, y={((int)_region.Y)})";
            SummaryFps.Text = $"{GetFps()} fps";
            SummaryMic.Text = MicCheck.IsChecked == true ? "Включён" : "Выключен";
            SummaryAudio.Text = SysAudioCheck.IsChecked == true ? "Включён" : "Выключен";

            Step2Panel.Visibility = Visibility.Collapsed;
            Step3Panel.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Visible;
            NextButton.Content = "▶  Начать запись";
            Dot2.Fill = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
        }

        private void StartRecording()
        {
            var fps = GetFps();
            var s = App.SettingsService.Settings;
            s.DefaultFps = fps;
            App.SettingsService.Save();

            _tcs.TrySetResult(new WizardResult
            {
                Region = _region,
                Fps = fps,
                MicEnabled = MicCheck.IsChecked == true,
                SystemAudioEnabled = SysAudioCheck.IsChecked == true
            });
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
