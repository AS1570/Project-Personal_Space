using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfApp3.Models;
using WpfApp3.Services;
using WpfApp3.Widgets;

namespace WpfApp3
{
    public partial class UserControl1 : UserControl
    {
        public event Action? NavigateToLock;

        private readonly DispatcherTimer _clockTimer;
        private DispatcherTimer? _autoHideTimer;
        private bool _subscribedToAudio;
        private bool _isDraggingPopupProgress;
        private bool _isScrollingText;
        private string? _lastScrollingSongName;
        private bool _isCenterRightLayout;
        private StackPanel? _centerButtonsPanel;
        private readonly List<WidgetBase> _activeWidgets = new();
        private int _nextGridCol;
        private int _nextGridRow;
        private bool _showGridLines;
        private bool _showCenterLines = true;
        private Color _gridLineColor = Color.FromRgb(0xFF, 0x44, 0x44);
        private string _gridLineStyle = "Dashed";

        public UserControl1()
        {
            InitializeComponent();

            BottomBarFullBg.SetResourceReference(Border.BackgroundProperty, "DesktopBg");

            Button1.Click += (s, e) => SetDesktopContent(null);

            var lockMenuItem = new MenuItem { Header = "锁定" };
            lockMenuItem.Click += (s, e) =>
            {
                App.Database.Disconnect();
                NavigateToLock?.Invoke();
            };

            Button1.ContextMenu = new ContextMenu();
            Button1.ContextMenu.Items.Add(lockMenuItem);

            Button2.Click += (s, e) => SetDesktopContent(new UserControl2());
            Button3.Click += (s, e) => SetDesktopContent(new UserControl3());
            Button4.Click += (s, e) => SetDesktopContent(new UserControl4());
            Button5.Click += (s, e) => SetDesktopContent(new UserControl5());
            Button6.Click += (s, e) => SetDesktopContent(new UserControl6());

            Button7.Click += (s, e) =>
            {
                var uc7 = new UserControl7();
                uc7.NavigateToViewer += OnNavigateToViewer;
                SetDesktopContent(uc7);
            };

            SetupMiniPlayer();

            SetupButton9();

            Button10.Click += (s, e) => SetDesktopContent(null);

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Alt)
                {
                    NavigateToLock?.Invoke();
                }
            };

            AddHandler(PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnGlobalPreviewMouseDown), true);

            Loaded += (s, e) =>
            {
                Focus();
                SubscribeToAudio();
                ApplyDesktopWallpaper();
                ApplyDesktopBarModes();
            };

            Unloaded += (s, e) =>
            {
                StopAutoHideDetection();
                UnsubscribeFromAudio();
            };

            SetupWidgetSystem();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();

            ProgramSettingsService.Instance.OnSettingsChanged += ApplyProgramSettings;
            ApplyProgramSettings();
            ProgramSettingsService.Instance.OnSettingsChanged += ApplyBottomBarVisibilityMode;
            ApplyBottomBarVisibilityMode();
            ProgramSettingsService.Instance.OnSettingsChanged += ApplyDesktopBarModes;

            this.SizeChanged += (s, e) =>
            {
                UpdateCenterButtonPosition();
                RepositionAllWidgets();
            };

            UpdateClock();
        }

        public void ApplyBottomBarVisibilityMode()
        {
            var s = ProgramSettingsService.Instance.Current;

            StopAutoHideDetection();

            var bottomBarStyle = s.BottomBarStyle;

            switch (s.BottomBarVisibility)
            {
                case "Floating":
                    BottomBarWrap.Opacity = 0.95;
                    BottomBarWrap.IsHitTestVisible = true;
                    BottomBarFullBg.Visibility = Visibility.Collapsed;
                    break;
                case "AutoHide":
                    BottomBarWrap.Height = 4;
                    BottomBarFullBg.Height = 4;
                    BottomBarWrap.Opacity = 0;
                    BottomBarWrap.IsHitTestVisible = true;
                    StartAutoHideDetection();
                    break;
                default:
                    BottomBarWrap.Opacity = 1;
                    BottomBarWrap.IsHitTestVisible = true;
                    BottomBarFullBg.Visibility = Visibility.Visible;
                    break;
            }

            ApplyProgramSettings();
        }

        private void StartAutoHideDetection()
        {
            if (_autoHideTimer != null) return;
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _autoHideTimer.Tick += OnAutoHideCheck;
            _autoHideTimer.Start();
        }

        private void StopAutoHideDetection()
        {
            if (_autoHideTimer == null) return;
            _autoHideTimer.Stop();
            _autoHideTimer.Tick -= OnAutoHideCheck;
            _autoHideTimer = null;
        }

        private void OnAutoHideCheck(object? sender, EventArgs e)
        {
            var mousePos = Mouse.GetPosition(this);
            double threshold = 50;

            if (mousePos.Y >= ActualHeight - threshold && mousePos.Y <= ActualHeight)
            {
                if (BottomBarWrap.Opacity < 1)
                {
                    var s = ProgramSettingsService.Instance.Current;
                    bool isConnected = s.BottomBarStyle == "Connected";
                    double h = isConnected ? 43 : 70;
                    BottomBarWrap.Height = h;
                    BottomBarFullBg.Height = h;
                    BottomBarWrap.Opacity = 1;
                }
            }
            else
            {
                if (BottomBarWrap.Opacity > 0)
                {
                    BottomBarWrap.Height = 4;
                    BottomBarFullBg.Height = 4;
                    BottomBarWrap.Opacity = 0;
                }
            }
        }

        private void ApplyProgramSettings()
        {
            var s = ProgramSettingsService.Instance.Current;

            string style = s.BottomBarStyle;
            string layout = s.BottomBarLayout;
            ApplyBottomBarMode(style, layout);

            bool isConnected = style == "Connected";
            bool isLight = WpfApp3.Services.ThemeService.Instance.GetEffectiveVariant() == "Light";
            bool useDarkText = isConnected && isLight;

            var barForeground = useDarkText
                ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
                : (SolidColorBrush)FindResource("White");

            foreach (var btn in new[] { Button1, Button2, Button3, Button4, Button5, Button6, Button7, Button8, Button9, Button10 })
            {
                btn.Foreground = barForeground;
            }
            Btn8Text.Foreground = barForeground;
            Btn9Text.Foreground = barForeground;

            bool show8 = s.Button8Show;
            bool show9 = s.Button9Show;
            bool show10 = s.Button10Show;

            Button8.Visibility = show8 ? Visibility.Visible : Visibility.Collapsed;
            Separator1.Visibility = show8 ? Visibility.Visible : Visibility.Collapsed;

            Button9.Visibility = show9 ? Visibility.Visible : Visibility.Collapsed;
            Separator2.Visibility = show9 ? Visibility.Visible : Visibility.Collapsed;

            Button10.Visibility = show10 ? Visibility.Visible : Visibility.Collapsed;

            UpdateButton8Display();
            UpdateClock();
        }

        private void ApplyBottomBarMode(string style, string layout)
        {
            bool isConnected = style == "Connected";
            bool isFull = layout == "Full";
            bool isCenterRight = layout == "CenterRight";
            string bottomVisMode = ProgramSettingsService.Instance.Current.BottomBarVisibility;

            if (isConnected)
            {
                FrostedGlass.Visibility = Visibility.Collapsed;
                ConnectedTopLine.Visibility = Visibility.Visible;
                BackgroundBlocker.Visibility = Visibility.Visible;
                BackgroundBlocker.Opacity = bottomVisMode == "Floating" ? 0.9 : 1.0;

                BottomBarWrap.Height = 43;
                TaskBar.Margin = new Thickness(0, 1, 0, 0);
                TaskBar.VerticalAlignment = VerticalAlignment.Bottom;

                BottomBarWrap.HorizontalAlignment = HorizontalAlignment.Stretch;

                if (isFull)
                {
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ReorderButtonsFull();
                }
                else if (isCenterRight)
                {
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ReorderButtonsCenterRight();
                }
                else
                {
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Center;
                    ReorderButtonsCenter();
                }
            }
            else
            {
                FrostedGlass.Visibility = Visibility.Visible;
                ConnectedTopLine.Visibility = Visibility.Collapsed;
                BackgroundBlocker.Visibility = Visibility.Collapsed;
                BottomBarWrap.Height = 70;
                TaskBar.Margin = new Thickness(18, 8, 18, 8);
                TaskBar.VerticalAlignment = VerticalAlignment.Center;

                if (isFull)
                {
                    BottomBarWrap.HorizontalAlignment = HorizontalAlignment.Stretch;
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ReorderButtonsFull();
                }
                else if (isCenterRight)
                {
                    BottomBarWrap.HorizontalAlignment = HorizontalAlignment.Stretch;
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Stretch;
                    ReorderButtonsCenterRight();
                }
                else
                {
                    BottomBarWrap.HorizontalAlignment = HorizontalAlignment.Center;
                    TaskBar.HorizontalAlignment = HorizontalAlignment.Center;
                    ReorderButtonsCenter();
                }
            }

            if (bottomVisMode == "Docked")
            {
                DesktopContent.Margin = new Thickness(0, 0, 0, BottomBarWrap.Height);
            }
            else
            {
                DesktopContent.Margin = new Thickness(0);
            }

            BottomBarFullBg.Height = BottomBarWrap.Height;
        }

        private static void DetachFromParent(FrameworkElement element)
        {
            var parent = element.Parent as Panel;
            if (parent != null)
            {
                parent.Children.Remove(element);
            }
        }

        private void DetachAllFromSubPanels()
        {
            DetachFromParent(Button1);
            DetachFromParent(Button6);
            DetachFromParent(Button7);
            DetachFromParent(Button4);
            DetachFromParent(Button2);
            DetachFromParent(Button3);
            DetachFromParent(Button4);
            DetachFromParent(Button5);
            DetachFromParent(Separator1);
            DetachFromParent(Button8);
            DetachFromParent(Separator2);
            DetachFromParent(Button9);
            DetachFromParent(Button10);
        }

        private void ReorderButtonsCenter()
        {
            _isCenterRightLayout = false;
            _centerButtonsPanel = null;
            DetachAllFromSubPanels();
            TaskBar.Children.Clear();
            TaskBar.LastChildFill = false;
            TaskBar.Children.Add(Button1);
            TaskBar.Children.Add(Button6);
            TaskBar.Children.Add(Button7);
            TaskBar.Children.Add(Button4);
            TaskBar.Children.Add(Button2);
            TaskBar.Children.Add(Button3);
            TaskBar.Children.Add(Button5);
            TaskBar.Children.Add(Separator1);
            TaskBar.Children.Add(Button8);
            TaskBar.Children.Add(Separator2);
            TaskBar.Children.Add(Button9);
            TaskBar.Children.Add(Button10);
        }

        private void ReorderButtonsFull()
        {
            _isCenterRightLayout = false;
            _centerButtonsPanel = null;
            DetachAllFromSubPanels();
            TaskBar.Children.Clear();
            TaskBar.LastChildFill = false;

            Button1.Margin = new Thickness(0, 0, 8, 0);
            Button6.Margin = new Thickness(0, 0, 8, 0);
            Button7.Margin = new Thickness(0, 0, 8, 0);
            Button4.Margin = new Thickness(0, 0, 8, 0);
            Button2.Margin = new Thickness(0, 0, 8, 0);
            Button3.Margin = new Thickness(0, 0, 8, 0);
            Button5.Margin = new Thickness(0, 0, 8, 0);

            TaskBar.Children.Add(Button1);
            TaskBar.Children.Add(Button6);
            TaskBar.Children.Add(Button7);
            TaskBar.Children.Add(Button4);
            TaskBar.Children.Add(Button2);
            TaskBar.Children.Add(Button3);
            TaskBar.Children.Add(Button5);

            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal };
            rightPanel.Children.Add(Separator1);
            rightPanel.Children.Add(Button8);
            rightPanel.Children.Add(Separator2);
            rightPanel.Children.Add(Button9);
            rightPanel.Children.Add(Button10);
            DockPanel.SetDock(rightPanel, Dock.Right);
            TaskBar.Children.Add(rightPanel);
        }

        private void ReorderButtonsCenterRight()
        {
            DetachAllFromSubPanels();
            TaskBar.Children.Clear();
            TaskBar.LastChildFill = true;
            _isCenterRightLayout = true;

            Button1.Margin = new Thickness(0, 0, 8, 0);
            Button6.Margin = new Thickness(0, 0, 8, 0);
            Button7.Margin = new Thickness(0, 0, 8, 0);
            Button4.Margin = new Thickness(0, 0, 8, 0);
            Button2.Margin = new Thickness(0, 0, 8, 0);
            Button3.Margin = new Thickness(0, 0, 8, 0);
            Button5.Margin = new Thickness(0, 0, 8, 0);

            var contentGrid = new Grid();

            var centerButtons = new StackPanel { Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left };
            centerButtons.Children.Add(Button1);
            centerButtons.Children.Add(Button6);
            centerButtons.Children.Add(Button7);
            centerButtons.Children.Add(Button4);
            centerButtons.Children.Add(Button2);
            centerButtons.Children.Add(Button3);
            centerButtons.Children.Add(Button5);
            _centerButtonsPanel = centerButtons;
            contentGrid.Children.Add(centerButtons);

            var rightPanel = new StackPanel { Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right };
            rightPanel.Children.Add(Separator1);
            rightPanel.Children.Add(Button8);
            rightPanel.Children.Add(Separator2);
            rightPanel.Children.Add(Button9);
            rightPanel.Children.Add(Button10);
            contentGrid.Children.Add(rightPanel);

            TaskBar.Children.Add(contentGrid);

            UpdateCenterButtonPosition();
        }

        private void UpdateCenterButtonPosition()
        {
            if (!_isCenterRightLayout || _centerButtonsPanel == null) return;

            double windowWidth = this.ActualWidth;
            if (windowWidth <= 0) return;

            double percentage = CalculateCenterPercentage(windowWidth);
            double centerX = percentage * windowWidth;

            _centerButtonsPanel.UpdateLayout();
            double buttonsWidth = _centerButtonsPanel.ActualWidth;
            if (double.IsNaN(buttonsWidth) || buttonsWidth <= 0)
                buttonsWidth = 350;

            double leftMargin = centerX - buttonsWidth / 2;
            _centerButtonsPanel.Margin = new Thickness(Math.Max(0, leftMargin), 0, 0, 0);
        }

        private static double CalculateCenterPercentage(double width)
        {
            if (width >= 1000) return 0.50;
            if (width <= 800) return 0.35;
            double t = (width - 800) / 200;
            return 0.35 + t * 0.15;
        }

        private void SetupMiniPlayer()
        {
            Button8.Click += (s, e) =>
            {
                MiniPlayerPopup.IsOpen = !MiniPlayerPopup.IsOpen;
                if (MiniPlayerPopup.IsOpen)
                    UpdatePopupUI();
            };

            MiniPlayerPopup.Opened += (s, e) => UpdatePopupUI();

            PopupBtnPlayPause.Click += (s, e) =>
            {
                var audio = AudioService.Instance;
                if (audio.CurrentTrack == null && audio.Playlist.Count > 0)
                {
                    audio.PlayTrack(0);
                }
                else
                {
                    audio.TogglePlayPause();
                }
            };
            PopupBtnPrev.Click += (s, e) => AudioService.Instance.Previous();
            PopupBtnNext.Click += (s, e) => AudioService.Instance.Next();

            PopupBtnClose.Click += (s, e) =>
            {
                AudioService.Instance.ClearTrack();
                MiniPlayerPopup.IsOpen = false;
            };

            PopupBtnQuickSelect.Click += (s, e) =>
            {
                var settings = ProgramSettingsService.Instance.Current;
                string path = settings.AudioQuickSelectPath ?? "";
                string mode = settings.AudioQuickSelectMode ?? "Single";

                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                {
                    MessageBox.Show("尚未设置快速选择音频文件，请在设置中配置。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var audio = AudioService.Instance;
                int loopMode = mode == "Single" ? 1 : 0;
                double volume = settings.AudioQuickSelectVolume;

                var trackIndex = audio.FindTrackIndex(path);
                if (trackIndex < 0)
                {
                    return;
                }

                audio.Volume = volume;
                audio.LoopMode = loopMode;
                audio.PlayTrack(trackIndex);

                if (MiniPlayerPopup.IsOpen)
                    UpdatePopupUI();
            };

            PopupBtnLoop.Click += (s, e) =>
            {
                var audio = AudioService.Instance;
                audio.LoopMode = audio.LoopMode == 0 ? 1 : 0;
            };

            PopupProgressSlider.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _isDraggingPopupProgress = true;
            };
            PopupProgressSlider.PreviewMouseLeftButtonUp += (s, e) =>
            {
                _isDraggingPopupProgress = false;
                if (AudioService.Instance.CurrentIndex >= 0)
                    AudioService.Instance.Seek(TimeSpan.FromSeconds(PopupProgressSlider.Value));
            };
            PopupProgressSlider.ValueChanged += (s, e) =>
            {
                if (!_isDraggingPopupProgress) return;
                PopupCurrentTime.Text = FormatTime(TimeSpan.FromSeconds(PopupProgressSlider.Value));
            };

            PopupBtnVolume.Click += (s, e) =>
            {
                VolumeNestedPopup.IsOpen = !VolumeNestedPopup.IsOpen;
                if (VolumeNestedPopup.IsOpen)
                {
                    var vol = AudioService.Instance.Volume;
                    PopupVolumeNestedSlider.Value = vol;
                    VolumeNestedLabel.Text = $"{(int)(vol * 100)}%";
                }
            };

            PopupVolumeNestedSlider.ValueChanged += (s, e) =>
            {
                var vol = PopupVolumeNestedSlider.Value;
                AudioService.Instance.Volume = vol;
                VolumeNestedLabel.Text = $"{(int)(vol * 100)}%";
                UpdatePopupVolumeIcon(vol);
            };
        }

        private void OnGlobalPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement == null) return;

            if (MiniPlayerPopup.IsOpen)
            {
                if (IsDescendantOf(clickedElement, MiniPlayerPopup.Child) ||
                    IsDescendantOf(clickedElement, Button8))
                {
                }
                else if (VolumeNestedPopup.IsOpen &&
                         (IsDescendantOf(clickedElement, VolumeNestedPopup.Child) ||
                          IsDescendantOf(clickedElement, PopupBtnVolume)))
                {
                }
                else
                {
                    MiniPlayerPopup.IsOpen = false;
                    VolumeNestedPopup.IsOpen = false;
                }
            }
        }

        private static bool IsDescendantOf(DependencyObject element, DependencyObject? parent)
        {
            if (parent == null) return false;
            var current = element;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current) ??
                          LogicalTreeHelper.GetParent(current);
            }
            return false;
        }

        private void SubscribeToAudio()
        {
            if (_subscribedToAudio) return;
            AudioService.Instance.StateChanged += OnAudioStateChanged;
            _subscribedToAudio = true;
        }

        private void UnsubscribeFromAudio()
        {
            if (!_subscribedToAudio) return;
            AudioService.Instance.StateChanged -= OnAudioStateChanged;
            _subscribedToAudio = false;
        }

        private void OnAudioStateChanged()
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateButton8Display();
                UpdatePopupVolumeIcon(AudioService.Instance.Volume);
                if (MiniPlayerPopup.IsOpen)
                    UpdatePopupUI();
                if (VolumeNestedPopup.IsOpen)
                {
                    var vol = AudioService.Instance.Volume;
                    PopupVolumeNestedSlider.Value = vol;
                    VolumeNestedLabel.Text = $"{(int)(vol * 100)}%";
                }
            });
        }

        private void UpdateButton8Display()
        {
            var s = ProgramSettingsService.Instance.Current;
            var audio = AudioService.Instance;
            var track = audio.CurrentTrack;

            if (track != null)
            {
                var icon = audio.IsPlaying ? "⏸" : "▶";
                if (s.Button8ShowSongName)
                {
                    var name = track.FileName;
                    if (name.Length > 10)
                        name = name[..10] + "…";
                    Btn8Text.Text = $"{icon} {name}";
                }
                else
                {
                    Btn8Text.Text = icon;
                }
                Button8.ToolTip = track.DisplayName;
            }
            else
            {
                Btn8Text.Text = "音频控制";
                Button8.ToolTip = "点击展开播放控制面板";
            }
        }

        private void UpdatePopupUI()
        {
            var audio = AudioService.Instance;
            var track = audio.CurrentTrack;

            if (track != null)
            {
                var displayName = track.DisplayName;

                if (_lastScrollingSongName != displayName)
                {
                    _lastScrollingSongName = displayName;
                    PopupSongName.Text = displayName;
                    StartScrollIfNeeded(displayName);
                }

                PopupBtnPlayPause.Content = audio.IsPlaying ? "⏸" : "▶";

                if (audio.NaturalDuration.HasTimeSpan)
                {
                    var duration = audio.NaturalDuration.TimeSpan;
                    PopupProgressSlider.Maximum = duration.TotalSeconds;
                    PopupTotalTime.Text = FormatTime(duration);
                    if (!_isDraggingPopupProgress)
                    {
                        PopupProgressSlider.Value = audio.Position.TotalSeconds;
                        PopupCurrentTime.Text = FormatTime(audio.Position);
                    }
                }
                else
                {
                    PopupProgressSlider.Maximum = 100;
                    PopupTotalTime.Text = "00:00";
                    PopupProgressSlider.Value = 0;
                    PopupCurrentTime.Text = "00:00";
                }
            }
            else
            {
                _lastScrollingSongName = null;
                PopupSongName.Text = "无音频播放";
                StopScroll();
                PopupBtnPlayPause.Content = "▶";
                PopupProgressSlider.Maximum = 100;
                PopupProgressSlider.Value = 0;
                PopupCurrentTime.Text = "00:00";
                PopupTotalTime.Text = "00:00";
            }

            PopupBtnLoop.Content = audio.LoopMode == 0 ? "🔁" : "🔂";
            PopupBtnLoop.ToolTip = audio.LoopMode == 0 ? "顺序循环" : "单曲循环";
        }

        private void UpdatePopupVolumeIcon(double vol)
        {
            if (vol < 0.01)
                PopupVolumeIcon.Text = "🔇";
            else if (vol < 0.35)
                PopupVolumeIcon.Text = "🔈";
            else
                PopupVolumeIcon.Text = "🔊";
        }

        private void StartScrollIfNeeded(string text)
        {
            StopScroll();

            var textBlock = PopupSongName;
            textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var textWidth = textBlock.DesiredSize.Width;
            var containerWidth = 260;

            if (textWidth <= containerWidth) return;

            textBlock.Width = textWidth;
            Canvas.SetLeft(textBlock, containerWidth);

            var animation = new DoubleAnimation
            {
                From = containerWidth,
                To = -textWidth,
                Duration = TimeSpan.FromSeconds(Math.Max(5, textWidth / 40)),
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(1)
            };

            _isScrollingText = true;
            textBlock.BeginAnimation(Canvas.LeftProperty, animation);
        }

        private void StopScroll()
        {
            if (!_isScrollingText) return;
            _isScrollingText = false;
            PopupSongName.BeginAnimation(Canvas.LeftProperty, null);
            PopupSongName.Width = double.NaN;
            Canvas.SetLeft(PopupSongName, 0);
        }

        private void OnNavigateToViewer(FileRecordInfo file)
        {
            int fileType = FileManagerService.GetFileTypeByExtension(file.Extension);
            UserControl viewer = fileType switch
            {
                2 => new UserControl2_1(),
                3 => new UserControl2_2(),
                0 => new UserControl4(),
                4 => CreateMusicViewer(file),
                _ => null!
            };

            if (viewer != null)
            {
                if (viewer is UserControl2_1 imgViewer)
                    imgViewer.LoadFile(file.FullPath);
                else if (viewer is UserControl2_2 vidViewer)
                    vidViewer.LoadFile(file.FullPath);
                else if (viewer is UserControl4 txtViewer)
                    txtViewer.OpenFile(file.FullPath);
                else if (viewer is UserControl3 musicViewer)
                    musicViewer.LoadAndPlay(file.FullPath);

                SetDesktopContent(viewer);
            }
            else
            {
                MessageBox.Show("不支持的文件格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private UserControl CreateMusicViewer(FileRecordInfo file)
        {
            var uc3 = new UserControl3();
            return uc3;
        }

        private void OnClockTick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            var s = ProgramSettingsService.Instance.Current;

            string text;

            bool showTime = s.Button9ShowTime;
            bool showDate = s.Button9ShowDate;

            if (showTime && showDate)
            {
                string timeStr = FormatButton9Time(now, s);
                string dateStr = MainWindow.FormatDate(now, s.Button9DateOrder, s.Button9DateStyle, s.Button9MonthStyle, s.Button9YearStyle);
                text = timeStr + "\n" + dateStr;
            }
            else if (showTime)
            {
                text = FormatButton9Time(now, s);
            }
            else if (showDate)
            {
                text = MainWindow.FormatDate(now, s.Button9DateOrder, s.Button9DateStyle, s.Button9MonthStyle, s.Button9YearStyle);
            }
            else
            {
                text = "";
            }

            Btn9Text.Text = text;

            switch (s.Button9Alignment)
            {
                case "Left":
                    Button9.HorizontalContentAlignment = HorizontalAlignment.Left;
                    Btn9Text.TextAlignment = TextAlignment.Left;
                    break;
                case "Right":
                    Button9.HorizontalContentAlignment = HorizontalAlignment.Right;
                    Btn9Text.TextAlignment = TextAlignment.Right;
                    break;
                default:
                    Button9.HorizontalContentAlignment = HorizontalAlignment.Center;
                    Btn9Text.TextAlignment = TextAlignment.Center;
                    break;
            }
        }

        private static string FormatButton9Time(DateTime now, ProgramSettings s)
        {
            bool is24Hour = s.Button9Time24Hour;
            bool showSeconds = s.Button9TimeShowSeconds;
            string format = is24Hour ? "HH" : "hh";
            format += showSeconds ? ":mm:ss" : ":mm";
            if (!is24Hour)
                format += " tt";
            return now.ToString(format);
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return string.Format("{0:0}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
            return string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
        }

        private void SetupWidgetSystem()
        {
            WidgetCanvas.MouseRightButtonDown += OnWidgetCanvasRightClick;
            DesktopGrid.MouseRightButtonDown += OnDesktopGridRightClick;

            WidgetGalleryControl.WidgetSelected += OnWidgetSelected;
            WidgetGalleryControl.Cancelled += () => WidgetGalleryPopup.IsOpen = false;

            Loaded += (s, e) =>
            {
                RepositionAllWidgets();
            };

            LoadWidgets();
        }

        private void OnWidgetCanvasRightClick(object sender, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();

            var addItem = new MenuItem { Header = "添加小组件" };
            addItem.Click += (s, args) =>
            {
                WidgetGalleryControl.Width = 420;
                WidgetGalleryPopup.PlacementTarget = this;
                WidgetGalleryPopup.Placement = PlacementMode.Center;
                WidgetGalleryPopup.IsOpen = true;
            };
            menu.Items.Add(addItem);

            menu.Items.Add(new Separator());

            var manageItem = new MenuItem
            {
                Header = _activeWidgets.Count > 0
                    ? $"管理小组件（共 {_activeWidgets.Count} 个）"
                    : "管理小组件"
            };
            manageItem.Click += (s, args) =>
            {
                WidgetManagePopup.Child = BuildWidgetManagePanel();
                WidgetManagePopup.IsOpen = true;
            };
            menu.Items.Add(manageItem);

            menu.Items.Add(new Separator());

            var saveItem = new MenuItem { Header = "保存小组件状态" };
            saveItem.Click += (s, args) =>
            {
                SaveAllWidgets();
            };
            menu.Items.Add(saveItem);

            var clearItem = new MenuItem { Header = "清除所有小组件" };
            clearItem.Click += (s, args) => ClearAllWidgets();
            menu.Items.Add(clearItem);

            menu.Items.Add(new Separator());

            var gridLinesItem = new MenuItem
            {
                Header = "📐 显示网格辅助线",
                IsCheckable = true,
                IsChecked = _showGridLines
            };
            gridLinesItem.Click += (s, args) => ToggleGridLines();
            menu.Items.Add(gridLinesItem);

            menu.Items.Add(BuildGridLineStyleMenu());
            menu.Items.Add(BuildGridLineColorMenu());

            var centerLinesItem = new MenuItem
            {
                Header = "📏 显示中心辅助线",
                IsCheckable = true,
                IsChecked = _showCenterLines
            };
            centerLinesItem.Click += (s, args) => ToggleCenterLines();
            menu.Items.Add(centerLinesItem);

            menu.IsOpen = true;
            e.Handled = true;
        }

        private void OnDesktopGridRightClick(object sender, MouseButtonEventArgs e)
        {
            if (DesktopContent.Content != null) return;

            var menu = new ContextMenu();

            var refreshItem = new MenuItem { Header = "🔄 刷新桌面" };
            refreshItem.Click += (s, args) =>
            {
                ApplyDesktopWallpaper();
                ApplyBottomBarVisibilityMode();
                ApplyProgramSettings();
            };
            menu.Items.Add(refreshItem);

            menu.Items.Add(new Separator());

            var addItem = new MenuItem { Header = "添加小组件" };
            addItem.Click += (s, args) =>
            {
                WidgetGalleryControl.Width = 420;
                WidgetGalleryPopup.PlacementTarget = this;
                WidgetGalleryPopup.Placement = PlacementMode.Center;
                WidgetGalleryPopup.IsOpen = true;
            };
            menu.Items.Add(addItem);

            menu.Items.Add(new Separator());

            var gridLinesItem = new MenuItem
            {
                Header = "📐 显示网格辅助线",
                IsCheckable = true,
                IsChecked = _showGridLines
            };
            gridLinesItem.Click += (s, args) => ToggleGridLines();
            menu.Items.Add(gridLinesItem);

            menu.Items.Add(BuildGridLineStyleMenu());
            menu.Items.Add(BuildGridLineColorMenu());

            var centerLinesItem = new MenuItem
            {
                Header = "📏 显示中心辅助线",
                IsCheckable = true,
                IsChecked = _showCenterLines
            };
            centerLinesItem.Click += (s, args) => ToggleCenterLines();
            menu.Items.Add(centerLinesItem);

            var settingsItem = new MenuItem { Header = "⚙ 个性化设置" };
            settingsItem.Click += (s, args) =>
            {
                SetDesktopContent(new UserControl6());
            };
            menu.Items.Add(settingsItem);

            menu.IsOpen = true;
            e.Handled = true;
        }

        private Border BuildWidgetManagePanel()
        {
            bool isLight = WpfApp3.Services.ThemeService.Instance.GetEffectiveVariant() == "Light";

            var panelBg = isLight
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x36));
            var panelBorder = isLight
                ? new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD8))
                : new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x55));
            var textPrimary = isLight
                ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E))
                : new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0));
            var textMuted = isLight
                ? new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x90))
                : new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x80));
            var textSecondary = isLight
                ? new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x70))
                : new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0));
            var rowEvenBg = isLight
                ? new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF5))
                : new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x44));
            var closeBtnBg = isLight
                ? new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD8))
                : new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x5A));

            var panel = new Border
            {
                Background = panelBg,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 16, 20, 16),
                MinWidth = 360,
                MaxHeight = 500,
                BorderBrush = panelBorder,
                BorderThickness = new Thickness(1)
            };
            panel.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 20,
                ShadowDepth = 4,
                Opacity = 0.5
            };

            var stack = new StackPanel();

            var title = new TextBlock
            {
                Text = $"小组件管理（共 {_activeWidgets.Count} 个）",
                Foreground = textPrimary,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(title);

            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 380,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var listPanel = new StackPanel();

            for (int i = _activeWidgets.Count - 1; i >= 0; i--)
            {
                var widget = _activeWidgets[i];
                int zOrder = i;
                string typeName = widget.Data.Type switch
                {
                    "Clock" => "时钟",
                    "Date" => "日期",
                    "StickyNote" => "便签",
                    "QuickLaunch" => "快捷启动",
                    "Image" => "图片展示",
                    "Audio" => "音频控制器",
                    _ => widget.Data.Type
                };

                var row = new Border
                {
                    Background = zOrder % 2 == 0
                        ? rowEvenBg
                        : Brushes.Transparent,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 8, 8),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var zText = new TextBlock
                {
                    Text = $"#{_activeWidgets.Count - zOrder}",
                    Foreground = textMuted,
                    FontSize = 11,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(zText, 0);

                var nameText = new TextBlock
                {
                    Text = typeName,
                    Foreground = textPrimary,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(nameText, 1);

                var capturedWidget = widget;

                var upBtn = new Button
                {
                    Content = "↑",
                    Width = 26,
                    Height = 26,
                    Background = Brushes.Transparent,
                    Foreground = textSecondary,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    FontSize = 12,
                    ToolTip = "上移一层",
                    IsEnabled = zOrder < _activeWidgets.Count - 1
                };
                upBtn.Template = CreateMiniBtnTemplate(isLight);
                upBtn.Click += (s, args) =>
                {
                    int idx = WidgetCanvas.Children.IndexOf(capturedWidget);
                    if (idx >= 0 && idx < WidgetCanvas.Children.Count - 1)
                    {
                        WidgetCanvas.Children.RemoveAt(idx);
                        WidgetCanvas.Children.Insert(idx + 1, capturedWidget);
                        var item = _activeWidgets[idx];
                        _activeWidgets.RemoveAt(idx);
                        _activeWidgets.Insert(idx + 1, item);
                        WidgetManagePopup.Child = BuildWidgetManagePanel();
                    }
                };
                Grid.SetColumn(upBtn, 2);

                var delBtn = new Button
                {
                    Content = "✕",
                    Width = 26,
                    Height = 26,
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    FontSize = 12,
                    ToolTip = "删除",
                    Margin = new Thickness(4, 0, 0, 0)
                };
                delBtn.Template = CreateMiniBtnTemplate(isLight);
                delBtn.Click += (s, args) =>
                {
                    WidgetCanvas.Children.Remove(capturedWidget);
                    _activeWidgets.Remove(capturedWidget);
                    if (capturedWidget is WidgetClock cw) cw.Cleanup();
                    if (capturedWidget is WidgetDate dw) dw.Cleanup();
                    if (capturedWidget is WidgetAudio aw) aw.Cleanup();
                    SaveAllWidgets();
                    if (_activeWidgets.Count == 0)
                    {
                        WidgetManagePopup.IsOpen = false;
                    }
                    else
                    {
                        WidgetManagePopup.Child = BuildWidgetManagePanel();
                    }
                };
                Grid.SetColumn(delBtn, 3);

                rowGrid.Children.Add(zText);
                rowGrid.Children.Add(nameText);
                rowGrid.Children.Add(upBtn);
                rowGrid.Children.Add(delBtn);
                row.Child = rowGrid;
                listPanel.Children.Add(row);
            }

            scrollViewer.Content = listPanel;
            stack.Children.Add(scrollViewer);

            var closeBtn = new Button
            {
                Content = "关闭",
                Width = 80,
                Height = 32,
                Background = closeBtnBg,
                Foreground = textPrimary,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeBtn.Click += (s, args) => WidgetManagePopup.IsOpen = false;
            stack.Children.Add(closeBtn);

            panel.Child = stack;
            return panel;
        }

        private static ControlTemplate CreateMiniBtnTemplate(bool isLight = false)
        {
            var t = new ControlTemplate(typeof(Button));
            var b = new FrameworkElementFactory(typeof(Border));
            b.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            b.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            b.Name = "bd";
            var p = new FrameworkElementFactory(typeof(ContentPresenter));
            p.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            p.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            b.AppendChild(p);
            t.VisualTree = b;
            var tr = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            var hoverColor = isLight
                ? Color.FromRgb(0xD0, 0xD0, 0xD8)
                : Color.FromRgb(0x35, 0x35, 0x5A);
            tr.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(hoverColor), "bd"));
            t.Triggers.Add(tr);
            return t;
        }

        private void OnWidgetSelected(string widgetType)
        {
            WidgetGalleryPopup.IsOpen = false;

            var widget = CreateWidget(widgetType);
            if (widget == null) return;

            double cw = DesktopGrid.ActualWidth;
            double ch = DesktopGrid.ActualHeight;
            var (cellW, cellH) = WidgetBase.GetGridCellSizes(cw, ch);

            FindNextEmptyGridCell(cellW, cellH, out int col, out int row);

            var widgetData = new WidgetData
            {
                Type = widgetType,
                GridColumn = col,
                GridRow = row,
                GridColSpan = 2,
                GridRowSpan = 2
            };

            widget.Data = widgetData;
            widget.OnClose += OnWidgetClose;
            widget.OnDragEnd += OnWidgetDragEnd;
            widget.OnResizeEnd += OnWidgetResizeEnd;

            widget.ApplyGridPosition(col, row, 2, 2, cellW, cellH);

            widget.LoadAppearance(widgetData);

            WidgetCanvas.Children.Add(widget);
            _activeWidgets.Add(widget);

            _nextGridCol = col + 2;
            _nextGridRow = row;
            if (_nextGridCol >= WidgetBase.GridColumns - 1)
            {
                _nextGridCol = 0;
                _nextGridRow += 2;
            }

            SaveAllWidgets();
        }

        private void FindNextEmptyGridCell(double cellW, double cellH, out int col, out int row)
        {
            int rows = WidgetBase.GridRows;

            for (int r = _nextGridRow; r < rows; r++)
            {
                int startCol = (r == _nextGridRow) ? _nextGridCol : 0;
                for (int c = startCol; c <= WidgetBase.GridColumns - 2; c++)
                {
                    if (!IsGridCellOccupied(c, r, 2, 2))
                    {
                        col = c;
                        row = r;
                        return;
                    }
                }
            }

            col = 0;
            row = 0;
            while (IsGridCellOccupied(col, row, 2, 2))
            {
                col += 2;
                if (col >= WidgetBase.GridColumns - 1)
                {
                    col = 0;
                    row += 2;
                }
            }
        }

        private bool IsGridCellOccupied(int col, int row, int colSpan, int rowSpan)
        {
            foreach (var w in _activeWidgets)
            {
                int wc = w.Data.GridColumn;
                int wr = w.Data.GridRow;
                int ws = w.Data.GridColSpan;
                int wh = w.Data.GridRowSpan;

                if (col < wc + ws && col + colSpan > wc &&
                    row < wr + wh && row + rowSpan > wr)
                    return true;
            }
            return false;
        }

        private WidgetBase? CreateWidget(string type)
        {
            return type switch
            {
                "Clock" => new WidgetClock(),
                "Date" => new WidgetDate(),
                "StickyNote" => new WidgetStickyNote(),
                "QuickLaunch" => new WidgetQuickLaunch(),
                "Image" => new WidgetImage(),
                "Audio" => new WidgetAudio(),
                _ => null
            };
        }

        private void OnWidgetClose(WidgetBase widget)
        {
            WidgetCanvas.Children.Remove(widget);
            _activeWidgets.Remove(widget);

            if (widget is WidgetClock clockWidget) clockWidget.Cleanup();
            if (widget is WidgetDate dateWidget) dateWidget.Cleanup();
            if (widget is WidgetAudio audioWidget) audioWidget.Cleanup();

            SaveAllWidgets();
        }

        private void OnWidgetDragEnd(WidgetBase widget)
        {
            SaveAllWidgets();
        }

        private void OnWidgetResizeEnd(WidgetBase widget)
        {
            SaveAllWidgets();
        }

        private void RepositionAllWidgets()
        {
            double cw = DesktopGrid.ActualWidth;
            double ch = DesktopGrid.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            var (cellW, cellH) = WidgetBase.GetGridCellSizes(cw, ch);

            foreach (var widget in _activeWidgets)
            {
                widget.ApplyGridPosition(
                    widget.Data.GridColumn, widget.Data.GridRow,
                    widget.Data.GridColSpan, widget.Data.GridRowSpan,
                    cellW, cellH);
            }

            if (_showGridLines) DrawGridLines();
        }

        private void DrawGridLines()
        {
            double cw = DesktopGrid.ActualWidth;
            double ch = DesktopGrid.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            GridLinesCanvas.Children.Clear();
            GridLinesCanvas.Visibility = Visibility.Visible;

            var (cellW, cellH) = WidgetBase.GetGridCellSizes(cw, ch);
            double gridW = (WidgetBase.GridColumns + 1) * WidgetBase.GridGap + WidgetBase.GridColumns * cellW;
            double gridH = (WidgetBase.GridRows + 1) * WidgetBase.GridGap + WidgetBase.GridRows * cellH;

            var outerColor = Color.FromArgb(0xF5, _gridLineColor.R, _gridLineColor.G, _gridLineColor.B);
            var innerColor = Color.FromArgb(0xE0, _gridLineColor.R, _gridLineColor.G, _gridLineColor.B);

            var outerStroke = new SolidColorBrush(outerColor);
            var innerStroke = new SolidColorBrush(innerColor);

            DoubleCollection? dashArray = _gridLineStyle switch
            {
                "Dashed" => new DoubleCollection { 4, 2 },
                "Dotted" => new DoubleCollection { 1, 3 },
                _ => null
            };

            for (int c = 0; c <= WidgetBase.GridColumns; c++)
            {
                double x = c * (cellW + WidgetBase.GridGap) + WidgetBase.GridGap;
                var line = new System.Windows.Shapes.Line
                {
                    X1 = x, Y1 = 0,
                    X2 = x, Y2 = gridH,
                    Stroke = c == 0 || c == WidgetBase.GridColumns ? outerStroke : innerStroke,
                    StrokeThickness = c == 0 || c == WidgetBase.GridColumns ? 2 : 1.2,
                    SnapsToDevicePixels = true
                };
                if (dashArray != null)
                    line.StrokeDashArray = dashArray;
                GridLinesCanvas.Children.Add(line);
            }

            for (int r = 0; r <= WidgetBase.GridRows; r++)
            {
                double y = r * (cellH + WidgetBase.GridGap) + WidgetBase.GridGap;
                var line = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = y,
                    X2 = gridW, Y2 = y,
                    Stroke = r == 0 || r == WidgetBase.GridRows ? outerStroke : innerStroke,
                    StrokeThickness = r == 0 || r == WidgetBase.GridRows ? 2 : 1.2,
                    SnapsToDevicePixels = true
                };
                if (dashArray != null)
                    line.StrokeDashArray = dashArray;
                GridLinesCanvas.Children.Add(line);
            }

            if (_showCenterLines)
            {
                var centerColor = Color.FromArgb(0xF8, _gridLineColor.R, _gridLineColor.G, _gridLineColor.B);
                var centerStroke = new SolidColorBrush(centerColor);

                double centerX = WidgetBase.GridColumns / 2 * (cellW + WidgetBase.GridGap) + WidgetBase.GridGap;
                var vLine = new System.Windows.Shapes.Line
                {
                    X1 = centerX, Y1 = 0,
                    X2 = centerX, Y2 = gridH,
                    Stroke = centerStroke,
                    StrokeThickness = 2.4,
                    SnapsToDevicePixels = true
                };
                if (dashArray != null)
                    vLine.StrokeDashArray = new DoubleCollection { dashArray[0] * 1.5, dashArray[1] };
                GridLinesCanvas.Children.Add(vLine);

                double centerY = WidgetBase.GridRows / 2 * (cellH + WidgetBase.GridGap) + WidgetBase.GridGap;
                var hLine = new System.Windows.Shapes.Line
                {
                    X1 = 0, Y1 = centerY,
                    X2 = gridW, Y2 = centerY,
                    Stroke = centerStroke,
                    StrokeThickness = 2.4,
                    SnapsToDevicePixels = true
                };
                if (dashArray != null)
                    hLine.StrokeDashArray = new DoubleCollection { dashArray[0] * 1.5, dashArray[1] };
                GridLinesCanvas.Children.Add(hLine);
            }
        }

        private void ClearGridLines()
        {
            GridLinesCanvas.Children.Clear();
            GridLinesCanvas.Visibility = Visibility.Collapsed;
        }

        private void ToggleGridLines()
        {
            _showGridLines = !_showGridLines;
            if (_showGridLines)
                DrawGridLines();
            else
                ClearGridLines();
        }

        private void ToggleCenterLines()
        {
            _showCenterLines = !_showCenterLines;
            if (_showGridLines) DrawGridLines();
        }

        private MenuItem BuildGridLineColorMenu()
        {
            var menu = new MenuItem { Header = "辅助线颜色" };

            var colors = new (string Name, Color Color)[]
            {
                ("白色", Colors.White),
                ("红色", Color.FromRgb(0xFF, 0x44, 0x44)),
                ("橙色", Color.FromRgb(0xFF, 0xA0, 0x44)),
                ("黄色", Color.FromRgb(0xFF, 0xD7, 0x44)),
                ("绿色", Color.FromRgb(0x44, 0xFF, 0x66)),
                ("青色", Color.FromRgb(0x44, 0xDD, 0xFF)),
                ("蓝色", Color.FromRgb(0x44, 0x88, 0xFF)),
                ("紫色", Color.FromRgb(0xC0, 0x84, 0xFC)),
                ("粉红", Color.FromRgb(0xFF, 0x88, 0xCC)),
            };

            foreach (var (name, color) in colors)
            {
                var item = new MenuItem
                {
                    Header = name,
                    IsCheckable = true,
                    IsChecked = _gridLineColor == color,
                    Tag = color
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is Color c)
                    {
                        _gridLineColor = c;
                        if (_showGridLines) DrawGridLines();
                    }
                };
                menu.Items.Add(item);
            }

            return menu;
        }

        private MenuItem BuildGridLineStyleMenu()
        {
            var menu = new MenuItem { Header = "辅助线样式" };

            var styles = new[] { "Solid", "Dashed", "Dotted" };
            var labels = new[] { "实线", "虚线", "点线" };

            for (int i = 0; i < styles.Length; i++)
            {
                var item = new MenuItem
                {
                    Header = labels[i],
                    IsCheckable = true,
                    IsChecked = _gridLineStyle == styles[i],
                    Tag = styles[i]
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is string style)
                    {
                        _gridLineStyle = style;
                        if (_showGridLines) DrawGridLines();
                    }
                };
                menu.Items.Add(item);
            }

            return menu;
        }

        private void ClearAllWidgets()
        {
            var result = MessageBox.Show(
                "确定要删除桌面上所有小组件吗？此操作不可撤销。",
                "删除确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            foreach (var widget in _activeWidgets.ToList())
            {
                WidgetCanvas.Children.Remove(widget);
                if (widget is WidgetClock clockWidget) clockWidget.Cleanup();
                if (widget is WidgetDate dateWidget) dateWidget.Cleanup();
                if (widget is WidgetAudio audioWidget) audioWidget.Cleanup();
            }
            _activeWidgets.Clear();
            _nextGridCol = 0;
            _nextGridRow = 0;
            SaveAllWidgets();
        }

        private void LoadWidgets()
        {
            var settings = ProgramSettingsService.Instance.Current;
            if (settings.Widgets == null || settings.Widgets.Count == 0) return;

            double cw = DesktopGrid.ActualWidth;
            double ch = DesktopGrid.ActualHeight;
            var (cellW, cellH) = WidgetBase.GetGridCellSizes(cw, ch);

            foreach (var widgetData in settings.Widgets)
            {
                var widget = CreateWidget(widgetData.Type);
                if (widget == null) continue;

                widget.Data = widgetData;
                widget.OnClose += OnWidgetClose;
                widget.OnDragEnd += OnWidgetDragEnd;
                widget.OnResizeEnd += OnWidgetResizeEnd;

                if (widgetData.GridColSpan <= 0)
                {
                    if (widgetData.ProportionalX > 0 || widgetData.ProportionalY > 0)
                    {
                        widget.Width = widgetData.Width > 0 ? widgetData.Width : 280;
                        widget.Height = widgetData.Height > 0 ? widgetData.Height : 220;
                        widget.SetCenterPosition(widgetData.X, widgetData.Y);
                    }
                    else
                    {
                        widget.Width = widgetData.Width > 0 ? widgetData.Width : 280;
                        widget.Height = widgetData.Height > 0 ? widgetData.Height : 220;
                        widget.SetCenterPosition(widgetData.X, widgetData.Y);
                    }

                    widget.SaveGridDataFromPosition();
                    widgetData.GridColSpan = Math.Max(1, widgetData.GridColSpan);
                    widgetData.GridRowSpan = Math.Max(1, widgetData.GridRowSpan);

                    widget.ApplyGridPosition(
                        widgetData.GridColumn, widgetData.GridRow,
                        widgetData.GridColSpan, widgetData.GridRowSpan,
                        cellW, cellH);
                }
                else
                {
                    widget.ApplyGridPosition(
                        widgetData.GridColumn, widgetData.GridRow,
                        widgetData.GridColSpan, widgetData.GridRowSpan,
                        cellW, cellH);
                }

                widget.LoadAppearance(widgetData);

                WidgetCanvas.Children.Add(widget);
                _activeWidgets.Add(widget);
            }

            RepositionAllWidgets();
        }

        private void SaveAllWidgets()
        {
            var settings = ProgramSettingsService.Instance.Current;
            settings.Widgets = _activeWidgets.Select(w => w.Data).ToList();
            ProgramSettingsService.Instance.Save();
        }

        private void SetDesktopContent(UIElement? content)
        {
            DesktopContent.Content = content;
            WidgetCanvas.Visibility = content == null ? Visibility.Visible : Visibility.Collapsed;
            ApplyDesktopBarModes();
        }

        private void SetupButton9()
        {
            Button9.Click += (s, e) =>
            {
                if (Button9DetailPopup.IsOpen)
                {
                    Button9DetailPopup.IsOpen = false;
                    return;
                }
                Button9DetailPopup.IsOpen = true;
            };

            Button9DetailPopup.Child = BuildTimeDetailPanel();
        }

        private void Button9DetailPopup_Opened(object sender, EventArgs e)
        {
            Button9DetailPopup.Child = BuildTimeDetailPanel();
            var transform = Button9.TransformToAncestor(BottomBarWrap);
            var buttonPos = transform.Transform(new Point(0, 0));
            Button9DetailPopup.HorizontalOffset = buttonPos.X + Button9.ActualWidth / 2 - 130;
        }

        private Border BuildTimeDetailPanel()
        {
            var now = DateTime.Now;
            var s = ProgramSettingsService.Instance.Current;

            var outerClip = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true
            };

            var panel = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(20, 16, 20, 16),
                MinWidth = 260,
                BorderThickness = new Thickness(1)
            };
            panel.SetResourceReference(Border.BackgroundProperty, "BgPanel");
            panel.SetResourceReference(Border.BorderBrushProperty, "BorderColor");
            panel.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 20,
                ShadowDepth = 4,
                Opacity = 0.5
            };

            outerClip.Child = panel;

            var stack = new StackPanel();

            var timePeriod = GetTimePeriod(now.Hour);

            var periodBlock = new TextBlock
            {
                Text = timePeriod,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            periodBlock.SetResourceReference(TextBlock.ForegroundProperty, "AccentLight");
            stack.Children.Add(periodBlock);

            bool is24Hour = s.Button9Time24Hour;
            bool showSeconds = s.Button9TimeShowSeconds;
            string timeFormat = is24Hour ? "HH" : "hh";
            timeFormat += showSeconds ? ":mm:ss" : ":mm";
            if (!is24Hour) timeFormat += " tt";

            var timeBlock = new TextBlock
            {
                Text = now.ToString(timeFormat),
                FontSize = 36,
                FontWeight = FontWeights.Light,
                FontFamily = new FontFamily("Segoe UI"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            timeBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            stack.Children.Add(timeBlock);

            var dateBlock = new TextBlock
            {
                Text = MainWindow.FormatDate(now, s.Button9DateOrder, s.Button9DateStyle, s.Button9MonthStyle, s.Button9YearStyle),
                FontSize = 18,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 2)
            };
            dateBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            stack.Children.Add(dateBlock);

            string[] weekDays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
            var weekBlock = new TextBlock
            {
                Text = weekDays[(int)now.DayOfWeek],
                FontSize = 15,
                Margin = new Thickness(0, 0, 0, 0)
            };
            weekBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            stack.Children.Add(weekBlock);

            panel.Child = stack;
            return outerClip;
        }

        private static string GetTimePeriod(int hour)
        {
            return hour switch
            {
                23 or 0 or 1 => "午夜",
                2 or 3 => "凌晨",
                4 or 5 => "黎明",
                6 => "清晨",
                7 => "清晨",
                >= 8 and <= 10 => "上午",
                11 or 12 => "中午",
                13 => "中午",
                16 => "黄昏",
                17 => "傍晚",
                14 or 15 => "下午",
                >= 18 and <= 22 => "晚上",
                _ => "夜间"
            };
        }

        public void ApplyDesktopWallpaper()
        {
            var s = ProgramSettingsService.Instance.Current;

            DesktopVideoWallpaper.Visibility = Visibility.Collapsed;
            DesktopVideoWallpaper.Stop();

            switch (s.DesktopWallpaperType)
            {
                case "SolidColor":
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(s.DesktopWallpaperColor);
                        RootGridUC1.Background = new SolidColorBrush(color);
                    }
                    catch
                    {
                        RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    }
                    break;
                case "Image":
                    if (!string.IsNullOrEmpty(s.DesktopWallpaperImagePath) && System.IO.File.Exists(s.DesktopWallpaperImagePath))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(s.DesktopWallpaperImagePath);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            RootGridUC1.Background = new ImageBrush(bmp)
                            {
                                Stretch = Stretch.UniformToFill,
                                AlignmentX = AlignmentX.Center,
                                AlignmentY = AlignmentY.Center
                            };
                        }
                        catch
                        {
                            RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                        }
                    }
                    else
                    {
                        RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    }
                    break;
                case "Video":
                    if (!string.IsNullOrEmpty(s.DesktopWallpaperVideoPath) && System.IO.File.Exists(s.DesktopWallpaperVideoPath))
                    {
                        try
                        {
                            RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                            DesktopVideoWallpaper.Visibility = Visibility.Visible;
                            DesktopVideoWallpaper.Source = new Uri(s.DesktopWallpaperVideoPath);
                            DesktopVideoWallpaper.IsMuted = true;
                            DesktopVideoWallpaper.MediaEnded += (sender, args) =>
                            {
                                DesktopVideoWallpaper.Position = TimeSpan.Zero;
                                DesktopVideoWallpaper.Play();
                            };
                            DesktopVideoWallpaper.Play();
                        }
                        catch
                        {
                            DesktopVideoWallpaper.Visibility = Visibility.Collapsed;
                            RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                        }
                    }
                    else
                    {
                        RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    }
                    break;
                default:
                    RootGridUC1.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    break;
            }
        }

        public void ApplyDesktopBarModes()
        {
            var s = ProgramSettingsService.Instance.Current;

            if (HasAnyDesktopOverride(s))
            {
                if (DesktopContent.Content == null)
                {
                    ApplyDesktopSpecificTopBarMode(s.DesktopTopBarMode);
                    ApplyDesktopSpecificBottomBarMode(s.DesktopBottomBarMode);
                    ApplyDesktopSpecificBarStyleAndButtons();
                }
                else
                {
                    Window mainWindow = Window.GetWindow(this);
                    if (mainWindow is MainWindow mw)
                    {
                        mw.ApplyOpenSettings();
                    }
                    ApplyBottomBarVisibilityMode();
                }
            }
        }

        private static bool HasAnyDesktopOverride(ProgramSettings s)
        {
            return s.DesktopTopBarMode != "Original"
                || s.DesktopBottomBarMode != "Original"
                || s.DesktopBottomBarStyle != "Original"
                || s.DesktopBottomBarLayout != "Original"
                || !s.DesktopButton8Show
                || !s.DesktopButton9Show
                || !s.DesktopButton10Show;
        }

        private void ApplyDesktopSpecificTopBarMode(string mode)
        {
            if (mode == "Original") return;
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            mainWindow.ApplyTopBarModeOverride(mode);
        }

        private void ApplyDesktopSpecificBottomBarMode(string mode)
        {
            if (mode == "Original") return;

            StopAutoHideDetection();

            switch (mode)
            {
                case "Floating":
                    BottomBarWrap.Opacity = 0.95;
                    BottomBarWrap.IsHitTestVisible = true;
                    BottomBarFullBg.Visibility = Visibility.Collapsed;
                    break;
                case "AutoHide":
                    BottomBarWrap.Height = 4;
                    BottomBarFullBg.Height = 4;
                    BottomBarWrap.Opacity = 0;
                    BottomBarWrap.IsHitTestVisible = true;
                    StartAutoHideDetection();
                    break;
                default:
                    BottomBarWrap.Opacity = 1;
                    BottomBarWrap.IsHitTestVisible = true;
                    BottomBarFullBg.Visibility = Visibility.Visible;
                    break;
            }

            ApplyProgramSettings();
        }

        private void ApplyDesktopSpecificBarStyleAndButtons()
        {
            var s = ProgramSettingsService.Instance.Current;

            string style = s.DesktopBottomBarStyle;
            string layout = s.DesktopBottomBarLayout;

            if (style != "Original" || layout != "Original")
            {
                string effectiveStyle = style == "Original" ? s.BottomBarStyle : style;
                string effectiveLayout = layout == "Original" ? s.BottomBarLayout : layout;
                ApplyBottomBarMode(effectiveStyle, effectiveLayout);
            }

            Button8.Visibility = s.DesktopButton8Show ? Visibility.Visible : Visibility.Collapsed;
            Separator1.Visibility = s.DesktopButton8Show ? Visibility.Visible : Visibility.Collapsed;
            Button9.Visibility = s.DesktopButton9Show ? Visibility.Visible : Visibility.Collapsed;
            Separator2.Visibility = s.DesktopButton9Show ? Visibility.Visible : Visibility.Collapsed;
            Button10.Visibility = s.DesktopButton10Show ? Visibility.Visible : Visibility.Collapsed;
            UpdateButton8Display();
        }

    }
}
