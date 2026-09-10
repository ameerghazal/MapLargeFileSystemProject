using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestProject.Models;
using TestProject.Options;
using TestProject.Services;

namespace TestProject.Controllers;

[ApiController]
[Route("api/files")] // base route.
public class FileController : ControllerBase
{
    // Service instance.
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<FileController> _logger;
    private readonly IOptions<FileBrowserOptions> _options;
    public FileController(
        IFileSystemService fileSystemService,
        ILogger<FileController> logger,
        IOptions<FileBrowserOptions> options)
    {
        _fileSystemService = fileSystemService;
        _logger = logger;
        _options = options;
    }

    [HttpGet("Settings")]
    public FileBrowserSettings Settings() => new(
        _options.Value.MaximumUploadSizeInBytes,
        _options.Value.MaximumSearchResults);

    [HttpGet("Browse")]
    public ActionResult<BrowseResponse> Browse(
        [FromQuery] string? path = null) // from query tells .NET to read path from URL query string.
    {
        try
        {
            var response = _fileSystemService.Browse(path);
            return Ok(response);
        } 
        // Error handling, ai'd.
        catch (UnauthorizedAccessException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DirectoryNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            // Server log gets the exception, while the client gets a simpler version.
            _logger.LogError(exception, "Failed to browse path {Path}.", path);
            return StatusCode(500, new { error = "The directory could not be read" });
        }
    }

    [HttpGet("Search")]
    public ActionResult<SearchResponse> Search(
        [FromQuery] string? path, [FromQuery] string? query)
    {
        try
        {
            var response = _fileSystemService.Search(path, query ?? string.Empty);
            return Ok(response);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(403, new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DirectoryNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to search path {Path} with query {Query}.", path, query);
            return StatusCode(500, new { error = "The search operation failed." });
        }
    }

    [HttpGet("Download")]
    public IActionResult Download([FromQuery] string path)
    {
        try
        {
            var download = _fileSystemService.Download(path);
            return File(download.Content, "application/octet-stream", download.FileName, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(403, new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (FileNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to download file at path {Path}.", path);
            return StatusCode(500, new { error = "The file could not be downloaded." });
        }
    }

    [HttpPost("Upload")]
    public async Task<ActionResult<FileSystemItem>> Upload(
        [FromQuery] string? path, 
        [FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file was uploaded." });
        }

        if (file.Length > _options.Value.MaximumUploadSizeInBytes)
        {
            return BadRequest(new { error = $"File size exceeds the maximum limit of {_options.Value.MaximumUploadSizeInBytes / (1000 * 1000)} MB." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploadedFile = await _fileSystemService.UploadAsync(path, file.FileName, stream);
            var downloadUrl = $"/api/files/download?path={Uri.EscapeDataString(uploadedFile.Path)}";
            return Created(downloadUrl, uploadedFile);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(403, new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DirectoryNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to upload file to path {Path}.", path);
            return StatusCode(500, new { error = "The file could not be uploaded." });
        }
    }

    [HttpDelete("Delete")]
    public IActionResult Delete([FromQuery] string path)
    {
        try
        {
            _fileSystemService.Delete(path);
            return NoContent();
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(403, new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (FileNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (IOException exception)
        {
            _logger.LogWarning(
                exception,
                "Could not delete file at path {Path}.",
                path);

            return Conflict(new
            {
                error = "The file could not be deleted. It may be in use."
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to delete file at path {Path}.",
                path);

            return StatusCode(500, new
            {
                error = "The file could not be deleted."
            });
        }
    }
}
