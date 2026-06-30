using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl2_1 : UserControl
    {
        private string? _filePath;
        private BitmapImage? _bitmap;
        private int _originalWidth;
        private int _originalHeight;
        private double _zoomLevel = 1.0;
        private double _fitScale = 1.0;
        private bool _initialFitDone;

        private const double MinZoom = 0.1;
        private const double MaxZoom = 5.0;

        public UserControl2_1()
        {
            InitializeComponent();

            BtnRotateLeft.Click += (s, e) => Rotate(-90);
            BtnRotateRight.Click += (s, e) => Rotate(90);
            BtnZoomIn.Click += (s, e) => ZoomIn();
            BtnZoomOut.Click += (s, e) => ZoomOut();
            BtnFitToWindow.Click += (s, e) => FitToWindow();
            BtnZoom100.Click += (s, e) => SetZoom(1.0);
            BtnFullscreen.Click += (s, e) => EnterFullscreen();
            BtnToggleLeftPanel.Click += (s, e) => ToggleParentLeftPanel();

            BtnHide.Click += (s, e) =>
            {
                if (Parent is ContentControl cc) cc.Content = null;
            };

            ImageViewer.MouseWheel += OnMouseWheel;
            ImageScrollViewer.PreviewMouseLeftButtonDown += OnScrollViewerMouseDown;
            ImageScrollViewer.PreviewMouseMove += OnScrollViewerMouseMove;
            ImageScrollViewer.PreviewMouseLeftButtonUp += OnScrollViewerMouseUp;

            ImageScrollViewer.SizeChanged += OnSizeChanged;
        }

        public void LoadFile(string filePath)
        {
            _filePath = filePath;

            try
            {
                _bitmap = new BitmapImage();
                _bitmap.BeginInit();
                _bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                _bitmap.CacheOption = BitmapCacheOption.OnLoad;
                _bitmap.EndInit();
                _bitmap.Freeze();

                _originalWidth = _bitmap.PixelWidth;
                _originalHeight = _bitmap.PixelHeight;

                ImageViewer.Source = _bitmap;
                ImageViewer.Width = _originalWidth;
                ImageViewer.Height = _originalHeight;
                ImageRotateTransform.Angle = 0;
                _zoomLevel = 1.0;
                _initialFitDone = false;

                var fi = new FileInfo(filePath);
                FileNameLabel.Text = fi.Name;
                FileSaveTimeLabel.Text = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                DimensionsLabel.Text = $"{_originalWidth} × {_originalHeight}";
                FileSizeLabel.Text = FileManagerService.FormatFileSize(fi.Length);

                UpdateZoomLabel();
                Dispatcher.BeginInvoke(new Action(() => DoFitToWindow()),
                    DispatcherPriority.Loaded);
            }
            catch
            {
                _bitmap = null;
            }
        }

        private void Rotate(double angle)
        {
            if (_bitmap == null) return;

            ImageRotateTransform.Angle = (ImageRotateTransform.Angle + angle) % 360;
            if (ImageRotateTransform.Angle < 0)
                ImageRotateTransform.Angle += 360;

            var rotatedAngle = ImageRotateTransform.Angle;
            var isRotated90 = rotatedAngle > 45 && rotatedAngle < 135
                           || rotatedAngle > 225 && rotatedAngle < 315;

            if (isRotated90)
            {
                ImageViewer.Width = _originalHeight * _zoomLevel;
                ImageViewer.Height = _originalWidth * _zoomLevel;
                DimensionsLabel.Text = $"{_originalHeight} × {_originalWidth}";
            }
            else
            {
                ImageViewer.Width = _originalWidth * _zoomLevel;
                ImageViewer.Height = _originalHeight * _zoomLevel;
                DimensionsLabel.Text = $"{_originalWidth} × {_originalHeight}";
            }
        }

        private void ZoomIn()
        {
            SetZoom(_zoomLevel * 1.25);
        }

        private void ZoomOut()
        {
            SetZoom(_zoomLevel / 1.25);
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

        private void SetZoom(double newZoom)
        {
            newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
            _zoomLevel = newZoom;

            var rotatedAngle = ImageRotateTransform.Angle;
            var isRotated90 = rotatedAngle > 45 && rotatedAngle < 135
                           || rotatedAngle > 225 && rotatedAngle < 315;

            if (isRotated90)
            {
                ImageViewer.Width = _originalHeight * _zoomLevel;
                ImageViewer.Height = _originalWidth * _zoomLevel;
            }
            else
            {
                ImageViewer.Width = _originalWidth * _zoomLevel;
                ImageViewer.Height = _originalHeight * _zoomLevel;
            }

            UpdateZoomLabel();
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (_bitmap == null) return;
            ImageViewer.Cursor = _zoomLevel > _fitScale + 0.01 ? Cursors.ScrollAll : Cursors.Arrow;
        }

        private double ComputeFitScale()
        {
            var rotatedAngle = ImageRotateTransform.Angle;
            var isRotated90 = rotatedAngle > 45 && rotatedAngle < 135
                           || rotatedAngle > 225 && rotatedAngle < 315;

            var imgW = isRotated90 ? _originalHeight : _originalWidth;
            var imgH = isRotated90 ? _originalWidth : _originalHeight;

            var viewW = ImageScrollViewer.ViewportWidth;
            var viewH = ImageScrollViewer.ViewportHeight;

            if (viewW <= 0 || viewH <= 0) return -1;

            var scaleX = viewW / imgW;
            var scaleY = viewH / imgH;
            return Math.Min(scaleX, scaleY) * 0.92;
        }

        private void DoFitToWindow()
        {
            var fit = ComputeFitScale();
            if (fit > 0)
            {
                _fitScale = fit;
                _initialFitDone = true;
                SetZoom(_fitScale);
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_bitmap == null) return;

            var newFit = ComputeFitScale();
            if (newFit <= 0) return;

            if (!_initialFitDone) return;

            var oldFit = _fitScale;
            _fitScale = newFit;
            var ratio = newFit / oldFit;
            SetZoom(_zoomLevel * ratio);
        }

        private void FitToWindow()
        {
            DoFitToWindow();
        }

        private void EnterFullscreen()
        {
            if (_bitmap == null || string.IsNullOrEmpty(_filePath)) return;

            var window = Window.GetWindow(this);
            if (window is not MainWindow mw) return;

            var f11 = new UserControl2_1_f11();
            f11.ExitFullscreenRequested += () => mw.ExitFullscreen();
            f11.LoadFile(_filePath, _zoomLevel, ImageRotateTransform.Angle);
            mw.EnterFullscreen(f11);
        }

        private void UpdateZoomLabel()
        {
            ZoomPercentLabel.Text = $"{(int)(_zoomLevel * 100)}%";
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                ZoomIn();
            else
                ZoomOut();
            e.Handled = true;
        }

        private bool _isPanning;
        private Point _panStartPoint;
        private Point _panStartOffset;

        private void OnScrollViewerMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_bitmap == null) return;
            _isPanning = true;
            _panStartPoint = e.GetPosition(ImageScrollViewer);
            _panStartOffset = new Point(
                ImageScrollViewer.HorizontalOffset,
                ImageScrollViewer.VerticalOffset);
            ImageScrollViewer.CaptureMouse();
            e.Handled = true;
        }

        private void OnScrollViewerMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPanning) return;
            var currentPoint = e.GetPosition(ImageScrollViewer);
            var delta = _panStartPoint - currentPoint;

            ImageScrollViewer.ScrollToHorizontalOffset(_panStartOffset.X + delta.X);
            ImageScrollViewer.ScrollToVerticalOffset(_panStartOffset.Y + delta.Y);
            e.Handled = true;
        }

        private void OnScrollViewerMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isPanning = false;
            ImageScrollViewer.ReleaseMouseCapture();
            e.Handled = true;
        }
    }
}
