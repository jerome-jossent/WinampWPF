using System.IO;
using WinampWPF.Helpers;
using WinampWPF.Models;
using WinampWPF.Services.Metadata;

namespace WinampWPF.Services.Playlist;

public sealed class PlaylistService : IPlaylistService
{
    private readonly IMetadataService _metadataService;

    public PlaylistService(IMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    public IReadOnlyList<Track> LoadFiles(IEnumerable<string> filePaths)
    {
        var tracks = new List<Track>();

        foreach (var filePath in filePaths)
        {
            if (!AudioFileExtensions.IsAudioFile(filePath))
                continue;

            if (!File.Exists(filePath))
                continue;

            try
            {
                var track = _metadataService.ReadMetadata(filePath);
                tracks.Add(track);
            }
            catch
            {
                // On ignore les fichiers problématiques.
            }
        }
        return tracks;
    }

    public IReadOnlyList<Track> LoadFolder(string folderPath, bool recursive = true)
    {
        if (!Directory.Exists(folderPath))
            return [];
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(folderPath, "*.*", option).Where(AudioFileExtensions.IsAudioFile);
        return LoadFiles(files);
    }
}