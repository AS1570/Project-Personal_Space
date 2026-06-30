using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl4 : UserControl
    {
        private List<FileRecordInfo> _allTextFiles = new();
        private FileRecordInfo? _currentFile;
        private bool _isModified;
        private bool _isLoading;
        private bool _suppressTextChanged;
        private bool _isLeftPanelVisible = true;

        private readonly Stack<string> _undoStack = new();
        private readonly Stack<string> _redoStack = new();
        private const int MaxHistoryCount = 100;

        private string _currentEncodingKey = "utf-8";

        public UserControl4()
        {
            InitializeComponent();
            EncodingComboBox.SelectedIndex = 0;
            RefreshFileList();
            UpdateStatusBar();
            UpdateUndoRedoButtons();
            Focus();
        }

        private void RefreshFileList()
        {
            var files = App.FileManager.GetAllFiles()
                .Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 0)
                .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _allTextFiles = files;

            ApplyFileFilter();
        }

        private void ApplyFileFilter()
        {
            var searchText = SearchTextBox.Text?.Trim() ?? "";
            var filtered = _allTextFiles.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(f =>
                    f.FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            FileListBox.ItemsSource = filtered.ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFileFilter();
        }

        private void NewFileButton_Click(object sender, RoutedEventArgs e)
        {
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = sender as UIElement,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Center,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = BuildNewFilePopup()
            };
            popup.IsOpen = true;
        }

        private Border BuildNewFilePopup()
        {
            var panel = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 16, 20, 16),
                MinWidth = 360,
                BorderThickness = new Thickness(1)
            };
            panel.SetResourceReference(Border.BackgroundProperty, "BgPanel");
            panel.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var stack = new StackPanel();

            var title = new TextBlock
            {
                Text = "新建文本文件",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            stack.Children.Add(title);

            var label = new TextBlock
            {
                Text = "文件名称",
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 6)
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            stack.Children.Add(label);

            var input = new System.Windows.Controls.TextBox
            {
                Text = "新建文本文档.txt",
                BorderThickness = new Thickness(1),
                Height = 34,
                FontSize = 13,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(0, 0, 0, 14)
            };
            input.SetResourceReference(System.Windows.Controls.TextBox.BackgroundProperty, "BgSurface");
            input.SetResourceReference(System.Windows.Controls.TextBox.ForegroundProperty, "TextPrimary");
            input.SetResourceReference(System.Windows.Controls.TextBox.BorderBrushProperty, "BorderColor");
            stack.Children.Add(input);

            var btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cancelBtn = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 32,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            cancelBtn.SetResourceReference(Button.BackgroundProperty, "BgHover");
            cancelBtn.SetResourceReference(Button.ForegroundProperty, "TextPrimary");
            Grid.SetColumn(cancelBtn, 1);

            var createBtn = new Button
            {
                Content = "创建",
                Width = 80,
                Height = 32,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12
            };
            createBtn.SetResourceReference(Button.BackgroundProperty, "Accent");
            createBtn.SetResourceReference(Button.ForegroundProperty, "White");
            Grid.SetColumn(createBtn, 3);

            cancelBtn.Click += (s, args) =>
            {
                var pp = panel.Parent as System.Windows.Controls.Primitives.Popup;
                pp!.IsOpen = false;
            };

            createBtn.Click += (s, args) =>
            {
                string fileName = input.Text.Trim();
                if (string.IsNullOrEmpty(fileName))
                {
                    MessageBox.Show("请输入文件名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    fileName += ".txt";

                string programFileDir = App.FileManager.ProgramFileDir;
                string fullPath = System.IO.Path.Combine(programFileDir, fileName);

                if (System.IO.File.Exists(fullPath))
                {
                    var result = MessageBox.Show($"文件 \"{fileName}\" 已存在，是否覆盖？",
                        "文件已存在", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result != MessageBoxResult.Yes) return;
                }

                try
                {
                    System.IO.File.WriteAllText(fullPath, "", System.Text.Encoding.UTF8);
                    RefreshFileList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"创建文件失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                var pp = panel.Parent as System.Windows.Controls.Primitives.Popup;
                pp!.IsOpen = false;
            };

            btnGrid.Children.Add(new Border());
            btnGrid.Children.Add(cancelBtn);
            btnGrid.Children.Add(new Border());
            btnGrid.Children.Add(createBtn);
            stack.Children.Add(btnGrid);

            input.Focus();
            input.SelectAll();

            panel.Child = stack;
            return panel;
        }

        private void FileListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListBox.SelectedItem is FileRecordInfo selectedFile)
            {
                OpenFile(selectedFile);
            }
        }

        private void OpenFile(FileRecordInfo fileInfo)
        {
            if (_isModified && _currentFile != null)
            {
                var result = MessageBox.Show(
                    $"文件 \"{_currentFile.DisplayName}\" 已修改，是否保存？",
                    "未保存的更改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SaveCurrentFile();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    FileListBox.SelectedItem = _currentFile;
                    return;
                }
            }

            var bytes = App.FileManager.ReadAllBytes(fileInfo.FullPath);
            if (bytes == null) return;

            _isLoading = true;
            _currentFile = fileInfo;
            _isModified = false;
            _suppressTextChanged = true;

            string text;
            try
            {
                text = DecodeBytes(bytes, _currentEncodingKey);
            }
            catch
            {
                text = Encoding.UTF8.GetString(bytes);
                _currentEncodingKey = "utf-8";
                EncodingComboBox.SelectedIndex = 0;
            }

            EditorTextBox.Text = text;
            EditorTextBox.CaretIndex = 0;

            _suppressTextChanged = false;
            _isLoading = false;

            _undoStack.Clear();
            _redoStack.Clear();

            FileNameLabel.Text = fileInfo.FileName;
            FileExtensionLabel.Text = fileInfo.Extension;
            FileModifiedIndicator.Visibility = Visibility.Collapsed;

            PushUndoState(text);
            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private string DecodeBytes(byte[] bytes, string encodingKey)
        {
            var enc = GetEncodingByKey(encodingKey);
            return enc.GetString(bytes);
        }

        private byte[] EncodeText(string text, string encodingKey)
        {
            var enc = GetEncodingByKey(encodingKey);
            if (encodingKey == "utf-8")
            {
                return Encoding.UTF8.GetBytes(text);
            }
            if (encodingKey == "utf-16le")
            {
                return Encoding.Unicode.GetBytes(text);
            }
            return enc.GetBytes(text);
        }

        private static Encoding GetEncodingByKey(string key)
        {
            return key switch
            {
                "utf-8" => new UTF8Encoding(false),
                "utf-16le" => new UnicodeEncoding(false, false),
                "utf-16be" => Encoding.BigEndianUnicode,
                "gb2312" => Encoding.GetEncoding("gb2312"),
                "ascii" => new ASCIIEncoding(),
                _ => new UTF8Encoding(false),
            };
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

        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading || _suppressTextChanged) return;

            _suppressTextChanged = true;
            PushUndoState(EditorTextBox.Text);
            _suppressTextChanged = false;

            if (!_isModified)
            {
                _isModified = true;
                FileModifiedIndicator.Visibility = Visibility.Visible;
            }

            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateStatusBar();
        }

        private void EditorTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                SaveCurrentFile();
            }
        }

        private void EditorTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                FindReplacePanel.Visibility = FindReplacePanel.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
                if (FindReplacePanel.Visibility == Visibility.Visible)
                {
                    FindTextBox.Focus();
                    FindTextBox.SelectAll();
                }
                else
                {
                    EditorTextBox.Focus();
                }
                return;
            }

            if (e.Key == Key.Escape)
            {
                if (FindReplacePanel.Visibility == Visibility.Visible)
                {
                    e.Handled = true;
                    FindReplacePanel.Visibility = Visibility.Collapsed;
                    EditorTextBox.Focus();
                    return;
                }
            }

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

        private void EditorTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void ZoomIn()
        {
            if (EditorTextBox.FontSize < 36)
            {
                EditorTextBox.FontSize = Math.Min(36, EditorTextBox.FontSize + 1);
                UpdateZoomLabel();
            }
        }

        private void ZoomOut()
        {
            if (EditorTextBox.FontSize > 10)
            {
                EditorTextBox.FontSize = Math.Max(10, EditorTextBox.FontSize - 1);
                UpdateZoomLabel();
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomIn();
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomOut();

        private void UpdateZoomLabel()
        {
            var percent = (int)Math.Round(EditorTextBox.FontSize / 14.0 * 100);
            ZoomRatioLabel.Text = $"{percent}%";
        }

        private void UpdateStatusBar()
        {
            var caretIndex = EditorTextBox.CaretIndex;
            var line = EditorTextBox.GetLineIndexFromCharacterIndex(caretIndex);
            var col = caretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(line);

            CursorInfoLabel.Text = $"行 {line + 1}, 列 {col + 1}";
            CharCountLabel.Text = $"字符: {EditorTextBox.Text.Length}";

            if (EditorTextBox.SelectionLength > 0)
            {
                SelectedCharCountLabel.Text = $"已选: {EditorTextBox.SelectionLength}";
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
            EditorTextBox.Text = previousText;
            EditorTextBox.CaretIndex = Math.Min(EditorTextBox.CaretIndex, previousText.Length);
            _suppressTextChanged = false;

            _isModified = true;
            FileModifiedIndicator.Visibility = Visibility.Visible;
            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;

            _suppressTextChanged = true;
            var redoText = _redoStack.Pop();
            _undoStack.Push(redoText);

            EditorTextBox.Text = redoText;
            EditorTextBox.CaretIndex = Math.Min(EditorTextBox.CaretIndex, redoText.Length);
            _suppressTextChanged = false;

            _isModified = true;
            FileModifiedIndicator.Visibility = Visibility.Visible;
            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e) => Undo();
        private void RedoButton_Click(object sender, RoutedEventArgs e) => Redo();

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            if (_currentFile == null) return;

            try
            {
                var text = EditorTextBox.Text;
                var bytes = EncodeText(text, _currentEncodingKey);
                App.FileManager.SaveFile(_currentFile.FullPath, bytes);

                _isModified = false;
                FileModifiedIndicator.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isModified && _currentFile != null)
            {
                var result = MessageBox.Show(
                    $"文件 \"{_currentFile.DisplayName}\" 已修改，是否保存？",
                    "未保存的更改",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    SaveCurrentFile();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            ClearEditor();
        }

        private void ClearEditor()
        {
            _currentFile = null;
            _isModified = false;
            _isLoading = true;
            _suppressTextChanged = true;

            EditorTextBox.Text = "";
            EditorTextBox.CaretIndex = 0;

            _suppressTextChanged = false;
            _isLoading = false;

            _undoStack.Clear();
            _redoStack.Clear();

            FileNameLabel.Text = "未打开文件";
            FileExtensionLabel.Text = "";
            FileModifiedIndicator.Visibility = Visibility.Collapsed;

            FileListBox.SelectedItem = null;
            UpdateStatusBar();
            UpdateUndoRedoButtons();
        }

        private void SaveCopyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "保存副本",
                Filter = "所有文件|*.*",
                FileName = _currentFile?.FileName ?? "untitled"
            };

            if (!string.IsNullOrEmpty(_currentFile?.Extension))
            {
                dialog.Filter = $"*{_currentFile.Extension}|*{_currentFile.Extension}|所有文件|*.*";
                dialog.DefaultExt = _currentFile.Extension;
            }

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var text = EditorTextBox.Text;
                    var bytes = EncodeText(text, _currentEncodingKey);
                    File.WriteAllBytes(dialog.FileName, bytes);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存副本失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ToggleLeftPanelButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleLeftPanel();
        }

        public void ToggleLeftPanel()
        {
            if (Content is not Grid grid || grid.ColumnDefinitions.Count < 3) return;

            if (_isLeftPanelVisible)
            {
                grid.ColumnDefinitions[0].Width = new GridLength(0);
                grid.ColumnDefinitions[1].Width = new GridLength(0);
            }
            else
            {
                grid.ColumnDefinitions[0].Width = new GridLength(260);
                grid.ColumnDefinitions[1].Width = new GridLength(5);
            }
            _isLeftPanelVisible = !_isLeftPanelVisible;
        }

        public void OpenFile(string filePath)
        {
            var fileInfo = App.FileManager.GetFileByPath(filePath);
            if (fileInfo != null) OpenFile(fileInfo);
        }

        private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindStatusText.Text = "";
        }

        private void FindNextButton_Click(object sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void FindPrevButton_Click(object sender, RoutedEventArgs e)
        {
            FindPrev();
        }

        private void FindNext()
        {
            var searchText = FindTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                FindStatusText.Text = "请输入查找内容";
                return;
            }

            var content = EditorTextBox.Text;
            var startIndex = EditorTextBox.CaretIndex + EditorTextBox.SelectionLength;

            if (startIndex >= content.Length)
            {
                startIndex = 0;
            }

            var index = content.IndexOf(searchText, startIndex, StringComparison.Ordinal);
            if (index == -1 && startIndex > 0)
            {
                index = content.IndexOf(searchText, 0, StringComparison.Ordinal);
            }

            if (index >= 0)
            {
                EditorTextBox.Select(index, searchText.Length);
                EditorTextBox.Focus();
                FindStatusText.Text = $"找到匹配项，位置 {index + 1}";
            }
            else
            {
                FindStatusText.Text = "未找到匹配项";
            }
        }

        private void FindPrev()
        {
            var searchText = FindTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
            {
                FindStatusText.Text = "请输入查找内容";
                return;
            }

            var content = EditorTextBox.Text;
            var startIndex = EditorTextBox.CaretIndex - 1;

            if (startIndex < 0)
            {
                startIndex = content.Length - 1;
            }

            var index = content.LastIndexOf(searchText, startIndex, StringComparison.Ordinal);
            if (index == -1 && startIndex < content.Length - 1)
            {
                index = content.LastIndexOf(searchText, content.Length - 1, StringComparison.Ordinal);
            }

            if (index >= 0)
            {
                EditorTextBox.Select(index, searchText.Length);
                EditorTextBox.Focus();
                FindStatusText.Text = $"找到匹配项，位置 {index + 1}";
            }
            else
            {
                FindStatusText.Text = "未找到匹配项";
            }
        }

        private void ReplaceAllButton_Click(object sender, RoutedEventArgs e)
        {
            var searchText = FindTextBox.Text;
            var replaceText = ReplaceTextBox.Text;

            if (string.IsNullOrEmpty(searchText))
            {
                FindStatusText.Text = "请输入查找内容";
                return;
            }

            var content = EditorTextBox.Text;
            var count = 0;
            var newContent = content.Replace(searchText, replaceText);
            count = (content.Length - newContent.Length) / Math.Max(1, searchText.Length - replaceText.Length);
            if (count > 0 || content != newContent)
            {
                EditorTextBox.Text = newContent;
                if (_currentFile != null)
                {
                    _isModified = true;
                    FileModifiedIndicator.Visibility = Visibility.Visible;
                }
                FindStatusText.Text = $"已替换 {count} 处";
            }
            else
            {
                FindStatusText.Text = "未找到匹配项";
            }
        }

        private void WordWrapToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (WordWrapToggleButton.IsChecked == true)
            {
                EditorTextBox.TextWrapping = TextWrapping.Wrap;
                WordWrapToggleButton.Content = "↩";
            }
            else
            {
                EditorTextBox.TextWrapping = TextWrapping.NoWrap;
                WordWrapToggleButton.Content = "→";
            }
        }

        private void EncodingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EncodingComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _currentEncodingKey = tag;
                EncodingLabel.Text = item.Content?.ToString() ?? tag;

                if (_currentFile != null && !_isLoading)
                {
                    var bytes = App.FileManager.ReadAllBytes(_currentFile.FullPath);
                    if (bytes != null)
                    {
                        _isLoading = true;
                        _suppressTextChanged = true;
                        try
                        {
                            var text = DecodeBytes(bytes, _currentEncodingKey);
                            EditorTextBox.Text = text;
                        }
                        catch
                        {
                            MessageBox.Show($"无法使用 {item.Content} 编码读取文件，已恢复为 UTF-8。",
                                "编码错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                            _currentEncodingKey = "utf-8";
                            EncodingComboBox.SelectedIndex = 0;
                        }
                        _suppressTextChanged = false;
                        _isLoading = false;
                        UpdateStatusBar();
                    }
                }
            }
        }
    }

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double size = bytes;
                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size /= 1024;
                }
                return string.Format("{0:0.##} {1}", size, sizes[order]);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
