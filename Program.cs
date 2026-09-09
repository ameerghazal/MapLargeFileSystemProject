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
            .ValidateOnStart();

        // Whenver IFileSystemService is asked for, a FileSystemService instance is created.
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