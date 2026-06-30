using System;

namespace WpfApp3.Services
{
    public class VideoService
    {
        private static readonly Lazy<VideoService> _instance = new(() => new VideoService());
        public static VideoService Instance => _instance.Value;

        public Uri? Source { get; set; }
        public TimeSpan Position { get; set; }
        public bool IsPlaying { get; set; }
        public double Volume { get; set; } = 0.5;
        public double SpeedRatio { get; set; } = 1.0;
        public TimeSpan? Duration { get; set; }
        public string? FileName { get; set; }
        public string? Dimensions { get; set; }
        public string? FileSize { get; set; }

        public event Action? StateChanged;

        public void SaveState(Uri? source, TimeSpan position, bool isPlaying,
            double volume, double speedRatio, TimeSpan? duration,
            string? fileName, string? dimensions, string? fileSize)
        {
            Source = source;
            Position = position;
            IsPlaying = isPlaying;
            Volume = volume;
            SpeedRatio = speedRatio;
            Duration = duration;
            FileName = fileName;
            Dimensions = dimensions;
            FileSize = fileSize;
            StateChanged?.Invoke();
        }

        public void UpdatePosition(TimeSpan position)
        {
            Position = position;
        }

        public void Clear()
        {
            Source = null;
            Position = TimeSpan.Zero;
            IsPlaying = false;
            Duration = null;
            FileName = null;
            Dimensions = null;
            FileSize = null;
        }
    }
}
