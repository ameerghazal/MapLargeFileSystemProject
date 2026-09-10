using Microsoft.Extensions.Options;
using TestProject.Models;
using TestProject.Options;

namespace TestProject.Services;

public sealed class FilePathResolve
{
    private readonly string _rootPath;
    private readonly string _rootPrefix;
    private readonly StringComparison _pathComparison;
    public FilePathResolve(IOptions<FileBrowserOptions> options,
        IWebHostEnvironment env)
    {
        // List of options, containing rootpath, max upload size, and max search results.
        var settings = options.Value;

        // Set the casing straight.
        _pathComparison = OperatingSystem.IsWindows() ?
            StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // Set the path. If rooted, return; else, create the rooted path with the env variable.
        _rootPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(settings.RootPath, env.ContentRootPath)
         );

        // Get the prefix of the rooted path, which is the root of the drive. For example, C:\ or /home/user.
        _rootPrefix = Path.EndsInDirectorySeparator(_rootPath) ? _rootPath : _rootPath + Path.DirectorySeparatorChar;

        // If the root does not exist, dir created.
        Directory.CreateDirectory(_rootPath);

        if (IsReparsePoint(new DirectoryInfo(_rootPath)))
        {
            throw new InvalidOperationException("The configured root cannot be a symbolic link.");
        }
    }
    public string ResolvePath(string? relativePath)
    {
        var cleanedPath = (relativePath ?? string.Empty)
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        if (Path.IsPathRooted(cleanedPath))
        {
            throw new UnauthorizedAccessException(
                "Absolute paths are not allowed.");
        }

        var fullPath = Path.TrimEndingDirectorySeparator(
            Path.GetFullPath(Path.Combine(_rootPath, cleanedPath)));

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
    public string ToRelativePath(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);

        return relativePath == "." ? string.Empty : relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }
    public string? GetParentPath(string fullPath)
    {
        if (string.Equals(fullPath, _rootPath, _pathComparison)) return null;

        var parentDir = Directory.GetParent(fullPath);

        return parentDir is null ? null : ToRelativePath(parentDir.FullName);
    }
    public static bool IsReparsePoint(FileSystemInfo item) => 
        (item.Attributes & FileAttributes.ReparsePoint) != 0;
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

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new UnauthorizedAccessException(
                    "No symbolic links allowed."
                );
            }
        }
    }
}
