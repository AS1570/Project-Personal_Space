using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl0 : UserControl
    {
        public event Action? NavigateToDesktop;

        private readonly DispatcherTimer _clockTimer;
        private bool _hasFailedOnce;

        public UserControl0()
        {
            InitializeComponent();

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += OnClockTick;
            _clockTimer.Start();
            UpdateClock();

            Loaded += OnLockScreenLoaded;

            BtnLogin.Click += (s, e) => TryLogin();

            PasswordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    TryLogin();
                }
            };

            BtnReturn.Click += (s, e) =>
            {
                LoginPanel.Visibility = Visibility.Collapsed;
                TimeDatePanel.Visibility = Visibility.Visible;
            };
        }

        private void OnLockScreenLoaded(object sender, RoutedEventArgs e)
        {
            ApplyLockScreenWallpaper();
            ApplyLockScreenTimeColors();
            UpdateHintLabel();
        }

        private void ApplyLockScreenTimeColors()
        {
            var s = OpenSettingsService.Instance.Current;

            try
            {
                var timeColor = (Color)ColorConverter.ConvertFromString(s.LockScreenTimeColor);
                TimeLabel.Foreground = new SolidColorBrush(timeColor);
            }
            catch
            {
                TimeLabel.Foreground = new SolidColorBrush(Colors.White);
            }

            try
            {
                var dateColor = (Color)ColorConverter.ConvertFromString(s.LockScreenDateColor);
                DateLabel.Foreground = new SolidColorBrush(dateColor);
            }
            catch
            {
                DateLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            }
        }

        private void ApplyLockScreenWallpaper()
        {
            var s = OpenSettingsService.Instance.Current;

            switch (s.LockScreenWallpaperType)
            {
                case "SolidColor":
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(s.LockScreenWallpaperColor);
                        RootGrid.Background = new SolidColorBrush(color);
                    }
                    catch
                    {
                        RootGrid.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    }
                    break;
                case "Image":
                    if (!string.IsNullOrEmpty(s.LockScreenWallpaperImagePath) && System.IO.File.Exists(s.LockScreenWallpaperImagePath))
                    {
                        try
                        {
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.UriSource = new Uri(s.LockScreenWallpaperImagePath);
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.EndInit();
                            bmp.Freeze();
                            RootGrid.Background = new ImageBrush(bmp)
                            {
                                Stretch = Stretch.UniformToFill,
                                AlignmentX = AlignmentX.Center,
                                AlignmentY = AlignmentY.Center
                            };
                        }
                        catch
                        {
                            RootGrid.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                        }
                    }
                    else
                    {
                        RootGrid.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    }
                    break;
                default:
                    RootGrid.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x16, 0x2A));
                    break;
            }
        }

        private void UpdateHintLabel()
        {
            string hint = OpenSettingsService.Instance.Current.PasswordHint ?? "初始密码：admin123";
            if (App.Database.IsConnected)
            {
                var dbHint = App.Database.GetPasswordHint();
                if (!string.IsNullOrEmpty(dbHint))
                    hint = dbHint;
            }
            HintLabel.Content = hint;
        }

        private void TryLogin()
        {
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(password))
            {
                SetStatus("请输入密码", Brushes.Orange);
                return;
            }

            bool success = App.Database.TryConnect(password);

            if (success)
            {
                SetStatus("密码正确", Brushes.Green);
                PasswordBox.Clear();
                NavigateToDesktop?.Invoke();
            }
            else
            {
                if (!_hasFailedOnce)
                {
                    _hasFailedOnce = true;
                    HintLabel.Visibility = Visibility.Visible;
                }

                SetStatus("密码错误", Brushes.Red);
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
        }

        private void SetStatus(string text, Brush color)
        {
            StatusLabel.Content = text;
            StatusLabel.Foreground = color;
        }

        private void OnClockTick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeLabel.Content = now.ToString("HH:mm:ss");
            DateLabel.Content = now.ToString("yyyy年MM月dd日 dddd");
        }

        private void OnLockScreenMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            TimeDatePanel.Visibility = Visibility.Collapsed;
            PasswordBox.Focus();
        }
    }
}
