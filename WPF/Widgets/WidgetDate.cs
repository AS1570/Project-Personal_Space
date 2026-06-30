using System;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp3.Widgets
{
    public class WidgetDate : WidgetBase
    {
        private readonly TextBlock _dateText;
        private readonly TextBlock _dayText;
        private readonly StackPanel _container;
        private readonly DispatcherTimer _timer;

        private string _dateOrder = "yyyy/MM/dd";
        private string _dateStyle = "Number";
        private string _monthStyle = "Number";
        private string _yearStyle = "Full";
        private bool _showDayOfWeek = true;
        private string _dayOfWeekPos = "Above";

        public WidgetDate()
        {
            SetTitle("日期");
            Width = 240;
            Height = 150;

            _container = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _dayText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAB, 0xFF)),
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            _dateText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                FontSize = 26,
                FontWeight = FontWeights.Light,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _container.Children.Add(_dayText);
            _container.Children.Add(_dateText);
            SetContent(_container);

            _dateText.MouseRightButtonDown += (s, e) =>
            {
                ShowDateMenu();
                e.Handled = true;
            };

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _timer.Tick += (s, e) => UpdateDate();
            _timer.Start();

            Loaded += (s, e) => LoadConfig();
        }

        private void LoadConfig()
        {
            try
            {
                var config = JsonSerializer.Deserialize<DateConfig>(Data?.Config ?? "{}");
                if (config != null)
                {
                    _dateOrder = config.DateOrder ?? "yyyy/MM/dd";
                    _dateStyle = config.DateStyle ?? "Number";
                    _monthStyle = config.MonthStyle ?? "Number";
                    _yearStyle = config.YearStyle ?? "Full";
                    _showDayOfWeek = config.ShowDayOfWeek;
                    _dayOfWeekPos = config.DayOfWeekPosition ?? "Above";
                }
            }
            catch { }
            RebuildLayout();
            UpdateDate();
        }

        private void SaveConfig()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["DateOrder"] = _dateOrder;
            root["DateStyle"] = _dateStyle;
            root["MonthStyle"] = _monthStyle;
            root["YearStyle"] = _yearStyle;
            root["ShowDayOfWeek"] = _showDayOfWeek;
            root["DayOfWeekPosition"] = _dayOfWeekPos;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        private void ShowDateMenu()
        {
            var menu = new ContextMenu();

            var orderHeader = new MenuItem { Header = "日期排布逻辑", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(orderHeader);

            foreach (var o in new[] { ("年/月/日", "yyyy/MM/dd"), ("月/日/年", "MM/dd/yyyy"), ("日/月/年", "dd/MM/yyyy"), ("年/日/月", "yyyy/dd/MM") })
            {
                var item = new MenuItem
                {
                    Header = o.Item1,
                    IsCheckable = true,
                    IsChecked = _dateOrder == o.Item2
                };
                item.Click += (s, args) => { _dateOrder = o.Item2; RebuildLayout(); UpdateDate(); SaveConfig(); };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var styleHeader = new MenuItem { Header = "日期样式", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(styleHeader);

            foreach (var s in new[] { ("数字", "Number"), ("数字+英文后缀", "NumberSuffix") })
            {
                var item = new MenuItem
                {
                    Header = s.Item1,
                    IsCheckable = true,
                    IsChecked = _dateStyle == s.Item2
                };
                item.Click += (s2, args) => { _dateStyle = s.Item2; UpdateDate(); SaveConfig(); };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var monthHeader = new MenuItem { Header = "月份样式", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(monthHeader);

            foreach (var m in new[] { ("数字", "Number"), ("英文缩写", "Abbr") })
            {
                var item = new MenuItem
                {
                    Header = m.Item1,
                    IsCheckable = true,
                    IsChecked = _monthStyle == m.Item2
                };
                item.Click += (s2, args) => { _monthStyle = m.Item2; UpdateDate(); SaveConfig(); };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var yearHeader = new MenuItem { Header = "年份样式", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(yearHeader);

            foreach (var y in new[] { ("XXXX（完整年份）", "Full"), ("XX（年份后两位）", "Short") })
            {
                var item = new MenuItem
                {
                    Header = y.Item1,
                    IsCheckable = true,
                    IsChecked = _yearStyle == y.Item2
                };
                item.Click += (s2, args) => { _yearStyle = y.Item2; UpdateDate(); SaveConfig(); };
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());

            var dowItem = new MenuItem
            {
                Header = "显示星期",
                IsCheckable = true,
                IsChecked = _showDayOfWeek
            };
            dowItem.Click += (s2, args) => { _showDayOfWeek = !_showDayOfWeek; RebuildLayout(); UpdateDate(); SaveConfig(); };
            menu.Items.Add(dowItem);

            menu.Items.Add(new Separator());

            var posHeader = new MenuItem { Header = "星期-日期排版", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(posHeader);

            foreach (var p in new[] { ("星期在日期上", "Above"), ("星期在日期下", "Below"), ("星期在日期左侧", "Left"), ("星期在日期右侧", "Right") })
            {
                var item = new MenuItem
                {
                    Header = p.Item1,
                    IsCheckable = true,
                    IsChecked = _dayOfWeekPos == p.Item2
                };
                item.Click += (s2, args) => { _dayOfWeekPos = p.Item2; RebuildLayout(); UpdateDate(); SaveConfig(); };
                menu.Items.Add(item);
            }

            menu.IsOpen = true;
        }

        private void RebuildLayout()
        {
            _container.Children.Clear();

            _dayText.Visibility = _showDayOfWeek ? Visibility.Visible : Visibility.Collapsed;

            _dayText.HorizontalAlignment = HorizontalAlignment.Center;
            _dateText.HorizontalAlignment = HorizontalAlignment.Center;
            _dayText.Margin = new Thickness(0);
            _dateText.Margin = new Thickness(0);

            switch (_dayOfWeekPos)
            {
                case "Above":
                    _container.Orientation = Orientation.Vertical;
                    _container.Children.Add(_dayText);
                    _container.Children.Add(_dateText);
                    _dayText.Margin = new Thickness(0, 0, 0, 4);
                    break;
                case "Below":
                    _container.Orientation = Orientation.Vertical;
                    _container.Children.Add(_dateText);
                    _container.Children.Add(_dayText);
                    _dayText.Margin = new Thickness(0, 4, 0, 0);
                    break;
                case "Left":
                    _container.Orientation = Orientation.Horizontal;
                    _container.Children.Add(_dayText);
                    _container.Children.Add(_dateText);
                    _dayText.Margin = new Thickness(0, 0, 8, 0);
                    _dayText.VerticalAlignment = VerticalAlignment.Center;
                    break;
                case "Right":
                    _container.Orientation = Orientation.Horizontal;
                    _container.Children.Add(_dateText);
                    _container.Children.Add(_dayText);
                    _dayText.Margin = new Thickness(8, 0, 0, 0);
                    _dayText.VerticalAlignment = VerticalAlignment.Center;
                    break;
            }
        }

        private void UpdateDate()
        {
            var now = DateTime.Now;
            _dateText.Text = MainWindow.FormatDate(now, _dateOrder, _dateStyle, _monthStyle, _yearStyle);

            if (_showDayOfWeek)
            {
                string[] weekDays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
                _dayText.Text = weekDays[(int)now.DayOfWeek];
            }
        }

        public void Cleanup()
        {
            _timer.Stop();
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            _dateText.Foreground = new SolidColorBrush(textColor);
            _dayText.Foreground = new SolidColorBrush(GetEffectiveAccentColor());
        }

        private class DateConfig
        {
            public string DateOrder { get; set; } = "yyyy/MM/dd";
            public string DateStyle { get; set; } = "Number";
            public string MonthStyle { get; set; } = "Number";
            public string YearStyle { get; set; } = "Full";
            public bool ShowDayOfWeek { get; set; } = true;
            public string DayOfWeekPosition { get; set; } = "Above";
        }
    }
}
