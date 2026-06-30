using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfApp3.Widgets
{
    public class WidgetQuickLaunch : WidgetBase
    {
        private readonly StackPanel _listPanel;
        private readonly List<LaunchEntry> _entries = new();
        private readonly Button _addButton;
        private readonly List<Button> _entryButtons = new();

        public WidgetQuickLaunch()
        {
            SetTitle("快捷启动");
            Width = 240;
            Height = 280;

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            _listPanel = new StackPanel { Margin = new Thickness(0) };
            scrollViewer.Content = _listPanel;
            SetContentDirect(scrollViewer);

            _addButton = new Button
            {
                Content = "+ 添加",
                Background = new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x5A)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xAB, 0xFF)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };
            _addButton.Template = CreateAddButtonTemplate(GetEffectiveAccentColor());
            _addButton.Click += (s, e) => AddEntry();

            _listPanel.Children.Add(_addButton);

            Loaded += (s, e) => LoadEntries();
        }

        private void LoadEntries()
        {
            try
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<QuickLaunchConfig>(Data?.Config ?? "{}");
                if (config?.Entries != null)
                {
                    foreach (var entry in config.Entries)
                    {
                        _entries.Add(entry);
                        AddEntryButton(entry);
                    }
                }
            }
            catch { }
        }

        private void AddEntry()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择要快捷启动的程序或文件",
                Filter = "所有文件|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var entry = new LaunchEntry
                {
                    Name = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Path = dialog.FileName
                };
                _entries.Add(entry);
                AddEntryButton(entry);
                SaveEntries();
            }
        }

        private void AddEntryButton(LaunchEntry entry)
        {
            var button = new Button
            {
                Content = entry.Name,
                Tag = entry,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xE8, 0xF0)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Height = 32,
                FontSize = 13,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(8, 0, 8, 0),
                ToolTip = entry.Path
            };

            button.Template = CreateEntryButtonTemplate(Color.FromRgb(0x25, 0x25, 0x36));

            button.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = entry.Path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法启动: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            button.MouseRightButtonDown += (s, e) =>
            {
                var btn = (Button)s;
                var ent = (LaunchEntry)btn.Tag;

                var menu = new ContextMenu();
                var removeItem = new MenuItem { Header = "移除" };
                removeItem.Click += (s2, e2) =>
                {
                    _entries.Remove(ent);
                    _entryButtons.Remove(btn);
                    _listPanel.Children.Remove(btn);
                    SaveEntries();
                };
                menu.Items.Add(removeItem);
                menu.IsOpen = true;
                e.Handled = true;
            };

            var addIndex = _listPanel.Children.Count - 1;
            _listPanel.Children.Insert(Math.Max(0, addIndex), button);
            _entryButtons.Add(button);
        }

        private void SaveEntries()
        {
            var root = System.Text.Json.Nodes.JsonNode.Parse(Data.Config)?.AsObject()
                       ?? new System.Text.Json.Nodes.JsonObject();

            root["Entries"] = System.Text.Json.Nodes.JsonNode.Parse(
                System.Text.Json.JsonSerializer.Serialize(_entries));

            Data.Config = root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        protected override void RefreshContentStyles(Color bgColor, Color textColor)
        {
            foreach (var btn in _entryButtons)
            {
                btn.Foreground = new SolidColorBrush(textColor);
                btn.Template = CreateEntryButtonTemplate(bgColor);
            }
            var accent = GetEffectiveAccentColor();
            _addButton.Foreground = new SolidColorBrush(accent);
            _addButton.Background = new SolidColorBrush(bgColor);
            _addButton.Template = CreateAddButtonTemplate(accent);
        }

        private ControlTemplate CreateEntryButtonTemplate(Color bgColor)
        {
            double brightness = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255.0;
            Color hoverColor = brightness < 0.5
                ? Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x30, 0x00, 0x00, 0x00);

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            border.Name = "bd";

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var trigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(hoverColor),
                "bd"));
            template.Triggers.Add(trigger);

            return template;
        }

        private ControlTemplate CreateAddButtonTemplate(Color accent)
        {
            var lighter = Color.FromArgb(0x25, accent.R, accent.G, accent.B);

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(accent));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.Name = "bd";

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var trigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(lighter),
                "bd"));
            template.Triggers.Add(trigger);

            return template;
        }

        public class LaunchEntry
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
        }

        private class QuickLaunchConfig
        {
            public List<LaunchEntry> Entries { get; set; } = new();
        }
    }
}
