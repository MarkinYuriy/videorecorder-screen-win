using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace VideoRecorderScreen.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;

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

        private static ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Новая запись", null, OnNewRecording);
            menu.Items.Add("Открыть папку записей", null, OnOpenFolder);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Настройки", null, OnSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход", null, OnExit);
            return menu;
        }

        private static void OnNewRecording(object? sender, EventArgs e)
        {
            // TODO: Step 4 — open wizard
        }

        private static void OnOpenFolder(object? sender, EventArgs e)
        {
            // TODO: open recordings folder from settings
        }

        private static void OnSettings(object? sender, EventArgs e)
        {
            // TODO: Step 11 — open settings window
        }

        private static void OnExit(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
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
            _notifyIcon?.Dispose();
        }
    }
}
