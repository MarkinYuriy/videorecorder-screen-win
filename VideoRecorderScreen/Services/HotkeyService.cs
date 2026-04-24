using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace VideoRecorderScreen.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;

        private const uint MOD_ALT     = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT   = 0x0004;
        private const uint MOD_WIN     = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        private HwndSource? _hwndSource;
        private Action? _callback;
        private bool _registered;

        public void Initialize()
        {
            var p = new HwndSourceParameters("HotkeyHost")
            {
                Width = 0, Height = 0,
                WindowStyle = 0,
                ParentWindow = IntPtr.Zero
            };
            _hwndSource = new HwndSource(p);
            _hwndSource.AddHook(WndProc);
            AppLogger.Log("HotkeyService: message window created");
        }

        // Returns null on success, error message on failure.
        public string? Register(string hotkeyString, Action callback)
        {
            Unregister();
            _callback = callback;

            if (!TryParse(hotkeyString, out var mods, out var vk))
            {
                var msg = $"Не удалось разобрать хоткей: \"{hotkeyString}\"";
                AppLogger.Log($"HotkeyService.Register: {msg}");
                return msg;
            }

            if (_hwndSource == null) return "HotkeyService не инициализирован";

            bool ok = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID, mods | MOD_NOREPEAT, vk);
            if (!ok)
            {
                var msg = $"Хоткей \"{hotkeyString}\" занят другой программой";
                AppLogger.Log($"HotkeyService.Register: {msg}");
                return msg;
            }

            _registered = true;
            AppLogger.Log($"HotkeyService.Register: registered \"{hotkeyString}\"");
            return null;
        }

        public void Unregister()
        {
            if (_registered && _hwndSource != null)
            {
                UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID);
                _registered = false;
                AppLogger.Log("HotkeyService: unregistered");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                AppLogger.Log("HotkeyService: hotkey fired");
                _callback?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private static bool TryParse(string s, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;

            var parts = s.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            Key? key = null;

            foreach (var part in parts)
            {
                switch (part.ToUpperInvariant())
                {
                    case "CTRL":
                    case "CONTROL": modifiers |= MOD_CONTROL; break;
                    case "ALT":     modifiers |= MOD_ALT;     break;
                    case "SHIFT":   modifiers |= MOD_SHIFT;   break;
                    case "WIN":     modifiers |= MOD_WIN;     break;
                    default:
                        if (Enum.TryParse<Key>(part, ignoreCase: true, out var k))
                            key = k;
                        else
                            return false;
                        break;
                }
            }

            if (key == null) return false;
            vk = (uint)KeyInterop.VirtualKeyFromKey(key.Value);
            return vk != 0;
        }

        public void Dispose()
        {
            Unregister();
            _hwndSource?.Dispose();
        }
    }
}
