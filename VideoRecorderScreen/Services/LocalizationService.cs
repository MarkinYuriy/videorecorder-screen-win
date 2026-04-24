using System.Globalization;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace VideoRecorderScreen.Services
{
    public static class LocalizationService
    {
        public static readonly IReadOnlyList<(string Code, string Name)> SupportedLanguages =
        [
            ("ru", "Русский"),
            ("en", "English"),
            ("de", "Deutsch"),
            ("fr", "Français"),
            ("es", "Español"),
            ("it", "Italiano"),
            ("pl", "Polski"),
            ("pt", "Português"),
        ];

        public static event Action? LanguageChanged;

        public static void Initialize()
        {
            var s = App.SettingsService.Settings;
            string lang;
            if (s.LanguageUserSelected && !string.IsNullOrEmpty(s.Language))
            {
                lang = s.Language;
            }
            else
            {
                var sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                lang = SupportedLanguages.Any(l => l.Code == sys) ? sys : "en";
                s.Language = lang;
            }
            Apply(lang, notify: false);
        }

        public static void Apply(string langCode, bool notify = true)
        {
            var uri = new Uri(
                $"pack://application:,,,/Resources/Lang/{langCode}.xaml",
                UriKind.Absolute);

            var dict = new ResourceDictionary { Source = uri };

            var existing = WpfApp.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("/Lang/") == true);
            if (existing != null)
                WpfApp.Current.Resources.MergedDictionaries.Remove(existing);

            WpfApp.Current.Resources.MergedDictionaries.Add(dict);
            App.SettingsService.Settings.Language = langCode;

            AppLogger.Log($"LocalizationService: applied language '{langCode}'");
            if (notify) LanguageChanged?.Invoke();
        }

        public static string Get(string key)
        {
            if (WpfApp.Current.Resources.Contains(key))
                return WpfApp.Current.Resources[key]?.ToString() ?? key;
            return key;
        }
    }
}
