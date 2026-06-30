using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl2_2_f11 : UserControl
    {
        private readonly DispatcherTimer _progressTimer;
        private readonly DispatcherTimer _autoHideTimer;
        private bool _isUserDraggingSlider;
        private bool _isPlaying;
        private readonly double[] _speeds = { 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 };
        private int _speedIndex = 2;

        public event Action? ExitFullscreenRequested;

        public UserControl2_2_f11()
        {
            InitializeComponent();

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _progressTimer.Tick += OnProgressTick;

            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            _autoHideTimer.Tick += OnAutoHideTick;

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

            BtnRewind.Click += (s, e) => Seek(-5);
            BtnForward.Click += (s, e) => Seek(5);
            BtnSpeed.Click += (s, e) => CycleSpeed();
            BtnExitFullscreen.Click += (s, e) => ExitFullscreenRequested?.Invoke();

            ProgressSlider.AddHandler(Slider.MouseLeftButtonDownEvent,
                new MouseButtonEventHandler(OnSliderMouseDown), true);
            ProgressSlider.AddHandler(Slider.MouseLeftButtonUpEvent,
                new MouseButtonEventHandler(OnSliderMouseUp), true);
            ProgressSlider.ValueChanged += OnSliderValueChanged;

            VolumeSlider.ValueChanged += OnVolumeChanged;
            VolumeSlider.Value = 0.5;

            FullscreenRoot.MouseMove += OnFullscreenMouseMove;
            FullscreenRoot.MouseLeave += (s, e) => { };

            FullscreenRoot.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount >= 2)
                {
                    if (_isPlaying)
                        Pause();
                    else
                        Play();
                }
            };

            this.KeyDown += (s, e) =>
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        ExitFullscreenRequested?.Invoke();
                        break;
                    case Key.Space:
                        if (_isPlaying) Pause(); else Play();
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
                        SetVolume(Math.Min(1.0, VideoPlayer.Volume + 0.05));
                        e.Handled = true;
                        break;
                    case Key.Down:
                        SetVolume(Math.Max(0.0, VideoPlayer.Volume - 0.05));
                        e.Handled = true;
                        break;
                }
            };
        }

        private void SetVolume(double vol)
        {
            VideoPlayer.Volume = vol;
            VolumeSlider.Value = vol;
            UpdateVolumeIcon(vol);
        }

        public void LoadFromService()
        {
            var vs = VideoService.Instance;

            if (vs.Source == null) return;

            VideoPlayer.Source = vs.Source;
            VideoPlayer.Volume = vs.Volume;
            VideoPlayer.SpeedRatio = vs.SpeedRatio;

            _speedIndex = Array.FindIndex(_speeds, s => Math.Abs(s - vs.SpeedRatio) < 0.01);
            if (_speedIndex < 0) _speedIndex = 2;
            SpeedLabel.Text = $"{vs.SpeedRatio}x";

            VolumeSlider.Value = vs.Volume;
            UpdateVolumeIcon(vs.Volume);

            FileNameLabel.Text = vs.FileName ?? "--";
            DimensionsLabel.Text = vs.Dimensions ?? "-- × --";

            ShowToolbars();
            _autoHideTimer.Start();
        }

        public void SaveToService()
        {
            var vs = VideoService.Instance;
            vs.UpdatePosition(VideoPlayer.Position);
            vs.IsPlaying = _isPlaying;
            vs.Volume = VideoPlayer.Volume;
            vs.SpeedRatio = VideoPlayer.SpeedRatio;
        }

        public void DisposeResources()
        {
            _autoHideTimer.Stop();
            _progressTimer.Stop();
            SaveToService();
            VideoPlayer.Stop();
            VideoPlayer.Source = null;
        }

        private void Play()
        {
            if (VideoPlayer.Source == null) return;
            VideoPlayer.Play();
            _isPlaying = true;
            UpdatePlayButton();
            _progressTimer.Start();
        }

        private void Pause()
        {
            VideoPlayer.Pause();
            _isPlaying = false;
            UpdatePlayButton();
            _progressTimer.Stop();
        }

        private void Seek(double seconds)
        {
            if (!VideoPlayer.NaturalDuration.HasTimeSpan) return;
            var newPos = VideoPlayer.Position.TotalSeconds + seconds;
            newPos = Math.Max(0, Math.Min(newPos,
                VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds));
            VideoPlayer.Position = TimeSpan.FromSeconds(newPos);
        }

        private void CycleSpeed()
        {
            _speedIndex = (_speedIndex + 1) % _speeds.Length;
            var speed = _speeds[_speedIndex];
            VideoPlayer.SpeedRatio = speed;
            SpeedLabel.Text = $"{speed}x";
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                var duration = VideoPlayer.NaturalDuration.TimeSpan;
                ProgressSlider.Maximum = duration.TotalSeconds;
                TotalTimeLabel.Text = FormatTime(duration);

                DimensionsLabel.Text =
                    $"{VideoPlayer.NaturalVideoWidth} × {VideoPlayer.NaturalVideoHeight}";

                var vs = VideoService.Instance;
                vs.Duration = duration;

                if (vs.Position.TotalSeconds > 0)
                {
                    VideoPlayer.Position = vs.Position;
                    ProgressSlider.Value = vs.Position.TotalSeconds;
                    CurrentTimeLabel.Text = FormatTime(vs.Position);
                }
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
                CurrentTimeLabel.Text = FormatTime(TimeSpan.FromSeconds(ProgressSlider.Value));
        }

        private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            VideoPlayer.Volume = VolumeSlider.Value;
            UpdateVolumeIcon(VolumeSlider.Value);
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

        private void UpdatePlayButton()
        {
            BtnPlay.Content = new TextBlock
            {
                Text = _isPlaying ? "⏸" : "▶",
                FontSize = 18,
                TextAlignment = TextAlignment.Center
            };
        }

        private static string FormatTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        private void OnFullscreenMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(FullscreenRoot);
            var h = FullscreenRoot.ActualHeight;

            var nearTop = pos.Y < 60;
            var nearBottom = pos.Y > h - 120;

            if (nearTop || nearBottom)
            {
                if (nearTop) TopInfoBar.Opacity = 1.0;
                if (nearBottom) BottomToolbar.Opacity = 1.0;
                _autoHideTimer.Stop();
                _autoHideTimer.Start();
            }
        }

        private void OnAutoHideTick(object? sender, EventArgs e)
        {
            _autoHideTimer.Stop();
            TopInfoBar.Opacity = 0.0;
            BottomToolbar.Opacity = 0.0;
        }

        private void ShowToolbars()
        {
            TopInfoBar.Opacity = 1.0;
            BottomToolbar.Opacity = 1.0;
        }
    }
}
