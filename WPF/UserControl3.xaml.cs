using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class UserControl3 : UserControl
    {
        private bool _isDraggingProgress;
        private bool _subscribedToService;
        private bool _refreshingUI;

        public UserControl3()
        {
            InitializeComponent();

            var audio = AudioService.Instance;

            Loaded += (s, e) =>
            {
                if (!_subscribedToService)
                {
                    audio.StateChanged += OnAudioServiceStateChanged;
                    _subscribedToService = true;
                    Debug.WriteLine("[UC3] 订阅 AudioService.StateChanged");
                }
                RefreshUI();
            };

            Unloaded += (s, e) =>
            {
                if (_subscribedToService)
                {
                    audio.StateChanged -= OnAudioServiceStateChanged;
                    _subscribedToService = false;
                    Debug.WriteLine("[UC3] 取消订阅 AudioService.StateChanged");
                }
            };

            LoadAudioFiles();
            Debug.WriteLine($"[UC3] UserControl3 初始化完成, 音频文件数={AudioService.Instance.Playlist.Count}");
        }

        private void LoadAudioFiles()
        {
            var audio = AudioService.Instance;
            audio.ReloadPlaylist();
            var viewSource = (CollectionViewSource)Resources["AudioFilesViewSource"];
            viewSource.Source = audio.Playlist;
            FileListBox.ItemsSource = viewSource.View;
        }

        private void OnAudioServiceStateChanged()
        {
            Dispatcher.BeginInvoke(() => RefreshUI());
        }

        private void RefreshUI()
        {
            if (_refreshingUI) return;
            _refreshingUI = true;

            var audio = AudioService.Instance;
            var track = audio.CurrentTrack;

            if (track != null)
            {
                TxtFileInfo.Text = track.FileName + track.Extension + "  |  " +
                   FileManagerService.FormatFileSize(track.FileSize);

                if (audio.IsMediaOpening)
                {
                    TxtCurrentSong.Text = "⏳ " + track.FileName;
                }
                else
                {
                    TxtCurrentSong.Text = track.FileName;
                }
                TxtCurrentArtist.Text = track.Extension.TrimStart('.').ToUpperInvariant();
            }
            else if (audio.CurrentIndex < 0)
            {
                TxtFileInfo.Text = "未选择音频文件";
                TxtCurrentSong.Text = "🎵";
                TxtCurrentArtist.Text = "";
            }

            BtnPlayPause.Content = audio.IsPlaying ? "⏸" : "▶";

            if (audio.NaturalDuration.HasTimeSpan)
            {
                var duration = audio.NaturalDuration.TimeSpan;
                ProgressSlider.Maximum = duration.TotalSeconds;
                TxtTotalTime.Text = FormatTime(duration);
                if (!_isDraggingProgress)
                {
                    ProgressSlider.Value = audio.Position.TotalSeconds;
                    TxtCurrentTime.Text = FormatTime(audio.Position);
                }
            }
            else if (!audio.IsMediaOpening)
            {
                TxtCurrentTime.Text = "00:00";
                TxtTotalTime.Text = "00:00";
                ProgressSlider.Value = 0;
            }

            var loopMode = audio.LoopMode;
            if (loopMode == 0)
            {
                BtnLoopMode.Content = "🔁";
                BtnLoopMode.ToolTip = "顺序循环";
            }
            else
            {
                BtnLoopMode.Content = "🔂";
                BtnLoopMode.ToolTip = "单曲循环";
            }

            VolumeSlider.Value = audio.Volume;
            UpdateVolumeIcon(audio.Volume);

            if (track != null && FileListBox.SelectedItem != track)
            {
                FileListBox.SelectedItem = track;
            }

            _refreshingUI = false;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var view = GetActiveView();
            if (view == null) return;

            var filterText = SearchBox.Text?.Trim() ?? string.Empty;

            view.Filter = item =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;
                if (item is AudioItem audio)
                {
                    return audio.FileName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                }
                return true;
            };

            view.Refresh();
        }

        private void OnFileListDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[UC3] OnFileListDoubleClick 触发, SelectedItem={FileListBox.SelectedItem}");
            if (FileListBox.SelectedItem is AudioItem item)
            {
                var audio = AudioService.Instance;
                var index = audio.FindTrackIndex(item.FullPath);
                Debug.WriteLine($"[UC3]   选中: {item.DisplayName}, 索引={index}");
                if (index >= 0)
                {
                    audio.PlayTrack(index);
                }
            }
        }

        public void LoadAndPlay(string filePath)
        {
            Debug.WriteLine($"[UC3] LoadAndPlay 调用, filePath={filePath}");
            AudioService.Instance.LoadAndPlay(filePath);
        }

        private void OnCloseTrackClick(object sender, RoutedEventArgs e)
        {
            AudioService.Instance.ClearTrack();
        }

        private ICollectionView? GetActiveView()
        {
            if (FileListBox.ItemsSource is ICollectionView view)
                return view;
            return null;
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[UC3] OnPlayPauseClick");
            AudioService.Instance.TogglePlayPause();
        }

        private void OnPrevClick(object sender, RoutedEventArgs e)
        {
            AudioService.Instance.Previous();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            AudioService.Instance.Next();
        }

        private void OnProgressSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isDraggingProgress) return;
            var audio = AudioService.Instance;
            if (audio.CurrentIndex < 0) return;

            if (audio.NaturalDuration.HasTimeSpan)
            {
                var newPosition = TimeSpan.FromSeconds(ProgressSlider.Value);
                TxtCurrentTime.Text = FormatTime(newPosition);
            }
        }

        private void OnProgressDragStarted(object sender, DragStartedEventArgs e)
        {
            _isDraggingProgress = true;
        }

        private void OnProgressDragCompleted(object sender, DragCompletedEventArgs e)
        {
            _isDraggingProgress = false;
            var audio = AudioService.Instance;
            if (audio.CurrentIndex < 0) return;

            var newPosition = TimeSpan.FromSeconds(ProgressSlider.Value);
            audio.Seek(newPosition);
        }

        private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_refreshingUI) return;
            AudioService.Instance.Volume = VolumeSlider.Value;
            UpdateVolumeIcon(VolumeSlider.Value);
        }

        private void UpdateVolumeIcon(double vol)
        {
            if (vol < 0.01)
                VolumeIconLabel.Text = "🔇";
            else if (vol < 0.35)
                VolumeIconLabel.Text = "🔈";
            else
                VolumeIconLabel.Text = "🔊";
        }

        private void OnLoopModeClick(object sender, RoutedEventArgs e)
        {
            var audio = AudioService.Instance;
            audio.LoopMode = audio.LoopMode == 0 ? 1 : 0;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return string.Format("{0:0}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);
            return string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
        }
    }
}
