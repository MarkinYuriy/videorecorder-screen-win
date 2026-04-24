using System.Windows;
using FFMpegCore;
using VideoRecorderScreen.Services;

namespace VideoRecorderScreen
{
    public partial class App
    {
        public static SettingsService SettingsService { get; } = new();
        public static HotkeyService HotkeyService { get; } = new();

        private TrayService? _trayService;
        public TrayService? TrayService => _trayService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppLogger.Log("App starting");
            GlobalFFOptions.Configure(o => o.BinaryFolder = AppDomain.CurrentDomain.BaseDirectory);
            SettingsService.Load();

            LocalizationService.Initialize();
            HotkeyService.Initialize();

            _trayService = new TrayService();
            _trayService.Initialize();

            var err = HotkeyService.Register(SettingsService.Settings.Hotkey,
                () => { _ = _trayService.StopRecordingAsync(); });
            if (err != null)
                AppLogger.Log($"App: hotkey registration warning: {err}");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogger.Log("App exiting");
            SettingsService.Save();
            HotkeyService.Dispose();
            _trayService?.Dispose();
            base.OnExit(e);
        }
    }
}
