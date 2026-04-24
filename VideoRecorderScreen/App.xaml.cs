using System.Windows;
using FFMpegCore;
using VideoRecorderScreen.Services;

namespace VideoRecorderScreen
{
    public partial class App
    {
        public static SettingsService SettingsService { get; } = new();

        private TrayService? _trayService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GlobalFFOptions.Configure(o => o.BinaryFolder = AppDomain.CurrentDomain.BaseDirectory);
            SettingsService.Load();
            _trayService = new TrayService();
            _trayService.Initialize();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SettingsService.Save();
            _trayService?.Dispose();
            base.OnExit(e);
        }
    }
}
