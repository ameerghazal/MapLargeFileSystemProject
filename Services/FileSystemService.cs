using System.IO;
using Microsoft.Extensions.Options;
using TestProject.Models;
using TestProject.Options;

namespace TestProject.Services;

public sealed class FileSystemService : IFileSystemService
{
    private readonly string _rootPath;
    private readonly StringComparison _pathComparison;
    private const int MaxSearchResults = 500;

    public FileSystemService(
        IOptions<FileBrowserOptions> options,
        IWebHostEnvironment env)
    {
        // List of rootpath, etc.
        var settings = options.Value;

        // Set the casing straight.
        _pathComparison = OperatingSystem.IsWindows() ?
            StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Set the path. If rooted, return; else, create the rooted path with the env variable.
        _rootPath = Path.GetFullPath(
            Path.IsPathRooted(settings.RootPath)
            ? settings.RootPath : Path.Combine(env.ContentRootPath, settings.RootPath)
            );

        // If the root does not exist, dir created.
        Directory.CreateDirectory(_rootPath);

    
    }

    public BrowseResponse Browse(string? path)
    {
        var fullPath = ResolvePath(path);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                "Directory does not exist."
            );
        }

        var directory = new DirectoryInfo(fullPath);

        var folders = directory
            .EnumerateDirectories()
            .Where(folder => !IsReparsePoint(folder))
            .Select(ToFileSystemItem);

        var files = directory
            .EnumerateFiles()
            .Select(ToFileSystemItem);

        var items = folders
            .Concat(files)
            .OrderBy(item => item.IsDirectory ? 0 : 1)
            .ThenBy(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BrowseResponse(
            Path: ToRelativePath(fullPath),
            ParentPath: GetParentPath(fullPath),
            Items: items,
            Summary: BuildSummary(items)
        );
    }

    public SearchResponse Search(string? path, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query is required.",
                nameof(query)
            );
        }

        var searchQuery = query.Trim();
        var fullPath = ResolvePath(path);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                "Directory does not exist."
            );
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true, // search nested dir
            IgnoreInaccessible = true, // ignore inaccessible files
            ReturnSpecialDirectories = false, // ignore . and .. dirs
            AttributesToSkip = FileAttributes.ReparsePoint // don't follow symbolic links
        };

        var matches = new DirectoryInfo(fullPath)
            .EnumerateFileSystemInfos("*", options)
            .Where(entry => entry.Name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSearchResults + 1)
            .Select(ToFileSystemItem)
            .ToArray();

        var isTruncated = matches.Length > MaxSearchResults; // take 501 to see if we are greater than max of 500.

        var items = matches
            .Take(MaxSearchResults)
            .OrderByDescending(item => item.IsDirectory)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SearchResponse(
            Path: ToRelativePath(fullPath),
            Query: searchQuery,
            Items: items,
            Summary: BuildSummary(items),
            IsTruncated: isTruncated
        );

    }

    public FileDownload Download(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Path is required.",
                nameof(path)
            );
        }

        var fullPath = ResolvePath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "File does not exist.",
                fullPath
            );
        }

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
        string? path, string fileName, Stream content, CancellationToken cancellationToken)
    {
        var directoryPath = ResolvePath(path);

        if(!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException(
                "Destination directory does not exist."
            );
        }

        var cleanFileName = Path.GetFileName(fileName);
        
        if (string.IsNullOrWhiteSpace(cleanFileName))
        {
            throw new ArgumentException(
                "Invalid file name.",
                nameof(fileName)
            );
        }

        if (cleanFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                "File name contains invalid characters.",
                nameof(fileName)
            );
        }

        var destinationPath = Path.Combine(directoryPath, cleanFileName);

        if (File.Exists(destinationPath))
        {
            throw new IOException(
                "A file with the same name already exists."
            );
        }

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

            await content.CopyToAsync(destinationStream, cancellationToken);
        } catch
        {
            if (!fileCreated && File.Exists(destinationPath))
            {
                throw new IOException(
                    "A file with the same name already exists."
                );
            }

            if (fileCreated && File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            throw;
        }

        return ToFileSystemItem(new FileInfo(destinationPath));

    }
        
    private string ResolvePath(string? relativePath)
    {
        var cleanedPath = (relativePath ?? string.Empty)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // Guard clause.
        if (Path.IsPathRooted(cleanedPath))
        {
            throw new UnauthorizedAccessException(
                "Absolute paths are not allowed.");
        }

        var fullPath = Path.GetFullPath(
            Path.Combine(_rootPath, cleanedPath)
            );

        var isRoot = string.Equals(fullPath, _rootPath, _pathComparison);

        var isInsideRoot = fullPath.StartsWith(
            _rootPath + Path.DirectorySeparatorChar,
            _pathComparison
            );

        if (!isRoot && !isInsideRoot)
        {
            throw new UnauthorizedAccessException(
                "The requested path is outside the directory."
            );
        }

        EnsureExisitingSegmentsAreNotLinks(fullPath);

        return fullPath;
    }

    private void EnsureExisitingSegmentsAreNotLinks(string candidatePath)
    {
        var relativePath = Path.GetRelativePath(
            _rootPath,
            candidatePath
         );

        if (relativePath == ".") return;

        var currentPath = _rootPath;
        var segments = relativePath.Split(
            Path.DirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries
         );

        foreach (var segment in segments)
        {
            currentPath = Path.Combine(currentPath, segment);

            if (!File.Exists(currentPath) && !Directory.Exists(currentPath)) break;

            var attributes = File.GetAttributes(currentPath);

            // TODO: What is this.
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new UnauthorizedAccessException(
                    "No symbolic links allowed."
                );
            }
        }
    }

    private FileSystemItem ToFileSystemItem(FileSystemInfo item)
    {
        // Converts to a directory or file item, relative to its type. Both directory and fileInfo inherit from fileSystemInfo.
        return new FileSystemItem(
            Name: item.Name,
            Path: ToRelativePath(item.FullName),
            IsDirectory: item is DirectoryInfo,
            Size: item is FileInfo file ? file.Length : null,
            LastModifiedUtc: item.LastWriteTimeUtc
        );
    }

    private string ToRelativePath(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);

        if (relativePath == ".") return string.Empty;

        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private string? GetParentPath(string fullPath)
    {
        if (string.Equals(fullPath, _rootPath, _pathComparison)) return null;

        var parentDir = Directory.GetParent(fullPath);

        return parentDir is null ? null : ToRelativePath(parentDir.FullName);
    }

    private static ViewSummary BuildSummary(
        IReadOnlyCollection<FileSystemItem> items)
    {
        var fileCount = items.Count(item => !item.IsDirectory);
        var folderCount = items.Count(item => item.IsDirectory);
        var totalFileSize = items.Where(item => !item.IsDirectory).Sum(item => item.Size ?? 0);
        return new ViewSummary(
            FileCount: fileCount,
            FolderCount: folderCount,
            TotalFileSize: totalFileSize
        );
    }

    private static bool IsReparsePoint(FileSystemInfo item)
    {
        return (item.Attributes & FileAttributes.ReparsePoint) != 0;
    }
}
