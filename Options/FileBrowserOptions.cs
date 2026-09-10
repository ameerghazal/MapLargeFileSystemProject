namespace TestProject.Options;

public sealed class FileBrowserOptions
{
    public const string SectionName = "FileBrowser";
    public string RootPath { get; init; } = "MockDirectory";
    public long MaximumUploadSizeInBytes { get; init; } = 25 * 1000 * 1000; // 25 MB
    public int MaximumSearchResults { get; init; } = 500;
}
