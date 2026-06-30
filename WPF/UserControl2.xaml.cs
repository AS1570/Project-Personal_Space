using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl2 : UserControl
    {
        private List<FileRecordInfo> _allFiles = new();
        private string _currentFilter = "all";
        private string _searchText = string.Empty;
        private bool _leftPanelVisible = true;
        private bool _rightPanelVisible = true;

        public UserControl2()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadFiles();
        }

        public bool ToggleLeftPanel()
        {
            _leftPanelVisible = !_leftPanelVisible;
            LeftColumn.Width = _leftPanelVisible
                ? (_rightPanelVisible ? new GridLength(3.5, GridUnitType.Star) : new GridLength(1, GridUnitType.Star))
                : new GridLength(0);
            RightColumn.Width = _leftPanelVisible
                ? new GridLength(6.5, GridUnitType.Star)
                : new GridLength(1, GridUnitType.Star);
            SplitterBar.Visibility = _leftPanelVisible && _rightPanelVisible ? Visibility.Visible : Visibility.Collapsed;
            return _leftPanelVisible;
        }

        private void OnToggleRightPanelClick(object sender, RoutedEventArgs e)
        {
            _rightPanelVisible = !_rightPanelVisible;
            RightPanelBorder.Visibility = _rightPanelVisible ? Visibility.Visible : Visibility.Collapsed;
            SplitterBar.Visibility = _leftPanelVisible && _rightPanelVisible ? Visibility.Visible : Visibility.Collapsed;

            if (_rightPanelVisible)
            {
                LeftColumn.Width = new GridLength(3.5, GridUnitType.Star);
                RightColumn.Width = new GridLength(6.5, GridUnitType.Star);
            }
            else
            {
                LeftColumn.Width = new GridLength(1, GridUnitType.Star);
                RightColumn.Width = new GridLength(0);
            }

            BtnToggleRightPanel.Content = new TextBlock
            {
                Text = _rightPanelVisible ? "▶" : "◀",
                FontSize = 13
            };
        }

        private void LoadFiles()
        {
            _allFiles = App.FileManager.GetAllFiles()
                .Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) is 2 or 3)
                .OrderByDescending(f => f.SaveTime)
                .ToList();
            ApplyFilterAndSearch();
        }

        private void ApplyFilterAndSearch()
        {
            var filtered = _allFiles.AsEnumerable();

            if (_currentFilter == "image")
                filtered = filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 2);
            else if (_currentFilter == "video")
                filtered = filtered.Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 3);

            if (!string.IsNullOrEmpty(_searchText))
            {
                var search = _searchText.ToLowerInvariant();
                filtered = filtered.Where(f => f.FileName.ToLowerInvariant().Contains(search)
                    || f.Extension.ToLowerInvariant().Contains(search));
            }

            var fileList = filtered.ToList();
            ResultCountLabel.Text = $"共 {fileList.Count} 个文件";
            BuildThumbnails(fileList);
        }

        private void BuildThumbnails(List<FileRecordInfo> files)
        {
            ThumbnailPanel.Children.Clear();

            var groups = files
                .GroupBy(f => GetTimeCategory(f.SaveTime))
                .OrderByDescending(g => g.Max(f => f.SaveTime));

            foreach (var group in groups)
            {
                var header = new TextBlock
                {
                    Text = group.Key,
                    Foreground = FindResource("Accent") as SolidColorBrush,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(6, 8, 0, 4),
                    Width = 810
                };
                ThumbnailPanel.Children.Add(header);

                foreach (var file in group)
                {
                    var fileType = FileManagerService.GetFileTypeByExtension(file.Extension);

                    var border = new Border
                    {
                        Width = 120,
                        Height = 120,
                        Margin = new Thickness(3),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Cursor = Cursors.Hand,
                        Tag = file,
                        ClipToBounds = true
                    };
                    border.SetResourceReference(Border.BackgroundProperty, "BgSurface");
                    border.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

                    if (fileType == 2)
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(file.FullPath, UriKind.Absolute);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.DecodePixelWidth = 240;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            var img = new Image
                            {
                                Source = bitmap,
                                Stretch = Stretch.UniformToFill,
                                Width = 120,
                                Height = 120
                            };
                            border.Child = img;
                        }
                        catch
                        {
                            border.Child = CreateThumbnailPlaceholder(file);
                        }
                    }
                    else
                    {
                        border.Child = CreateThumbnailPlaceholder(file);
                    }

                    border.MouseLeftButtonDown += OnThumbnailClick;
                    border.MouseEnter += (s, e) =>
                    {
                        if (s is Border bd)
                            bd.BorderBrush = FindResource("Accent") as SolidColorBrush;
                    };
                    border.MouseLeave += (s, e) =>
                    {
                        if (s is Border bd)
                            bd.BorderBrush = FindResource("BorderColor") as SolidColorBrush;
                    };

                    ThumbnailPanel.Children.Add(border);
                }
            }
        }

        private static string GetTimeCategory(DateTime time)
        {
            var today = DateTime.Today;
            if (time.Date == today) return "今天";
            if (time.Date == today.AddDays(-1)) return "昨天";
            if (time.Date > today.AddDays(-7)) return "本周";
            if (time.Year == today.Year && time.Month == today.Month) return time.ToString("M月d日");
            if (time.Year == today.Year) return time.ToString("M月");
            return time.ToString("yyyy年");
        }

        private Grid CreateThumbnailPlaceholder(FileRecordInfo file)
        {
            var isVideo = FileManagerService.GetFileTypeByExtension(file.Extension) == 3;
            var grid = new Grid
            {
                Width = 120,
                Height = 120
            };

            var iconText = new TextBlock
            {
                Text = isVideo ? "🎬" : "🖼",
                FontSize = 32,
                Foreground = FindResource("TextMuted") as SolidColorBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            grid.Children.Add(iconText);

            var nameLabel = new TextBlock
            {
                Text = file.DisplayName,
                FontSize = 9,
                Foreground = FindResource("TextSecondary") as SolidColorBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(4, 0, 4, 4),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 112
            };
            grid.Children.Add(nameLabel);

            return grid;
        }

        private void OnThumbnailClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is FileRecordInfo file)
            {
                LoadFileInRightPanel(file);
            }
        }

        private void LoadFileInRightPanel(FileRecordInfo file)
        {
            var fileType = FileManagerService.GetFileTypeByExtension(file.Extension);

            if (fileType == 2)
            {
                var viewer = new UserControl2_1();
                viewer.LoadFile(file.FullPath);
                RightPanelHost.Content = viewer;
            }
            else if (fileType == 3)
            {
                var player = new UserControl2_2();
                player.LoadFile(file.FullPath);
                RightPanelHost.Content = player;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            ApplyFilterAndSearch();
        }

        private void OnFilterChanged(object sender, RoutedEventArgs e)
        {
            if (sender == BtnFilterAll && BtnFilterAll.IsChecked == true)
            {
                BtnFilterImage.IsChecked = false;
                BtnFilterVideo.IsChecked = false;
                _currentFilter = "all";
            }
            else if (sender == BtnFilterImage && BtnFilterImage.IsChecked == true)
            {
                BtnFilterAll.IsChecked = false;
                BtnFilterVideo.IsChecked = false;
                _currentFilter = "image";
            }
            else if (sender == BtnFilterVideo && BtnFilterVideo.IsChecked == true)
            {
                BtnFilterAll.IsChecked = false;
                BtnFilterImage.IsChecked = false;
                _currentFilter = "video";
            }
            else
            {
                if (!BtnFilterAll.IsChecked.GetValueOrDefault()
                    && !BtnFilterImage.IsChecked.GetValueOrDefault()
                    && !BtnFilterVideo.IsChecked.GetValueOrDefault())
                {
                    BtnFilterAll.IsChecked = true;
                    _currentFilter = "all";
                }
            }

            ApplyFilterAndSearch();
        }
    }
}
