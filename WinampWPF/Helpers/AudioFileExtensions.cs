using System.IO;

namespace WinampWPF.Helpers;

public static class AudioFileExtensions
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3",
            ".wav",
            ".flac",
            ".m4a",
            ".aac",
            ".wma",
            ".ogg",
            ".opus",
            ".aiff",
            ".ape"
        };

    public static bool IsAudioFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return SupportedExtensions.Contains(extension);
    }
}