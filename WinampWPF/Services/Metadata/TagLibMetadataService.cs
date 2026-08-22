using System;
using System.IO;
using TagLib;
using WinampWPF.Models;

namespace WinampWPF.Services.Metadata;

public sealed class TagLibMetadataService
{
    public Track ReadMetadata(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Le chemin du fichier est vide.", nameof(filePath));


        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("Le fichier audio est introuvable.", filePath);

        var track = new Track
        {
            FilePath = filePath,
            Title = Path.GetFileNameWithoutExtension(filePath),
            Artist = string.Empty,
            Album = string.Empty
        };

        try
        {
            using var file = TagLib.File.Create(filePath);

            var tag = file.Tag;

            if (!string.IsNullOrWhiteSpace(tag.Title))
                track.Title = tag.Title;

            if (tag.Performers is { Length: > 0 })
                track.Artist = tag.Performers[0];

            if (!string.IsNullOrWhiteSpace(tag.Album))
                track.Album = tag.Album;

            track.Duration = file.Properties.Duration;
        }
        catch
        {
            // Le fichier peut être lisible par NAudio
            // même si ses métadonnées sont absentes ou invalides.
        }

        return track;
    }
}