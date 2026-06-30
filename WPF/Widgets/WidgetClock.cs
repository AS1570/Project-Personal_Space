using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp3.Widgets
{
    public class WidgetClock : WidgetBase
    {
        private readonly TextBlock _clockText;
        private readonly StackPanel _clockPanel;
        private readonly DispatcherTimer _timer;
        private bool _showSeconds = true;
        private bool _is24Hour = true;
        private bool _isBold;
        private string _layout = "Horizontal";

        public WidgetClock()
        {
            SetTitle("时钟");
            Width = 220;
            Height = 140;

            _clockPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _clockText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                FontSize = 46,
                FontWeight = FontWeights.Light,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe UI")
            };

            _clockPanel.Children.Add(_clockText);
            SetContent(_clockPanel);

            _clockText.MouseRightButtonDown += (s, e) =>
            {
                ShowClockMenu();
                e.Handled = true;
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (s, e) => UpdateClock();
            _timer.Start();

            Loaded += (s, e) => LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<ClockConfig>(Data?.Config ?? "{}");
                if (config != null)
                {
                    _is24Hour = config.Is24Hour;
                    _showSeconds = config.ShowSeconds;
                    _isBold = config.IsBold;
                    _layout = config.Layout ?? "Horizontal";
                }
            }
            catch { }
            ApplyLayout();
            UpdateClock();
        }

        private void SaveConfig()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["Is24Hour"] = _is24Hour;
            root["ShowSeconds"] = _showSeconds;
            root["IsBold"] = _isBold;
            root["Layout"] = _layout;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        private void ShowClockMenu()
        {
            var menu = new ContextMenu();

            var secItem = new MenuItem
            {
                Header = "显示秒",
                IsCheckable = true,
                IsChecked = _showSeconds
            };
            secItem.Click += (s, args) =>
            {
                _showSeconds = !_showSeconds;
                ApplyLayout();
                UpdateClock();
                SaveConfig();
            };
            menu.Items.Add(secItem);

            var hourItem = new MenuItem
            {
                Header = "24小时制",
                IsCheckable = true,
                IsChecked = _is24Hour
            };
            hourItem.Click += (s, args) =>
            {
                _is24Hour = !_is24Hour;
                UpdateClock();
                SaveConfig();
            };
            menu.Items.Add(hourItem);

            var boldItem = new MenuItem
            {
                Header = "字体加粗",
                IsCheckable = true,
                IsChecked = _isBold
            };
            boldItem.Click += (s, args) =>
            {
                _isBold = !_isBold;
                ApplyLayout();
                UpdateClock();
                SaveConfig();
            };
            menu.Items.Add(boldItem);

            menu.Items.Add(new Separator());

            var layoutHeader = new MenuItem
            {
                Header = "排列方式",
                IsEnabled = false,
                FontWeight = FontWeights.Bold
            };
            menu.Items.Add(layoutHeader);

            var hItem = new MenuItem
            {
                Header = "横置（时:分:秒）",
                IsCheckable = true,
                IsChecked = _layout == "Horizontal"
            };
            hItem.Click += (s, args) =>
            {
                _layout = "Horizontal";
                ApplyLayout();
                UpdateClock();
                SaveConfig();
            };
            menu.Items.Add(hItem);

            var vItem = new MenuItem
            {
                Header = "竖置（时换行分换行秒）",
                IsCheckable = true,
                IsChecked = _layout == "Vertical"
            };
            vItem.Click += (s, args) =>
            {
                _layout = "Vertical";
                ApplyLayout();
                UpdateClock();
                SaveConfig();
            };
            menu.Items.Add(vItem);

            menu.IsOpen = true;
        }

        private void ApplyLayout()
        {
            _clockText.FontWeight = _isBold ? FontWeights.Bold : FontWeights.Light;
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;

            string format = _is24Hour ? "HH" : "hh";
            format += _showSeconds ? (!_is24Hour ? ":mm:ss tt" : ":mm:ss") : (!_is24Hour ? ":mm tt" : ":mm");

            if (_layout == "Vertical")
            {
                string h = now.ToString(_is24Hour ? "HH" : "hh");
                string m = now.ToString("mm");
                string s = _showSeconds ? now.ToString("ss") : "";
                string ampm = _is24Hour ? "" : now.ToString("tt");

                string text = h + "\n" + m;
                if (!string.IsNullOrEmpty(s)) text += "\n" + s;
                if (!string.IsNullOrEmpty(ampm)) text += "\n" + ampm;

                _clockText.Text = text;
                _clockText.FontSize = 28;
                _clockText.TextAlignment = TextAlignment.Center;
            }
            else
            {
                _clockText.Text = now.ToString(format);
                _clockText.FontSize = 46;
                _clockText.TextAlignment = TextAlignment.Center;
            }
        }

        public void Cleanup()
        {
            _timer.Stop();
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            _clockText.Foreground = new SolidColorBrush(textColor);
        }

        private class ClockConfig
        {
            public bool ShowSeconds { get; set; } = true;
            public bool Is24Hour { get; set; } = true;
            public bool IsBold { get; set; }
            public string Layout { get; set; } = "Horizontal";
        }
    }
}
