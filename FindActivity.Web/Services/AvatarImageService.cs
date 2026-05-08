using Microsoft.AspNetCore.Hosting;

namespace FindActivity.Web.Services;

/// <summary>
/// Saves and removes user avatar images. Mirrors <see cref="ActivityImageService"/> but writes to
/// wwwroot/uploads/avatars/ and uses a smaller max file size since avatars don't need to be large.
/// </summary>
public class AvatarImageService
{
    public const long MaxFileSizeBytes = 3 * 1024 * 1024; // 3 MB

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };

    private const string UploadFolderRelative = "uploads/avatars";

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AvatarImageService> _logger;

    public AvatarImageService(IWebHostEnvironment env, ILogger<AvatarImageService> logger)
    {
        _env = env;
        _logger = logger;
    }

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
        _logger.LogInformation("Saved avatar to {Path} ({Bytes} bytes).", relativeUrl, file.Length);
        return (relativeUrl, null);
    }

    public void TryDelete(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            return;
        }

        if (!relativeUrl.StartsWith("/" + UploadFolderRelative + "/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Refusing to delete file outside avatars folder: {Path}", relativeUrl);
            return;
        }

        var fileName = Path.GetFileName(relativeUrl);
        var fullPath = Path.Combine(_env.WebRootPath, UploadFolderRelative, fileName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted avatar {Path}.", relativeUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete avatar {Path}.", relativeUrl);
        }
    }
}
