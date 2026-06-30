using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _topBarClockTimer;
        private Rect _savedNormalBounds;
        private readonly string _originalTitle;
        private readonly System.Windows.Media.ImageSource? _originalIcon;

        public MainWindow()
        {
            InitializeComponent();

            _originalTitle = Title;
            _originalIcon = Icon;

            _savedNormalBounds = new Rect(Left, Top, Width, Height);

            OpenSettingsService.Instance.OnSettingsChanged += ApplyOpenSettings;

            _topBarClockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _topBarClockTimer.Tick += (_, _) => UpdateTopBarClock();
            _topBarClockTimer.Start();
            UpdateTopBarClock();

            ApplyOpenSettings();

            ShowUC0();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Normal)
                _savedNormalBounds = new Rect(Left, Top, Width, Height);
            base.OnStateChanged(e);
        }

        public void ApplyOpenSettings()
        {
            var s = OpenSettingsService.Instance.Current;
            ApplyTopBarMode(s.TopBarMode);
            BtnTopBarLock.Visibility = s.TopBarShowLockButton ? Visibility.Visible : Visibility.Collapsed;
            ApplyExtraScale(s.ExtraScale);
            ApplyDisguise(s);
            UpdateTopBarClock();
        }

        private void ApplyExtraScale(double scalePercent)
        {
            double scale = scalePercent / 100.0;
            scale = Math.Max(0.5, Math.Min(2.0, scale));
            RootLayout.LayoutTransform = new System.Windows.Media.ScaleTransform(scale, scale);
        }

        public void ApplyTopBarModeOverride(string mode)
        {
            ApplyTopBarMode(mode);
        }

        private void ApplyTopBarMode(string mode)
        {
            TopBarBorder.MouseEnter -= OnAutoHideMouseEnter;
            TopBarBorder.MouseLeave -= OnAutoHideMouseLeave;

            switch (mode)
            {
                case "Floating":
                    MainContent.Margin = new Thickness(0, 0, 0, 0);
                    {
                        var topBarBrush = (System.Windows.Media.SolidColorBrush)FindResource("TopBarBg");
                        TopBarBorder.Background = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromArgb(0xAA, topBarBrush.Color.R, topBarBrush.Color.G, topBarBrush.Color.B));
                        TopBarBorder.Height = 35;
                    }
                    SetTopBarChildrenVisible(true);
                    break;
                case "AutoHide":
                    MainContent.Margin = new Thickness(0, 0, 0, 0);
                    TopBarBorder.Height = 4;
                    TopBarBorder.Background = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
                    TopBarBorder.MouseEnter += OnAutoHideMouseEnter;
                    TopBarBorder.MouseLeave += OnAutoHideMouseLeave;
                    SetTopBarChildrenVisible(false);
                    break;
                default:
                    MainContent.Margin = new Thickness(0, 35, 0, 0);
                    TopBarBorder.Height = 35;
                    TopBarBorder.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "TopBarBg");
                    SetTopBarChildrenVisible(true);
                    break;
            }
        }

        private void SetTopBarChildrenVisible(bool visible)
        {
            var v = visible ? Visibility.Visible : Visibility.Collapsed;
            TopBarLeftPanel.Visibility = v;
            TopBarRightPanel.Visibility = v;
        }

        private void OnAutoHideMouseEnter(object sender, MouseEventArgs e)
        {
            TopBarBorder.Height = 35;
            var topBarBrush = (System.Windows.Media.SolidColorBrush)FindResource("TopBarBg");
            TopBarBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0xAA, topBarBrush.Color.R, topBarBrush.Color.G, topBarBrush.Color.B));
            SetTopBarChildrenVisible(true);
        }

        private void OnAutoHideMouseLeave(object sender, MouseEventArgs e)
        {
            TopBarBorder.Height = 4;
            TopBarBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            SetTopBarChildrenVisible(false);
        }

        private void OnTopBarLockClick(object sender, RoutedEventArgs e)
        {
            App.Database.Disconnect();
            ShowUC0();
        }

        private void UpdateTopBarClock()
        {
            var now = DateTime.Now;
            var s = OpenSettingsService.Instance.Current;

            bool showTime = s.TopBarShowTime;
            bool showDate = s.TopBarShowDate;
            bool is24Hour = s.TopBarTime24Hour;
            bool showSeconds = s.TopBarTimeShowSeconds;

            string format = is24Hour ? "HH" : "hh";
            format += showSeconds ? ":mm:ss" : ":mm";
            if (!is24Hour)
                format += " tt";

            TxtTopBarTime.Text = now.ToString(format);
            TxtTopBarTime.Visibility = showTime ? Visibility.Visible : Visibility.Collapsed;

            string dateStr = FormatDate(now, s.TopBarDateOrder, s.TopBarDateStyle, s.TopBarMonthStyle, s.TopBarYearStyle);

            if (s.TopBarShowDayOfWeek)
            {
                string[] weekDays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
                string dayOfWeek = weekDays[(int)now.DayOfWeek];
                dateStr = s.TopBarDayOfWeekPosition == "DayOfWeek/Date"
                    ? $"{dayOfWeek} {dateStr}"
                    : $"{dateStr} {dayOfWeek}";
            }

            TxtTopBarDate.Text = dateStr;
            TxtTopBarDate.Visibility = showDate ? Visibility.Visible : Visibility.Collapsed;

            TxtTopBarDateSep.Visibility = (showTime && showDate) ? Visibility.Visible : Visibility.Collapsed;
        }

        public static string FormatDate(DateTime dt, string order, string dateStyle, string monthStyle, string yearStyle = "Full")
        {
            string dayPart = dateStyle == "NumberSuffix" ? GetDayWithSuffix(dt.Day) : dt.Day.ToString();
            string monthNum = dt.Month.ToString("00");
            string monthPart = monthStyle == "Abbr" ? GetMonthAbbr(dt.Month) : monthNum;
            string yearPart = yearStyle == "Short" ? (dt.Year % 100).ToString("00") : dt.Year.ToString();

            return order switch
            {
                "yyyy/dd/MM" => $"{yearPart}/{dayPart}/{monthPart}",
                "dd/MM/yyyy" => $"{dayPart}/{monthPart}/{yearPart}",
                "MM/dd/yyyy" => $"{monthPart}/{dayPart}/{yearPart}",
                _ => $"{yearPart}/{monthPart}/{dayPart}"
            };
        }

        private static string GetDayWithSuffix(int day)
        {
            if (day >= 11 && day <= 13) return day + "th";
            return (day % 10) switch
            {
                1 => day + "st",
                2 => day + "nd",
                3 => day + "rd",
                _ => day + "th"
            };
        }

        private static string GetMonthAbbr(int month)
        {
            return month switch
            {
                1 => "Jan", 2 => "Feb", 3 => "Mar", 4 => "Apr",
                5 => "May", 6 => "Jun", 7 => "Jul", 8 => "Aug",
                9 => "Sep", 10 => "Oct", 11 => "Nov", 12 => "Dec",
                _ => month.ToString()
            };
        }

        public void ShowUC0()
        {
            App.Database.Disconnect();
            var uc0 = new UserControl0();
            uc0.NavigateToDesktop += () => ShowUC1();
            MainContent.Content = uc0;
        }

        public void ShowUC1()
        {
            var uc1 = new UserControl1();
            uc1.NavigateToLock += () => ShowUC0();
            MainContent.Content = uc1;
        }

        public void EnterFullscreen(UserControl control)
        {
            MainContent.Margin = new Thickness(0, 0, 0, 0);
            TopBarBorder.Height = 4;
            TopBarBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0x00, 0x00, 0x00, 0x00));
            SetTopBarChildrenVisible(false);

            if (MainContent.Content is UserControl1 uc1)
            {
                uc1.BottomBarWrap.Height = 4;
                uc1.BottomBarWrap.Opacity = 0;
            }

            FullscreenHost.Content = control;
            FullscreenOverlay.Visibility = Visibility.Visible;
            FullscreenOverlay.Focus();
        }

        public void ExitFullscreen()
        {
            FullscreenOverlay.Visibility = Visibility.Collapsed;

            if (FullscreenHost.Content is UserControl2_1_f11 imgF11)
                imgF11.DisposeResources();
            else if (FullscreenHost.Content is UserControl2_2_f11 vidF11)
                vidF11.DisposeResources();

            FullscreenHost.Content = null;

            ApplyOpenSettings();

            if (MainContent.Content is UserControl1 uc1)
                uc1.ApplyBottomBarVisibilityMode();
        }

        private void OnTopBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = (WindowState == WindowState.Maximized)
                    ? WindowState.Normal : WindowState.Maximized;
                return;
            }
            if (WindowState == WindowState.Maximized)
            {
                var mousePos = e.GetPosition(this);
                var screenPos = PointToScreen(mousePos);

                var source = PresentationSource.FromVisual(this);
                double dpiScaleX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                double dpiScaleY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

                double ratioX = mousePos.X / ActualWidth;
                double ratioY = mousePos.Y / ActualHeight;
                WindowState = WindowState.Normal;
                Left = (screenPos.X / dpiScaleX) - _savedNormalBounds.Width * ratioX;
                Top = (screenPos.Y / dpiScaleY) - _savedNormalBounds.Height * ratioY;
                DragMove();
            }
            else
            {
                DragMove();
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaxRestoreClick(object sender, RoutedEventArgs e)
        {
            WindowState = (WindowState == WindowState.Maximized)
                ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyDisguise(OpenSettings s)
        {
            if (s.DisguiseEnabled)
            {
                if (!string.IsNullOrWhiteSpace(s.DisguiseAppName))
                    Title = s.DisguiseAppName;

                if (!string.IsNullOrEmpty(s.DisguiseIconPath) && System.IO.File.Exists(s.DisguiseIconPath))
                {
                    try
                    {
                        var bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(s.DisguiseIconPath);
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        Icon = bitmapImage;
                    }
                    catch
                    {
                        Icon = _originalIcon;
                    }
                }
            }
            else
            {
                Title = _originalTitle;
                Icon = _originalIcon;
            }
        }
    }
}
