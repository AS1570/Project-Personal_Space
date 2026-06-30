using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl6 : UserControl
    {
        private bool _isLoading;
        private string _audioQuickSelectFullPath = "";
        private string _lockWallpaperImageFullPath = "";
        private string _desktopWallpaperImageFullPath = "";
        private string _desktopWallpaperVideoFullPath = "";
        private string _disguiseIconFullPath = "";

        public UserControl6()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoading = true;

            InitThemeModeCombo();
            InitAccentPresets();

            InitTopBarModeCombo();
            InitTopBarTimeFormatCombo();
            InitTopBarDateOrderCombo();
            InitTopBarDateStyleCombo();
            InitTopBarMonthStyleCombo();
            InitTopBarYearStyleCombo();
            InitTopBarDayOfWeekPositionCombo();

            InitBottomBarAdjustModeCombo();
            InitBottomBarStyleCombo();
            InitBottomBarLayoutCombo();
            InitButton9AlignmentCombo();
            InitButton9TimeFormatCombo();
            InitButton9DateOrderCombo();
            InitButton9DateStyleCombo();
            InitButton9MonthStyleCombo();
            InitButton9YearStyleCombo();
            InitAudioQuickSelectModeCombo();
            InitLockWallpaperTypeCombo();
            InitDesktopWallpaperTypeCombo();
            InitDesktopTopBarModeCombo();
            InitDesktopBottomBarModeCombo();
            InitDesktopBottomBarStyleCombo();
            InitDesktopBottomBarLayoutCombo();

            LoadOpenSettings();
            LoadProgramSettings();

            WireUpEvents();

            RefreshFolderPathDisplay();

            _isLoading = false;
        }

        private static void InitCombo(ComboBox combo, List<(string Display, string Value)> items, string selectedValue)
        {
            combo.Items.Clear();
            foreach (var (display, value) in items)
                combo.Items.Add(new ComboBoxItem { Content = display, Tag = value, VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Stretch });
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && (string?)item.Tag == selectedValue)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private static string GetSelectedTag(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
                return tag;
            return string.Empty;
        }

        private void InitThemeModeCombo()
        {
            InitCombo(CmbThemeMode, new List<(string, string)>
            {
                ("手动", "Manual"),
                ("时间切换", "Auto"),
                ("跟随系统", "FollowSystem")
            }, "Manual");
        }

        private void InitAccentPresets()
        {
            WrapAccentPresets.Children.Clear();
            foreach (var hex in ThemeService.AccentPresets)
            {
                var btn = new Button
                {
                    Width = 24, Height = 24,
                    Margin = new Thickness(0, 0, 6, 6),
                    Cursor = Cursors.Hand,
                    Tag = hex,
                    Background = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!
                };
                btn.Click += OnAccentPresetClick;
                WrapAccentPresets.Children.Add(btn);
            }
        }

        private void InitTopBarModeCombo()
        {
            InitCombo(CmbTopBarMode, new List<(string, string)>
            {
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, "Docked");
        }

        private void InitTopBarTimeFormatCombo()
        {
            InitCombo(CmbTopBarTimeFormat, new List<(string, string)>
            {
                ("12 小时制", "12"),
                ("24 小时制", "24")
            }, "24");
        }

        private void InitTopBarDateOrderCombo()
        {
            InitCombo(CmbTopBarDateOrder, new List<(string, string)>
            {
                ("年/月/日", "yyyy/MM/dd"),
                ("年/日/月", "yyyy/dd/MM"),
                ("日/月/年", "dd/MM/yyyy"),
                ("月/日/年", "MM/dd/yyyy")
            }, "yyyy/MM/dd");
        }

        private void InitTopBarDateStyleCombo()
        {
            InitCombo(CmbTopBarDateStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("数字+英文后缀", "NumberSuffix")
            }, "Number");
        }

        private void InitTopBarMonthStyleCombo()
        {
            InitCombo(CmbTopBarMonthStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("英文缩写", "Abbr")
            }, "Number");
        }

        private void InitTopBarYearStyleCombo()
        {
            InitCombo(CmbTopBarYearStyle, new List<(string, string)>
            {
                ("XXXX（完整年份）", "Full"),
                ("XX（年份后两位）", "Short")
            }, "Full");
        }

        private void InitTopBarDayOfWeekPositionCombo()
        {
            InitCombo(CmbTopBarDayOfWeekPosition, new List<(string, string)>
            {
                ("日期/星期", "Date/DayOfWeek"),
                ("星期/日期", "DayOfWeek/Date")
            }, "Date/DayOfWeek");
        }

        private void InitBottomBarAdjustModeCombo()
        {
            InitCombo(CmbBottomBarAdjustMode, new List<(string, string)>
            {
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, "Docked");
        }

        private void InitBottomBarStyleCombo()
        {
            InitCombo(CmbBottomBarStyle, new List<(string, string)>
            {
                ("悬浮式", "Floating"),
                ("底部联通式", "Connected")
            }, "Floating");
        }

        private void InitBottomBarLayoutCombo()
        {
            InitCombo(CmbBottomBarLayout, new List<(string, string)>
            {
                ("中心", "Center"),
                ("左右两侧", "Full"),
                ("中心+右侧", "CenterRight")
            }, "Center");
        }

        private void InitButton9AlignmentCombo()
        {
            InitCombo(CmbButton9Alignment, new List<(string, string)>
            {
                ("左侧停靠", "Left"),
                ("中心", "Center"),
                ("右侧停靠", "Right")
            }, "Center");
        }

        private void InitButton9TimeFormatCombo()
        {
            InitCombo(CmbButton9TimeFormat, new List<(string, string)>
            {
                ("12 小时制", "12"),
                ("24 小时制", "24")
            }, "24");
        }

        private void InitButton9DateOrderCombo()
        {
            InitCombo(CmbButton9DateOrder, new List<(string, string)>
            {
                ("年/月/日", "yyyy/MM/dd"),
                ("年/日/月", "yyyy/dd/MM"),
                ("日/月/年", "dd/MM/yyyy"),
                ("月/日/年", "MM/dd/yyyy")
            }, "yyyy/MM/dd");
        }

        private void InitButton9DateStyleCombo()
        {
            InitCombo(CmbButton9DateStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("数字+英文后缀", "NumberSuffix")
            }, "Number");
        }

        private void InitButton9MonthStyleCombo()
        {
            InitCombo(CmbButton9MonthStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("英文缩写", "Abbr")
            }, "Number");
        }

        private void InitButton9YearStyleCombo()
        {
            InitCombo(CmbButton9YearStyle, new List<(string, string)>
            {
                ("XXXX（完整年份）", "Full"),
                ("XX（年份后两位）", "Short")
            }, "Full");
        }

        private void InitAudioQuickSelectModeCombo()
        {
            InitCombo(CmbAudioQuickSelectMode, new List<(string, string)>
            {
                ("单曲循环", "Single"),
                ("列表循环", "List")
            }, "Single");
        }

        private void InitLockWallpaperTypeCombo()
        {
            InitCombo(CmbLockWallpaperType, new List<(string, string)>
            {
                ("无", "None"),
                ("纯色", "SolidColor"),
                ("图片", "Image")
            }, "None");
        }

        private void InitDesktopWallpaperTypeCombo()
        {
            InitCombo(CmbDesktopWallpaperType, new List<(string, string)>
            {
                ("无", "None"),
                ("纯色", "SolidColor"),
                ("图片", "Image"),
                ("视频", "Video")
            }, "None");
        }

        private void InitDesktopTopBarModeCombo()
        {
            InitCombo(CmbDesktopTopBarMode, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, "Original");
        }

        private void InitDesktopBottomBarModeCombo()
        {
            InitCombo(CmbDesktopBottomBarMode, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, "Original");
        }

        private void InitDesktopBottomBarStyleCombo()
        {
            InitCombo(CmbDesktopBottomBarStyle, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("悬浮式", "Floating"),
                ("底部联通式", "Connected")
            }, "Original");
        }

        private void InitDesktopBottomBarLayoutCombo()
        {
            InitCombo(CmbDesktopBottomBarLayout, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("中心", "Center"),
                ("左右两侧", "Full"),
                ("中心+右侧", "CenterRight")
            }, "Original");
        }

        private void LoadOpenSettings()
        {
            var s = OpenSettingsService.Instance.Current;

            InitCombo(CmbTopBarMode, new List<(string, string)>
            {
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, s.TopBarMode);

            ChkTopBarShowTime.IsChecked = s.TopBarShowTime;
            ChkTopBarShowDate.IsChecked = s.TopBarShowDate;
            ChkTopBarShowLockButton.IsChecked = s.TopBarShowLockButton;

            InitCombo(CmbTopBarTimeFormat, new List<(string, string)>
            {
                ("12 小时制", "12"),
                ("24 小时制", "24")
            }, s.TopBarTime24Hour ? "24" : "12");

            ChkTopBarTimeShowSeconds.IsChecked = s.TopBarTimeShowSeconds;

            InitCombo(CmbTopBarDateOrder, new List<(string, string)>
            {
                ("年/月/日", "yyyy/MM/dd"),
                ("年/日/月", "yyyy/dd/MM"),
                ("日/月/年", "dd/MM/yyyy"),
                ("月/日/年", "MM/dd/yyyy")
            }, s.TopBarDateOrder);

            InitCombo(CmbTopBarDateStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("数字+英文后缀", "NumberSuffix")
            }, s.TopBarDateStyle);

            InitCombo(CmbTopBarMonthStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("英文缩写", "Abbr")
            }, s.TopBarMonthStyle);

            InitCombo(CmbTopBarYearStyle, new List<(string, string)>
            {
                ("XXXX（完整年份）", "Full"),
                ("XX（年份后两位）", "Short")
            }, s.TopBarYearStyle);

            ChkTopBarShowDayOfWeek.IsChecked = s.TopBarShowDayOfWeek;

            InitCombo(CmbTopBarDayOfWeekPosition, new List<(string, string)>
            {
                ("日期/星期", "Date/DayOfWeek"),
                ("星期/日期", "DayOfWeek/Date")
            }, s.TopBarDayOfWeekPosition);

            InitCombo(CmbLockWallpaperType, new List<(string, string)>
            {
                ("无", "None"),
                ("纯色", "SolidColor"),
                ("图片", "Image")
            }, s.LockScreenWallpaperType);

            TxtLockWallpaperColor.Text = s.LockScreenWallpaperColor;
            _lockWallpaperImageFullPath = s.LockScreenWallpaperImagePath ?? "";
            TxtLockWallpaperImage.Text = string.IsNullOrEmpty(_lockWallpaperImageFullPath)
                ? "" : System.IO.Path.GetFileName(_lockWallpaperImageFullPath);

            SldExtraScale.Value = s.ExtraScale;
            TxtExtraScaleLabel.Text = $"{(int)s.ExtraScale}%";

            UpdateLockTimeColorButtons(s.LockScreenTimeColor);
            UpdateLockDateColorButtons(s.LockScreenDateColor);

            TxtPasswordHint.Text = s.PasswordHint;

            UpdateLockWallpaperVisibility();
            UpdateLockColorPreview();
            UpdateDesktopWallpaperVisibility();
            UpdateDesktopColorPreview();

            InitCombo(CmbThemeMode, new List<(string, string)>
            {
                ("手动", "Manual"),
                ("时间切换", "Auto"),
                ("跟随系统", "FollowSystem")
            }, s.ThemeMode);

            UpdateThemeVariantButtons(s.ThemeVariant);
            TxtThemeLightStartTime.Text = s.ThemeLightStartTime;
            TxtThemeDarkStartTime.Text = s.ThemeDarkStartTime;
            TxtAccentColor.Text = s.AccentColor;
            UpdateThemeModeVisibility();
            UpdateAccentColorPreview();
            UpdateCurrentThemeLabel();

            ChkDisguiseEnabled.IsChecked = s.DisguiseEnabled;
            TxtDisguiseAppName.Text = s.DisguiseAppName ?? "";
            _disguiseIconFullPath = s.DisguiseIconPath ?? "";
            TxtDisguiseIconPath.Text = string.IsNullOrEmpty(_disguiseIconFullPath)
                ? "" : System.IO.Path.GetFileName(_disguiseIconFullPath);
            UpdateDisguisePanelVisibility();
        }

        private void LoadProgramSettings()
        {
            var s = ProgramSettingsService.Instance.Current;

            ChkButton8ShowSongName.IsChecked = s.Button8ShowSongName;
            ChkButton8Show.IsChecked = s.Button8Show;
            ChkButton9Show.IsChecked = s.Button9Show;
            ChkButton10Show.IsChecked = s.Button10Show;

            InitCombo(CmbButton9Alignment, new List<(string, string)>
            {
                ("左侧停靠", "Left"),
                ("中心", "Center"),
                ("右侧停靠", "Right")
            }, s.Button9Alignment);

            ChkButton9ShowTime.IsChecked = s.Button9ShowTime;

            InitCombo(CmbButton9TimeFormat, new List<(string, string)>
            {
                ("12 小时制", "12"),
                ("24 小时制", "24")
            }, s.Button9Time24Hour ? "24" : "12");

            ChkButton9TimeShowSeconds.IsChecked = s.Button9TimeShowSeconds;

            ChkButton9ShowDate.IsChecked = s.Button9ShowDate;

            InitCombo(CmbButton9DateOrder, new List<(string, string)>
            {
                ("年/月/日", "yyyy/MM/dd"),
                ("年/日/月", "yyyy/dd/MM"),
                ("日/月/年", "dd/MM/yyyy"),
                ("月/日/年", "MM/dd/yyyy")
            }, s.Button9DateOrder);

            InitCombo(CmbButton9DateStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("数字+英文后缀", "NumberSuffix")
            }, s.Button9DateStyle);

            InitCombo(CmbButton9MonthStyle, new List<(string, string)>
            {
                ("数字", "Number"),
                ("英文缩写", "Abbr")
            }, s.Button9MonthStyle);

            InitCombo(CmbButton9YearStyle, new List<(string, string)>
            {
                ("XXXX（完整年份）", "Full"),
                ("XX（年份后两位）", "Short")
            }, s.Button9YearStyle);

            InitCombo(CmbBottomBarAdjustMode, new List<(string, string)>
            {
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, s.BottomBarVisibility);

            _audioQuickSelectFullPath = s.AudioQuickSelectPath ?? "";
            TxtAudioQuickSelectPath.Text = string.IsNullOrEmpty(_audioQuickSelectFullPath)
                ? ""
                : System.IO.Path.GetFileName(_audioQuickSelectFullPath);
            InitCombo(CmbAudioQuickSelectMode, new List<(string, string)>
            {
                ("单曲循环", "Single"),
                ("列表循环", "List")
            }, s.AudioQuickSelectMode);

            SldAudioQuickSelectVolume.Value = s.AudioQuickSelectVolume;
            TxtAudioQuickSelectVolumeLabel.Text = $"{(int)(s.AudioQuickSelectVolume * 100)}%";

            InitCombo(CmbBottomBarStyle, new List<(string, string)>
            {
                ("悬浮式", "Floating"),
                ("底部联通式", "Connected")
            }, s.BottomBarStyle);

            InitCombo(CmbBottomBarLayout, new List<(string, string)>
            {
                ("中心", "Center"),
                ("左右两侧", "Full"),
                ("中心+右侧", "CenterRight")
            }, s.BottomBarLayout);

            InitCombo(CmbDesktopWallpaperType, new List<(string, string)>
            {
                ("无", "None"),
                ("纯色", "SolidColor"),
                ("图片", "Image"),
                ("视频", "Video")
            }, s.DesktopWallpaperType);

            TxtDesktopWallpaperColor.Text = s.DesktopWallpaperColor;
            _desktopWallpaperImageFullPath = s.DesktopWallpaperImagePath ?? "";
            TxtDesktopWallpaperImage.Text = string.IsNullOrEmpty(_desktopWallpaperImageFullPath)
                ? "" : System.IO.Path.GetFileName(_desktopWallpaperImageFullPath);
            _desktopWallpaperVideoFullPath = s.DesktopWallpaperVideoPath ?? "";
            TxtDesktopWallpaperVideo.Text = string.IsNullOrEmpty(_desktopWallpaperVideoFullPath)
                ? "" : System.IO.Path.GetFileName(_desktopWallpaperVideoFullPath);

            InitCombo(CmbDesktopTopBarMode, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, s.DesktopTopBarMode);

            InitCombo(CmbDesktopBottomBarMode, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("展开  （显示，限位）", "Docked"),
                ("悬浮  （显示，不限位）", "Floating"),
                ("自动隐藏  （自动隐藏，不限位）", "AutoHide")
            }, s.DesktopBottomBarMode);

            InitCombo(CmbDesktopBottomBarStyle, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("悬浮式", "Floating"),
                ("底部联通式", "Connected")
            }, s.DesktopBottomBarStyle);

            InitCombo(CmbDesktopBottomBarLayout, new List<(string, string)>
            {
                ("原选项（使用上方设置）", "Original"),
                ("中心", "Center"),
                ("左右两侧", "Full"),
                ("中心+右侧", "CenterRight")
            }, s.DesktopBottomBarLayout);

            ChkDesktopButton8ShowSongName.IsChecked = s.DesktopButton8ShowSongName;
            ChkDesktopButton8Show.IsChecked = s.DesktopButton8Show;
            ChkDesktopButton9Show.IsChecked = s.DesktopButton9Show;
            ChkDesktopButton10Show.IsChecked = s.DesktopButton10Show;

            UpdateDesktopWallpaperVisibility();
            UpdateDesktopColorPreview();
        }

        private void WireUpEvents()
        {
            CmbThemeMode.SelectionChanged += (_, _) => { if (!_isLoading) { UpdateThemeModeVisibility(); SaveThemeSettings(); } };
            TxtThemeLightStartTime.TextChanged += (_, _) => { if (!_isLoading) SaveThemeSettings(); };
            TxtThemeDarkStartTime.TextChanged += (_, _) => { if (!_isLoading) SaveThemeSettings(); };
            TxtAccentColor.TextChanged += (_, _) => { if (!_isLoading) { UpdateAccentColorPreview(); SaveThemeSettings(); } };

            CmbTopBarMode.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowTime.Checked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowTime.Unchecked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowDate.Checked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowDate.Unchecked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowLockButton.Checked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowLockButton.Unchecked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarTimeFormat.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarTimeShowSeconds.Checked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarTimeShowSeconds.Unchecked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarDateOrder.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarDateStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarMonthStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarYearStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowDayOfWeek.Checked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            ChkTopBarShowDayOfWeek.Unchecked += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
            CmbTopBarDayOfWeekPosition.SelectionChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };

            CmbBottomBarAdjustMode.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbBottomBarStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbBottomBarLayout.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton8ShowSongName.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton8ShowSongName.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton8Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton8Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton10Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton10Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9Alignment.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9ShowTime.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9ShowTime.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9TimeFormat.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9TimeShowSeconds.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9TimeShowSeconds.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9ShowDate.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkButton9ShowDate.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9DateOrder.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9DateStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9MonthStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbButton9YearStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbAudioQuickSelectMode.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            SldAudioQuickSelectVolume.ValueChanged += (_, _) =>
            {
                TxtAudioQuickSelectVolumeLabel.Text = $"{(int)(SldAudioQuickSelectVolume.Value * 100)}%";
                if (!_isLoading) SaveProgramSettings();
            };

            CmbLockWallpaperType.SelectionChanged += (_, _) => { if (!_isLoading) { UpdateLockWallpaperVisibility(); SaveOpenSettings(); } };
            TxtLockWallpaperColor.TextChanged += (_, _) =>
            {
                if (!_isLoading)
                {
                    UpdateLockColorPreview();
                    SaveOpenSettings();
                }
            };
            CmbDesktopWallpaperType.SelectionChanged += (_, _) => { if (!_isLoading) { UpdateDesktopWallpaperVisibility(); SaveProgramSettings(); } };
            TxtDesktopWallpaperColor.TextChanged += (_, _) =>
            {
                if (!_isLoading)
                {
                    UpdateDesktopColorPreview();
                    SaveProgramSettings();
                }
            };
            SldExtraScale.ValueChanged += (_, _) =>
            {
                TxtExtraScaleLabel.Text = $"{(int)SldExtraScale.Value}%";
                if (!_isLoading) SaveOpenSettings();
            };
            CmbDesktopTopBarMode.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbDesktopBottomBarMode.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbDesktopBottomBarStyle.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            CmbDesktopBottomBarLayout.SelectionChanged += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton8ShowSongName.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton8ShowSongName.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton8Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton8Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton9Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton9Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton10Show.Checked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };
            ChkDesktopButton10Show.Unchecked += (_, _) => { if (!_isLoading) SaveProgramSettings(); };

            ChkDisguiseEnabled.Checked += (_, _) => { if (!_isLoading) { UpdateDisguisePanelVisibility(); SaveOpenSettings(); } };
            ChkDisguiseEnabled.Unchecked += (_, _) => { if (!_isLoading) { UpdateDisguisePanelVisibility(); SaveOpenSettings(); } };
            TxtDisguiseAppName.TextChanged += (_, _) => { if (!_isLoading) SaveOpenSettings(); };
        }

        private void SaveOpenSettings()
        {
            var s = OpenSettingsService.Instance.Current;
            s.TopBarMode = GetSelectedTag(CmbTopBarMode);
            s.TopBarShowTime = ChkTopBarShowTime.IsChecked == true;
            s.TopBarShowDate = ChkTopBarShowDate.IsChecked == true;
            s.TopBarShowLockButton = ChkTopBarShowLockButton.IsChecked == true;
            s.TopBarTime24Hour = GetSelectedTag(CmbTopBarTimeFormat) == "24";
            s.TopBarTimeShowSeconds = ChkTopBarTimeShowSeconds.IsChecked == true;
            s.TopBarDateOrder = GetSelectedTag(CmbTopBarDateOrder);
            s.TopBarDateStyle = GetSelectedTag(CmbTopBarDateStyle);
            s.TopBarMonthStyle = GetSelectedTag(CmbTopBarMonthStyle);
            s.TopBarYearStyle = GetSelectedTag(CmbTopBarYearStyle);
            s.TopBarShowDayOfWeek = ChkTopBarShowDayOfWeek.IsChecked == true;
            s.TopBarDayOfWeekPosition = GetSelectedTag(CmbTopBarDayOfWeekPosition);
            s.LockScreenWallpaperType = GetSelectedTag(CmbLockWallpaperType);
            s.LockScreenWallpaperColor = TxtLockWallpaperColor.Text;
            s.LockScreenWallpaperImagePath = _lockWallpaperImageFullPath;
            s.ExtraScale = SldExtraScale.Value;
            s.DisguiseEnabled = ChkDisguiseEnabled.IsChecked == true;
            s.DisguiseAppName = TxtDisguiseAppName.Text ?? "";
            s.DisguiseIconPath = _disguiseIconFullPath ?? "";
            OpenSettingsService.Instance.Save();

            RefreshDesktopWallpaperIfVisible();
        }

        private void SaveLockTimeColor()
        {
            var s = OpenSettingsService.Instance.Current;
            OpenSettingsService.Instance.Save();
        }

        private void SaveProgramSettings()
        {
            var s = ProgramSettingsService.Instance.Current;
            s.BottomBarStyle = GetSelectedTag(CmbBottomBarStyle);
            s.BottomBarLayout = GetSelectedTag(CmbBottomBarLayout);
            s.BottomBarVisibility = GetSelectedTag(CmbBottomBarAdjustMode);
            s.Button8ShowSongName = ChkButton8ShowSongName.IsChecked == true;
            s.Button8Show = ChkButton8Show.IsChecked == true;
            s.Button9Show = ChkButton9Show.IsChecked == true;
            s.Button10Show = ChkButton10Show.IsChecked == true;
            s.Button9Alignment = GetSelectedTag(CmbButton9Alignment);
            s.Button9ShowTime = ChkButton9ShowTime.IsChecked == true;
            s.Button9Time24Hour = GetSelectedTag(CmbButton9TimeFormat) == "24";
            s.Button9TimeShowSeconds = ChkButton9TimeShowSeconds.IsChecked == true;
            s.Button9ShowDate = ChkButton9ShowDate.IsChecked == true;
            s.Button9DateOrder = GetSelectedTag(CmbButton9DateOrder);
            s.Button9DateStyle = GetSelectedTag(CmbButton9DateStyle);
            s.Button9MonthStyle = GetSelectedTag(CmbButton9MonthStyle);
            s.Button9YearStyle = GetSelectedTag(CmbButton9YearStyle);
            s.AudioQuickSelectPath = _audioQuickSelectFullPath;
            s.AudioQuickSelectMode = GetSelectedTag(CmbAudioQuickSelectMode);
            s.AudioQuickSelectVolume = SldAudioQuickSelectVolume.Value;
            s.DesktopWallpaperType = GetSelectedTag(CmbDesktopWallpaperType);
            s.DesktopWallpaperColor = TxtDesktopWallpaperColor.Text;
            s.DesktopWallpaperImagePath = _desktopWallpaperImageFullPath;
            s.DesktopWallpaperVideoPath = _desktopWallpaperVideoFullPath;
            s.DesktopTopBarMode = GetSelectedTag(CmbDesktopTopBarMode);
            s.DesktopBottomBarMode = GetSelectedTag(CmbDesktopBottomBarMode);
            s.DesktopBottomBarStyle = GetSelectedTag(CmbDesktopBottomBarStyle);
            s.DesktopBottomBarLayout = GetSelectedTag(CmbDesktopBottomBarLayout);
            s.DesktopButton8ShowSongName = ChkDesktopButton8ShowSongName.IsChecked == true;
            s.DesktopButton8Show = ChkDesktopButton8Show.IsChecked == true;
            s.DesktopButton9Show = ChkDesktopButton9Show.IsChecked == true;
            s.DesktopButton10Show = ChkDesktopButton10Show.IsChecked == true;
            ProgramSettingsService.Instance.Save();

            RefreshDesktopWallpaperIfVisible();
        }

        private void RefreshDesktopWallpaperIfVisible()
        {
            DependencyObject? current = this.Parent;
            while (current != null)
            {
                if (current is UserControl1 uc1)
                {
                    uc1.ApplyDesktopWallpaper();
                    break;
                }
                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }
        }

        private void BtnAudioQuickSelectBrowse_Click(object sender, RoutedEventArgs e)
        {
            var popupContent = BuildAudioFileListPopup();
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = BtnAudioQuickSelectBrowse,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                HorizontalOffset = 0,
                VerticalOffset = 4,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = popupContent
            };
            popup.IsOpen = true;
        }

        private Border BuildAudioFileListPopup()
        {
            var panel = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                MinWidth = 380,
                MaxHeight = 400,
                BorderThickness = new Thickness(1)
            };
            panel.SetResourceReference(Border.BackgroundProperty, "BgPanel");
            panel.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var stack = new StackPanel();

            var title = new TextBlock
            {
                Text = "选择音频文件",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            stack.Children.Add(title);

            var scroll = new ScrollViewer
            {
                MaxHeight = 320,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var listPanel = new StackPanel();

            string programFileDir = App.FileManager.ProgramFileDir;
            string[] audioFiles = Array.Empty<string>();

            if (Directory.Exists(programFileDir))
            {
                audioFiles = Directory.GetFiles(programFileDir, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        var ext = Path.GetExtension(f).ToLowerInvariant();
                        return ext is ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" or ".wma" or ".m4a";
                    })
                    .OrderBy(f => f)
                    .ToArray();
            }

            if (audioFiles.Length == 0)
            {
                var noFiles = new TextBlock
                {
                    Text = "ProgramFile 中没有音乐文件",
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 16, 0, 16)
                };
                noFiles.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
                listPanel.Children.Add(noFiles);
            }
            else
            {
                foreach (var filePath in audioFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string dirName = Path.GetDirectoryName(filePath) ?? "";
                    string relative = "";
                    if (dirName != programFileDir)
                    {
                        int idx = dirName.IndexOf(programFileDir, StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                            relative = dirName.Substring(idx + programFileDir.Length).TrimStart('\\', '/') + "\\";
                    }

                    var row = new Border
                    {
                        Background = Brushes.Transparent,
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(10, 6, 10, 6),
                        Margin = new Thickness(0, 0, 0, 2),
                        Cursor = Cursors.Hand,
                        Tag = filePath
                    };

                    var rowGrid = new Grid();
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var nameText = new TextBlock
                    {
                        Text = fileName,
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 0, 0, 2)
                    };
                    nameText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                    Grid.SetRow(nameText, 0);

                    var pathText = new TextBlock
                    {
                        Text = relative + fileName,
                        FontSize = 11,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    pathText.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
                    Grid.SetRow(pathText, 1);

                    rowGrid.RowDefinitions.Add(new RowDefinition());
                    rowGrid.RowDefinitions.Add(new RowDefinition());
                    rowGrid.Children.Add(nameText);
                    rowGrid.Children.Add(pathText);
                    row.Child = rowGrid;

                    row.MouseLeftButtonDown += (s, args) =>
                    {
                        _audioQuickSelectFullPath = filePath;
                        TxtAudioQuickSelectPath.Text = Path.GetFileName(filePath);
                        if (!_isLoading) SaveProgramSettings();
                        if (panel.Parent is System.Windows.Controls.Primitives.Popup pp)
                            pp.IsOpen = false;
                    };
                    row.MouseEnter += (s, args) =>
                    {
                        if (s is Border bd)
                            bd.SetResourceReference(Border.BackgroundProperty, "BgHover");
                    };
                    row.MouseLeave += (s, args) =>
                    {
                        if (s is Border bd && bd.Tag is string)
                            bd.Background = Brushes.Transparent;
                    };

                    listPanel.Children.Add(row);
                }
            }

            scroll.Content = listPanel;
            stack.Children.Add(scroll);

            var closeBtn = new Button
            {
                Content = "关闭",
                Width = 80,
                Height = 30,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            closeBtn.SetResourceReference(Button.BackgroundProperty, "BgHover");
            closeBtn.SetResourceReference(Button.ForegroundProperty, "TextPrimary");
            closeBtn.Click += (s, args) =>
            {
                if (panel.Parent is System.Windows.Controls.Primitives.Popup pp)
                    pp.IsOpen = false;
            };
            stack.Children.Add(closeBtn);

            panel.Child = stack;
            return panel;
        }

        private void UpdateLockColorPreview()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TxtLockWallpaperColor.Text);
                LockColorPreview.Color = color;
            }
            catch { }
        }

        private void UpdateDesktopColorPreview()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TxtDesktopWallpaperColor.Text);
                DesktopColorPreview.Color = color;
            }
            catch { }
        }

        private void OnLockColorPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                TxtLockWallpaperColor.Text = colorHex;
                UpdateLockColorPreview();
                if (!_isLoading) SaveOpenSettings();
            }
        }

        private void OnDesktopColorPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                TxtDesktopWallpaperColor.Text = colorHex;
                UpdateDesktopColorPreview();
                if (!_isLoading) SaveProgramSettings();
            }
        }

        private void OnScalePresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag && double.TryParse(tag, out double val))
            {
                SldExtraScale.Value = val;
            }
        }

        private void UpdateLockWallpaperVisibility()
        {
            var type = GetSelectedTag(CmbLockWallpaperType);
            GridLockColor.Visibility = type == "SolidColor" ? Visibility.Visible : Visibility.Collapsed;
            GridLockImage.Visibility = type == "Image" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateDesktopWallpaperVisibility()
        {
            var type = GetSelectedTag(CmbDesktopWallpaperType);
            GridDesktopColor.Visibility = type == "SolidColor" ? Visibility.Visible : Visibility.Collapsed;
            GridDesktopImage.Visibility = type == "Image" ? Visibility.Visible : Visibility.Collapsed;
            GridDesktopVideo.Visibility = type == "Video" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnLockWallpaperBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*",
                Title = "选择锁屏壁纸图片"
            };
            if (dlg.ShowDialog() == true)
            {
                _lockWallpaperImageFullPath = dlg.FileName;
                TxtLockWallpaperImage.Text = System.IO.Path.GetFileName(dlg.FileName);
                if (!_isLoading) SaveOpenSettings();
            }
        }

        private void BtnDesktopWallpaperImageBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|所有文件|*.*",
                Title = "选择桌面壁纸图片"
            };
            if (dlg.ShowDialog() == true)
            {
                _desktopWallpaperImageFullPath = dlg.FileName;
                TxtDesktopWallpaperImage.Text = System.IO.Path.GetFileName(dlg.FileName);
                if (!_isLoading) SaveProgramSettings();
            }
        }

        private void BtnDesktopWallpaperVideoBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "视频文件|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|所有文件|*.*",
                Title = "选择桌面壁纸视频"
            };
            if (dlg.ShowDialog() == true)
            {
                _desktopWallpaperVideoFullPath = dlg.FileName;
                TxtDesktopWallpaperVideo.Text = System.IO.Path.GetFileName(dlg.FileName);
                if (!_isLoading) SaveProgramSettings();
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = PwdOldPassword.Password;
            string newPassword = PwdNewPassword.Password;
            string confirmPassword = PwdConfirmPassword.Password;

            if (string.IsNullOrEmpty(oldPassword))
            {
                System.Windows.MessageBox.Show("请输入当前密码", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                System.Windows.MessageBox.Show("请输入新密码", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (newPassword != confirmPassword)
            {
                System.Windows.MessageBox.Show("两次输入的新密码不一致", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (newPassword == oldPassword)
            {
                System.Windows.MessageBox.Show("新密码不能与当前密码相同", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            bool result = App.Database.ChangePassword(oldPassword, newPassword);
            if (result)
            {
                System.Windows.MessageBox.Show("密码修改成功！新密码将在下次锁屏后生效。", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                PwdOldPassword.Clear();
                PwdNewPassword.Clear();
                PwdConfirmPassword.Clear();
            }
            else
            {
                System.Windows.MessageBox.Show("密码修改失败，请确认当前密码正确且数据库已连接。", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void BtnSavePasswordHint_Click(object sender, RoutedEventArgs e)
        {
            string hint = TxtPasswordHint.Text;
            var openSettings = OpenSettingsService.Instance.Current;
            openSettings.PasswordHint = hint;
            OpenSettingsService.Instance.Save();

            if (App.Database.IsConnected)
            {
                App.Database.SetSetting("PasswordHint", hint);
            }

            System.Windows.MessageBox.Show("密码提示词已保存", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void RefreshFolderPathDisplay()
        {
            TxtCurrentFolderPath.Text = App.FileManager.ProgramFileDir;
            TxtNewFolderPath.Text = "";
            TxtDefaultFolderHint.Text = $"默认文件夹地址：{App.FileManager.DefaultProgramFileDir}";
        }

        private void OnThemeVariantClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string variant)
            {
                var ts = ThemeService.Instance;
                ts.ThemeVariant = variant;
                ts.RefreshMode();
                UpdateThemeVariantButtons(variant);
                UpdateCurrentThemeLabel();
            }
        }

        private void OnAccentPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string hex)
            {
                TxtAccentColor.Text = hex;
                UpdateAccentColorPreview();
                if (!_isLoading) SaveThemeSettings();
            }
        }

        private void SaveThemeSettings()
        {
            var ts = ThemeService.Instance;
            var s = OpenSettingsService.Instance.Current;
            s.ThemeMode = GetSelectedTag(CmbThemeMode);
            s.ThemeLightStartTime = TxtThemeLightStartTime.Text;
            s.ThemeDarkStartTime = TxtThemeDarkStartTime.Text;
            s.AccentColor = TxtAccentColor.Text;
            ts.RefreshMode();
            UpdateCurrentThemeLabel();
        }

        private void UpdateThemeModeVisibility()
        {
            var mode = GetSelectedTag(CmbThemeMode);
            PanelManualTheme.Visibility = mode == "Manual" ? Visibility.Visible : Visibility.Collapsed;
            PanelAutoTheme.Visibility = mode == "Auto" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateThemeVariantButtons(string activeVariant)
        {
            bool isLight = activeVariant == "Light";

            var lightStyle = new Style(typeof(Button));
            lightStyle.Setters.Add(new Setter(Button.BackgroundProperty,
                isLight ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeService.Instance.AccentColor)!)
                        : Application.Current.Resources["BgSurface"]));
            lightStyle.Setters.Add(new Setter(Button.ForegroundProperty,
                isLight ? Brushes.White : Application.Current.Resources["TextSecondary"]));
            lightStyle.Setters.Add(new Setter(Button.BorderBrushProperty,
                isLight ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeService.Instance.AccentColor)!)
                        : Application.Current.Resources["BorderColor"]));
            BtnThemeLight.Style = lightStyle;

            var darkStyle = new Style(typeof(Button));
            darkStyle.Setters.Add(new Setter(Button.BackgroundProperty,
                !isLight ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeService.Instance.AccentColor)!)
                         : Application.Current.Resources["BgSurface"]));
            darkStyle.Setters.Add(new Setter(Button.ForegroundProperty,
                !isLight ? Brushes.White : Application.Current.Resources["TextSecondary"]));
            darkStyle.Setters.Add(new Setter(Button.BorderBrushProperty,
                !isLight ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(ThemeService.Instance.AccentColor)!)
                         : Application.Current.Resources["BorderColor"]));
            BtnThemeDark.Style = darkStyle;
        }

        private void UpdateCurrentThemeLabel()
        {
            var variant = ThemeService.Instance.GetEffectiveVariant();
            RunCurrentThemeLabel.Text = variant == "Light" ? "☀ 浅色" : "🌙 深色";
        }

        private void UpdateAccentColorPreview()
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TxtAccentColor.Text);
                AccentColorPreview.Color = color;
            }
            catch { }
        }

        private void BtnFolderPreview_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择文件夹位置",
                ShowNewFolderButton = true
            };
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtNewFolderPath.Text = dlg.SelectedPath;
            }
        }

        private void BtnChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            string newPath = TxtNewFolderPath.Text.Trim();
            if (string.IsNullOrEmpty(newPath))
            {
                System.Windows.MessageBox.Show("请输入新文件夹路径", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (!App.Database.IsConnected)
            {
                System.Windows.MessageBox.Show("数据库未连接，无法保存文件夹地址", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            string oldDir = App.FileManager.ProgramFileDir;

            if (oldDir == newPath)
            {
                System.Windows.MessageBox.Show("新文件夹与当前文件夹相同", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            var migrationPopup = BuildMigrationOptionPopup(newPath, oldDir);
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                PlacementTarget = BtnChangeFolder,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Center,
                StaysOpen = false,
                AllowsTransparency = true,
                Child = migrationPopup
            };
            popup.IsOpen = true;
        }

        private Border BuildMigrationOptionPopup(string newPath, string oldDir)
        {
            var innerPanel = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24, 20, 24, 20),
                MinWidth = 420,
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 20,
                    ShadowDepth = 4,
                    Opacity = 0.5
                }
            };
            innerPanel.SetResourceReference(Border.BackgroundProperty, "BgPanel");
            innerPanel.SetResourceReference(Border.BorderBrushProperty, "BorderColor");

            var outerPanel = new Border
            {
                Background = Brushes.Transparent,
                ClipToBounds = true,
                CornerRadius = new CornerRadius(14),
                Child = innerPanel
            };

            var stack = new StackPanel();

            var titleText = new TextBlock
            {
                Text = "选择迁移方式",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            stack.Children.Add(titleText);

            var newPathText = new TextBlock
            {
                Text = $"新文件夹：{newPath}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            newPathText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            stack.Children.Add(newPathText);

            var oldPathText = new TextBlock
            {
                Text = $"原文件夹：{oldDir}",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 16),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            oldPathText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");
            stack.Children.Add(oldPathText);

            var btnMigrate = new Button
            {
                Content = "迁移（复制到新文件夹并删除原文件夹）",
                Height = 40,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };
            btnMigrate.SetResourceReference(Button.BackgroundProperty, "Accent");
            btnMigrate.SetResourceReference(Button.ForegroundProperty, "White");
            btnMigrate.Click += (s, args) =>
            {
                if (outerPanel.Parent is System.Windows.Controls.Primitives.Popup pp) pp.IsOpen = false;
                var result = System.Windows.MessageBox.Show(
                    $"确认迁移文件？\n原文件夹将被复制到新文件夹后删除。",
                    "二次确认 - 迁移",
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.OK) return;

                App.FileManager.ChangeFolder(newPath);
                var (success, errors) = App.FileManager.MigrateFiles(oldDir, newPath, deleteOldDir: true);
                if (errors.Count > 0)
                    System.Windows.MessageBox.Show(string.Join("\n", errors), "迁移结果", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                else
                    System.Windows.MessageBox.Show("文件迁移完成", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                RefreshFolderPathDisplay();
            };
            stack.Children.Add(btnMigrate);

            var btnCopy = new Button
            {
                Content = "只复制（保留原文件夹）",
                Height = 40,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 8)
            };
            btnCopy.SetResourceReference(Button.BackgroundProperty, "BgHover");
            btnCopy.SetResourceReference(Button.ForegroundProperty, "TextPrimary");
            btnCopy.Click += (s, args) =>
            {
                if (outerPanel.Parent is System.Windows.Controls.Primitives.Popup pp) pp.IsOpen = false;
                var result = System.Windows.MessageBox.Show(
                    $"确认复制文件到新文件夹？\n原文件夹将保留。",
                    "二次确认 - 复制",
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.OK) return;

                App.FileManager.ChangeFolder(newPath);
                 var (success, errors) = App.FileManager.MigrateFiles(oldDir, newPath, deleteOldDir: false);
                 if (errors.Count > 0)
                     System.Windows.MessageBox.Show(string.Join("\n", errors), "复制结果", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                else
                    System.Windows.MessageBox.Show("文件复制完成，原文件夹已保留", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                RefreshFolderPathDisplay();
            };
            stack.Children.Add(btnCopy);

            var btnReplace = new Button
            {
                Content = "直接更换（不复制，保留原文件夹）",
                Height = 40,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 12)
            };
            btnReplace.SetResourceReference(Button.BackgroundProperty, "BgSurface");
            btnReplace.SetResourceReference(Button.ForegroundProperty, "TextPrimary");
            btnReplace.Click += (s, args) =>
            {
                if (outerPanel.Parent is System.Windows.Controls.Primitives.Popup pp) pp.IsOpen = false;
                var result = System.Windows.MessageBox.Show(
                    $"确认更换文件夹？\n程序将使用新文件夹，原文件夹保留不变。",
                    "二次确认 - 直接更换",
                    System.Windows.MessageBoxButton.OKCancel,
                    System.Windows.MessageBoxImage.Warning);
                if (result != System.Windows.MessageBoxResult.OK) return;

                bool success = App.FileManager.ChangeFolder(newPath);
                if (success)
                    System.Windows.MessageBox.Show("文件夹地址已更新", "成功", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                else
                    System.Windows.MessageBox.Show("文件夹地址更新失败", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                RefreshFolderPathDisplay();
            };
            stack.Children.Add(btnReplace);

            var btnCancel = new Button
            {
                Content = "取消",
                Height = 36,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnCancel.SetResourceReference(Button.ForegroundProperty, "TextSecondary");
            btnCancel.Click += (s, args) =>
            {
                if (outerPanel.Parent is System.Windows.Controls.Primitives.Popup pp) pp.IsOpen = false;
            };
            stack.Children.Add(btnCancel);

            innerPanel.Child = stack;
            return outerPanel;
        }

        private void OnLockTimeColorClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                var s = OpenSettingsService.Instance.Current;
                s.LockScreenTimeColor = colorHex;
                OpenSettingsService.Instance.Save();
                UpdateLockTimeColorButtons(colorHex);
            }
        }

        private void OnLockDateColorClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                var s = OpenSettingsService.Instance.Current;
                s.LockScreenDateColor = colorHex;
                OpenSettingsService.Instance.Save();
                UpdateLockDateColorButtons(colorHex);
            }
        }

        private void UpdateLockTimeColorButtons(string activeColor)
        {
            var accentBorder = (SolidColorBrush)Application.Current.Resources["BorderColor"];
            BtnLockTimeLight.BorderBrush = activeColor == "#FFFFFF" ? accentBorder : Brushes.Transparent;
            BtnLockTimeDark.BorderBrush = activeColor == "#333333" ? accentBorder : Brushes.Transparent;
        }

        private void UpdateLockDateColorButtons(string activeColor)
        {
            var accentBorder = (SolidColorBrush)Application.Current.Resources["BorderColor"];
            BtnLockDateLight.BorderBrush = activeColor == "#CCCCCC" ? accentBorder : Brushes.Transparent;
            BtnLockDateDark.BorderBrush = activeColor == "#666666" ? accentBorder : Brushes.Transparent;
        }

        private void UpdateDisguisePanelVisibility()
        {
            PanelDisguiseSettings.Visibility = ChkDisguiseEnabled.IsChecked == true
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnDisguiseIconBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择图标文件",
                Filter = "所有图片|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif|图标文件 (*.ico)|*.ico|PNG 图片 (*.png)|*.png|JPEG 图片 (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP 图片 (*.bmp)|*.bmp|GIF 图片 (*.gif)|*.gif|所有文件 (*.*)|*.*",
                DefaultExt = ".png"
            };
            if (dlg.ShowDialog() == true)
            {
                _disguiseIconFullPath = dlg.FileName;
                TxtDisguiseIconPath.Text = System.IO.Path.GetFileName(_disguiseIconFullPath);
                if (!_isLoading) SaveOpenSettings();
            }
        }

        private void BtnDisguiseRestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            ChkDisguiseEnabled.IsChecked = false;
            TxtDisguiseAppName.Text = "Personal Zone";
            _disguiseIconFullPath = "";
            TxtDisguiseIconPath.Text = "";
            UpdateDisguisePanelVisibility();
            SaveOpenSettings();
        }
    }
}
