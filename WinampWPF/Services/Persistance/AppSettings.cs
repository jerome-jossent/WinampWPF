using WinampWPF.Models;

namespace WinampWPF.Services.Persistence;

public sealed class AppSettings
{
    // PLAYLIST
    public List<PlaylistFileEntry> PlaylistTracks { get; set; } = [];

    // Ancien format (chemins seuls, sans dossier racine).
    // Conservé uniquement pour migrer automatiquement les
    // fichiers settings.json créés par les versions précédentes ;
    // n'est plus écrit par les versions actuelles.
    public List<string>? PlaylistFiles { get; set; }

    // LECTEUR
    public bool ShuffleEnabled { get; set; }
    public string? CurrentTrackPath { get; set; }
    public double Volume { get; set; } = 1.0;

    // FENÊTRE PRINCIPALE (LECTEUR)
    public double WindowWidth { get; set; } = 1250;
    public double WindowHeight { get; set; } = 750;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;

    // FENÊTRE PLAYLIST
    // Décrochée du lecteur : sa propre visibilité, taille et
    // position sont mémorisées indépendamment.
    public bool PlaylistWindowOpen { get; set; } = true;
    public double PlaylistWindowWidth { get; set; } = 700;
    public double PlaylistWindowHeight { get; set; } = 500;
    public double PlaylistWindowLeft { get; set; } = double.NaN;
    public double PlaylistWindowTop { get; set; } = double.NaN;
}