using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp3.Widgets
{
    public class WidgetImage : WidgetBase
    {
        private readonly Image _image;
        private readonly TextBlock _placeholder;

        public WidgetImage()
        {
            SetTitle("图片展示");
            Width = 300;
            Height = 250;

            var container = new Grid();

            _placeholder = new TextBlock
            {
                Text = "右键此处选择图片",
                Foreground = new SolidColorBrush(Color.FromRgb(0x6A, 0x6A, 0x80)),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _image = new Image
            {
                Stretch = Stretch.Uniform,
                Visibility = Visibility.Collapsed
            };

            container.Children.Add(_placeholder);
            container.Children.Add(_image);
            SetContent(container);

            container.MouseRightButtonDown += (s, e) =>
            {
                var menu = new ContextMenu();

                var selectItem = new MenuItem { Header = "选择图片" };
                selectItem.Click += (s2, e2) => SelectImage();
                menu.Items.Add(selectItem);

                var clearItem = new MenuItem { Header = "清除图片" };
                clearItem.Click += (s2, e2) => ClearImage();
                menu.Items.Add(clearItem);

                menu.IsOpen = true;
                e.Handled = true;
            };

            Loaded += (s, e) => LoadImage();
        }

        private void LoadImage()
        {
            string imagePath = "";
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<ImageConfig>(Data?.Config ?? "{}");
                if (config != null)
                    imagePath = config.ImagePath ?? "";
            }
            catch { }

            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                LoadImageFromPath(imagePath);
            }
        }

        private void SelectImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择图片",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImageFromPath(dialog.FileName);
                SaveImagePath(dialog.FileName);
            }
        }

        private void LoadImageFromPath(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _image.Source = bitmap;
                _image.Visibility = Visibility.Visible;
                _placeholder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法加载图片: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearImage()
        {
            _image.Source = null;
            _image.Visibility = Visibility.Collapsed;
            _placeholder.Visibility = Visibility.Visible;
            SaveImagePath("");
        }

        private void SaveImagePath(string path)
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["ImagePath"] = path;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            _placeholder.Foreground = new SolidColorBrush(
                Color.FromRgb(0x6A, 0x6A, 0x80));
        }

        private class ImageConfig
        {
            public string ImagePath { get; set; } = "";
        }
    }
}
