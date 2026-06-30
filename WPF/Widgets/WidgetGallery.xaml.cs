using System;
using System.Windows.Controls;

namespace WpfApp3.Widgets
{
    public partial class WidgetGallery : UserControl
    {
        public event Action<string>? WidgetSelected;
        public event Action? Cancelled;

        public WidgetGallery()
        {
            InitializeComponent();
        }

        private void BtnClock_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("Clock");
        }

        private void BtnDate_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("Date");
        }

        private void BtnStickyNote_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("StickyNote");
        }

        private void BtnQuickLaunch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("QuickLaunch");
        }

        private void BtnImage_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("Image");
        }

        private void BtnAudio_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            WidgetSelected?.Invoke("Audio");
        }

        private void BtnCancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Cancelled?.Invoke();
        }
    }
}
