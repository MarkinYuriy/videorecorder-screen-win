using VideoRecorderScreen.Views;
using WinSaveDialog = Microsoft.Win32.SaveFileDialog;

namespace VideoRecorderScreen.Services
{
    public static class SaveService
    {
        // Returns final path, or null if user cancelled.
        public static async Task<string?> SaveAsync(string tempPath)
        {
            AppLogger.Log($"SaveAsync: tempPath={tempPath} exists={File.Exists(tempPath)}");
            var s = App.SettingsService.Settings;
            Directory.CreateDirectory(s.RecordingsFolder);

            string finalPath;

            if (s.AutoFormatFilename)
            {
                var name = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm}.mp4";
                finalPath = Path.Combine(s.RecordingsFolder, name);

                if (File.Exists(finalPath))
                {
                    var choice = await ConflictDialog.ShowAsync(name);
                    switch (choice)
                    {
                        case ConflictChoice.Overwrite:
                            File.Delete(finalPath);
                            break;
                        case ConflictChoice.SaveAsNumbered:
                            finalPath = FindFreeNumbered(s.RecordingsFolder,
                                Path.GetFileNameWithoutExtension(name), ".mp4");
                            break;
                        case ConflictChoice.Cancel:
                        default:
                            return null;
                    }
                }
            }
            else
            {
                var dlg = new WinSaveDialog
                {
                    Title = "Сохранить запись",
                    Filter = "MP4 video|*.mp4",
                    InitialDirectory = s.RecordingsFolder,
                    FileName = $"Recording_{DateTime.Now:yyyy-MM-dd_HH-mm}.mp4"
                };
                if (dlg.ShowDialog() != true) return null;
                finalPath = dlg.FileName;

                if (File.Exists(finalPath))
                    File.Delete(finalPath);
            }

            AppLogger.Log($"SaveAsync: moving to {finalPath}");
            File.Move(tempPath, finalPath);
            AppLogger.Log("SaveAsync: done");
            return finalPath;
        }

        private static string FindFreeNumbered(string folder, string baseName, string ext)
        {
            for (int n = 1; ; n++)
            {
                var candidate = Path.Combine(folder, $"{baseName} ({n}){ext}");
                if (!File.Exists(candidate)) return candidate;
            }
        }
    }
}
