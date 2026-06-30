using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfApp3.Widgets
{
    public class WidgetStickyNote : WidgetBase
    {
        private readonly TextBox _textBox;
        private double _fontSize = 14;
        private string _textColor = "";

        private static readonly Dictionary<string, (string Name, Color Color)> TextColorPresets = new()
        {
            ["white"] = ("白色", Colors.White),
            ["lightgray"] = ("浅灰", Color.FromRgb(0xCC, 0xCC, 0xCC)),
            ["dark"] = ("深黑", Color.FromRgb(0x1E, 0x1E, 0x2E)),
            ["gray"] = ("灰色", Color.FromRgb(0x33, 0x33, 0x33)),
            ["blue"] = ("蓝色", Color.FromRgb(0x3B, 0x82, 0xF6)),
            ["red"] = ("红色", Color.FromRgb(0xEF, 0x44, 0x44)),
            ["green"] = ("绿色", Color.FromRgb(0x10, 0xB9, 0x81)),
            ["yellow"] = ("黄色", Color.FromRgb(0xF5, 0x9E, 0x0B)),
        };

        private static readonly double[] FontSizePresets = { 12, 14, 16, 18, 20, 24, 28 };

        public WidgetStickyNote()
        {
            SetTitle("便签");
            Width = 260;
            Height = 220;

            _textBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                BorderThickness = new Thickness(0),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                CaretBrush = new SolidColorBrush(Color.FromRgb(0x7C, 0x8A, 0xFF))
            };

            _textBox.TextChanged += (s, e) => SaveText();

            SetContentDirect(_textBox);

            Loaded += (s, e) => LoadStickyConfig();
        }

        private void LoadStickyConfig()
        {
            string text = "";
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<StickyConfig>(Data?.Config ?? "{}");
                if (config != null)
                {
                    text = config.Text ?? "";
                    _fontSize = config.FontSize > 0 ? config.FontSize : 14;
                    _textColor = config.TextColor ?? "";
                }
            }
            catch { }

            _textBox.Text = text;
            _textBox.FontSize = _fontSize;
            ApplyTextColor();
        }

        private void ApplyTextColor()
        {
            if (!string.IsNullOrEmpty(_textColor) && TextColorPresets.TryGetValue(_textColor, out var tuple))
            {
                _textBox.Foreground = new SolidColorBrush(tuple.Color);
            }
        }

        private void SaveFullConfig()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["Text"] = _textBox.Text;
            root["FontSize"] = _fontSize;
            root["TextColor"] = _textColor;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        private void SaveText()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["Text"] = _textBox.Text;

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            if (string.IsNullOrEmpty(_textColor))
            {
                _textBox.Foreground = new SolidColorBrush(textColor);
            }
            _textBox.CaretBrush = new SolidColorBrush(GetEffectiveAccentColor());
        }

        protected override void AddCustomContextMenuItems(ContextMenu menu)
        {
            menu.Items.Add(new Separator());

            var fontSizeItem = new MenuItem { Header = "字体大小" };
            foreach (var size in FontSizePresets)
            {
                var item = new MenuItem
                {
                    Header = size.ToString(),
                    IsCheckable = true,
                    IsChecked = Math.Abs(_fontSize - size) < 0.01,
                    Tag = size
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is double sz)
                    {
                        _fontSize = sz;
                        _textBox.FontSize = sz;
                        SaveFullConfig();
                    }
                };
                fontSizeItem.Items.Add(item);
            }
            menu.Items.Add(fontSizeItem);

            var textColorItem = new MenuItem { Header = "文字颜色" };
            foreach (var (key, (name, color)) in TextColorPresets)
            {
                var item = new MenuItem
                {
                    Header = name,
                    IsCheckable = true,
                    IsChecked = _textColor == key,
                    Tag = key
                };
                item.Click += (s, args) =>
                {
                    if (s is MenuItem mi && mi.Tag is string h)
                    {
                        _textColor = h;
                        ApplyTextColor();
                        SaveFullConfig();
                    }
                };
                textColorItem.Items.Add(item);
            }
            menu.Items.Add(textColorItem);
        }

        private class StickyConfig
        {
            public string Text { get; set; } = "";
            public double FontSize { get; set; }
            public string TextColor { get; set; } = "";
        }
    }
}
