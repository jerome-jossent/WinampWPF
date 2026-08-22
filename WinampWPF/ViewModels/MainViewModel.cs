using WinampWPF.Services.Audio;
using WinampWPF.Services.Metadata;
using WinampWPF.Services.Persistence;

namespace WinampWPF.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    public PlayerViewModel Player { get; }
    public PlaylistViewModel Playlist { get; }
    private readonly SettingsService _settingsService;
    private AppSettings? _loadedSettings;

    public string ApplicationName => "WinampWPF";

    public MainViewModel()
    {
        // PARAMÈTRES
        _settingsService = new SettingsService();

        // MOTEUR AUDIO
        var audioPlayer = new NAudioPlayer();

        // CONTROLEUR
        var controller = new PlaybackController(audioPlayer);

        // PLAYER
        Player = new PlayerViewModel(controller, audioPlayer);

        // MÉTADONNÉES
        var metadataService = new TagLibMetadataService();

        // PLAYLIST
        Playlist = new PlaylistViewModel(controller, metadataService);
    }

    // CHARGEMENT
    public async Task<AppSettings> LoadSettingsAsync()
    {
        var settings = _settingsService.Load();
        _loadedSettings = settings;

        // PLAYLIST (chargement asynchrone, comme pour l'ajout par dossier)
        if (settings.PlaylistTracks.Count > 0)
            await Playlist.RestorePlaylistAsync(settings.PlaylistTracks);

        // SHUFFLE
        if (settings.ShuffleEnabled && !Player.ShuffleEnabled)
            Player.ToggleShuffleCommand.Execute(null);

        // VOLUME
        Player.Volume = Math.Clamp(settings.Volume, 0.0, 1.0);

        return settings;
    }

    // SAUVEGARDE
    public void SaveSettings(
        double width, double height, double left, double top,
        bool playlistWindowOpen, double playlistWidth, double playlistHeight, double playlistLeft, double playlistTop)
    {
        var currentTrack = Player.CurrentTrack;
        var settings =
            new AppSettings
            {
                // Playlist
                PlaylistTracks = Playlist.GetPlaylistEntries().ToList(),

                // Player
                ShuffleEnabled = Player.ShuffleEnabled,

                CurrentTrackPath = currentTrack?.FilePath,
                Volume = Player.Volume,

                // Fenêtre principale (lecteur)
                WindowWidth = width,
                WindowHeight = height,
                WindowLeft = left,
                WindowTop = top,

                // Fenêtre playlist
                PlaylistWindowOpen = playlistWindowOpen,
                PlaylistWindowWidth = playlistWidth,
                PlaylistWindowHeight = playlistHeight,
                PlaylistWindowLeft = playlistLeft,
                PlaylistWindowTop = playlistTop
            };

        _settingsService.Save(settings);
    }

    // DISPOSE
    public void Dispose()
    {
        Playlist.Dispose();
        Player.Dispose();
    }
}