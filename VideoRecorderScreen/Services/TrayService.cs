using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using VideoRecorderScreen.Models;
using VideoRecorderScreen.Views;
using Rect = System.Windows.Rect;
using Application = System.Windows.Application;

namespace VideoRecorderScreen.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private RecordingService? _recording;
        private RecordingTimerPopup? _timerPopup;
        private System.Windows.Threading.DispatcherTimer? _blinkTimer;
        private bool _blinkOn;
        private ToolStripMenuItem? _itemNewRecording;
        private ToolStripMenuItem? _itemStopRecording;

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = CreateIcon(),
                Text = "ScreenRecorder",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            LocalizationService.LanguageChanged += RebuildMenu;
        }

        private static string L(string key) => LocalizationService.Get(key);

        private ContextMenuStrip BuildMenu()
        {
            _itemNewRecording  = new ToolStripMenuItem(L("Menu_NewRecording"),  null, OnNewRecording);
            _itemStopRecording = new ToolStripMenuItem(L("Menu_StopRecording"), null, OnStopRecording)
                { Visible = false };

            var menu = new ContextMenuStrip();
            menu.Items.Add(_itemNewRecording);
            menu.Items.Add(_itemStopRecording);
            menu.Items.Add(L("Menu_OpenFolder"), null, OnOpenFolder);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L("Menu_Settings"), null, OnSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(L("Menu_Exit"), null, OnExit);
            return menu;
        }

        private void RebuildMenu()
        {
            if (_notifyIcon == null) return;
            var wasRecording = _itemStopRecording?.Visible == true;
            var old = _notifyIcon.ContextMenuStrip;
            _notifyIcon.ContextMenuStrip = BuildMenu();
            old?.Dispose();
            if (wasRecording) SetRecordingState(true);
        }

        private async void OnNewRecording(object? sender, EventArgs e)
        {
            if (_recording?.IsRecording == true) return;
            AppLogger.Log("OnNewRecording: started");

            try
            {
                var s = App.SettingsService.Settings;
                var initial = new Rect(s.RegionX, s.RegionY, s.RegionWidth, s.RegionHeight);
                AppLogger.Log($"OnNewRecording: opening overlay, initial region={initial}");

                var region = await OverlayWindow.ShowAsync(initial);
                if (region is null) { AppLogger.Log("OnNewRecording: overlay cancelled"); return; }
                AppLogger.Log($"OnNewRecording: region selected={region}");

                s.RegionX = (int)region.Value.X;
                s.RegionY = (int)region.Value.Y;
                s.RegionWidth = (int)region.Value.Width;
                s.RegionHeight = (int)region.Value.Height;

                var result = await WizardWindow.ShowAsync(region.Value);
                if (result is null) { AppLogger.Log("OnNewRecording: wizard cancelled"); return; }
                AppLogger.Log($"OnNewRecording: wizard done, fps={result.Fps} mic={result.MicEnabled} sys={result.SystemAudioEnabled}");

                _recording?.Dispose();
                _recording = new RecordingService();
                _recording.Start(result);
                AppLogger.Log("OnNewRecording: recording started");

                SetRecordingState(true);
            }
            catch (Exception ex)
            {
                AppLogger.LogException("OnNewRecording", ex);
                System.Windows.MessageBox.Show(string.Format(L("Error_StartFailed"), ex.Message),
                    "ScreenRecorder", MessageBoxButton.OK, MessageBoxImage.Error);
                SetRecordingState(false);
            }
        }

        private async void OnStopRecording(object? sender, EventArgs e)
            => await StopRecordingAsync();

        public async Task StopRecordingAsync()
        {
            if (_recording?.IsRecording != true) return;
            AppLogger.Log("StopRecordingAsync: stopping");
            SetRecordingState(false);

            try
            {
                var tempPath = await _recording.StopAsync();
                AppLogger.Log($"StopRecordingAsync: encoding done, tempPath={tempPath}");

                var finalPath = await SaveService.SaveAsync(tempPath);
                _recording.Cleanup();
                AppLogger.Log($"StopRecordingAsync: saved to finalPath={finalPath ?? "cancelled"}");

                if (finalPath != null)
                    _notifyIcon?.ShowBalloonTip(3000, "ScreenRecorder",
                        string.Format(L("Balloon_Saved"), finalPath), ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                AppLogger.LogException("StopRecordingAsync", ex);
                _recording?.Cleanup();
                System.Windows.MessageBox.Show(string.Format(L("Error_SaveFailed"), ex.Message),
                    "ScreenRecorder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetRecordingState(bool recording)
        {
            if (_itemNewRecording  != null) _itemNewRecording.Visible  = !recording;
            if (_itemStopRecording != null) _itemStopRecording.Visible = recording;
            if (recording)
            {
                _blinkOn = true;
                SetRecordingIcon(recording: true, blinkOn: true);

                _timerPopup?.Close();
                _timerPopup = new RecordingTimerPopup(DateTime.Now);
                _timerPopup.StopRequested += async () => await StopRecordingAsync();
                _timerPopup.Show();
                _timerPopup.Tick(blinkOn: true);

                _blinkTimer = new System.Windows.Threading.DispatcherTimer
                    { Interval = TimeSpan.FromMilliseconds(700) };
                _blinkTimer.Tick += (_, _) =>
                {
                    _blinkOn = !_blinkOn;
                    SetRecordingIcon(recording: true, blinkOn: _blinkOn);
                    _timerPopup?.Tick(blinkOn: _blinkOn);
                };
                _blinkTimer.Start();
            }
            else
            {
                _blinkTimer?.Stop();
                _blinkTimer = null;
                SetRecordingIcon(recording: false, blinkOn: false);

                _timerPopup?.Close();
                _timerPopup = null;
            }
        }

        private void SetRecordingIcon(bool recording, bool blinkOn)
        {
            if (_notifyIcon == null) return;
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            if (recording)
            {
                var brush = blinkOn
                    ? Brushes.Red
                    : new SolidBrush(Color.FromArgb(60, 180, 0, 0));
                g.FillEllipse(brush, 1, 1, 14, 14);
            }
            else
            {
                // idle: white ring + red fill — clearly visible on any taskbar
                g.FillEllipse(Brushes.White, 1, 1, 14, 14);
                g.FillEllipse(Brushes.Red, 3, 3, 10, 10);
            }
            _notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            _notifyIcon.Text = recording ? L("Tray_Recording") : "ScreenRecorder";
        }

        private static void OnOpenFolder(object? sender, EventArgs e)
        {
            var folder = App.SettingsService.Settings.RecordingsFolder;
            if (Directory.Exists(folder))
                System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        private static void OnSettings(object? sender, EventArgs e)
            => Views.SettingsWindow.ShowOrActivate();

        private void OnExit(object? sender, EventArgs e)
        {
            _recording?.Dispose();
            _notifyIcon!.Visible = false;
            Environment.Exit(0);
        }

        private static Icon CreateIcon()
        {
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            g.FillEllipse(Brushes.White, 1, 1, 14, 14);
            g.FillEllipse(Brushes.Red, 3, 3, 10, 10);
            return Icon.FromHandle(bmp.GetHicon());
        }

        public void Dispose()
        {
            _blinkTimer?.Stop();
            _timerPopup?.Close();
            _recording?.Dispose();
            _notifyIcon?.Dispose();
        }
    }
}
