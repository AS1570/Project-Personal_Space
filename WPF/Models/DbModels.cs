using SQLite;

namespace WpfApp3.Models
{
    [Table("Diary")]
    public class DiaryEntry
    {
        [PrimaryKey]
        public string Date { get; set; } = string.Empty;

        public string Weather { get; set; } = string.Empty;

        public string Mood { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }

    [Table("Settings")]
    public class EncryptedSetting
    {
        [PrimaryKey]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public class AppSettings
    {
        public bool BottomBarShowClock { get; set; } = true;
        public bool BottomBarShowReturnShortcut { get; set; } = true;
        public string BottomBarMode { get; set; } = "Docked";
        public bool TopBarShowClock { get; set; } = true;
        public string TopBarMode { get; set; } = "Docked";
    }
}
