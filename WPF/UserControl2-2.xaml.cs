using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl2_2 : UserControl
    {
        private readonly DispatcherTimer _progressTimer;
        private bool _isUserDraggingSlider;
        private bool _isPlaying;
        private readonly double[] _speeds = { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
        private int _speedIndex = 2;

        public UserControl2_2()
        {
            InitializeComponent();

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _progressTimer.Tick += OnProgressTick;

            VideoPlayer.MediaOpened += OnMediaOpened;
            VideoPlayer.MediaEnded += OnMediaEnded;
            VideoPlayer.MediaFailed += OnMediaFailed;

            BtnPlay.Click += (s, e) =>
            {
                if (_isPlaying)
                    Pause();
                else
                    Play();
            };

            BtnHide.Click += (s, e) =>
            {
                Stop();
                if (Parent is ContentControl cc) cc.Content = null;
            };

            BtnRewind.Click += (s, e) => Seek(-5);
            BtnForward.Click += (s, e) => Seek(5);
            BtnFullscreen.Click += (s, e) => EnterFullscreen();
            BtnSpeed.Click += (s, e) => CycleSpeed();
            BtnToggleLeftPanel.Click += (s, e) => ToggleParentLeftPanel();

            ProgressSlider.AddHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(OnSliderMouseDown), true);
            ProgressSlider.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnSliderMouseUp), true);
            ProgressSlider.ValueChanged += OnSliderValueChanged;

            VolumeSlider.ValueChanged += OnVolumeChanged;
            VolumeSlider.Value = 0.5;
            VideoPlayer.Volume = 0.5;

            VideoArea.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount >= 2)
                    EnterFullscreen();
            };

            this.PreviewKeyDown += OnPreviewKeyDown;
            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    if (_isPlaying)
                        Pause();
                    else
                        Play();
                    e.Handled = true;
                    break;
                case Key.Left:
                    Seek(-5);
                    e.Handled = true;
                    break;
                case Key.Right:
                    Seek(5);
                    e.Handled = true;
                    break;
                case Key.Up:
                    VolumeSlider.Value = Math.Min(1.0, VolumeSlider.Value + 0.05);
                    e.Handled = true;
                    break;
                case Key.Down:
                    VolumeSlider.Value = Math.Max(0.0, VolumeSlider.Value - 0.05);
                    e.Handled = true;
                    break;
            }
        }

        public void LoadFile(string filePath)
        {
            Stop();

            try
            {
                VideoPlayer.Source = new Uri(filePath, UriKind.Absolute);
                var fi = new FileInfo(filePath);
                FileNameLabel.Text = fi.Name;
                DimensionsLabel.Text = "-- × --";
                FileSizeLabel.Text = FileManagerService.FormatFileSize(fi.Length);
            }
            catch
            {
            }
        }

        public void Play()
        {
            if (VideoPlayer.Source == null) return;
            VideoPlayer.Play();
            _isPlaying = true;
            UpdatePlayButton();
            _progressTimer.Start();
        }

        public void Pause()
        {
            VideoPlayer.Pause();
            _isPlaying = false;
            UpdatePlayButton();
            _progressTimer.Stop();
        }

        public void Stop()
        {
            VideoPlayer.Stop();
            _isPlaying = false;
            UpdatePlayButton();
            _progressTimer.Stop();
            ProgressSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
            TotalTimeLabel.Text = "00:00";
        }

        private void Seek(double seconds)
        {
            var newPos = VideoPlayer.Position.TotalSeconds + seconds;
            newPos = Math.Max(0, Math.Min(newPos, VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds));
            VideoPlayer.Position = TimeSpan.FromSeconds(newPos);
        }

        private void EnterFullscreen()
        {
            if (VideoPlayer.Source == null) return;

            var window = Window.GetWindow(this);
            if (window is not MainWindow mw) return;

            var vs = VideoService.Instance;

            if (_isPlaying)
            {
                Pause();
                vs.SaveState(
                    VideoPlayer.Source,
                    VideoPlayer.Position,
                    true,
                    VideoPlayer.Volume,
                    VideoPlayer.SpeedRatio,
                    VideoPlayer.NaturalDuration.HasTimeSpan
                        ? VideoPlayer.NaturalDuration.TimeSpan : null,
                    FileNameLabel.Text,
                    DimensionsLabel.Text,
                    FileSizeLabel.Text);
            }
            else
            {
                vs.SaveState(
                    VideoPlayer.Source,
                    VideoPlayer.Position,
                    false,
                    VideoPlayer.Volume,
                    VideoPlayer.SpeedRatio,
                    VideoPlayer.NaturalDuration.HasTimeSpan
                        ? VideoPlayer.NaturalDuration.TimeSpan : null,
                    FileNameLabel.Text,
                    DimensionsLabel.Text,
                    FileSizeLabel.Text);
            }

            var f11 = new UserControl2_2_f11();
            f11.ExitFullscreenRequested += () =>
            {
                mw.ExitFullscreen();
                OnVideoFullscreenExited();
            };
            mw.EnterFullscreen(f11);
            f11.Loaded += (s, e) => f11.LoadFromService();
        }

        private void OnVideoFullscreenExited()
        {
            var vs = VideoService.Instance;

            try
            {
                VideoPlayer.Volume = vs.Volume;
                VideoPlayer.SpeedRatio = vs.SpeedRatio;
                _speedIndex = Array.FindIndex(_speeds,
                    s => Math.Abs(s - vs.SpeedRatio) < 0.01);
                if (_speedIndex < 0) _speedIndex = 2;
                SpeedLabel.Text = $"{vs.SpeedRatio}x";
                VolumeSlider.Value = vs.Volume;
                UpdateVolumeIcon(vs.Volume);

                if (vs.Duration.HasValue)
                {
                    ProgressSlider.Maximum = vs.Duration.Value.TotalSeconds;
                    TotalTimeLabel.Text = FormatTime(vs.Duration.Value);
                }

                VideoPlayer.Position = vs.Position;
                ProgressSlider.Value = vs.Position.TotalSeconds;
                CurrentTimeLabel.Text = FormatTime(vs.Position);

                if (vs.IsPlaying)
                    Play();
                else
                    UpdatePlayButton();
            }
            catch { }
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                var duration = VideoPlayer.NaturalDuration.TimeSpan;
                ProgressSlider.Maximum = duration.TotalSeconds;
                TotalTimeLabel.Text = FormatTime(duration);

                DimensionsLabel.Text = $"{VideoPlayer.NaturalVideoWidth} × {VideoPlayer.NaturalVideoHeight}";
            }

            Play();
        }

        private void OnMediaEnded(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            UpdatePlayButton();
            _progressTimer.Stop();
            VideoPlayer.Position = TimeSpan.Zero;
            ProgressSlider.Value = 0;
            CurrentTimeLabel.Text = "00:00";
        }

        private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            _isPlaying = false;
            UpdatePlayButton();
            _progressTimer.Stop();
        }

        private void OnProgressTick(object? sender, EventArgs e)
        {
            if (_isUserDraggingSlider) return;

            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                ProgressSlider.Value = VideoPlayer.Position.TotalSeconds;
                CurrentTimeLabel.Text = FormatTime(VideoPlayer.Position);
            }
        }

        private void OnSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isUserDraggingSlider = true;
            ProgressSlider.CaptureMouse();
        }

        private void OnSliderMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isUserDraggingSlider = false;
            ProgressSlider.ReleaseMouseCapture();
            VideoPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUserDraggingSlider)
            {
                CurrentTimeLabel.Text = FormatTime(TimeSpan.FromSeconds(ProgressSlider.Value));
            }
        }

        private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoPlayer.Volume = VolumeSlider.Value;
            var pct = (int)(VolumeSlider.Value * 100);
            VolumeLabel.Text = $"{pct}%";

            if (VolumeSlider.Value < 0.01)
                VolumeIcon.Text = "🔇";
            else if (VolumeSlider.Value < 0.35)
                VolumeIcon.Text = "🔈";
            else
                VolumeIcon.Text = "🔊";
        }

        private void ToggleParentLeftPanel()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent is UserControl2 uc2)
                {
                    var visible = uc2.ToggleLeftPanel();
                    BtnToggleLeftPanel.Content = new TextBlock
                    {
                        Text = visible ? "◀" : "▶",
                        FontSize = 13
                    };
                    return;
                }
                parent = (parent as FrameworkElement)?.Parent;
            }
        }

        private void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % _speeds.Length;
            var speed = _speeds[_speedIndex];
            VideoPlayer.SpeedRatio = speed;
            SpeedLabel.Text = $"{speed}x";
        }

        private void UpdatePlayButton()
        {
            BtnPlay.Content = new TextBlock
            {
                Text = _isPlaying ? "⏸" : "▶",
                FontSize = 18,
                TextAlignment = TextAlignment.Center
            };
        }

        private void UpdateVolumeIcon(double vol)
        {
            if (vol < 0.01)
                VolumeIcon.Text = "🔇";
            else if (vol < 0.35)
                VolumeIcon.Text = "🔈";
            else
                VolumeIcon.Text = "🔊";
        }

        private static string FormatTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }
    }
}
