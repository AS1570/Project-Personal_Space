using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl7 : UserControl
    {
        public event Action<FileRecordInfo>? NavigateToViewer;

        private List<FileRecordInfo> _allFiles = new();
        private List<FileRecordInfo> _displayFiles = new();
        private FileRecordInfo? _currentFile;
        private string _currentFilter = "all";
        private string _currentSortType = "name";
        private bool _currentSortAsc = true;
        private bool _isListView = true;
        private bool _isLeftPanelVisible = true;
        private bool _isRightPanelVisible = true;

        public UserControl7()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadFiles();
        }

        private void LoadFiles()
        {
            _allFiles = App.FileManager.GetAllFiles();
            ApplyFilterSortAndView();
            UpdateStatistics();
        }

        private void ApplyFilterSortAndView()
        {
            var filtered = _allFiles.AsEnumerable();

            filtered = _currentFilter switch
            {
                "image" => filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 2),
                "video" => filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 3),
                "music" => filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 4),
                "text" => filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 0),
                "other" => filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) is 1 or 5),
                _ => filtered
            };

            var searchText = SearchTextBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(f =>
                    f.FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    f.Extension.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            filtered = _currentSortType switch
            {
                "name" => _currentSortAsc
                    ? filtered.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                    : filtered.OrderByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase),
                "time" => _currentSortAsc
                    ? filtered.OrderBy(f => f.SaveTime)
                    : filtered.OrderByDescending(f => f.SaveTime),
                "type" => _currentSortAsc
                    ? filtered.OrderBy(f => FileManagerService.GetFileTypeByExtension(f.Extension))
                        .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                    : filtered.OrderByDescending(f => FileManagerService.GetFileTypeByExtension(f.Extension))
                        .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase),
                "size" => _currentSortAsc
                    ? filtered.OrderBy(f => f.FileSize)
                    : filtered.OrderByDescending(f => f.FileSize),
                _ => filtered.OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            };

            _displayFiles = filtered.ToList();
            FileCountLabel.Text = $"共 {_displayFiles.Count} 个文件";
            UpdateView();
        }

        private void UpdateView()
        {
            if (_isListView)
            {
                FileListView.Visibility = Visibility.Visible;
                TileScrollViewer.Visibility = Visibility.Collapsed;
                FileListView.ItemsSource = _displayFiles;

                var selectedFile = _currentFile;
                if (selectedFile != null && _displayFiles.Contains(selectedFile))
                    FileListView.SelectedItem = selectedFile;
            }
            else
            {
                FileListView.Visibility = Visibility.Collapsed;
                TileScrollViewer.Visibility = Visibility.Visible;
                BuildTileView();
            }
        }

        private void BuildTileView()
        {
            TileWrapPanel.Children.Clear();

            foreach (var file in _displayFiles)
            {
                var fileType = FileManagerService.GetFileTypeByExtension(file.Extension);

                var border = new Border
                {
                    Width = 140,
                    Height = 140,
                    Margin = new Thickness(4),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D44")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Cursor = Cursors.Hand,
                    Tag = file
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

                var iconText = fileType switch
                {
                    2 => "🖼",
                    3 => "🎬",
                    4 => "🎵",
                    0 => "📄",
                    _ => "📦"
                };

                var iconBlock = new TextBlock
                {
                    Text = iconText,
                    FontSize = 36,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6A6A80")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                Grid.SetRow(iconBlock, 0);
                grid.Children.Add(iconBlock);

                var nameBlock = new TextBlock
                {
                    Text = file.FileName,
                    FontSize = 10,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8F0")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 130,
                    Margin = new Thickness(4, 4, 4, 0)
                };
                Grid.SetRow(nameBlock, 1);
                grid.Children.Add(nameBlock);

                var infoBlock = new TextBlock
                {
                    Text = $"{file.Extension}  {FileManagerService.FormatFileSize(file.FileSize)}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9898B0")),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 130,
                    Margin = new Thickness(4, 18, 4, 0)
                };
                Grid.SetRow(infoBlock, 1);
                grid.Children.Add(infoBlock);

                border.Child = grid;

                border.MouseLeftButtonDown += TileItem_Click;
                border.MouseEnter += (s, args) =>
                {
                    if (s is Border bd)
                        bd.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7C8AFF"));
                };
                border.MouseLeave += (s, args) =>
                {
                    if (s is Border bd)
                    {
                        var isSelected = bd.Tag is FileRecordInfo f && f == _currentFile;
                        bd.BorderBrush = isSelected
                            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0ABFF"))
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3A3A55"));
                    }
                };

                if (file == _currentFile)
                    border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0ABFF"));

                TileWrapPanel.Children.Add(border);
            }
        }

        private void TileItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is FileRecordInfo file)
            {
                if (e.ClickCount >= 2)
                {
                    OpenFileInViewer(file);
                }
                else
                {
                    SelectFile(file);
                }
            }
        }

        private void UpdateStatistics()
        {
            var countAll = _allFiles.Count;
            var countImage = _allFiles.Count(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 2);
            var countVideo = _allFiles.Count(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 3);
            var countMusic = _allFiles.Count(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 4);
            var countText = _allFiles.Count(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 0);
            var countOther = _allFiles.Count(f => FileManagerService.GetFileTypeByExtension(f.Extension) is 1 or 5);

            StatAll.Text = $"全部: {countAll}";
            StatImage.Text = $"图片: {countImage}";
            StatVideo.Text = $"视频: {countVideo}";
            StatMusic.Text = $"音乐: {countMusic}";
            StatText.Text = $"文本: {countText}";
            StatOther.Text = $"其他: {countOther}";
        }

        private void SelectFile(FileRecordInfo file)
        {
            _currentFile = file;
            UpdateRightPanel();

            if (_isListView)
            {
                FileListView.SelectedItem = file;
            }
            else
            {
                BuildTileView();
            }
        }

        private void UpdateRightPanel()
        {
            if (_currentFile == null)
            {
                FileNameTextBox.Text = "";
                ExtensionTextBox.Text = "";
                FileSizeLabel.Text = "—";
                SaveTimeLabel.Text = "—";
                FullPathLabel.Text = "—";
                BtnOpenFile.IsEnabled = false;
                BtnExportFile.IsEnabled = false;
                BtnDeleteFile.IsEnabled = false;
                BtnRename.IsEnabled = false;
                BtnChangeExtension.IsEnabled = false;
                return;
            }

            FileNameTextBox.Text = _currentFile.FileName;
            ExtensionTextBox.Text = _currentFile.Extension.TrimStart('.');
            FileSizeLabel.Text = FileManagerService.FormatFileSize(_currentFile.FileSize);
            SaveTimeLabel.Text = _currentFile.SaveTime.ToString("yyyy-MM-dd HH:mm:ss");
            FullPathLabel.Text = _currentFile.FullPath;

            BtnOpenFile.IsEnabled = true;
            BtnExportFile.IsEnabled = true;
            BtnDeleteFile.IsEnabled = true;
            BtnRename.IsEnabled = true;
            BtnChangeExtension.IsEnabled = true;
        }

        private void OpenFileInViewer(FileRecordInfo file)
        {
            var fileType = FileManagerService.GetFileTypeByExtension(file.Extension);

            if (fileType == 1 || fileType == 5)
            {
                MessageBox.Show("不支持的文件格式", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigateToViewer?.Invoke(file);
        }

        private void FilterRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                _currentFilter = tag;
                ApplyFilterSortAndView();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterSortAndView();
        }

        private void BtnSortToggle_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = !SortPopup.IsOpen;
        }

        private void SortRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                _currentSortType = tag;
                SortTypeLabel.Text = tag switch
                {
                    "name" => "名称",
                    "time" => "时间",
                    "type" => "类型",
                    "size" => "大小",
                    _ => "名称"
                };
                ApplyFilterSortAndView();
            }
        }

        private void BtnSortOrder_Click(object sender, RoutedEventArgs e)
        {
            _currentSortAsc = !_currentSortAsc;
            SortOrderIcon.Text = _currentSortAsc ? "↑" : "↓";
            ApplyFilterSortAndView();
        }

        private void ViewToggle_Click(object sender, RoutedEventArgs e)
        {
            _isListView = !_isListView;
            ViewToggleIcon.Text = _isListView ? "☰" : "▦";
            UpdateView();
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileListView.SelectedItem is FileRecordInfo file)
            {
                _currentFile = file;
                UpdateRightPanel();
            }
        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FileListView.SelectedItem is FileRecordInfo file)
            {
                OpenFileInViewer(file);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadFiles();
        }

        private void BtnOpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile != null)
                App.FileManager.OpenInExplorer(_currentFile.FullPath);
            else
                App.FileManager.OpenInExplorer("");
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "导入文件",
                Multiselect = true,
                Filter = "所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    try
                    {
                        App.FileManager.ImportFile(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导入文件失败: {ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                LoadFiles();
                AudioService.Instance.ReloadPlaylist();
            }
        }

        private void BtnToggleLeftPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_isLeftPanelVisible)
            {
                LeftColumn.Width = new GridLength(0);
                ToggleLeftIcon.Text = "▶";
            }
            else
            {
                LeftColumn.Width = new GridLength(220);
                ToggleLeftIcon.Text = "◀";
            }
            _isLeftPanelVisible = !_isLeftPanelVisible;
        }

        private void BtnToggleRightPanel_Click(object sender, RoutedEventArgs e)
        {
            if (_isRightPanelVisible)
            {
                RightColumn.Width = new GridLength(0);
                ToggleRightIcon.Text = "◀";
            }
            else
            {
                RightColumn.Width = new GridLength(260);
                ToggleRightIcon.Text = "▶";
            }
            _isRightPanelVisible = !_isRightPanelVisible;
        }

        private void FileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void ExtensionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void BtnRename_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null) return;

            var newName = FileNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("文件名不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                FileNameTextBox.Text = _currentFile.FileName;
                return;
            }

            if (newName == _currentFile.FileName) return;

            try
            {
                App.FileManager.RenameFile(_currentFile.FullPath, newName);
                LoadFiles();
                _currentFile = null;
                UpdateRightPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnChangeExtension_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null) return;

            var newExt = ExtensionTextBox.Text?.Trim().TrimStart('.');
            if (string.IsNullOrEmpty(newExt))
            {
                MessageBox.Show("扩展名不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                ExtensionTextBox.Text = _currentFile.Extension.TrimStart('.');
                return;
            }

            if (newExt == _currentFile.Extension.TrimStart('.')) return;

            try
            {
                App.FileManager.ChangeExtension(_currentFile.FullPath, newExt);
                LoadFiles();
                _currentFile = null;
                UpdateRightPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改扩展名失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null) return;
            OpenFileInViewer(_currentFile);
        }

        private void BtnExportFile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null) return;

            var dialog = new SaveFileDialog
            {
                Title = "导出文件",
                FileName = _currentFile.DisplayName,
                Filter = $"*{_currentFile.Extension}|*{_currentFile.Extension}|所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    App.FileManager.ExportFile(_currentFile.FullPath, dialog.FileName);
                    MessageBox.Show("文件导出成功", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteFile_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null) return;

            var result = MessageBox.Show(
                $"确定要删除文件 \"{_currentFile.DisplayName}\" 吗？\n\n此操作不可撤销。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    App.FileManager.DeleteFile(_currentFile.FullPath);
                    _currentFile = null;
                    LoadFiles();
                    UpdateRightPanel();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class FileSizeFormatConverter : IValueConverter
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

    public class DateTimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                return dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
