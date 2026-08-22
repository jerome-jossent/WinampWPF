using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinampWPF.Models;

public partial class Track : ObservableObject
{
    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string artist = string.Empty;

    [ObservableProperty]
    private string album = string.Empty;

    [ObservableProperty]
    private TimeSpan duration = TimeSpan.Zero;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private bool isMissing;

    public string FileName => Path.GetFileName(FilePath);

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Path.GetFileNameWithoutExtension(FilePath) : Title;

    public string DisplayArtist => string.IsNullOrWhiteSpace(Artist) ? "Artiste inconnu" : Artist;

    public string DisplayAlbum => string.IsNullOrWhiteSpace(Album) ? "Album inconnu" : Album;

    public string DurationText => Duration.TotalHours >= 1
            ? Duration.ToString(@"h\:mm\:ss")
            : Duration.ToString(@"mm\:ss");
}