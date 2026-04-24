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
        }

        private ContextMenuStrip BuildMenu()
        {
            _itemNewRecording  = new ToolStripMenuItem("Новая запись",        null, OnNewRecording);
            _itemStopRecording = new ToolStripMenuItem("⏹  Остановить запись", null, OnStopRecording)
                { Visible = false };

            var menu = new ContextMenuStrip();
            menu.Items.Add(_itemNewRecording);
            menu.Items.Add(_itemStopRecording);
            menu.Items.Add("Открыть папку записей", null, OnOpenFolder);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Настройки", null, OnSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, OnExit);
            return menu;
        }

        private async void OnNewRecording(object? sender, EventArgs e)
        {
            if (_recording?.IsRecording == true) return;

            try
            {
                var s = App.SettingsService.Settings;
                var initial = new Rect(s.RegionX, s.RegionY, s.RegionWidth, s.RegionHeight);

                var region = await OverlayWindow.ShowAsync(initial);
                if (region is null) return;

                s.RegionX = (int)region.Value.X;
                s.RegionY = (int)region.Value.Y;
                s.RegionWidth = (int)region.Value.Width;
                s.RegionHeight = (int)region.Value.Height;

                var result = await WizardWindow.ShowAsync(region.Value);
                if (result is null) return;

                _recording?.Dispose();
                _recording = new RecordingService();
                _recording.Start(result);

                SetRecordingState(true);

                var overlay = new RecordingOverlay(result.Region);
                overlay.StopRequested += async () => await StopRecordingAsync();
                overlay.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка запуска записи:\n{ex.Message}",
                    "ScreenRecorder", MessageBoxButton.OK, MessageBoxImage.Error);
                SetRecordingState(false);
            }
        }

        private async void OnStopRecording(object? sender, EventArgs e)
            => await StopRecordingAsync();

        public async Task StopRecordingAsync()
        {
            if (_recording?.IsRecording != true) return;
            SetRecordingState(false);
            try
            {
                var path = await _recording.StopAsync();
                _notifyIcon?.ShowBalloonTip(3000, "ScreenRecorder",
                    $"Запись сохранена:\n{path}", ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка сохранения записи:\n{ex.Message}",
                    "ScreenRecorder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetRecordingState(bool recording)
        {
            if (_itemNewRecording  != null) _itemNewRecording.Visible  = !recording;
            if (_itemStopRecording != null) _itemStopRecording.Visible = recording;
            SetRecordingIcon(recording);
        }

        private void SetRecordingIcon(bool recording)
        {
            if (_notifyIcon == null) return;
            var bmp = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);
            if (recording)
            {
                g.FillEllipse(Brushes.Red, 1, 1, 14, 14);
                g.FillEllipse(Brushes.White, 5, 5, 6, 6);
            }
            else
            {
                g.FillEllipse(Brushes.DarkRed, 1, 1, 14, 14);
                g.FillEllipse(Brushes.Red, 3, 3, 10, 10);
            }
            _notifyIcon.Icon = Icon.FromHandle(bmp.GetHicon());
            _notifyIcon.Text = recording ? "ScreenRecorder — запись..." : "ScreenRecorder";
        }

        private static void OnOpenFolder(object? sender, EventArgs e)
        {
            var folder = App.SettingsService.Settings.RecordingsFolder;
            if (Directory.Exists(folder))
                System.Diagnostics.Process.Start("explorer.exe", folder);
        }

        private static void OnSettings(object? sender, EventArgs e)
        {
            // TODO: Step 11 — open settings window
        }

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
            g.FillEllipse(Brushes.DarkRed, 1, 1, 14, 14);
            g.FillEllipse(Brushes.Red, 3, 3, 10, 10);
            return Icon.FromHandle(bmp.GetHicon());
        }

        public void Dispose()
        {
            _recording?.Dispose();
            _notifyIcon?.Dispose();
        }
    }
}
