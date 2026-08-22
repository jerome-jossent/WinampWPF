using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using WinampWPF.Models;
using WinampWPF.Services.Audio;
using WinampWPF.Services.Metadata;

namespace WinampWPF.ViewModels;

public partial class PlaylistViewModel : ViewModelBase
{
    private readonly PlaybackController _controller;
    private readonly TagLibMetadataService _metadataService;

    public event EventHandler? CurrentTrackChanged;

    // PLAYLIST
    public ObservableCollection<Track> Tracks { get; } = [];
    public ObservableCollection<Track> FilteredTracks { get; } = [];
    public ObservableCollection<Track> SelectedTracks { get; } = [];

    public Track? CurrentTrack => _controller.CurrentTrack;

    // SELECTION
    [ObservableProperty]
    private Track? _selectedTrack;

    // RECHERCHE
    [ObservableProperty]
    private string _searchText = string.Empty;

    // TRI
    private string _sortColumn = "Title";
    private bool _sortAscending = true;

    // Tri secondaire (sous-tri), utilisé pour départager
    // les égalités du tri primaire. Déclenché par un
    // clic droit sur un en-tête de colonne.
    private string? _secondaryColumn;

    private bool _secondaryAscending = true;

    public string TitleSortHeader => BuildSortHeader("Titre", "Title");
    public string ArtistSortHeader => BuildSortHeader("Artiste", "Artist");
    public string AlbumSortHeader => BuildSortHeader("Album", "Album");
    public string DurationSortHeader => BuildSortHeader("Durée", "Duration");
    public string FileSortHeader => BuildSortHeader("Fichier", "File");

    // INFORMATIONS PLAYLIST
    public int TrackCount => Tracks.Count;

    public string TotalDurationText => FormatTime(Tracks.Sum(t => t.Duration.TotalSeconds));

    // CONSTRUCTEUR
    public PlaylistViewModel(
        PlaybackController controller,
        TagLibMetadataService metadataService)
    {
        _controller = controller;
        _metadataService = metadataService;

        _controller.CurrentTrackChanged += Controller_CurrentTrackChanged;
    }

    // RESTAURATION
    public IReadOnlyList<string> GetPlaylistFilePaths()
    {
        return Tracks.Select(track => track.FilePath).ToList();
    }

    public void RestorePlaylist(IEnumerable<string> filePaths)
    {
        AddFilesInternal(filePaths);
    }

    // AJOUT DE FICHIERS
    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter =
                    "Fichiers audio|" +
                    "*.mp3;*.wav;*.flac;*.ogg;*.m4a;*.aac;*.wma|" +
                    "Tous les fichiers|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        AddFilesInternal(dialog.FileNames);
    }

    // AJOUT D'UN DOSSIER
    [RelayCommand]
    private void AddFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Sélectionner un dossier musical",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        var files = Directory.GetFiles(dialog.FolderName, "*.*", SearchOption.AllDirectories).Where(IsSupportedAudioFile);
        AddFilesInternal(files);
    }

    // DRAG & DROP
    public void AddDroppedFiles(IEnumerable<string> paths)
    {
        var files = new List<string>();

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                if (IsSupportedAudioFile(path))
                    files.Add(path);
                continue;
            }

            if (Directory.Exists(path))
                files.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(IsSupportedAudioFile));
        }

        AddFilesInternal(files);
    }

    // AJOUT INTERNE
    private void AddFilesInternal(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            if (!File.Exists(file))
                continue;

            if (!IsSupportedAudioFile(file))
                continue;

            if (Tracks.Any(t => string.Equals(t.FilePath, file, StringComparison.OrdinalIgnoreCase)))
                continue;


            try
            {
                var track = _metadataService.ReadMetadata(file);
                Tracks.Add(track);
            }
            catch
            {
                // On ignore les fichiers impossibles à lire.
            }
        }

        ApplyCurrentSort();

        RefreshFilteredTracks();

        SyncController();

        NotifyPlaylistChanged();
    }

    // SUPPRESSION
    [RelayCommand]
    private void RemoveSelected()
    {
        foreach (var track in SelectedTracks.ToList())
            Tracks.Remove(track);

        SelectedTracks.Clear();

        ApplyCurrentSort();

        RefreshFilteredTracks();

        SyncController();

        NotifyPlaylistChanged();
    }

    // VIDER
    [RelayCommand]
    private void Clear()
    {
        Tracks.Clear();

        SelectedTracks.Clear();

        RefreshFilteredTracks();

        SyncController();

        NotifyPlaylistChanged();
    }

    // TRI
    [RelayCommand]
    private void Sort(string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
            return;

        if (string.Equals(_sortColumn, column, StringComparison.Ordinal))
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumn = column;
            _sortAscending = true;

            // Le tri secondaire ne peut pas porter
            // sur la même colonne que le tri primaire.
            if (string.Equals(_secondaryColumn, column, StringComparison.Ordinal))
                _secondaryColumn = null;
        }

        ApplyCurrentSort();

        RefreshFilteredTracks();

        NotifySortHeadersChanged();
    }

    // SOUS-TRI (TRI SECONDAIRE)
    [RelayCommand]
    private void SecondarySort(string? column)
    {
        if (string.IsNullOrWhiteSpace(column))
            return;

        // On ne peut pas sous-trier sur la colonne qui sert déjà de tri primaire.
        if (string.Equals(_sortColumn, column, StringComparison.Ordinal))
            return;

        if (string.Equals(_secondaryColumn, column, StringComparison.Ordinal))
        {
            _secondaryAscending = !_secondaryAscending;
        }
        else
        {
            _secondaryColumn = column;
            _secondaryAscending = true;
        }

        ApplyCurrentSort();

        RefreshFilteredTracks();

        NotifySortHeadersChanged();
    }

    private void ApplyCurrentSort()
    {
        var ordered = ApplyOrderBy(Tracks, _sortColumn, _sortAscending);

        if (!string.IsNullOrEmpty(_secondaryColumn))
            ordered = ApplyThenBy(ordered, _secondaryColumn, _secondaryAscending);

        var list = ordered.ToList();

        Tracks.Clear();

        foreach (var track in list)
            Tracks.Add(track);
    }

    private static IOrderedEnumerable<Track> ApplyOrderBy(IEnumerable<Track> source, string column, bool ascending)
    {
        return column switch
        {
            "Artist" => ascending ? source.OrderBy(t => t.DisplayArtist, StringComparer.CurrentCultureIgnoreCase)
                    : source.OrderByDescending(t => t.DisplayArtist, StringComparer.CurrentCultureIgnoreCase),

            "Album" => ascending ? source.OrderBy(t => t.DisplayAlbum, StringComparer.CurrentCultureIgnoreCase)
                    : source.OrderByDescending(t => t.DisplayAlbum, StringComparer.CurrentCultureIgnoreCase),

            "Duration" => ascending ? source.OrderBy(t => t.Duration) : source.OrderByDescending(t => t.Duration),

            "File" => ascending ? source.OrderBy(t => t.FileName, StringComparer.CurrentCultureIgnoreCase)
                    : source.OrderByDescending(t => t.FileName, StringComparer.CurrentCultureIgnoreCase),

            // "Title" et valeur par défaut.
            _ => ascending ? source.OrderBy(t => t.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
                    : source.OrderByDescending(t => t.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
        };
    }

    private static IOrderedEnumerable<Track> ApplyThenBy(IOrderedEnumerable<Track> source, string column, bool ascending)
    {
        return column switch
        {
            "Artist" => ascending ? source.ThenBy(t => t.DisplayArtist, StringComparer.CurrentCultureIgnoreCase)
                    : source.ThenByDescending(t => t.DisplayArtist, StringComparer.CurrentCultureIgnoreCase),

            "Album" => ascending ? source.ThenBy(t => t.DisplayAlbum, StringComparer.CurrentCultureIgnoreCase)
                    : source.ThenByDescending(t => t.DisplayAlbum, StringComparer.CurrentCultureIgnoreCase),

            "Duration" => ascending ? source.ThenBy(t => t.Duration) : source.ThenByDescending(t => t.Duration),

            "File" => ascending ? source.ThenBy(t => t.FileName, StringComparer.CurrentCultureIgnoreCase)
                    : source.ThenByDescending(t => t.FileName, StringComparer.CurrentCultureIgnoreCase),

            "Title" => ascending ? source.ThenBy(t => t.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
                    : source.ThenByDescending(t => t.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),

            _ => source,
        };
    }

    private string BuildSortHeader(string title, string column)
    {
        if (string.Equals(_sortColumn, column, StringComparison.Ordinal))
            return _sortAscending ? $"{title} ▲" : $"{title} ▼";

        if (string.Equals(_secondaryColumn, column, StringComparison.Ordinal))
            return _secondaryAscending ? $"{title} ▲₂" : $"{title} ▼₂";

        return title;
    }

    private void NotifySortHeadersChanged()
    {
        OnPropertyChanged(nameof(TitleSortHeader));
        OnPropertyChanged(nameof(ArtistSortHeader));
        OnPropertyChanged(nameof(AlbumSortHeader));
        OnPropertyChanged(nameof(DurationSortHeader));
        OnPropertyChanged(nameof(FileSortHeader));
    }

    // RECHERCHE
    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredTracks();
    }

    private void RefreshFilteredTracks()
    {
        var query = SearchText.Trim();

        FilteredTracks.Clear();

        IEnumerable<Track> result = Tracks;

        if (!string.IsNullOrWhiteSpace(query))
        {
            result = Tracks.Where(track => track.DisplayTitle.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || track.DisplayArtist.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || track.DisplayAlbum.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || track.FileName.Contains(query, StringComparison.CurrentCultureIgnoreCase));
        }

        foreach (var track in result)
            FilteredTracks.Add(track);
    }

    // QUEUE
    [RelayCommand]
    private void EnqueueSelected()
    {
        foreach (var track in SelectedTracks.ToList())
            _controller.Enqueue(track);
    }

    // LECTURE
    public void PlaySelectedTrack(Track track)
    {
        if (track is null)
            return;
        _controller.Play(track);
    }

    // SYNCHRONISATION CONTROLEUR
    private void SyncController()
    {
        _controller.SetTracks(Tracks);
    }

    // MORCEAU COURANT
    private void Controller_CurrentTrackChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher is null)
        {
            NotifyCurrentTrackChanged();
            return;
        }
        Application.Current.Dispatcher.Invoke(NotifyCurrentTrackChanged);
    }

    private void NotifyCurrentTrackChanged()
    {
        OnPropertyChanged(nameof(CurrentTrack));
        RefreshFilteredTracks();
        CurrentTrackChanged?.Invoke(this, EventArgs.Empty);
    }

    // NOTIFICATIONS
    private void NotifyPlaylistChanged()
    {
        OnPropertyChanged(nameof(TrackCount));
        OnPropertyChanged(nameof(TotalDurationText));
    }

    // EXTENSIONS AUDIO
    private static bool IsSupportedAudioFile(string file)
    {
        var extension = Path.GetExtension(file);
        return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".flac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".aac", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wma", StringComparison.OrdinalIgnoreCase);
    }

    // FORMATAGE TEMPS
    private static string FormatTime(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    // DISPOSE
    public void Dispose()
    {
        _controller.CurrentTrackChanged -= Controller_CurrentTrackChanged;
    }
}