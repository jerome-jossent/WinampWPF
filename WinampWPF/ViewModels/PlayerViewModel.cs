using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WinampWPF.Models;
using WinampWPF.Services.Audio;

namespace WinampWPF.ViewModels;

public partial class PlayerViewModel : ViewModelBase, IDisposable
{
    private readonly PlaybackController _controller;
    private readonly IAudioPlayer _audioPlayer;

    [ObservableProperty]
    private Track? _currentTrack;

    [ObservableProperty]
    private string _title = "Aucun morceau";

    [ObservableProperty]
    private string _artist = "";

    [ObservableProperty]
    private string _album = "";

    [ObservableProperty]
    private TimeSpan _position = TimeSpan.Zero;

    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;

    [ObservableProperty]
    private double _positionSeconds;

    [ObservableProperty]
    private double _durationSeconds;

    [ObservableProperty]
    private double _volume = 1.0;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _shuffleEnabled;

    public string ElapsedTime => FormatTime(Position);

    public string RemainingTime => FormatTime(Duration - Position);

    public PlayerViewModel(PlaybackController controller, IAudioPlayer audioPlayer)
    {
        _controller = controller;
        _audioPlayer = audioPlayer;

        // AUDIO PLAYER
        _audioPlayer.PlaybackStarted += AudioPlayer_PlaybackStarted;
        _audioPlayer.PlaybackPaused += AudioPlayer_PlaybackPaused;
        _audioPlayer.PlaybackStopped += AudioPlayer_PlaybackStopped;
        _audioPlayer.PlaybackEnded += AudioPlayer_PlaybackEnded;
        _audioPlayer.PositionChanged += AudioPlayer_PositionChanged;

        // PLAYBACK CONTROLLER
        _controller.CurrentTrackChanged += Controller_CurrentTrackChanged;
        _controller.ShuffleChanged += Controller_ShuffleChanged;

        // Synchronisation initiale
        ShuffleEnabled = _controller.ShuffleEnabled;
    }

    // MORCEAU COURANT
    private void Controller_CurrentTrackChanged(object? sender, EventArgs e)
    {
        RunOnUi(() =>
        {
            CurrentTrack = _controller.CurrentTrack;
            UpdateTrackMetadata();
            UpdatePosition();
        });
    }

    private void UpdateTrackMetadata()
    {
        if (CurrentTrack is null)
        {
            Title = "Aucun morceau";
            Artist = "";
            Album = "";

            return;
        }

        Title = CurrentTrack.DisplayTitle;
        Artist = CurrentTrack.DisplayArtist;
        Album = CurrentTrack.DisplayAlbum;
    }

    // PLAY TRACK
    public void PlayTrack(Track track)
    {
        if (track is null)
            return;
        _controller.Play(track);
    }

    // PLAY
    [RelayCommand]
    private void Play()
    {
        _controller.PlayCurrent();
    }

    // PAUSE
    [RelayCommand]
    private void Pause()
    {
        _controller.Pause();
    }

    // STOP
    [RelayCommand]
    private void Stop()
    {
        _controller.Stop();
    }

    // PLAY / PAUSE
    [RelayCommand]
    private void TogglePlayPause()
    {
        if (IsPlaying)
            _controller.Pause();
        else
            _controller.PlayCurrent();

    }

    // PREVIOUS
    [RelayCommand]
    private void Previous()
    {
        _controller.Previous();
    }

    // NEXT
    [RelayCommand]
    private void Next()
    {
        _controller.Next();
    }

    // SHUFFLE
    [RelayCommand]
    private void ToggleShuffle()
    {
        _controller.ToggleShuffle();
        ShuffleEnabled = _controller.ShuffleEnabled;
    }

    private void Controller_ShuffleChanged(object? sender, EventArgs e)
    {
        RunOnUi(() => { ShuffleEnabled = _controller.ShuffleEnabled; });
    }

    // MUTE
    [RelayCommand]
    private void ToggleMute()
    {
        _audioPlayer.ToggleMute();
        IsMuted = _audioPlayer.IsMuted;
    }

    // VOLUME
    partial void OnVolumeChanged(double value)
    {
        _audioPlayer.Volume = (float)Math.Clamp(value, 0.0, 1.0);
    }

    // POSITION / SEEK
    partial void OnPositionSecondsChanged(double value)
    {
        if (_audioPlayer.Duration <= TimeSpan.Zero)
            return;

        // Évite de rappeler Seek() lorsque
        // la valeur vient simplement du timer
        // de lecture.
        if (Math.Abs(_audioPlayer.Position.TotalSeconds - value) < 0.2)
            return;

        _audioPlayer.Seek(TimeSpan.FromSeconds(value));
    }

    // AUDIO EVENTS
    private void AudioPlayer_PlaybackStarted(object? sender, EventArgs e)
    {
        RunOnUi(() =>
        {
            IsPlaying = true;
            IsPaused = false;

            UpdatePosition();
        });
    }

    private void AudioPlayer_PlaybackPaused(object? sender, EventArgs e)
    {
        RunOnUi(() =>
        {
            IsPlaying = false;
            IsPaused = true;

            UpdatePosition();
        });
    }

    private void AudioPlayer_PlaybackStopped(object? sender, EventArgs e)
    {
        RunOnUi(() =>
        {
            IsPlaying = false;
            IsPaused = false;

            UpdatePosition();
        });
    }

    private void AudioPlayer_PlaybackEnded(object? sender, EventArgs e)
    {
        RunOnUi(() =>
        {
            IsPlaying = false;
            IsPaused = false;

            UpdatePosition();
        });
    }

    private void AudioPlayer_PositionChanged(object? sender, EventArgs e)
    {
        RunOnUi(UpdatePosition);
    }

    // POSITION
    private void UpdatePosition()
    {
        Position = _audioPlayer.Position;
        Duration = _audioPlayer.Duration;
        PositionSeconds = Position.TotalSeconds;
        DurationSeconds = Duration.TotalSeconds;
        OnPropertyChanged(nameof(ElapsedTime));
        OnPropertyChanged(nameof(RemainingTime));
    }

    // FORMAT TIME
    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
            time = TimeSpan.Zero;

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    // UI THREAD
    private static void RunOnUi(Action action)
    {
        if (Application.Current?.Dispatcher is null)
        {
            action();
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
        {
            action();
            return;
        }
        Application.Current.Dispatcher.Invoke(action);
    }

    // DISPOSE
    public void Dispose()
    {
        // AudioPlayer
        _audioPlayer.PlaybackStarted -= AudioPlayer_PlaybackStarted;
        _audioPlayer.PlaybackPaused -= AudioPlayer_PlaybackPaused;
        _audioPlayer.PlaybackStopped -= AudioPlayer_PlaybackStopped;
        _audioPlayer.PlaybackEnded -= AudioPlayer_PlaybackEnded;
        _audioPlayer.PositionChanged -= AudioPlayer_PositionChanged;

        // Controller
        _controller.CurrentTrackChanged -= Controller_CurrentTrackChanged;
        _controller.ShuffleChanged -= Controller_ShuffleChanged;
    }
}