using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        private readonly DispatcherTimer _autoTimer;

        public string ThemeMode
        {
            get => OpenSettingsService.Instance.Current.ThemeMode;
            set => OpenSettingsService.Instance.Current.ThemeMode = value;
        }

        public string ThemeVariant
        {
            get => OpenSettingsService.Instance.Current.ThemeVariant;
            set => OpenSettingsService.Instance.Current.ThemeVariant = value;
        }

        public string AccentColor
        {
            get => OpenSettingsService.Instance.Current.AccentColor;
            set => OpenSettingsService.Instance.Current.AccentColor = value;
        }

        public string ThemeLightStartTime
        {
            get => OpenSettingsService.Instance.Current.ThemeLightStartTime;
            set => OpenSettingsService.Instance.Current.ThemeLightStartTime = value;
        }

        public string ThemeDarkStartTime
        {
            get => OpenSettingsService.Instance.Current.ThemeDarkStartTime;
            set => OpenSettingsService.Instance.Current.ThemeDarkStartTime = value;
        }

        public event Action? OnThemeChanged;

        private ThemeService()
        {
            _autoTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _autoTimer.Tick += OnAutoTimerTick;
        }

        public void Initialize()
        {
            ApplyTheme();
            ConfigureMode();
        }

        private void ConfigureMode()
        {
            _autoTimer.Stop();
            SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;

            switch (ThemeMode)
            {
                case "Auto":
                    _autoTimer.Start();
                    EvaluateAutoTheme();
                    break;
                case "FollowSystem":
                    SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
                    EvaluateSystemTheme();
                    break;
            }
        }

        private void OnAutoTimerTick(object? sender, EventArgs e)
        {
            if (ThemeMode == "Auto")
                EvaluateAutoTheme();
        }

        private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General && ThemeMode == "FollowSystem")
                EvaluateSystemTheme();
        }

        private void EvaluateAutoTheme()
        {
            if (ThemeMode != "Auto") return;

            var now = DateTime.Now.TimeOfDay;
            var lightTime = ParseTimeOfDay(ThemeLightStartTime);
            var darkTime = ParseTimeOfDay(ThemeDarkStartTime);

            string target;
            if (lightTime <= darkTime)
            {
                target = (now >= lightTime && now < darkTime) ? "Light" : "Dark";
            }
            else
            {
                target = (now >= lightTime || now < darkTime) ? "Light" : "Dark";
            }

            if (GetEffectiveVariant() != target)
            {
                ThemeVariant = target;
                ApplyTheme();
            }
        }

        private void EvaluateSystemTheme()
        {
            if (ThemeMode != "FollowSystem") return;

            string target = IsSystemDarkMode() ? "Dark" : "Light";
            if (GetEffectiveVariant() != target)
            {
                ThemeVariant = target;
                ApplyTheme();
            }
        }

        public string GetEffectiveVariant()
        {
            return ThemeMode switch
            {
                "Manual" => ThemeVariant,
                "Auto" => GetAutoVariant(),
                "FollowSystem" => IsSystemDarkMode() ? "Dark" : "Light",
                _ => "Dark"
            };
        }

        private string GetAutoVariant()
        {
            var now = DateTime.Now.TimeOfDay;
            var lightTime = ParseTimeOfDay(ThemeLightStartTime);
            var darkTime = ParseTimeOfDay(ThemeDarkStartTime);

            if (lightTime <= darkTime)
                return (now >= lightTime && now < darkTime) ? "Light" : "Dark";
            else
                return (now >= lightTime || now < darkTime) ? "Light" : "Dark";
        }

        public void ApplyTheme()
        {
            string variant = GetEffectiveVariant();
            string accentHex = AccentColor;

            SwapResourceDictionary(variant);
            ApplyAccent(accentHex);

            OnThemeChanged?.Invoke();
        }

        public void RefreshMode()
        {
            ConfigureMode();
            ApplyTheme();
            OpenSettingsService.Instance.Save();
        }

        private void SwapResourceDictionary(string variant)
        {
            string source = variant == "Light" ? "Dictionary1.xaml" : "Dictionary2.xaml";
            var appResources = Application.Current.Resources;
            var dict = appResources.MergedDictionaries;

            if (dict.Count > 0 && dict[0] is ResourceDictionary existing)
            {
                var currentSource = existing.Source?.ToString() ?? "";
                if (currentSource.EndsWith(source, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            dict.Clear();
            dict.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
        }

        private void ApplyAccent(string accentHex)
        {
            try
            {
                var baseColor = (Color)ColorConverter.ConvertFromString(accentHex);
                var lighter = Color.FromArgb(
                    baseColor.A,
                    (byte)Math.Min(255, baseColor.R + 40),
                    (byte)Math.Min(255, baseColor.G + 40),
                    (byte)Math.Min(255, baseColor.B + 40));
                var darker = Color.FromArgb(
                    baseColor.A,
                    (byte)Math.Max(0, baseColor.R - 20),
                    (byte)Math.Max(0, baseColor.G - 20),
                    (byte)Math.Max(0, baseColor.B - 20));

                SetBrush("Accent", accentHex);
                SetBrush("AccentLight", $"#{lighter.A:X2}{lighter.R:X2}{lighter.G:X2}{lighter.B:X2}");
                SetBrush("AccentDark", $"#{darker.A:X2}{darker.R:X2}{darker.G:X2}{darker.B:X2}");

                var accentBg08 = Color.FromArgb(0x1A, baseColor.R, baseColor.G, baseColor.B);
                var accentBg10 = Color.FromArgb(0x25, baseColor.R, baseColor.G, baseColor.B);
                SetBrush("AccentBg08", $"#{accentBg08.A:X2}{accentBg08.R:X2}{accentBg08.G:X2}{accentBg08.B:X2}");
                SetBrush("AccentBg10", $"#{accentBg10.A:X2}{accentBg10.R:X2}{accentBg10.G:X2}{accentBg10.B:X2}");

                var checkBg = Color.FromArgb(0x25, baseColor.R, baseColor.G, baseColor.B);
                SetBrush("CheckBg", $"#{checkBg.A:X2}{checkBg.R:X2}{checkBg.G:X2}{checkBg.B:X2}");
            }
            catch { }
        }

        private void SetBrush(string key, string hexColor)
        {
            try
            {
                var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hexColor)!;
                brush.Freeze();
                if (Application.Current.Resources[key] is SolidColorBrush)
                    Application.Current.Resources[key] = brush;
            }
            catch { }
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                const string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                var value = Microsoft.Win32.Registry.GetValue(key, "AppsUseLightTheme", 1);
                return value is 0;
            }
            catch
            {
                return false;
            }
        }

        private static TimeSpan ParseTimeOfDay(string time)
        {
            if (TimeSpan.TryParse(time, out var ts))
                return ts;
            return TimeSpan.FromHours(18);
        }

        public static readonly string[] AccentPresets = new[]
        {
            "#7C8AFF", "#FF6B9D", "#FF8C42", "#5CE1E6",
            "#6BCB77", "#C084FC", "#F59E0B", "#EF4444",
            "#3B82F6", "#10B981", "#8B5CF6", "#EC4899"
        };
    }
}
