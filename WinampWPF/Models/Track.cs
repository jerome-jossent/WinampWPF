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

    // Dossier racine depuis lequel ce morceau a été chargé
    // (ex : le dossier choisi via "+ Dossier" ou glissé-déposé).
    // Sert à n'afficher que le chemin "incomplet" (sous-dossiers)
    // dans la colonne Fichier. Null si le fichier a été ajouté
    // individuellement : dans ce cas on affiche juste son nom.
    [ObservableProperty]
    private string? sourceRootFolder;

    public string FileName => Path.GetFileName(FilePath);

    // Chemin affiché dans la colonne "Fichier" : le chemin relatif
    // au dossier racine chargé (ex : "B\c.mp3" pour d:\A\B\c.mp3
    // chargé depuis d:\A\), ou simplement le nom du fichier si
    // aucun dossier racine n'est connu.
    public string DisplayFile
    {
        get
        {
            if (string.IsNullOrEmpty(SourceRootFolder))
                return FileName;

            try
            {
                var relative = Path.GetRelativePath(SourceRootFolder, FilePath);

                // Si le calcul échoue à produire un sous-chemin
                // cohérent (lecteurs différents, etc.), on retombe
                // sur le simple nom de fichier.
                return relative.StartsWith("..") ? FileName : relative;
            }
            catch
            {
                return FileName;
            }
        }
    }

    partial void OnSourceRootFolderChanged(string? value) => OnPropertyChanged(nameof(DisplayFile));

    partial void OnFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(FileName));
        OnPropertyChanged(nameof(DisplayFile));
    }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Path.GetFileNameWithoutExtension(FilePath) : Title;

    public string DisplayArtist => string.IsNullOrWhiteSpace(Artist) ? "Artiste inconnu" : Artist;

    public string DisplayAlbum => string.IsNullOrWhiteSpace(Album) ? "Album inconnu" : Album;

    public string DurationText => Duration.TotalHours >= 1
            ? Duration.ToString(@"h\:mm\:ss")
            : Duration.ToString(@"mm\:ss");
}