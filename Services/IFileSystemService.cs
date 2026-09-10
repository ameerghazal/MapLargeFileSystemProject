using TestProject.Models;

namespace TestProject.Services;

public interface IFileSystemService
{
    BrowseResponse Browse(string? path);
    SearchResponse Search(string? path, string query);
    FileDownload Download(string path);
    Task<FileSystemItem> UploadAsync(
        string? path, 
        string fileName, 
        Stream content
    );
    void Delete(string path);
}
