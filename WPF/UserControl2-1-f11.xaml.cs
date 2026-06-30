using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WpfApp3
{
    public partial class UserControl2_1_f11 : UserControl
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

        private readonly DispatcherTimer _autoHideTimer;

        public event Action? ExitFullscreenRequested;

        public UserControl2_1_f11()
        {
            InitializeComponent();

            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
            _autoHideTimer.Tick += OnAutoHideTick;

            BtnRotateLeft.Click += (s, e) => Rotate(-90);
            BtnRotateRight.Click += (s, e) => Rotate(90);
            BtnZoomIn.Click += (s, e) => ZoomIn();
            BtnZoomOut.Click += (s, e) => ZoomOut();
            BtnFitToWindow.Click += (s, e) => FitToWindow();
            BtnZoom100.Click += (s, e) => SetZoom(1.0);
            BtnExitFullscreen.Click += (s, e) => ExitFullscreenRequested?.Invoke();

            ImageViewer.MouseWheel += OnMouseWheel;
            ImageScrollViewer.PreviewMouseLeftButtonDown += OnScrollViewerMouseDown;
            ImageScrollViewer.PreviewMouseMove += OnScrollViewerMouseMove;
            ImageScrollViewer.PreviewMouseLeftButtonUp += OnScrollViewerMouseUp;

            FullscreenRoot.MouseMove += OnFullscreenMouseMove;
            FullscreenRoot.MouseLeave += (s, e) => { };

            ImageScrollViewer.SizeChanged += OnSizeChanged;
        }

        public void LoadFile(string filePath, double currentZoom, double rotationAngle)
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
                ImageRotateTransform.Angle = rotationAngle;
                _zoomLevel = 1.0;
                _initialFitDone = false;

                var fi = new FileInfo(filePath);
                FileNameLabel.Text = fi.Name;
                DimensionsLabel.Text = $"{_originalWidth} × {_originalHeight}";

                UpdateZoomLabel();
                Dispatcher.BeginInvoke(new Action(() => DoFitToWindow()),
                    DispatcherPriority.Loaded);

                ShowToolbars();
                _autoHideTimer.Start();
            }
            catch
            {
                _bitmap = null;
            }
        }

        public void DisposeResources()
        {
            _autoHideTimer.Stop();
            ImageViewer.Source = null;
            _bitmap = null;
        }

        private void OnFullscreenMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(FullscreenRoot);
            var h = FullscreenRoot.ActualHeight;

            var nearTop = pos.Y < 60;
            var nearBottom = pos.Y > h - 80;

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

        private void Rotate(double angle)
        {
            if (_bitmap == null) return;

            ImageRotateTransform.Angle = (ImageRotateTransform.Angle + angle) % 360;
            if (ImageRotateTransform.Angle < 0)
                ImageRotateTransform.Angle += 360;

            ApplyImageDimensions();
        }

        private void ApplyImageDimensions()
        {
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

        private void SetZoom(double newZoom)
        {
            newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
            _zoomLevel = newZoom;
            ApplyImageDimensions();
            UpdateZoomLabel();
            UpdateCursor();
        }

        private void UpdateCursor()
        {
            if (_bitmap == null) return;
            ImageScrollViewer.Cursor = _zoomLevel > _fitScale + 0.01 ? Cursors.ScrollAll : Cursors.Arrow;
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
