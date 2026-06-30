using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl5 : UserControl
    {
        private DateTime _currentMonth;
        private string? _selectedDate;
        private DiaryEntry? _currentEntry;
        private bool _isLoading;
        private bool _suppressTextChanged;

        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private const int MaxHistoryCount = 100;

        private readonly Dictionary<string, DiaryEntry> _monthDiaries = new();

        private double _moodIconSize = 18;

        private static readonly List<string> WeatherPresets = new()
        {
            "☀️ 晴", "🌤 多云", "⛅ 阴", "🌧 小雨", "⛈ 雷雨", "🌨 雪", "❄️ 冷", "🌪 大风"
        };

        private static readonly List<string> MoodPresets = new()
        {
            "😊 开心", "😢 伤心", "😡 生气", "😴 困倦", "🤩 兴奋", "😐 一般", "😰 焦虑", "😍 喜爱"
        };

        private static readonly Dictionary<string, string> WeatherEmojiMap = new()
        {
            {"☀️ 晴", "☀️"}, {"🌤 多云", "🌤"}, {"⛅ 阴", "⛅"}, {"🌧 小雨", "🌧"},
            {"⛈ 雷雨", "⛈"}, {"🌨 雪", "🌨"}, {"❄️ 冷", "❄️"}, {"🌪 大风", "🌪"}
        };

        private static readonly Dictionary<string, string> MoodEmojiMap = new()
        {
            {"😊 开心", "😊"}, {"😢 伤心", "😢"}, {"😡 生气", "😡"}, {"😴 困倦", "😴"},
            {"🤩 兴奋", "🤩"}, {"😐 一般", "😐"}, {"😰 焦虑", "😰"}, {"😍 喜爱", "😍"}
        };

        public UserControl5()
        {
            InitializeComponent();
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            WeatherComboBox.ItemsSource = WeatherPresets;
            MoodComboBox.ItemsSource = MoodPresets;

            CalendarGridHolder.SizeChanged += (s, e) =>
            {
                var oldSize = _moodIconSize;
                _moodIconSize = CalculateMoodIconSize(CalendarGridHolder.ActualHeight);
                if (Math.Abs(_moodIconSize - oldSize) > 0.5)
                    BuildMonthView();
            };

            RefreshCalendar();
            ClearEditor();
            UpdateStatusBar();
            UpdateUndoRedoButtons();
            Focus();
        }

        #region Calendar

        private void RefreshCalendar()
        {
            MonthYearLabel.Text = _currentMonth.ToString("yyyy年 M月");
            LoadMonthDiaries();
            BuildMonthView();
        }

        private void LoadMonthDiaries()
        {
            _monthDiaries.Clear();
            var db = App.Database.Connection;
            if (db == null || !App.Database.IsConnected) return;

            var firstDay = _currentMonth.ToString("yyyy-MM-01");
            var lastDay = _currentMonth.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");

            try
            {
                var entries = db.Query<DiaryEntry>(
                    "SELECT * FROM Diary WHERE Date >= ? AND Date <= ?", firstDay, lastDay);
                foreach (var entry in entries)
                {
                    _monthDiaries[entry.Date] = entry;
                }
            }
            catch
            {
            }
        }

        private void BuildMonthView()
        {
            if (CalendarGridHolder.ActualHeight > 0)
                _moodIconSize = CalculateMoodIconSize(CalendarGridHolder.ActualHeight);

            var grid = new Grid();

            for (int i = 0; i < 7; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < 6; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            string[] dayHeaders = { "日", "一", "二", "三", "四", "五", "六" };
            for (int col = 0; col < 7; col++)
            {
                var isWeekend = col == 0 || col == 6;

                var headerText = new TextBlock
                {
                    Text = dayHeaders[col],
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerText.SetResourceReference(TextBlock.ForegroundProperty,
                    isWeekend ? "Danger" : "TextMuted");

                var headerBorder = new Border
                {
                    Padding = new Thickness(2, 4, 2, 4),
                    BorderThickness = new Thickness(0.5),
                    Child = headerText
                };
                headerBorder.SetResourceReference(Border.BackgroundProperty, "BgSurface");
                headerBorder.SetResourceReference(Border.BorderBrushProperty, "BorderAlt");

                Grid.SetRow(headerBorder, 0);
                Grid.SetColumn(headerBorder, col);
                grid.Children.Add(headerBorder);
            }

            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int startDayOfWeek = (int)firstDay.DayOfWeek;
            var today = DateTime.Today;

            int day = 1;
            for (int row = 1; row <= 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int dayIndex = (row - 1) * 7 + col;

                    if (dayIndex < startDayOfWeek || day > daysInMonth)
                    {
                        var emptyBorder = new Border
                        {
                            BorderThickness = new Thickness(0.5)
                        };
                        emptyBorder.SetResourceReference(Border.BackgroundProperty, "InputBg");
                        emptyBorder.SetResourceReference(Border.BorderBrushProperty, "InputBorder");
                        Grid.SetRow(emptyBorder, row);
                        Grid.SetColumn(emptyBorder, col);
                        grid.Children.Add(emptyBorder);
                        continue;
                    }

                    var dateStr = $"{_currentMonth:yyyy-MM}-{day:D2}";
                    _monthDiaries.TryGetValue(dateStr, out var entry);
                    bool isToday = today.Year == _currentMonth.Year
                                   && today.Month == _currentMonth.Month
                                   && today.Day == day;
                    bool isSelected = _selectedDate == dateStr;

                    var cellBorder = new Border
                    {
                        BorderThickness = new Thickness(0.5),
                        Cursor = Cursors.Hand
                    };
                    cellBorder.SetResourceReference(Border.BorderBrushProperty, "BorderAlt");

                    if (isSelected)
                    {
                        cellBorder.SetResourceReference(Border.BackgroundProperty, "ToggleTrack");
                        cellBorder.SetResourceReference(Border.BorderBrushProperty, "Accent");
                        cellBorder.BorderThickness = new Thickness(1.5);
                    }
                    else if (isToday)
                    {
                        cellBorder.SetResourceReference(Border.BackgroundProperty, "ToggleOff");
                    }
                    else
                    {
                        cellBorder.SetResourceReference(Border.BackgroundProperty, "BgPanel");
                    }

                    var cellGrid = new Grid();

                    var dayText = new TextBlock
                    {
                        Text = day.ToString(),
                        FontSize = 13,
                        FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(4, 3, 0, 0)
                    };
                    dayText.SetResourceReference(TextBlock.ForegroundProperty,
                        isToday ? "Accent" : "TextPrimary");
                    cellGrid.Children.Add(dayText);

                    if (entry != null)
                    {
                        var checkMark = new TextBlock
                        {
                            Text = "✓",
                            FontSize = 10,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(24, 4, 0, 0)
                        };
                        checkMark.SetResourceReference(TextBlock.ForegroundProperty, "Accent");
                        cellGrid.Children.Add(checkMark);

                        if (!string.IsNullOrEmpty(entry.Weather))
                        {
                            var weatherEmoji = new TextBlock
                            {
                                Text = GetWeatherEmoji(entry.Weather),
                                FontSize = 11,
                                Opacity = 0.9,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                VerticalAlignment = VerticalAlignment.Top,
                                Margin = new Thickness(0, 3, 3, 0)
                            };
                            weatherEmoji.SetResourceReference(TextBlock.ForegroundProperty, "TextBright");
                            cellGrid.Children.Add(weatherEmoji);
                        }

                        if (!string.IsNullOrEmpty(entry.Mood))
                        {
                            var moodEmoji = new TextBlock
                            {
                                Text = GetMoodEmoji(entry.Mood),
                                FontSize = _moodIconSize,
                                Opacity = 0.9,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            moodEmoji.SetResourceReference(TextBlock.ForegroundProperty, "TextBright");
                            cellGrid.Children.Add(moodEmoji);
                        }
                    }

                    cellBorder.Child = cellGrid;

                    var dateKey = dateStr;
                    cellBorder.MouseLeftButtonDown += (_, _) => SelectDate(dateKey);

                    Grid.SetRow(cellBorder, row);
                    Grid.SetColumn(cellBorder, col);
                    grid.Children.Add(cellBorder);

                    day++;
                }
            }

            CalendarGridHolder.Child = grid;
        }

        private double CalculateMoodIconSize(double holderHeight)
        {
            const double headerRowHeight = 25;
            const double maxRows = 6;
            var cellHeight = (holderHeight - headerRowHeight) / maxRows;
            var size = cellHeight * 0.45;
            return Math.Max(10, Math.Min(36, size));
        }

        private static string GetWeatherEmoji(string weather)
        {
            if (WeatherEmojiMap.TryGetValue(weather, out var emoji)) return emoji;
            if (WeatherPresets.Contains(weather)) return weather[..2];
            return weather.Length >= 2 ? weather[..2] : "?";
        }

        private static string GetMoodEmoji(string mood)
        {
            if (MoodEmojiMap.TryGetValue(mood, out var emoji)) return emoji;
            if (MoodPresets.Contains(mood)) return mood[..2];
            return mood.Length >= 2 ? mood[..2] : "?";
        }

        private void SelectDate(string dateKey)
        {
            if (_selectedDate == dateKey) return;

            _selectedDate = dateKey;
            LoadDiaryEntry(dateKey);
            BuildMonthView();
        }

        private void PrevMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            RefreshCalendar();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            RefreshCalendar();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _selectedDate = DateTime.Now.ToString("yyyy-MM-dd");
            LoadDiaryEntry(_selectedDate);
            RefreshCalendar();
        }

        #endregion

        #region Diary Editor

        private void LoadDiaryEntry(string dateKey)
        {
            var db = App.Database.Connection;
            if (db == null || !App.Database.IsConnected)
            {
                ClearEditor();
                SelectedDateLabel.Text = DateTime.ParseExact(dateKey, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    .ToString("yyyy年 M月 d日") + " (数据库未连接)";
                return;
            }

            _isLoading = true;
            _suppressTextChanged = true;

            try
            {
                _currentEntry = db.Find<DiaryEntry>(dateKey);
            }
            catch
            {
                _currentEntry = null;
            }

            if (_currentEntry != null)
            {
                DiaryTextBox.Text = _currentEntry.Content ?? "";
                WeatherComboBox.Text = _currentEntry.Weather ?? "";
                MoodComboBox.Text = _currentEntry.Mood ?? "";
            }
            else
            {
                _currentEntry = new DiaryEntry { Date = dateKey, Weather = "", Mood = "", Content = "" };
                DiaryTextBox.Text = "";
                WeatherComboBox.Text = "";
                MoodComboBox.Text = "";
            }

            _suppressTextChanged = false;
            _isLoading = false;

            _undoStack.Clear();
            _redoStack.Clear();
            PushUndoState(DiaryTextBox.Text);

            var parsedDate = DateTime.ParseExact(dateKey, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            SelectedDateLabel.Text = parsedDate.ToString("yyyy年 M月 d日 dddd",
                CultureInfo.GetCultureInfo("zh-CN"));

            UpdateStatusBar();
            UpdateUndoRedoButtons();
            DiaryTextBox.Focus();
        }

        private void PushUndoState(string text)
        {
            if (_isLoading) return;

            if (_undoStack.Count >= MaxHistoryCount)
            {
                var list = _undoStack.ToList();
                list.RemoveAt(0);
                _undoStack.Clear();
                foreach (var item in list)
                    _undoStack.Push(item);
            }
            _undoStack.Push(text);
        }

        private void Undo()
        {
            if (_undoStack.Count <= 1) return;

            _suppressTextChanged = true;
            var currentText = _undoStack.Pop();
            _redoStack.Push(currentText);

            if (_redoStack.Count > MaxHistoryCount)
            {
                var list = _redoStack.ToList();
                list.RemoveAt(0);
                _redoStack.Clear();
                foreach (var item in list)
                    _redoStack.Push(item);
            }

            var previousText = _undoStack.Peek();
            DiaryTextBox.Text = previousText;
            DiaryTextBox.CaretIndex = Math.Min(DiaryTextBox.CaretIndex, previousText.Length);
            _suppressTextChanged = false;

            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            _suppressTextChanged = true;
            var redoText = _redoStack.Pop();
            _undoStack.Push(redoText);

            DiaryTextBox.Text = redoText;
            DiaryTextBox.CaretIndex = Math.Min(DiaryTextBox.CaretIndex, redoText.Length);
            _suppressTextChanged = false;

            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void SaveDiary()
        {
            if (_currentEntry == null || _selectedDate == null) return;

            var db = App.Database.Connection;
            if (db == null || !App.Database.IsConnected)
            {
                MessageBox.Show("数据库未连接，无法保存日记。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _currentEntry.Content = DiaryTextBox.Text;
            _currentEntry.Weather = WeatherComboBox.Text ?? "";
            _currentEntry.Mood = MoodComboBox.Text ?? "";

            try
            {
                db.InsertOrReplace(_currentEntry);

                _monthDiaries[_currentEntry.Date] = _currentEntry;
                BuildMonthView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearEditor()
        {
            _isLoading = true;
            _suppressTextChanged = true;

            _currentEntry = null;
            DiaryTextBox.Text = "";
            WeatherComboBox.Text = "";
            MoodComboBox.Text = "";

            _suppressTextChanged = false;
            _isLoading = false;

            _undoStack.Clear();
            _redoStack.Clear();

            SelectedDateLabel.Text = "未选择日期";
            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void ZoomIn()
        {
            if (DiaryTextBox.FontSize < 36)
            {
                DiaryTextBox.FontSize = Math.Min(36, DiaryTextBox.FontSize + 1);
                UpdateZoomLabel();
            }
        }

        private void ZoomOut()
        {
            if (DiaryTextBox.FontSize > 10)
            {
                DiaryTextBox.FontSize = Math.Max(10, DiaryTextBox.FontSize - 1);
                UpdateZoomLabel();
            }
        }

        private void UpdateZoomLabel()
        {
            var percent = (int)Math.Round(DiaryTextBox.FontSize / 14.0 * 100);
            ZoomRatioLabel.Text = $"{percent}%";
        }

        private void UpdateStatusBar()
        {
            var caretIndex = DiaryTextBox.CaretIndex;
            var line = DiaryTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            var col = caretIndex - DiaryTextBox.GetCharacterIndexFromLineIndex(line);

            CursorInfoLabel.Text = $"行 {line + 1}, 列 {col + 1}";
            CharCountLabel.Text = $"字数: {DiaryTextBox.Text.Length}";

            if (DiaryTextBox.SelectionLength > 0)
            {
                SelectedCharCountLabel.Text = $"已选: {DiaryTextBox.SelectionLength}";
            }
            else
            {
                SelectedCharCountLabel.Text = "";
            }
        }

        private void UpdateUndoRedoButtons()
        {
            UndoButton.IsEnabled = _undoStack.Count > 1;
            RedoButton.IsEnabled = _redoStack.Count > 0;
        }

        private static bool IsTextChangingKey(Key key)
        {
            return key switch
            {
                Key.Back or Key.Delete or Key.Enter or Key.Tab or Key.Space => true,
                _ => key >= Key.A && key <= Key.Z
                     || key >= Key.D0 && key <= Key.D9
                     || key >= Key.NumPad0 && key <= Key.NumPad9
                     || key == Key.OemPeriod || key == Key.OemComma || key == Key.OemQuestion
                     || key == Key.OemMinus || key == Key.OemPlus || key == Key.OemQuotes
                     || key == Key.OemSemicolon || key == Key.OemOpenBrackets
                     || key == Key.OemCloseBrackets || key == Key.OemPipe
                     || key == Key.OemBackslash || key == Key.OemTilde,
            };
        }

        #endregion

        #region Editor Events

        private void DiaryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading || _suppressTextChanged) return;

            _suppressTextChanged = true;
            PushUndoState(DiaryTextBox.Text);
            _suppressTextChanged = false;

            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void DiaryTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusBar();
        }

        private void DiaryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SaveDiary();
            }
        }

        private void DiaryTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                Undo();
                return;
            }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                Redo();
                return;
            }
        }

        private void DiaryTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                if (e.Delta > 0)
                    ZoomIn();
                else
                    ZoomOut();
            }
        }

        #endregion

        #region Toolbar Button Events

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDiary();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            Undo();
        }

        private void RedoButton_Click(object sender, RoutedEventArgs e)
        {
            Redo();
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void MoodSizeUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (_moodIconSize < 40)
            {
                _moodIconSize += 2;
                BuildMonthView();
            }
        }

        private void MoodSizeDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (_moodIconSize > 8)
            {
                _moodIconSize -= 2;
                BuildMonthView();
            }
        }

        private void ExportTodayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEntry == null || string.IsNullOrEmpty(DiaryTextBox.Text))
            {
                MessageBox.Show("当前没有日记内容可导出。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "导出今日日记",
                Filter = "文本文件|*.txt|所有文件|*.*",
                DefaultExt = ".txt",
                FileName = $"日记_{_selectedDate}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    var parsedDate = DateTime.ParseExact(_selectedDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    sb.AppendLine($"日期: {parsedDate:yyyy年 M月 d日 dddd}");
                    sb.AppendLine($"天气: {WeatherComboBox.Text}");
                    sb.AppendLine($"心情: {MoodComboBox.Text}");
                    sb.AppendLine(new string('─', 40));
                    sb.AppendLine();
                    sb.AppendLine(DiaryTextBox.Text);

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"日记已导出到:\n{dialog.FileName}", "导出成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedDate = null;
            ClearEditor();
            BuildMonthView();
        }

        private void WordWrapToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (WordWrapToggleButton.IsChecked == true)
            {
                DiaryTextBox.TextWrapping = TextWrapping.Wrap;
                WordWrapToggleButton.Content = "↩";
            }
            else
            {
                DiaryTextBox.TextWrapping = TextWrapping.NoWrap;
                WordWrapToggleButton.Content = "→";
            }
        }

        #endregion

        #region Weather / Mood

        private void WeatherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void MoodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        #endregion

        #region Export

        private void ExportMonthButton_Click(object sender, RoutedEventArgs e)
        {
            var db = App.Database.Connection;
            if (db == null || !App.Database.IsConnected)
            {
                MessageBox.Show("数据库未连接。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firstDay = _currentMonth.ToString("yyyy-MM-01");
            var lastDay = _currentMonth.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");

            List<DiaryEntry> entries;
            try
            {
                entries = db.Query<DiaryEntry>(
                    "SELECT * FROM Diary WHERE Date >= ? AND Date <= ? ORDER BY Date",
                    firstDay, lastDay);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("本月没有日记记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "导出本月日记",
                Filter = "文本文件|*.txt|所有文件|*.*",
                DefaultExt = ".txt",
                FileName = $"日记_{_currentMonth:yyyy-MM}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"日记导出 - {_currentMonth:yyyy年 M月}");
                    sb.AppendLine(new string('═', 50));
                    sb.AppendLine();

                    foreach (var entry in entries)
                    {
                        var parsedDate = DateTime.ParseExact(entry.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        sb.AppendLine($"📅 {parsedDate:yyyy年 M月 d日 dddd}");
                        sb.AppendLine($"   天气: {entry.Weather}");
                        sb.AppendLine($"   心情: {entry.Mood}");
                        sb.AppendLine($"   内容:");
                        sb.AppendLine($"   {entry.Content.Replace("\n", "\n   ")}");
                        sb.AppendLine(new string('─', 40));
                        sb.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"本月 {entries.Count} 篇日记已导出到:\n{dialog.FileName}",
                        "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportYearButton_Click(object sender, RoutedEventArgs e)
        {
            var db = App.Database.Connection;
            if (db == null || !App.Database.IsConnected)
            {
                MessageBox.Show("数据库未连接。", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firstDay = $"{_currentMonth.Year}-01-01";
            var lastDay = $"{_currentMonth.Year}-12-31";

            List<DiaryEntry> entries;
            try
            {
                entries = db.Query<DiaryEntry>(
                    "SELECT * FROM Diary WHERE Date >= ? AND Date <= ? ORDER BY Date",
                    firstDay, lastDay);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (entries.Count == 0)
            {
                MessageBox.Show("本年度没有日记记录。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "导出本年日记",
                Filter = "文本文件|*.txt|所有文件|*.*",
                DefaultExt = ".txt",
                FileName = $"日记_{_currentMonth.Year}年.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"日记导出 - {_currentMonth.Year}年");
                    sb.AppendLine(new string('═', 50));
                    sb.AppendLine();

                    foreach (var entry in entries)
                    {
                        var parsedDate = DateTime.ParseExact(entry.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        sb.AppendLine($"📅 {parsedDate:yyyy年 M月 d日 dddd}");
                        sb.AppendLine($"   天气: {entry.Weather}");
                        sb.AppendLine($"   心情: {entry.Mood}");
                        sb.AppendLine($"   内容:");
                        sb.AppendLine($"   {entry.Content.Replace("\n", "\n   ")}");
                        sb.AppendLine(new string('─', 40));
                        sb.AppendLine();
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"本年度 {entries.Count} 篇日记已导出到:\n{dialog.FileName}",
                        "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
