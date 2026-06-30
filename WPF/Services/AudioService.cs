using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfApp3.Services
{
    public class AudioService
    {
        public static AudioService Instance { get; } = new AudioService();

        private readonly MediaPlayer _player;
        private readonly DispatcherTimer _progressTimer;
        private readonly List<AudioItem> _playlist = new();
        private int _currentIndex = -1;
        private int _loopMode = 0;
        private bool _isPlaying;
        private bool _isMediaOpening;
        private double _volume = 0.7;

        public event Action? StateChanged;

        public IReadOnlyList<AudioItem> Playlist => _playlist;
        public int CurrentIndex => _currentIndex;
        public int LoopMode
        {
            get => _loopMode;
            set
            {
                _loopMode = value;
                NotifyStateChanged();
            }
        }
        public bool IsPlaying => _isPlaying;
        public bool IsMediaOpening => _isMediaOpening;
        public double Volume
        {
            get => _volume;
            set
            {
                if (Math.Abs(_volume - value) < 0.001) return;
                _volume = value;
                _player.Volume = value;
            }
        }
        public TimeSpan Position => _player.Position;
        public Duration NaturalDuration => _player.NaturalDuration;
        public bool HasAudio => _player.HasAudio;

        public AudioItem? CurrentTrack => _currentIndex >= 0 && _currentIndex < _playlist.Count
            ? _playlist[_currentIndex] : null;

        private AudioService()
        {
            _player = new MediaPlayer();
            _player.MediaOpened += OnMediaOpened;
            _player.MediaEnded += OnMediaEnded;
            _player.MediaFailed += OnMediaFailed;
            _player.Volume = _volume;

            _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _progressTimer.Tick += OnProgressTick;

            LoadPlaylist();
        }

        public void ReloadPlaylist()
        {
            LoadPlaylist();
            NotifyStateChanged();
        }

        private void LoadPlaylist()
        {
            _playlist.Clear();
            var allFiles = App.FileManager.GetAllFiles();

            var audioFiles = allFiles
                .Where(f => FileManagerService.GetFileTypeByExtension(f.Extension) == 4)
                .OrderBy(f => f.FileName)
                .ToList();

            foreach (var file in audioFiles)
            {
                _playlist.Add(new AudioItem
                {
                    FullPath = file.FullPath,
                    FileName = file.FileName,
                    Extension = file.Extension,
                    FileSize = file.FileSize,
                    SaveTime = file.SaveTime
                });
            }

            Debug.WriteLine($"[AudioService] 加载了 {_playlist.Count} 个音频文件");
        }

        public void PlayTrack(int index)
        {
            if (index < 0 || index >= _playlist.Count) return;

            _currentIndex = index;
            var item = _playlist[index];
            _isPlaying = false;
            _isMediaOpening = true;

            _player.Volume = _volume;
            _player.Open(new Uri(item.FullPath, UriKind.Absolute));

            Debug.WriteLine($"[AudioService] PlayTrack index={index}: {item.DisplayName}");
            NotifyStateChanged();
        }

        public void TogglePlayPause()
        {
            if (_currentIndex < 0 || _currentIndex >= _playlist.Count) return;
            if (_isMediaOpening) return;

            if (_isPlaying)
                Pause();
            else
                Play();
        }

        public void Play()
        {
            _player.Play();
            _isPlaying = true;
            _isMediaOpening = false;
            _progressTimer.Start();
            Debug.WriteLine("[AudioService] Play");
            NotifyStateChanged();
        }

        public void Pause()
        {
            _player.Pause();
            _isPlaying = false;
            _progressTimer.Stop();
            Debug.WriteLine("[AudioService] Pause");
            NotifyStateChanged();
        }

        public void Stop()
        {
            _player.Stop();
            _isPlaying = false;
            _isMediaOpening = false;
            _progressTimer.Stop();
            Debug.WriteLine("[AudioService] Stop");
            NotifyStateChanged();
        }

        public void ClearTrack()
        {
            _player.Stop();
            _player.Close();
            _isPlaying = false;
            _isMediaOpening = false;
            _currentIndex = -1;
            _progressTimer.Stop();
            Debug.WriteLine("[AudioService] ClearTrack");
            NotifyStateChanged();
        }

        public void Next()
        {
            if (_playlist.Count == 0) return;
            EnsureIndexValid();
            var index = _currentIndex + 1;
            if (index >= _playlist.Count) index = 0;
            PlayTrack(index);
        }

        public void Previous()
        {
            if (_playlist.Count == 0) return;
            EnsureIndexValid();
            var index = _currentIndex - 1;
            if (index < 0) index = _playlist.Count - 1;
            PlayTrack(index);
        }

        public void Seek(TimeSpan position)
        {
            _player.Position = position;
        }

        public int FindTrackIndex(string fullPath)
        {
            return _playlist.FindIndex(a =>
                a.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
        }

        public void LoadAndPlay(string filePath)
        {
            var index = FindTrackIndex(filePath);
            if (index < 0) return;
            PlayTrack(index);
        }

        private void EnsureIndexValid()
        {
            if (_currentIndex < 0 || _currentIndex >= _playlist.Count)
                _currentIndex = 0;
        }

        private void OnMediaOpened(object? sender, EventArgs e)
        {
            Debug.WriteLine($"[AudioService] OnMediaOpened: Duration={_player.NaturalDuration}, HasAudio={_player.HasAudio}");
            Play();
        }

        private void OnMediaEnded(object? sender, EventArgs e)
        {
            Debug.WriteLine($"[AudioService] OnMediaEnded: LoopMode={_loopMode}");
            _progressTimer.Stop();

            if (_loopMode == 1)
            {
                _player.Position = TimeSpan.Zero;
                _player.Play();
                _isPlaying = true;
                _progressTimer.Start();
                NotifyStateChanged();
            }
            else
            {
                Next();
            }
        }

        private void OnMediaFailed(object? sender, ExceptionEventArgs e)
        {
            Debug.WriteLine($"[AudioService] OnMediaFailed: {e.ErrorException?.Message}");
            _isMediaOpening = false;
            _progressTimer.Stop();
            _isPlaying = false;
            NotifyStateChanged();
            MessageBox.Show($"无法播放此文件。\n{e.ErrorException?.Message ?? "不支持的格式或文件已损坏"}",
                "播放失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OnProgressTick(object? sender, EventArgs e)
        {
            if (!_isPlaying) return;
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }
    }

    public class AudioItem
    {
        public string FullPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime SaveTime { get; set; }

        public string DisplayName => FileName + Extension;

        public string GroupKey
        {
            get
            {
                if (string.IsNullOrEmpty(FileName)) return "#";
                var first = char.ToUpperInvariant(FileName[0]);
                if (first >= 'A' && first <= 'Z')
                    return first.ToString();
                if (char.IsDigit(first))
                    return "#";
                return "#";
            }
        }
    }
}
