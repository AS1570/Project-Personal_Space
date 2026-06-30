namespace WpfApp3.Models
{
    public class OpenSettings
    {
        public string TopBarMode { get; set; } = "Docked";

        public bool TopBarShowTime { get; set; } = true;
        public bool TopBarShowDate { get; set; } = true;
        public bool TopBarShowLockButton { get; set; } = false;

        public bool TopBarTime24Hour { get; set; } = true;
        public bool TopBarTimeShowSeconds { get; set; } = false;

        public string TopBarDateOrder { get; set; } = "yyyy/MM/dd";
        public string TopBarDateStyle { get; set; } = "Number";
        public string TopBarMonthStyle { get; set; } = "Number";
        public string TopBarYearStyle { get; set; } = "Full";
        public bool TopBarShowDayOfWeek { get; set; } = true;
        public string TopBarDayOfWeekPosition { get; set; } = "Date/DayOfWeek";

        public string LockScreenWallpaperType { get; set; } = "None";
        public string LockScreenWallpaperColor { get; set; } = "#16162A";
        public string LockScreenWallpaperImagePath { get; set; } = "";

        public string LockScreenTimeColor { get; set; } = "#FFFFFF";
        public string LockScreenDateColor { get; set; } = "#CCCCCC";

        public double ExtraScale { get; set; } = 100.0;

        public string PasswordHint { get; set; } = "初始密码：admin123";

        public string ThemeMode { get; set; } = "Manual";
        public string ThemeVariant { get; set; } = "Dark";
        public string ThemeLightStartTime { get; set; } = "06:00";
        public string ThemeDarkStartTime { get; set; } = "18:00";
        public string AccentColor { get; set; } = "#7C8AFF";

        public bool DisguiseEnabled { get; set; } = false;
        public string DisguiseAppName { get; set; } = "";
        public string DisguiseIconPath { get; set; } = "";
    }
}
