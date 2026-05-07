using Microsoft.AspNetCore.Hosting;

namespace FindActivity.Web.Services;

/// <summary>
/// Saves and removes cover images for activities. Files live under wwwroot/uploads/activities/
/// and are referenced by their relative URL (e.g. "/uploads/activities/{guid}.jpg") on the entity.
/// </summary>
public class ActivityImageService
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private const string UploadFolderRelative = "uploads/activities";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ActivityImageService> _logger;

    public ActivityImageService(IWebHostEnvironment env, ILogger<ActivityImageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Validates the upload, writes it to disk with a Guid-based filename, and returns the
    /// browser-relative path (e.g. "/uploads/activities/{guid}.jpg") to persist on the entity.
    /// Returns (null, error) if validation fails.
    /// </summary>
    public async Task<(string? RelativePath, string? Error)> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "No file provided.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return (null, $"File is too large. Maximum is {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return (null, "Only JPG, PNG, or WebP files are allowed.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant()))
        {
            return (null, "File content type doesn't look like an image.");
        }

        var folderAbsolute = Path.Combine(_env.WebRootPath, UploadFolderRelative);
        Directory.CreateDirectory(folderAbsolute);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folderAbsolute, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativeUrl = $"/{UploadFolderRelative}/{fileName}";
        _logger.LogInformation("Saved activity cover image to {Path} ({Bytes} bytes).", relativeUrl, file.Length);
        return (relativeUrl, null);
    }

    /// <summary>
    /// Best-effort delete of a previously-saved cover image. Pass the relative URL stored on the entity.
    /// Failures are logged but not thrown — a missing file shouldn't break the activity edit.
    /// </summary>
    public void TryDelete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return;
        }

        // Only delete files inside our uploads folder — defense against path traversal if the value got tampered with.
        if (!relativeUrl.StartsWith("/" + UploadFolderRelative + "/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refusing to delete file outside uploads folder: {Path}", relativeUrl);
            return;
        }

        var fileName = Path.GetFileName(relativeUrl);
        var fullPath = Path.Combine(_env.WebRootPath, UploadFolderRelative, fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted activity cover image {Path}.", relativeUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete activity cover image {Path}.", relativeUrl);
        }
    }
}
