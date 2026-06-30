using System;
using System.IO;
using System.Text.Json;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class ProgramSettingsService
    {
        private static ProgramSettingsService? _instance;
        public static ProgramSettingsService Instance => _instance ??= new ProgramSettingsService();

        public ProgramSettings Current { get; private set; } = new ProgramSettings();

        public event Action? OnSettingsChanged;

        private string SettingsPath =>
            Path.Combine(App.FileManager.ProgramFileDir, "programsettings.json");

        private ProgramSettingsService()
        {
            Load();
        }

        private void Load()
        {
            try
            {
                var path = SettingsPath;
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    Current = JsonSerializer.Deserialize<ProgramSettings>(json) ?? new ProgramSettings();
                }
                else
                {
                    Current = new ProgramSettings();
                    Save();
                }
            }
            catch
            {
                Current = new ProgramSettings();
            }
        }

        public void Save()
        {
            try
            {
                var path = SettingsPath;
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                OnSettingsChanged?.Invoke();
            }
            catch { }
        }
    }
}
