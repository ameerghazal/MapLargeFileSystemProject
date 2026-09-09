using TestProject.Models;

namespace TestProject.Services;

// Defines what the service can do without defining how.
public interface IFileSystemService
{
    BrowseResponse Browse(string? path);
    SearchResponse Search(string? path, string query);
    FileDownload Download(string path);
    Task<FileSystemItem> UploadAsync(
        string? path, 
        string fileName, 
        Stream content,
        CancellationToken cancellationToken
    );
}
