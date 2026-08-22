using WinampWPF.Models;

namespace WinampWPF.Services.Playlist;

public interface IPlaylistService
{
    IReadOnlyList<Track> LoadFiles(IEnumerable<string> filePaths);
    IReadOnlyList<Track> LoadFolder(string folderPath, bool recursive = true);
}