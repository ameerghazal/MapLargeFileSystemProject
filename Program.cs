using TestProject.Options;
using TestProject.Services;

namespace TestProject; 
public class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddOptions<FileBrowserOptions>()
            .Bind(builder.Configuration.GetSection(
                FileBrowserOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(
                options.RootPath),
                "FileBrowser:RootPath is required.")
            .Validate(settings => settings.MaximumUploadSizeInBytes > 0 &&
                settings.MaximumUploadSizeInBytes <= long.MaxValue - 1000 * 1000,
                "MaximumUploadSizeInBytes must be positive and leave room for request overhead.")
            .Validate(settings => settings.MaximumSearchResults > 0 &&
                settings.MaximumSearchResults < int.MaxValue,
                "MaximumSearchResults must be between 1 and 2147483646.")
            .ValidateOnStart();

        // Whenver IFileSystemService is asked for, a FileSystemService instance is created.
        builder.Services.AddSingleton<FilePathResolve>();
        builder.Services.AddSingleton<
            IFileSystemService, FileSystemService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseHttpsRedirection();

        // Rewrites the file path, must be used before the static files.
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.MapControllers();
        app.Run();
    }
}