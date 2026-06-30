using System.IO;
using System.Text.Json;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class SettingsService
    {
        private static SettingsService? _instance;
        public static SettingsService Instance => _instance ??= new SettingsService();

        private readonly string _settingsPath;
        public AppSettings Current { get; private set; } = new AppSettings();

        public event Action? OnSettingsChanged;

        private SettingsService()
        {
            string baseDir = App.AppDirectory;
            string appDataDir = Path.Combine(baseDir, "appdata");
            if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            _settingsPath = Path.Combine(appDataDir, "setting.json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    Current = new AppSettings();
                    Save();
                }
            }
            catch
            {
                Current = new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                OnSettingsChanged?.Invoke();
            }
            catch { }
        }
    }
}
