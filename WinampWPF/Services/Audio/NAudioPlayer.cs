using NAudio.Wave;
using System.IO;
using System.Windows.Threading;

namespace WinampWPF.Services.Audio;

public sealed class NAudioPlayer : IAudioPlayer
{
    private WaveOut? _outputDevice;
    private AudioFileReader? _audioFile;

    private readonly DispatcherTimer _positionTimer;

    private bool _disposed;
    private bool _manualStop;
    private bool _isMuted;

    private float _volumeBeforeMute = 1.0f;

    public PlaybackState State { get; private set; } = PlaybackState.Stopped;
    public string? CurrentFilePath { get; private set; }
    public TimeSpan Position => _audioFile?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _audioFile?.TotalTime ?? TimeSpan.Zero;

    public float Volume
    {
        get => _audioFile?.Volume ?? _volumeBeforeMute;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            _volumeBeforeMute = clamped;
            if (_audioFile is not null && !_isMuted)
                _audioFile.Volume = clamped;
        }
    }

    public bool IsMuted => _isMuted;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackPaused;
    public event EventHandler? PlaybackStopped;
    public event EventHandler? PlaybackEnded;
    public event EventHandler? PositionChanged;

    public NAudioPlayer()
    {
        _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _positionTimer.Tick += PositionTimer_Tick;
    }

    public void Load(string filePath)
    {
        ThrowIfDisposed();

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Le fichier audio est introuvable.", filePath);

        StopInternal();

        _audioFile = new AudioFileReader(filePath);

        CurrentFilePath = filePath;

        _audioFile.Volume = _isMuted ? 0f : _volumeBeforeMute;

        _outputDevice = new WaveOut();

        _outputDevice.Init(_audioFile);

        _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;

        State = PlaybackState.Stopped;

        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Play()
    {
        ThrowIfDisposed();

        if (_audioFile is null || _outputDevice is null)
            return;


        if (Position >= Duration)
            _audioFile.Position = 0;

        _manualStop = false;

        _outputDevice.Play();

        State = PlaybackState.Playing;

        _positionTimer.Start();

        PlaybackStarted?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        ThrowIfDisposed();

        if (_outputDevice is null || State != PlaybackState.Playing)
            return;

        _outputDevice.Pause();

        State = PlaybackState.Paused;

        PlaybackPaused?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        ThrowIfDisposed();

        StopInternal();

        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Seek(TimeSpan position)
    {
        ThrowIfDisposed();

        if (_audioFile is null)
            return;

        var clamped = position;

        if (clamped < TimeSpan.Zero)
            clamped = TimeSpan.Zero;

        if (clamped > Duration)
            clamped = Duration;

        _audioFile.CurrentTime = clamped;

        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleMute()
    {
        ThrowIfDisposed();

        if (_audioFile is null)
            return;

        if (_isMuted)
        {
            _isMuted = false;
            _audioFile.Volume = Math.Clamp(_volumeBeforeMute, 0f, 1f);
        }
        else
        {
            _volumeBeforeMute = _audioFile.Volume;
            _audioFile.Volume = 0f;
            _isMuted = true;
        }
    }

    private void PositionTimer_Tick(object? sender, EventArgs e)
    {
        PositionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _positionTimer.Stop();

        if (_audioFile is null)
            return;

        var reachedEnd = !_manualStop && _audioFile.Position >= _audioFile.Length - 1;

        if (reachedEnd)
        {
            State = PlaybackState.Stopped;
            PositionChanged?.Invoke(this, EventArgs.Empty);
            PlaybackEnded?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (!_manualStop)
        {
            State = PlaybackState.Stopped;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private void StopInternal()
    {
        _manualStop = true;
        _positionTimer.Stop();

        if (_outputDevice is not null)
        {
            _outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }

        _audioFile?.Dispose();
        _audioFile = null;
        CurrentFilePath = null;
        State = PlaybackState.Stopped;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        StopInternal();
    }
}