using System;
using System.IO;
using System.Text.Json;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class OpenSettingsService
    {
        private static OpenSettingsService? _instance;
        public static OpenSettingsService Instance => _instance ??= new OpenSettingsService();

        private readonly string _settingsPath;
        public OpenSettings Current { get; private set; } = new OpenSettings();

        public event Action? OnSettingsChanged;

        private OpenSettingsService()
        {
            string baseDir = App.AppDirectory;
            string appDataDir = Path.Combine(baseDir, "appdata");
            if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            _settingsPath = Path.Combine(appDataDir, "opensettings.json");
            Load();
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    Current = JsonSerializer.Deserialize<OpenSettings>(json) ?? new OpenSettings();
                }
                else
                {
                    Current = new OpenSettings();
                    Save();
                }
            }
            catch
            {
                Current = new OpenSettings();
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
