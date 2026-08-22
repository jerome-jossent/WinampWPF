namespace WinampWPF.Services.Persistence;

public sealed class AppSettings
{
    // PLAYLIST
    public List<string> PlaylistFiles { get; set; } = [];

    // LECTEUR
    public bool ShuffleEnabled { get; set; }
    public string? CurrentTrackPath { get; set; }
    public double Volume { get; set; } = 1.0;

    // FENÊTRE
    public double WindowWidth { get; set; } = 1250;
    public double WindowHeight { get; set; } = 750;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
}