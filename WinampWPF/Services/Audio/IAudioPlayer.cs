namespace WinampWPF.Services.Audio;

public interface IAudioPlayer : IDisposable
{
    PlaybackState State { get; }

    string? CurrentFilePath { get; }

    TimeSpan Position { get; }

    TimeSpan Duration { get; }

    float Volume { get; set; }

    bool IsMuted { get; }

    void Load(string filePath);

    void Play();

    void Pause();

    void Stop();

    void Seek(TimeSpan position);

    void ToggleMute();

    event EventHandler? PlaybackStarted;

    event EventHandler? PlaybackPaused;

    event EventHandler? PlaybackStopped;

    event EventHandler? PlaybackEnded;

    event EventHandler? PositionChanged;
}