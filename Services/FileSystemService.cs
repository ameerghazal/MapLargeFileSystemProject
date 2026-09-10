using Microsoft.Extensions.Options;
using TestProject.Models;
using TestProject.Options;

namespace TestProject.Services;

public sealed class FileSystemService(
    FilePathResolve paths,
    IOptions<FileBrowserOptions> options,
    ILogger<FileSystemService> logger) : IFileSystemService
{
    private readonly FileBrowserOptions _settings = options.Value;

    public BrowseResponse Browse(string? path)
    {
        var directory = GetDirectory(path);

        var items = SortItems(directory.EnumerateFileSystemInfos()
            .Where(entry => !FilePathResolve.IsReparsePoint(entry))
            .Select(ToFileSystemItem));

        return new BrowseResponse(
            Path: paths.ToRelativePath(directory.FullName),
            ParentPath: paths.GetParentPath(directory.FullName),
            Items: items,
            Summary: BuildSummary(items)
        );
    }
    public SearchResponse Search(string? path, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) 
            throw new ArgumentException("A search query is required.", nameof(query));
       
        var directory = GetDirectory(path);
        var searchQuery = query.Trim();

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true, // search nested dir
            IgnoreInaccessible = true, // ignore inaccessible files
            ReturnSpecialDirectories = false, // ignore . and .. dirs
            AttributesToSkip = FileAttributes.ReparsePoint // don't follow symbolic links
        };

        var matches = directory
            .EnumerateFileSystemInfos("*", options)
            .Where(entry => entry.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            .Take(_settings.MaximumSearchResults + 1)
            .ToArray();

        var isTruncated = matches.Length > _settings.MaximumSearchResults; // take 501 to see if we are greater than max of 500.

        var items = SortItems(matches
            .Take(_settings.MaximumSearchResults)
            .Select(ToFileSystemItem)
         );

        return new SearchResponse(
            Path: paths.ToRelativePath(directory.FullName),
            Query: searchQuery,
            Items: items,
            Summary: BuildSummary(items),
            IsTruncated: isTruncated
        );

    }
    public FileDownload Download(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) 
            throw new ArgumentException("A file path is required.", nameof(path));

        var fullPath = paths.ResolvePath(path);

        if (!File.Exists(fullPath)) 
            throw new FileNotFoundException("The file does not exist.");
        
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024, // 64 KB buffer
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        return new FileDownload(
            Content: stream,
            FileName: Path.GetFileName(fullPath),
            Size: stream.Length
        );
    }
    public async Task<FileSystemItem> UploadAsync(
        string? path, string fileName, Stream content)
    {
        var directory = GetDirectory(path);
        var cleanFileName = Path.GetFileName(
                fileName.Replace('\\', '/')
        );

        if (string.IsNullOrWhiteSpace(cleanFileName) || cleanFileName is "." or "..")
            throw new ArgumentException("The filename is invalid.", nameof(fileName));

        if (cleanFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) 
            throw new ArgumentException("File name contains invalid characters.", nameof(fileName));
        
        var destinationPath = paths.ResolvePath(Path.Combine(paths.ToRelativePath(directory.FullName), cleanFileName));

        if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
            throw new IOException("An item with the same name already exists.");
        
        var fileCreated = false;

        try
        {
            await using var destinationStream = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024, // 64 KB buffer
                options: FileOptions.Asynchronous | FileOptions.SequentialScan
            );

            fileCreated = true;

            await content.CopyToAsync(destinationStream);
        }
        catch (IOException) when (
            !fileCreated && (File.Exists(destinationPath) || Directory.Exists(destinationPath)))
        {
            throw new IOException("An item with the same name already exists.");
        }
        catch
        {
            if (fileCreated && File.Exists(destinationPath))
            {
                try
                {
                    File.Delete(destinationPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete the file after an error occurred during upload.");
                }
            }

            throw;
        }
        
        return ToFileSystemItem(new FileInfo(destinationPath));
    }
    public void Delete(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A file path is required.", nameof(path));

        var fullPath = paths.ResolvePath(path);

        if (Directory.Exists(fullPath))
            throw new ArgumentException("Only files can be deleted.", nameof(path));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The file does not exist.");

        File.Delete(fullPath);
    }
    private DirectoryInfo GetDirectory(string? path)
    {
        var fullPath = paths.ResolvePath(path);
        
        if (!Directory.Exists(fullPath)) 
            throw new DirectoryNotFoundException("Directory does not exist.");
        
        return new DirectoryInfo(fullPath);
    }
    private FileSystemItem ToFileSystemItem(FileSystemInfo item) => new(
            Name: item.Name,
            Path: paths.ToRelativePath(item.FullName),
            IsDirectory: item is DirectoryInfo,
            Size: item is FileInfo file ? file.Length : null,
            LastModifiedUtc: item.LastWriteTimeUtc
    );
    private static FileSystemItem[] SortItems(IEnumerable<FileSystemItem> items) => items
    .OrderByDescending(item => item.IsDirectory)
    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
    .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
    .ToArray();
    private static ViewSummary BuildSummary(IReadOnlyCollection<FileSystemItem> items) => new(
        items.Count(item => !item.IsDirectory),
        items.Count(item => item.IsDirectory),
        items.Where(item => !item.IsDirectory).Sum(item => item.Size ?? 0));
}
