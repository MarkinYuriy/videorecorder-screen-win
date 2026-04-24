using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VideoRecorderScreen.Services;
using WinFolderDialog = System.Windows.Forms.FolderBrowserDialog;
using WinDialogResult = System.Windows.Forms.DialogResult;
using RegKey = Microsoft.Win32.Registry;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using RadioButton = System.Windows.Controls.RadioButton;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace VideoRecorderScreen.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _hotkeyCapturing;
        private string _pendingHotkey = string.Empty;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        public static void ShowOrActivate()
        {
            var existing = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            if (existing != null) { existing.Activate(); return; }
            new SettingsWindow().Show();
        }

        private void LoadSettings()
        {
            var s = App.SettingsService.Settings;

            ChkAutoFormat.IsChecked = s.AutoFormatFilename;
            TxtFolder.Text = s.RecordingsFolder;
            TxtHotkey.Text = s.Hotkey;
            _pendingHotkey = s.Hotkey;
            ChkLaunchWithWindows.IsChecked = s.LaunchWithWindows;

            // FPS
            foreach (ComboBoxItem item in CmbFps.Items)
                if ((string)item.Content == s.DefaultFps.ToString())
                    { CmbFps.SelectedItem = item; break; }
            if (CmbFps.SelectedItem == null) CmbFps.SelectedIndex = 1; // default 10

            // Bitrate
            TxtBitrate.Text = s.VideoBitrate.ToString();
            switch (s.VideoBitrate)
            {
                case 1000: RbLow.IsChecked    = true; break;
                case 4000: RbMedium.IsChecked = true; break;
                case 8000: RbHigh.IsChecked   = true; break;
                default:   TxtBitrate.Text    = s.VideoBitrate.ToString(); break;
            }
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new WinFolderDialog
            {
                Description = "Папка для записей",
                SelectedPath = TxtFolder.Text
            };
            if (dlg.ShowDialog() == WinDialogResult.OK)
                TxtFolder.Text = dlg.SelectedPath;
        }

        private void Quality_Checked(object sender, RoutedEventArgs e)
        {
            if (TxtBitrate == null) return;
            var rb = (RadioButton)sender;
            TxtBitrate.Text = rb.Tag.ToString();
        }

        private void Bitrate_PreviewInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Bitrate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BitrateError == null) return;
            bool valid = int.TryParse(TxtBitrate.Text, out int v) && v >= 500 && v <= 50000;
            BitrateError.Visibility = valid || string.IsNullOrEmpty(TxtBitrate.Text)
                ? Visibility.Collapsed : Visibility.Visible;
            TxtBitrate.Background = valid || string.IsNullOrEmpty(TxtBitrate.Text)
                ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xFF, 0xDD, 0xDD));
        }

        // Hotkey capture
        private void Hotkey_GotFocus(object sender, RoutedEventArgs e)
        {
            _hotkeyCapturing = true;
            TxtHotkey.Text = "Нажмите комбинацию...";
            TxtHotkey.Foreground = Brushes.Gray;
        }

        private void Hotkey_LostFocus(object sender, RoutedEventArgs e)
        {
            _hotkeyCapturing = false;
            TxtHotkey.Text = _pendingHotkey;
            TxtHotkey.Foreground = Brushes.Black;
        }

        private void Hotkey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_hotkeyCapturing) return;
            e.Handled = true;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin)
                return; // wait for a non-modifier key

            var parts = new List<string>();
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt)     != 0) parts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift)   != 0) parts.Add("Shift");
            if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) parts.Add("Win");
            parts.Add(key.ToString());

            _pendingHotkey = string.Join("+", parts);
            TxtHotkey.Text = _pendingHotkey;
            TxtHotkey.Foreground = Brushes.Black;
            _hotkeyCapturing = false;
            Keyboard.ClearFocus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate bitrate
            if (!int.TryParse(TxtBitrate.Text, out int bitrate) || bitrate < 500 || bitrate > 50000)
            {
                MessageBox.Show("Битрейт должен быть от 500 до 50000 кбит/с.",
                    "Настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var s = App.SettingsService.Settings;

            s.AutoFormatFilename  = ChkAutoFormat.IsChecked == true;
            s.RecordingsFolder    = TxtFolder.Text.Trim();
            s.LaunchWithWindows   = ChkLaunchWithWindows.IsChecked == true;
            s.VideoBitrate        = bitrate;

            if (CmbFps.SelectedItem is ComboBoxItem fpsItem &&
                int.TryParse((string)fpsItem.Content, out int fps))
                s.DefaultFps = fps;

            // Hotkey
            if (_pendingHotkey != s.Hotkey)
            {
                var tray = ((App)Application.Current).TrayService;
                var err = App.HotkeyService.Register(_pendingHotkey,
                    () => tray?.StopRecordingAsync());
                if (err != null)
                {
                    HotkeyError.Text = err;
                    HotkeyError.Visibility = Visibility.Visible;
                    return;
                }
                s.Hotkey = _pendingHotkey;
                HotkeyError.Visibility = Visibility.Collapsed;
            }

            ApplyLaunchWithWindows(s.LaunchWithWindows);
            App.SettingsService.Save();
            AppLogger.Log("SettingsWindow: settings saved");
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private static void ApplyLaunchWithWindows(bool enable)
        {
            try
            {
                using var key = RegKey.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (enable)
                    key?.SetValue("ScreenRecorder",
                        $"\"{Environment.ProcessPath}\"");
                else
                    key?.DeleteValue("ScreenRecorder", throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                AppLogger.LogException("ApplyLaunchWithWindows", ex);
            }
        }
    }
}
