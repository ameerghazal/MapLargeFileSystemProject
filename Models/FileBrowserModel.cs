namespace TestProject.Models
{

    // Using a relative path s.t. we don't reveal server's internal dir structure.
    public sealed record FileSystemItem(
        string Name,
        string Path,
        bool IsDirectory,
        long? Size,
        DateTime LastModifiedUtc);


    // Used in the minature view.
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

    // TODO: Delete, move, copy files and folders



}
