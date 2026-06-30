using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using WpfApp3.Models;
using WpfApp3.Services;

namespace WpfApp3.Widgets
{
    public class WidgetBase : UserControl
    {
        public const int GridColumns = 32;
        public const int GridRows = 18;
        public const double GridGap = 3.0;
        public const double GridMinCellSize = 20.0;

        public WidgetData Data { get; set; } = null!;
        public event Action<WidgetBase>? OnClose;
        public event Action<WidgetBase>? OnDragEnd;
        public event Action<WidgetBase>? OnResizeEnd;

        protected Border MainBorder = null!;
        protected Grid RootGrid = null!;
        protected Border TitleBar = null!;
        protected TextBlock TitleText = null!;
        protected Button CloseButton = null!;
        protected Border ContentArea = null!;
        protected Viewbox ContentViewbox = null!;

        private bool _isDragging;
        private Point _dragStartPoint;
        private double _dragStartWidgetLeft;
        private double _dragStartWidgetTop;

        private bool _isResizing;
        private double _originalWidth;
        private double _originalHeight;
        private double _resizeStartLeft;
        private double _resizeStartTop;

        private bool _isMoveLocked;
        private bool _isResizeLocked = true;
        private bool _showBorder = true;
        private bool _showShadow = true;
        private bool _showTitleBar = true;
        private bool _snapToGrid = true;
        private bool _snapToEdges = true;
        private bool _autoScale = true;
        private Color _bgColor = Color.FromRgb(0x25, 0x25, 0x36);
        private double _bgOpacity = 1.0;

        private string _widgetThemeVariant = "System";
        private string _widgetAccentColor = "";
        private bool _transparentTextLight;

        protected string WidgetThemeVariant => _widgetThemeVariant;
        protected string WidgetAccentColor => _widgetAccentColor;

        protected Color GetEffectiveAccentColor()
        {
            if (!string.IsNullOrEmpty(_widgetAccentColor))
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(_widgetAccentColor);
                }
                catch { }
            }

            try
            {
                var brush = Application.Current.FindResource("Accent") as SolidColorBrush;
                if (brush != null) return brush.Color;
            }
            catch { }

            return Color.FromRgb(0x7C, 0x8A, 0xFF);
        }

        private static string GetSystemThemeVariant()
        {
            try
            {
                return WpfApp3.Services.ThemeService.Instance.GetEffectiveVariant();
            }
            catch
            {
                return "Dark";
            }
        }

        private const double SnapThreshold = 15.0;

        private readonly Thumb _resizeGrip;

        private static readonly List<BgPreset> BgPresets = new()
        {
            new("深蓝",       Color.FromRgb(0x1A, 0x2A, 0x45)),
            new("暗紫",       Color.FromRgb(0x35, 0x28, 0x4A)),
            new("墨绿",       Color.FromRgb(0x1E, 0x3A, 0x2E)),
            new("暗红",       Color.FromRgb(0x45, 0x22, 0x28)),
            new("深灰",       Color.FromRgb(0x30, 0x30, 0x38)),
            new("灰蓝",       Color.FromRgb(0x28, 0x32, 0x40)),
            new("茶色",       Color.FromRgb(0x3D, 0x32, 0x25)),
            new("浅灰",       Color.FromRgb(0xD8, 0xD8, 0xE0)),
            new("暖黄",       Color.FromRgb(0xFF, 0xEE, 0xC0)),
            new("纯白",       Color.FromRgb(0xFF, 0xFF, 0xFF)),
            new("浅蓝",       Color.FromRgb(0xD0, 0xE0, 0xFF)),
            new("透明-白色/浅色文字", Colors.Transparent),
            new("透明-黑色/深色文字", Colors.Transparent),
        };

        private static readonly (string Name, string Hex)[] AccentPresetNames = new[]
        {
            ("紫色", "#7C8AFF"), ("粉色", "#FF6B9D"), ("橙色", "#FF8C42"), ("青色", "#5CE1E6"),
            ("绿色", "#6BCB77"), ("紫罗兰", "#C084FC"), ("琥珀", "#F59E0B"), ("红色", "#EF4444"),
            ("蓝色", "#3B82F6"), ("翠绿", "#10B981"), ("靛蓝", "#8B5CF6"), ("玫红", "#EC4899")
        };

        public WidgetBase()
        {
            BuildVisualTree();

            _resizeGrip = CreateResizeGrip();
            RootGrid.Children.Add(_resizeGrip);
            Grid.SetRow(_resizeGrip, 1);

            this.SizeChanged += OnMySizeChanged;
        }

        private void BuildVisualTree()
        {
            MainBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(_bgColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x55)),
                BorderThickness = new Thickness(1),
            };
            MainBorder.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 16,
                ShadowDepth = 3,
                Opacity = 0.4
            };
            MainBorder.MouseRightButtonDown += OnWidgetRightClick;

            RootGrid = new Grid();
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TitleBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x44)),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                Padding = new Thickness(12, 6, 8, 6),
                Cursor = Cursors.SizeAll
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TitleText = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(TitleText, 0);

            CloseButton = new Button
            {
                Width = 22,
                Height = 22,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 12,
                Content = "\u2715",
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            CloseButton.Template = CreateIconButtonTemplate();
            CloseButton.Click += (s, e) => OnClose?.Invoke(this);
            Grid.SetColumn(CloseButton, 1);

            titleGrid.Children.Add(TitleText);
            titleGrid.Children.Add(CloseButton);
            TitleBar.Child = titleGrid;

            ContentArea = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(0, 0, 12, 12)
            };
            ContentArea.MouseRightButtonDown += OnContentRightClick;

            ContentViewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.Both
            };

            ContentArea.Child = ContentViewbox;

            RootGrid.Children.Add(TitleBar);
            RootGrid.Children.Add(ContentArea);
            Grid.SetRow(TitleBar, 0);
            Grid.SetRow(ContentArea, 1);

            MainBorder.Child = RootGrid;
            this.Content = MainBorder;

            TitleBar.MouseLeftButtonDown += OnTitleBarMouseDown;
            this.MouseMove += OnWidgetMouseMove;
            this.MouseLeftButtonUp += OnWidgetMouseUp;
            TitleBar.MouseRightButtonDown += OnTitleBarRightClick;
        }

        private Thumb CreateResizeGrip()
        {
            var grip = new Thumb
            {
                Width = 16,
                Height = 16,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = Cursors.SizeNWSE,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 2, 2),
                Template = new ControlTemplate(typeof(Thumb))
                {
                    VisualTree = CreateResizeGripVisual()
                }
            };

            grip.DragStarted += OnResizeStarted;
            grip.DragDelta += OnResizeDelta;
            grip.DragCompleted += OnResizeCompleted;

            return grip;
        }

        private static FrameworkElementFactory CreateResizeGripVisual()
        {
            var grid = new FrameworkElementFactory(typeof(Grid));
            grid.SetValue(Grid.WidthProperty, 16.0);
            grid.SetValue(Grid.HeightProperty, 16.0);
            grid.SetValue(Grid.BackgroundProperty, Brushes.Transparent);

            var poly = new FrameworkElementFactory(typeof(System.Windows.Shapes.Polygon));
            poly.SetValue(System.Windows.Shapes.Polygon.FillProperty,
                new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x80)));
            poly.SetValue(System.Windows.Shapes.Polygon.PointsProperty,
                new PointCollection { new Point(16, 0), new Point(16, 16), new Point(0, 16) });

            grid.AppendChild(poly);
            return grid;
        }

        public void SetTitle(string title)
        {
            TitleText.Text = title;
        }

        public void SetContent(UIElement element)
        {
            ContentViewbox.Child = element;
        }

        public void LoadAppearance(WidgetData data)
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<AppearanceConfig>(data.Config);
                if (config != null)
                {
                    _isMoveLocked = config.IsMoveLocked;
                    _isResizeLocked = config.IsResizeLocked;
                    _showBorder = config.ShowBorder;
                    _showShadow = config.ShowShadow;
                    _showTitleBar = config.ShowTitleBar;

                    _snapToGrid = config.SnapToGrid;
                    _snapToEdges = config.SnapToEdges;
                    _autoScale = config.AutoScale;

                    _widgetThemeVariant = config.WidgetThemeVariant ?? "System";
                    _widgetAccentColor = config.WidgetAccentColor ?? "";
                    _transparentTextLight = config.TransparentTextLight;
                    _bgOpacity = config.BackgroundOpacity > 0 ? config.BackgroundOpacity : 1.0;

                    if (!string.IsNullOrEmpty(config.BackgroundColorHex))
                    {
                        var c = (Color)ColorConverter.ConvertFromString(config.BackgroundColorHex);
                        _bgColor = c;
                    }
                }
            }
            catch { }

            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            TitleBar.Cursor = _isMoveLocked ? Cursors.Arrow : Cursors.SizeAll;
            _resizeGrip.Visibility = _isResizeLocked ? Visibility.Collapsed : Visibility.Visible;
            TitleBar.Visibility = _showTitleBar ? Visibility.Visible : Visibility.Collapsed;

            if (!_showTitleBar)
            {
                MainBorder.CornerRadius = new CornerRadius(12);
                ContentArea.CornerRadius = new CornerRadius(12);
            }
            else
            {
                MainBorder.CornerRadius = new CornerRadius(12);
                ContentArea.CornerRadius = new CornerRadius(0, 0, 12, 12);
            }

            if (!_showBorder)
            {
                MainBorder.BorderThickness = new Thickness(0);
            }
            else
            {
                MainBorder.BorderThickness = new Thickness(1);
            }

            if (!_showShadow)
            {
                MainBorder.Effect = null;
            }
            else
            {
                MainBorder.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 3,
                    Opacity = 0.4
                };
            }

            ApplyBackgroundColors();
        }

        private void ApplyBackgroundColors()
        {
            Color bg = _bgColor;

            if (bg == Colors.Transparent)
            {
                MainBorder.Background = Brushes.Transparent;
                MainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x55));
                TitleBar.Background = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x44));

                if (_transparentTextLight)
                {
                    TitleText.Foreground = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x2E));
                    CloseButton.Foreground = new SolidColorBrush(Color.FromRgb(0x58, 0x58, 0x70));
                    RefreshContentStyles(Color.FromRgb(0xFF, 0xFF, 0xFF), Color.FromRgb(0x1E, 0x1E, 0x2E));
                }
                else
                {
                    TitleText.Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0));
                    CloseButton.Foreground = new SolidColorBrush(Color.FromRgb(0x98, 0x98, 0xB0));
                    RefreshContentStyles(Color.FromRgb(0x25, 0x25, 0x36), Color.FromRgb(0xE8, 0xE8, 0xF0));
                }
                return;
            }

            string variant = _widgetThemeVariant;

            if (variant == "System")
            {
                var sysV = GetSystemThemeVariant();
                bg = sysV == "Light" ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x25, 0x25, 0x36);
            }

            double brightness = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255.0;
            bool isBgDark = brightness < 0.5;

            Color textColor, mutedTextColor;

            if (variant == "Light" || (variant == "System" && GetSystemThemeVariant() == "Light"))
            {
                textColor = Color.FromRgb(0x1E, 0x1E, 0x2E);
                mutedTextColor = Color.FromRgb(0x58, 0x58, 0x70);
            }
            else
            {
                textColor = Color.FromRgb(0xE8, 0xE8, 0xF0);
                mutedTextColor = Color.FromRgb(0x98, 0x98, 0xB0);
            }

            double borderFactor = isBgDark ? 1.25 : 0.85;
            byte BorderClamp(double v) => (byte)Math.Clamp((int)(v * borderFactor), 0, 255);
            Color borderColor = Color.FromRgb(BorderClamp(bg.R), BorderClamp(bg.G), BorderClamp(bg.B));

            double titleFactor = isBgDark ? 1.18 : 0.92;
            byte TitleClamp(double v) => (byte)Math.Clamp((int)(v * titleFactor), 0, 255);
            Color titleBgColor = Color.FromRgb(TitleClamp(bg.R), TitleClamp(bg.G), TitleClamp(bg.B));

            byte bgAlpha = (byte)(_bgOpacity * 255);
            Color bgWithAlpha = Color.FromArgb(bgAlpha, bg.R, bg.G, bg.B);

            MainBorder.Background = new SolidColorBrush(bgWithAlpha);
            MainBorder.BorderBrush = new SolidColorBrush(borderColor);
            TitleBar.Background = new SolidColorBrush(titleBgColor);
            TitleText.Foreground = new SolidColorBrush(textColor);
            CloseButton.Foreground = new SolidColorBrush(mutedTextColor);

            RefreshContentStyles(bg, textColor);
        }

        protected virtual void RefreshContentStyles(Color bgColor, Color textColor)
        {
        }

        protected virtual void AddCustomContextMenuItems(ContextMenu menu)
        {
        }

        private void SaveAppearance()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["IsMoveLocked"] = _isMoveLocked;
            root["IsResizeLocked"] = _isResizeLocked;
            root["ShowBorder"] = _showBorder;
            root["ShowShadow"] = _showShadow;
            root["ShowTitleBar"] = _showTitleBar;
            root["SnapToGrid"] = _snapToGrid;
            root["SnapToEdges"] = _snapToEdges;
            root["AutoScale"] = _autoScale;
            root["BackgroundColorHex"] = _bgColor.ToString();
            root["WidgetThemeVariant"] = _widgetThemeVariant;
            root["WidgetAccentColor"] = _widgetAccentColor;
            root["TransparentTextLight"] = _transparentTextLight;
            root["BackgroundOpacity"] = _bgOpacity;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        public void SetContentDirect(UIElement element)
        {
            ContentArea.Child = element;
        }

        public double GetCenterX()
        {
            double left = Canvas.GetLeft(this);
            if (double.IsNaN(left)) left = 0;
            double w = double.IsNaN(ActualWidth) || ActualWidth <= 0 ? Width : ActualWidth;
            return left + w / 2;
        }

        public double GetCenterY()
        {
            double top = Canvas.GetTop(this);
            if (double.IsNaN(top)) top = 0;
            double h = double.IsNaN(ActualHeight) || ActualHeight <= 0 ? Height : ActualHeight;
            return top + h / 2;
        }

        public void SetCenterPosition(double cx, double cy)
        {
            double w = double.IsNaN(ActualWidth) || ActualWidth <= 0 ? Width : ActualWidth;
            double h = double.IsNaN(ActualHeight) || ActualHeight <= 0 ? Height : ActualHeight;
            Canvas.SetLeft(this, cx - w / 2);
            Canvas.SetTop(this, cy - h / 2);
        }

        public static double GetScaleFactor()
        {
            double s = OpenSettingsService.Instance.Current.ExtraScale / 100.0;
            return s > 0 ? s : 1.0;
        }

        public static double GetGridCellSize(double canvasWidth)
        {
            double scale = GetScaleFactor();
            double cw = scale > 0 ? canvasWidth / scale : canvasWidth;
            double cellSize = (cw - (GridColumns + 1) * GridGap) / GridColumns;
            return Math.Max(GridMinCellSize, cellSize);
        }

        public static (double cellW, double cellH) GetGridCellSizes(double canvasWidth, double canvasHeight)
        {
            double scale = GetScaleFactor();
            double cw = scale > 0 ? canvasWidth / scale : canvasWidth;
            double ch = scale > 0 ? canvasHeight / scale : canvasHeight;
            double cellW = (cw - (GridColumns + 1) * GridGap) / GridColumns;
            cellW = Math.Max(GridMinCellSize, cellW);
            double cellH = (ch - (GridRows + 1) * GridGap) / GridRows;
            cellH = Math.Max(GridMinCellSize, cellH);
            return (cellW, cellH);
        }

        public static (double x, double y, double w, double h) GridToPixel(int col, int row, int colSpan, int rowSpan, double cellW, double cellH)
        {
            double x = GridGap + col * (cellW + GridGap);
            double y = GridGap + row * (cellH + GridGap);
            double w = colSpan * cellW + (colSpan - 1) * GridGap;
            double h = rowSpan * cellH + (rowSpan - 1) * GridGap;
            return (x, y, w, h);
        }

        public void ApplyGridPosition(int col, int row, int colSpan, int rowSpan, double cellW, double cellH)
        {
            var (x, y, w, h) = GridToPixel(col, row, colSpan, rowSpan, cellW, cellH);
            Width = w;
            Height = h;
            Canvas.SetLeft(this, x);
            Canvas.SetTop(this, y);

            Data.GridColumn = col;
            Data.GridRow = row;
            Data.GridColSpan = colSpan;
            Data.GridRowSpan = rowSpan;
        }

        private (int col, int row) SnapToNearestGridCell(Canvas canvas, double pixelX, double pixelY, double widgetW, double widgetH)
        {
            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;
            var (cellW, cellH) = GetGridCellSizes(cw, ch);

            double centerX = pixelX + widgetW / 2;
            double centerY = pixelY + widgetH / 2;

            int col = (int)Math.Round((centerX - GridGap - cellW / 2) / (cellW + GridGap));
            int row = (int)Math.Round((centerY - GridGap - cellH / 2) / (cellH + GridGap));

            col = Math.Max(0, Math.Min(col, GridColumns - Data.GridColSpan));
            row = Math.Max(0, Math.Min(row, GridRows - Data.GridRowSpan));

            return (col, row);
        }

        public void SaveProportionalData(double canvasWidth, double canvasHeight)
        {
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double cx = GetCenterX();
            double cy = GetCenterY();
            double w = double.IsNaN(ActualWidth) || ActualWidth <= 0 ? Width : ActualWidth;
            double h = double.IsNaN(ActualHeight) || ActualHeight <= 0 ? Height : ActualHeight;

            Data.ProportionalX = cx / canvasWidth;
            Data.ProportionalY = cy / canvasHeight;
            Data.ProportionalW = w / canvasWidth;
            Data.ProportionalH = h / canvasHeight;
            Data.X = cx;
            Data.Y = cy;
            Data.Width = w;
            Data.Height = h;
        }

        public void SaveGridDataFromPosition()
        {
            var canvas = this.Parent as Canvas;
            if (canvas == null) return;

            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            var (cellW, cellH) = GetGridCellSizes(cw, ch);

            double left = Canvas.GetLeft(this);
            if (double.IsNaN(left)) left = 0;
            double top = Canvas.GetTop(this);
            if (double.IsNaN(top)) top = 0;

            Data.GridColumn = Math.Max(0, (int)Math.Round((left - GridGap) / (cellW + GridGap)));
            Data.GridRow = Math.Max(0, (int)Math.Round((top - GridGap) / (cellH + GridGap)));
        }

        public void ApplyProportionalPositionAndScale(double canvasWidth, double canvasHeight)
        {
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            bool hasProportions = Data.ProportionalX > 0 || Data.ProportionalY > 0;

            double newW, newH, cx, cy;

            if (hasProportions && _autoScale && Data.ProportionalW > 0 && Data.ProportionalH > 0)
            {
                newW = Data.ProportionalW * canvasWidth;
                newH = Data.ProportionalH * canvasHeight;
                newW = Math.Max(140, Math.Min(newW, canvasWidth));
                newH = Math.Max(100, Math.Min(newH, canvasHeight));
            }
            else
            {
                newW = double.IsNaN(Width) || Width <= 0 ? (Data.Width > 0 ? Data.Width : 280) : Width;
                newH = double.IsNaN(Height) || Height <= 0 ? (Data.Height > 0 ? Data.Height : 220) : Height;
            }

            Width = newW;
            Height = newH;

            if (hasProportions)
            {
                cx = Data.ProportionalX * canvasWidth;
                cy = Data.ProportionalY * canvasHeight;
            }
            else
            {
                cx = Data.X > 0 ? Data.X : newW / 2;
                cy = Data.Y > 0 ? Data.Y : newH / 2;
            }

            cx = Math.Max(newW / 2, Math.Min(cx, canvasWidth - newW / 2));
            cy = Math.Max(newH / 2, Math.Min(cy, canvasHeight - newH / 2));

            SetCenterPosition(cx, cy);
        }

        public void ClampToCanvas(double canvasWidth, double canvasHeight)
        {
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double myW = double.IsNaN(ActualWidth) || ActualWidth <= 0 ? Width : ActualWidth;
            double myH = double.IsNaN(ActualHeight) || ActualHeight <= 0 ? Height : ActualHeight;

            double cx = GetCenterX();
            double cy = GetCenterY();

            cx = Math.Max(myW / 2, Math.Min(cx, canvasWidth - myW / 2));
            cy = Math.Max(myH / 2, Math.Min(cy, canvasHeight - myH / 2));

            SetCenterPosition(cx, cy);
        }

        private void OnMySizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void OnTitleBarRightClick(object sender, MouseButtonEventArgs e)
        {
            ShowContextMenu();
            e.Handled = true;
        }

        private void OnWidgetRightClick(object sender, MouseButtonEventArgs e)
        {
            ShowBackgroundMenu();
            e.Handled = true;
        }

        private void OnContentRightClick(object sender, MouseButtonEventArgs e)
        {
            ShowBackgroundMenu();
            e.Handled = true;
        }

        private void ShowContextMenu()
        {
            var menu = BuildFullContextMenu();
            menu.IsOpen = true;
        }

        private void ShowBackgroundMenu()
        {
            if (_showTitleBar)
            {
                var menu = new ContextMenu();
                var deleteItem = new MenuItem { Header = "删除小组件" };
                deleteItem.Click += (s, args) => OnClose?.Invoke(this);
                menu.Items.Add(deleteItem);
                menu.IsOpen = true;
            }
            else
            {
                var menu = BuildFullContextMenu();
                menu.IsOpen = true;
            }
        }

        private ContextMenu BuildFullContextMenu()
        {
            var menu = new ContextMenu();

            var toggleTitleItem = new MenuItem
            {
                Header = _showTitleBar ? "隐藏顶部栏" : "显示顶部栏",
            };
            toggleTitleItem.Click += (s, args) =>
            {
                _showTitleBar = !_showTitleBar;
                ApplyAppearance();
                SaveAppearance();
            };
            menu.Items.Add(toggleTitleItem);

            menu.Items.Add(new Separator());

            var removeItem = new MenuItem { Header = "移除小组件" };
            removeItem.Click += (s, args) => OnClose?.Invoke(this);
            menu.Items.Add(removeItem);

            menu.Items.Add(new Separator());

            var bringTopItem = new MenuItem { Header = "置于顶层" };
            bringTopItem.Click += (s, args) =>
            {
                var canvas = this.Parent as Panel;
                if (canvas != null)
                {
                    canvas.Children.Remove(this);
                    canvas.Children.Add(this);
                }
            };
            menu.Items.Add(bringTopItem);

            var raiseItem = new MenuItem { Header = "上移一层" };
            raiseItem.Click += (s, args) =>
            {
                var canvas = this.Parent as Panel;
                if (canvas != null)
                {
                    int idx = canvas.Children.IndexOf(this);
                    if (idx >= 0 && idx < canvas.Children.Count - 1)
                    {
                        canvas.Children.RemoveAt(idx);
                        canvas.Children.Insert(idx + 1, this);
                    }
                }
            };
            menu.Items.Add(raiseItem);

            var lowerItem = new MenuItem { Header = "下移一层" };
            lowerItem.Click += (s, args) =>
            {
                var canvas = this.Parent as Panel;
                if (canvas != null)
                {
                    int idx = canvas.Children.IndexOf(this);
                    if (idx > 0)
                    {
                        canvas.Children.RemoveAt(idx);
                        canvas.Children.Insert(idx - 1, this);
                    }
                }
            };
            menu.Items.Add(lowerItem);

            var sendBottomItem = new MenuItem { Header = "置于底层" };
            sendBottomItem.Click += (s, args) =>
            {
                var canvas = this.Parent as Panel;
                if (canvas != null)
                {
                    canvas.Children.Remove(this);
                    canvas.Children.Insert(0, this);
                }
            };
            menu.Items.Add(sendBottomItem);

            menu.Items.Add(new Separator());

            var moveItem = new MenuItem
            {
                Header = _isMoveLocked ? "解锁移动" : "锁定移动",
                IsCheckable = true,
                IsChecked = !_isMoveLocked
            };
            moveItem.Click += (s, args) =>
            {
                _isMoveLocked = !_isMoveLocked;
                ApplyAppearance();
                SaveAppearance();
            };
            menu.Items.Add(moveItem);

            var resizeItem = new MenuItem
            {
                Header = _isResizeLocked ? "解锁调整大小" : "锁定调整大小",
                IsCheckable = true,
                IsChecked = !_isResizeLocked
            };
            resizeItem.Click += (s, args) =>
            {
                _isResizeLocked = !_isResizeLocked;
                ApplyAppearance();
                SaveAppearance();
            };
            menu.Items.Add(resizeItem);

            var themeBgItem = new MenuItem { Header = "主题 / 背景" };

            var defaultDark = Color.FromRgb(0x25, 0x25, 0x36);
            var pureWhite = Color.FromRgb(0xFF, 0xFF, 0xFF);

            var themeSysItem = new MenuItem
            {
                Header = "跟随系统",
                IsCheckable = true,
                IsChecked = _widgetThemeVariant == "System"
            };
            themeSysItem.Click += (s, args) =>
            {
                _widgetThemeVariant = "System";
                ApplyAppearance();
                SaveAppearance();
            };
            themeBgItem.Items.Add(themeSysItem);

            var themeLightItem = new MenuItem
            {
                Header = "浅色（浅色背景+黑色文字）",
                IsCheckable = true,
                IsChecked = _widgetThemeVariant == "Light" && _bgColor == pureWhite
            };
            themeLightItem.Click += (s, args) =>
            {
                _widgetThemeVariant = "Light";
                _bgColor = pureWhite;
                ApplyAppearance();
                SaveAppearance();
            };
            themeBgItem.Items.Add(themeLightItem);

            var themeDarkItem = new MenuItem
            {
                Header = "深色（深色背景+白色文字）",
                IsCheckable = true,
                IsChecked = _widgetThemeVariant == "Dark" && _bgColor == defaultDark
            };
            themeDarkItem.Click += (s, args) =>
            {
                _widgetThemeVariant = "Dark";
                _bgColor = defaultDark;
                ApplyAppearance();
                SaveAppearance();
            };
            themeBgItem.Items.Add(themeDarkItem);

            themeBgItem.Items.Add(new Separator());

            foreach (var preset in BgPresets)
            {
                bool isTransparent = preset.Color == Colors.Transparent;
                bool isThisTransparent = isTransparent &&
                    (preset.Name == "透明-黑色/深色文字") == _transparentTextLight;

                var item = new MenuItem
                {
                    Header = preset.Name,
                    IsCheckable = true,
                    IsChecked = isTransparent
                        ? (_widgetThemeVariant == "Custom" && _bgColor == Colors.Transparent && isThisTransparent)
                        : (_widgetThemeVariant == "Custom" && _bgColor == preset.Color),
                    Tag = preset
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is BgPreset p)
                    {
                        _widgetThemeVariant = "Custom";
                        _bgColor = p.Color;
                        _transparentTextLight = p.Color == Colors.Transparent && p.Name == "透明-黑色/深色文字";
                        ApplyAppearance();
                        SaveAppearance();
                    }
                };
                themeBgItem.Items.Add(item);
            }
            menu.Items.Add(themeBgItem);

            var opacityItem = new MenuItem { Header = $"背景透明度 ({(int)(_bgOpacity * 100)}%)" };
            for (int pct = 100; pct >= 30; pct -= 10)
            {
                var oi = new MenuItem
                {
                    Header = $"{pct}%",
                    IsCheckable = true,
                    IsChecked = (int)(_bgOpacity * 100) == pct,
                    Tag = pct / 100.0
                };
                oi.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is double val)
                    {
                        _bgOpacity = val;
                        ApplyAppearance();
                        SaveAppearance();
                    }
                };
                opacityItem.Items.Add(oi);
            }
            menu.Items.Add(opacityItem);

            var accentItem = new MenuItem { Header = "主题色" };
            var accentNoneItem = new MenuItem
            {
                Header = "跟随系统",
                IsCheckable = true,
                IsChecked = string.IsNullOrEmpty(_widgetAccentColor)
            };
            accentNoneItem.Click += (s, args) =>
            {
                _widgetAccentColor = "";
                ApplyAppearance();
                SaveAppearance();
            };
            accentItem.Items.Add(accentNoneItem);
            accentItem.Items.Add(new Separator());

            foreach (var (name, hex) in AccentPresetNames)
            {
                var item = new MenuItem
                {
                    Header = name,
                    IsCheckable = true,
                    IsChecked = _widgetAccentColor == hex,
                    Tag = hex
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is string h)
                    {
                        _widgetAccentColor = h;
                        ApplyAppearance();
                        SaveAppearance();
                    }
                };
                accentItem.Items.Add(item);
            }
            menu.Items.Add(accentItem);

            menu.Items.Add(new Separator());

            AddCustomContextMenuItems(menu);

            var borderItem = new MenuItem
            {
                Header = "显示边框",
                IsCheckable = true,
                IsChecked = _showBorder
            };
            borderItem.Click += (s, args) =>
            {
                _showBorder = !_showBorder;
                ApplyAppearance();
                SaveAppearance();
            };
            menu.Items.Add(borderItem);

            var shadowItem = new MenuItem
            {
                Header = "显示阴影",
                IsCheckable = true,
                IsChecked = _showShadow
            };
            shadowItem.Click += (s, args) =>
            {
                _showShadow = !_showShadow;
                ApplyAppearance();
                SaveAppearance();
            };
            menu.Items.Add(shadowItem);

            return menu;
        }

        private static ControlTemplate CreateIconButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.Name = "bd";

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var mouseOverTrigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            mouseOverTrigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
                "bd"));
            template.Triggers.Add(mouseOverTrigger);

            return template;
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (_isMoveLocked) return;

            var canvas = this.Parent as Canvas;
            if (canvas == null) return;

            _isDragging = true;
            _dragStartPoint = e.GetPosition(canvas);
            _dragStartWidgetLeft = Canvas.GetLeft(this);
            _dragStartWidgetTop = Canvas.GetTop(this);
            if (double.IsNaN(_dragStartWidgetLeft)) _dragStartWidgetLeft = 0;
            if (double.IsNaN(_dragStartWidgetTop)) _dragStartWidgetTop = 0;
            this.CaptureMouse();
            e.Handled = true;
        }

        private void OnWidgetMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            var canvas = this.Parent as Canvas;
            if (canvas == null) return;

            var currentPos = e.GetPosition(canvas);
            double deltaX = currentPos.X - _dragStartPoint.X;
            double deltaY = currentPos.Y - _dragStartPoint.Y;

            double myW = double.IsNaN(this.ActualWidth) || this.ActualWidth <= 0 ? this.Width : this.ActualWidth;
            double myH = double.IsNaN(this.ActualHeight) || this.ActualHeight <= 0 ? this.Height : this.ActualHeight;

            double newLeft = _dragStartWidgetLeft + deltaX;
            double newTop = _dragStartWidgetTop + deltaY;
            newLeft = Math.Max(0, Math.Min(newLeft, canvas.ActualWidth - myW));
            newTop = Math.Max(0, Math.Min(newTop, canvas.ActualHeight - myH));

            Canvas.SetLeft(this, newLeft);
            Canvas.SetTop(this, newTop);
        }

        private void OnWidgetMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            this.ReleaseMouseCapture();

            var canvas = this.Parent as Canvas;
            if (canvas != null)
            {
                double cw = canvas.ActualWidth;
                double ch = canvas.ActualHeight;
                var (cellW, cellH) = GetGridCellSizes(cw, ch);

                double left = Canvas.GetLeft(this);
                double top = Canvas.GetTop(this);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                int col = Math.Max(0, Math.Min(
                    (int)Math.Round((left - GridGap) / (cellW + GridGap)),
                    GridColumns - Data.GridColSpan));
                int row = Math.Max(0, Math.Min(
                    (int)Math.Round((top - GridGap) / (cellH + GridGap)),
                    GridRows - Data.GridRowSpan));

                ApplyGridPosition(col, row, Data.GridColSpan, Data.GridRowSpan, cellW, cellH);
            }

            OnDragEnd?.Invoke(this);
        }

        private void OnResizeStarted(object sender, DragStartedEventArgs e)
        {
            _isResizing = true;
            _originalWidth = this.ActualWidth;
            _originalHeight = this.ActualHeight;
            _resizeStartLeft = Canvas.GetLeft(this);
            _resizeStartTop = Canvas.GetTop(this);
            if (double.IsNaN(_resizeStartLeft)) _resizeStartLeft = 0;
            if (double.IsNaN(_resizeStartTop)) _resizeStartTop = 0;
        }

        private void OnResizeDelta(object sender, DragDeltaEventArgs e)
        {
            if (!_isResizing) return;

            var canvas = this.Parent as Canvas;
            if (canvas == null) return;

            var mousePos = Mouse.GetPosition(canvas);
            double newWidth = mousePos.X - _resizeStartLeft;
            double newHeight = mousePos.Y - _resizeStartTop;

            newWidth = Math.Max(GridMinCellSize, newWidth);
            newHeight = Math.Max(GridMinCellSize, newHeight);

            double cw = canvas.ActualWidth;
            double ch = canvas.ActualHeight;
            double maxWidth = cw - _resizeStartLeft;
            double maxHeight = ch - _resizeStartTop;
            newWidth = Math.Min(newWidth, maxWidth);
            newHeight = Math.Min(newHeight, maxHeight);

            this.Width = newWidth;
            this.Height = newHeight;
        }

        private void OnResizeCompleted(object sender, DragCompletedEventArgs e)
        {
            if (!_isResizing) return;
            _isResizing = false;

            var canvas = this.Parent as Canvas;
            if (canvas != null)
            {
                double cw = canvas.ActualWidth;
                double ch = canvas.ActualHeight;
                var (cellW, cellH) = GetGridCellSizes(cw, ch);

                double left = Canvas.GetLeft(this);
                double top = Canvas.GetTop(this);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                double myW = this.ActualWidth;
                double myH = this.ActualHeight;

                int col = Math.Max(0, Math.Min(
                    (int)Math.Round((left - GridGap) / (cellW + GridGap)),
                    GridColumns - 1));
                int row = Math.Max(0, Math.Min(
                    (int)Math.Round((top - GridGap) / (cellH + GridGap)),
                    GridRows - 1));

                int colSpan = Math.Max(1, Math.Min(
                    (int)Math.Round((myW + GridGap) / (cellW + GridGap)),
                    GridColumns - col));
                int rowSpan = Math.Max(1, Math.Min(
                    (int)Math.Round((myH + GridGap) / (cellH + GridGap)),
                    GridRows - row));

                ApplyGridPosition(col, row, colSpan, rowSpan, cellW, cellH);
            }

            OnResizeEnd?.Invoke(this);
        }

        internal class BgPreset
        {
            public string Name { get; }
            public Color Color { get; }

            public BgPreset(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        private class AppearanceConfig
        {
            public bool IsMoveLocked { get; set; }
            public bool IsResizeLocked { get; set; } = true;
            public bool ShowBorder { get; set; } = true;
            public bool ShowShadow { get; set; } = true;
            public bool ShowTitleBar { get; set; } = true;
            public bool SnapToGrid { get; set; } = true;
            public bool SnapToEdges { get; set; } = true;
            public bool AutoScale { get; set; } = true;
            public string BackgroundColorHex { get; set; } = "#FF252536";
            public string WidgetThemeVariant { get; set; } = "System";
            public string WidgetAccentColor { get; set; } = "";
            public bool TransparentTextLight { get; set; }
            public double BackgroundOpacity { get; set; } = 1.0;
        }
    }
}
