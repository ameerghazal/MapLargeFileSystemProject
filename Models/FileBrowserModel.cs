namespace TestProject.Models;
public sealed record FileSystemItem(
    string Name,
    string Path,
    bool IsDirectory,
    long? Size,
    DateTime LastModifiedUtc);
public sealed record ViewSummary(
    int FileCount,
    int FolderCount,
    long TotalFileSize);

public sealed record BrowseResponse(
    string Path,
    string? ParentPath,
    IReadOnlyList<FileSystemItem> Items,
    ViewSummary Summary);

public sealed record SearchResponse(
    string Path,
    string Query,
    IReadOnlyList<FileSystemItem> Items,
    ViewSummary Summary,
    bool IsTruncated);

public sealed record FileDownload(
    Stream Content,
    string FileName,
    long Size);

public sealed record FileBrowserSettings(
    long MaximumUploadSizeInBytes,
    int MaximumSearchResults);

// TODO: Delete, move, copy files and folders
