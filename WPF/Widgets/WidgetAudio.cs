using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.Widgets
{
    public class WidgetAudio : WidgetBase
    {
        private TextBlock _songName = null!;
        private TextBlock _currentTime = null!;
        private TextBlock _totalTime = null!;
        private Slider _progressSlider = null!;
        private Button _btnPlayPause = null!;
        private Button _btnPrev = null!;
        private Button _btnNext = null!;
        private Button _btnLoop = null!;
        private Button _btnVolume = null!;
        private Button _btnQuickSelect = null!;
        private TextBlock _volumeIcon = null!;
        private Popup _volumePopup = null!;
        private Slider _volumeSlider = null!;
        private TextBlock _volumeLabel = null!;
        private bool _isDraggingProgress;
        private readonly DispatcherTimer _updateTimer;

        public WidgetAudio()
        {
            SetTitle("音频控制器");
            Width = 340;
            Height = 160;
            BuildAudioUI();

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _updateTimer.Tick += (s, e) => UpdateUI();

            Loaded += (s, e) =>
            {
                AudioService.Instance.StateChanged += OnAudioState;
                _updateTimer.Start();
                UpdateUI();
            };

            Unloaded += (s, e) =>
            {
                _updateTimer.Stop();
                AudioService.Instance.StateChanged -= OnAudioState;
            };
        }

        private void BuildAudioUI()
        {
            var stack = new StackPanel();

            var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var btnClose = new Button
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Content = "\u2715",
                ToolTip = "关闭当前音乐",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            btnClose.Template = CreateIconBtnTemplate();
            btnClose.Click += (s, e) =>
            {
                AudioService.Instance.ClearTrack();
                UpdateUI();
            };
            Grid.SetColumn(btnClose, 0);

            _songName = new TextBlock
            {
                Text = "无音频播放",
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(_songName, 1);

            _btnQuickSelect = new Button
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xFF)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Content = "#",
                ToolTip = "快速选择预设音频",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            _btnQuickSelect.Template = CreateIconBtnTemplate();
            _btnQuickSelect.Click += (s, e) =>
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
                if (trackIndex >= 0)
                {
                    audio.Volume = volume;
                    audio.LoopMode = loopMode;
                    audio.PlayTrack(trackIndex);
                }
                UpdateUI();
            };
            Grid.SetColumn(_btnQuickSelect, 2);

            header.Children.Add(btnClose);
            header.Children.Add(_songName);
            header.Children.Add(_btnQuickSelect);
            stack.Children.Add(header);

            var progressGrid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progressGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _currentTime = new TextBlock
            {
                Text = "00:00",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x80)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(_currentTime, 0);

            _progressSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                IsMoveToPointEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            _progressSlider.PreviewMouseLeftButtonDown += (s, e) => _isDraggingProgress = true;
            _progressSlider.PreviewMouseLeftButtonUp += (s, e) =>
            {
                _isDraggingProgress = false;
                var audio = AudioService.Instance;
                if (audio.CurrentIndex >= 0)
                    audio.Seek(TimeSpan.FromSeconds(_progressSlider.Value));
            };
            _progressSlider.ValueChanged += (s, e) =>
            {
                if (!_isDraggingProgress) return;
                _currentTime.Text = FormatTime(TimeSpan.FromSeconds(_progressSlider.Value));
            };
            Grid.SetColumn(_progressSlider, 1);

            _totalTime = new TextBlock
            {
                Text = "00:00",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x80)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            Grid.SetColumn(_totalTime, 2);

            progressGrid.Children.Add(_currentTime);
            progressGrid.Children.Add(_progressSlider);
            progressGrid.Children.Add(_totalTime);
            stack.Children.Add(progressGrid);

            var controls = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            _btnLoop = CreateIconBtn("🔁", "顺序循环");
            _btnLoop.Click += (s, e) =>
            {
                var a = AudioService.Instance;
                a.LoopMode = a.LoopMode == 0 ? 1 : 0;
                UpdateUI();
            };
            controls.Children.Add(_btnLoop);

            _btnPrev = CreateIconBtn("⏮", "上一首");
            _btnPrev.Click += (s, e) => AudioService.Instance.Previous();
            controls.Children.Add(_btnPrev);

            _btnPlayPause = CreatePlayBtn();
            _btnPlayPause.Click += (s, e) =>
            {
                var a = AudioService.Instance;
                if (a.CurrentTrack == null && a.Playlist.Count > 0)
                    a.PlayTrack(0);
                else
                    a.TogglePlayPause();
                UpdateUI();
            };
            controls.Children.Add(_btnPlayPause);

            _btnNext = CreateIconBtn("⏭", "下一首");
            _btnNext.Click += (s, e) => AudioService.Instance.Next();
            controls.Children.Add(_btnNext);

            var volStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _btnVolume = new Button
            {
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 0, 0)
            };
            _btnVolume.Template = CreateIconBtnTemplate();
            _volumeIcon = new TextBlock { Text = "🔊", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0)) };
            _btnVolume.Content = _volumeIcon;
            _btnVolume.Click += (s, e) =>
            {
                _volumePopup.IsOpen = !_volumePopup.IsOpen;
                if (_volumePopup.IsOpen)
                {
                    _volumeSlider.Value = AudioService.Instance.Volume;
                    _volumeLabel.Text = $"{(int)(AudioService.Instance.Volume * 100)}%";
                }
            };
            volStack.Children.Add(_btnVolume);

            _volumePopup = new Popup
            {
                PlacementTarget = _btnVolume,
                Placement = PlacementMode.Top,
                HorizontalOffset = -46,
                VerticalOffset = -4,
                StaysOpen = false,
                AllowsTransparency = true
            };

            var volBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x36)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x55)),
                BorderThickness = new Thickness(1)
            };
            var volPanel = new StackPanel();
            _volumeLabel = new TextBlock
            {
                Text = "70%",
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _volumeSlider = new Slider
            {
                Width = 100,
                Orientation = Orientation.Horizontal,
                Minimum = 0,
                Maximum = 1,
                Value = 0.7,
                IsMoveToPointEnabled = true
            };
            _volumeSlider.ValueChanged += (s, e) =>
            {
                var v = _volumeSlider.Value;
                AudioService.Instance.Volume = v;
                _volumeLabel.Text = $"{(int)(v * 100)}%";
                _volumeIcon.Text = v < 0.01 ? "🔇" : (v < 0.35 ? "🔈" : "🔊");
            };
            volPanel.Children.Add(_volumeLabel);
            volPanel.Children.Add(_volumeSlider);
            volBorder.Child = volPanel;
            _volumePopup.Child = volBorder;

            volStack.Children.Add(_volumePopup);
            controls.Children.Add(volStack);

            stack.Children.Add(controls);
            SetContent(stack);
        }

        private Button CreateIconBtn(string content, string tooltip)
        {
            var btn = new Button
            {
                Width = 28,
                Height = 28,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Content = content,
                ToolTip = tooltip,
                Margin = new Thickness(2, 0, 2, 0)
            };
            btn.Template = CreateIconBtnTemplate();
            return btn;
        }

        private Button CreatePlayBtn()
        {
            var btn = new Button
            {
                Width = 38,
                Height = 38,
                Background = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xFF)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 16,
                Content = "▶",
                Margin = new Thickness(4, 0, 4, 0)
            };
            btn.Template = CreatePlayBtnTemplate(Color.FromRgb(0x7C, 0x8A, 0xFF));
            return btn;
        }

        private static ControlTemplate CreateIconBtnTemplate()
        {
            var t = new ControlTemplate(typeof(Button));
            var b = new FrameworkElementFactory(typeof(Border));
            b.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            b.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            b.Name = "bd";
            var p = new FrameworkElementFactory(typeof(ContentPresenter));
            p.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            p.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            b.AppendChild(p);
            t.VisualTree = b;
            var tr = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            tr.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x5A)), "bd"));
            t.Triggers.Add(tr);
            return t;
        }

        private ControlTemplate CreatePlayBtnTemplate(Color accent)
        {
            var lighter = Color.FromArgb(accent.A,
                (byte)Math.Min(255, accent.R + 40),
                (byte)Math.Min(255, accent.G + 40),
                (byte)Math.Min(255, accent.B + 40));

            var t = new ControlTemplate(typeof(Button));
            var b = new FrameworkElementFactory(typeof(Border));
            b.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            b.SetValue(Border.CornerRadiusProperty, new CornerRadius(19));
            b.Name = "bd";
            var p = new FrameworkElementFactory(typeof(ContentPresenter));
            p.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            p.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            b.AppendChild(p);
            t.VisualTree = b;
            var tr = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            tr.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(lighter), "bd"));
            t.Triggers.Add(tr);
            return t;
        }

        private void OnAudioState()
        {
            Dispatcher.BeginInvoke(() => UpdateUI());
        }

        private void UpdateUI()
        {
            var audio = AudioService.Instance;
            var track = audio.CurrentTrack;

            if (track != null)
            {
                _songName.Text = track.DisplayName;
                _btnPlayPause.Content = audio.IsPlaying ? "⏸" : "▶";

                if (audio.NaturalDuration.HasTimeSpan)
                {
                    var dur = audio.NaturalDuration.TimeSpan;
                    _progressSlider.Maximum = dur.TotalSeconds;
                    _totalTime.Text = FormatTime(dur);
                    if (!_isDraggingProgress)
                    {
                        _progressSlider.Value = audio.Position.TotalSeconds;
                        _currentTime.Text = FormatTime(audio.Position);
                    }
                }
                else
                {
                    _progressSlider.Maximum = 100;
                    _progressSlider.Value = 0;
                    _currentTime.Text = "00:00";
                    _totalTime.Text = "00:00";
                }
            }
            else
            {
                _songName.Text = "无音频播放";
                _btnPlayPause.Content = "▶";
                _progressSlider.Maximum = 100;
                _progressSlider.Value = 0;
                _currentTime.Text = "00:00";
                _totalTime.Text = "00:00";
            }

            _btnLoop.Content = audio.LoopMode == 0 ? "🔁" : "🔂";
            _btnLoop.ToolTip = audio.LoopMode == 0 ? "顺序循环" : "单曲循环";

            var v = audio.Volume;
            _volumeIcon.Text = v < 0.01 ? "🔇" : (v < 0.35 ? "🔈" : "🔊");
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
            return $"{time.Minutes:00}:{time.Seconds:00}";
        }

        public void Cleanup()
        {
            _updateTimer.Stop();
            AudioService.Instance.StateChanged -= OnAudioState;
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            _songName.Foreground = new SolidColorBrush(textColor);
            var accentColor = GetEffectiveAccentColor();
            _btnPlayPause.Background = new SolidColorBrush(accentColor);
            _btnPlayPause.Template = CreatePlayBtnTemplate(accentColor);
            _btnQuickSelect.Foreground = new SolidColorBrush(accentColor);
        }
    }
}
