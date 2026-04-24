namespace VideoRecorderScreen.Services
{
    public static class AppLogger
    {
        private static readonly string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScreenRecorder", "app.log");

        static AppLogger()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
                // Keep last 500 lines to avoid unbounded growth
                if (File.Exists(_logPath))
                {
                    var lines = File.ReadAllLines(_logPath);
                    if (lines.Length > 500)
                        File.WriteAllLines(_logPath, lines[^400..]);
                }
            }
            catch { }
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(_logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void LogException(string context, Exception ex)
            => Log($"ERROR in {context}: {ex.GetType().Name}: {ex.Message}\n  {ex.StackTrace?.Replace("\n", "\n  ")}");
    }
}
