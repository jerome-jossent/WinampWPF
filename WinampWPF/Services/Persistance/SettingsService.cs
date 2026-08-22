using System.IO;
using System.Text.Json;
using WinampWPF.Models;

namespace WinampWPF.Services.Persistence;

public sealed class SettingsService
{
    private readonly string _settingsDirectory;
    private readonly string _settingsFile;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        // DOSSIER DE CONFIGURATION

        _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinampWPF");
        _settingsFile = Path.Combine(_settingsDirectory, "settings.json");
    }

    // CHARGEMENT
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFile))
                return new AppSettings();

            var json = File.ReadAllText(_settingsFile);

            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();

            // MIGRATION : anciens fichiers settings.json (liste de
            // simples chemins, sans dossier racine par morceau).
            if (settings.PlaylistTracks.Count == 0 && settings.PlaylistFiles is { Count: > 0 })
            {
                settings.PlaylistTracks = settings.PlaylistFiles
                    .Select(f => new PlaylistFileEntry(f))
                    .ToList();

                settings.PlaylistFiles = null;
            }

            return settings;
        }
        catch
        {
            // Si le fichier est corrompu ou illisible,
            // on repart sur les valeurs par défaut.
            return new AppSettings();
        }
    }

    // SAUVEGARDE
    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_settingsDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsFile, json);
        }
        catch
        {
            // On ne doit jamais faire planter
            // l'application simplement parce que
            // la configuration ne peut pas être sauvegardée.
        }
    }
}