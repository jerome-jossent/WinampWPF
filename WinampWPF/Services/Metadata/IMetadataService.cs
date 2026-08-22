using WinampWPF.Models;

namespace WinampWPF.Services.Metadata;

public interface IMetadataService
{
    Track ReadMetadata(string filePath);
}