using System.Collections.Generic;

namespace WpfApp3.Models
{
    public class ProgramSettings
    {
        public string BottomBarStyle { get; set; } = "Floating";

        public string BottomBarLayout { get; set; } = "Center";

        public string BottomBarVisibility { get; set; } = "Docked";

        public bool Button8ShowSongName { get; set; } = true;
        public bool Button8Show { get; set; } = true;
        public bool Button9Show { get; set; } = true;
        public bool Button10Show { get; set; } = true;

        public string Button9Alignment { get; set; } = "Center";

        public bool Button9ShowTime { get; set; } = true;
        public bool Button9Time24Hour { get; set; } = true;
        public bool Button9TimeShowSeconds { get; set; } = true;

        public bool Button9ShowDate { get; set; } = false;
        public string Button9DateOrder { get; set; } = "yyyy/MM/dd";
        public string Button9DateStyle { get; set; } = "Number";
        public string Button9MonthStyle { get; set; } = "Number";
        public string Button9YearStyle { get; set; } = "Full";
        public bool Button9ShowDayOfWeek { get; set; } = true;
        public string Button9DayOfWeekPosition { get; set; } = "Date/DayOfWeek";

        public List<WidgetData> Widgets { get; set; } = new List<WidgetData>();
        public string AudioQuickSelectPath { get; set; } = "";
        public string AudioQuickSelectMode { get; set; } = "Single";
        public double AudioQuickSelectVolume { get; set; } = 0.7;

        public string DesktopWallpaperType { get; set; } = "None";
        public string DesktopWallpaperColor { get; set; } = "#16162A";
        public string DesktopWallpaperImagePath { get; set; } = "";
        public string DesktopWallpaperVideoPath { get; set; } = "";

        public string DesktopTopBarMode { get; set; } = "Original";
        public string DesktopBottomBarMode { get; set; } = "Original";

        public string DesktopBottomBarStyle { get; set; } = "Original";
        public string DesktopBottomBarLayout { get; set; } = "Original";

        public bool DesktopButton8ShowSongName { get; set; } = true;
        public bool DesktopButton8Show { get; set; } = true;
        public bool DesktopButton9Show { get; set; } = true;
        public bool DesktopButton10Show { get; set; } = true;
    }

    public class WidgetData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 280;
        public double Height { get; set; } = 220;
        public string Config { get; set; } = "{}";
        public double ProportionalX { get; set; }
        public double ProportionalY { get; set; }
        public double ProportionalW { get; set; }
        public double ProportionalH { get; set; }

        public int GridColumn { get; set; }
        public int GridRow { get; set; }
        public int GridColSpan { get; set; } = 2;
        public int GridRowSpan { get; set; } = 2;
    }
}
