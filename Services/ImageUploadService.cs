namespace Jazmin.Services;

public interface IImageUploadService
{
    Task<string?> SaveAsync(IFormFile file);
    void Delete(string relativeUrl);
}

public class ImageUploadService : IImageUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageUploadService> _log;
    private static readonly string[] AllowedExt = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxSize = 5 * 1024 * 1024;

    public ImageUploadService(IWebHostEnvironment env, ILogger<ImageUploadService> log)
    {
        _env = env;
        _log = log;
    }

    public async Task<string?> SaveAsync(IFormFile file)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxSize)
        {
            _log.LogWarning("Imagen rechazada por tamaño: {Name} ({Size} bytes)", file.FileName, file.Length);
            return null;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExt.Contains(ext))
        {
            _log.LogWarning("Extensión no permitida: {Ext}", ext);
            return null;
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadsDir, safeName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream);

        return $"/uploads/{safeName}";
    }

    public void Delete(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return;
        if (!relativeUrl.StartsWith("/uploads/")) return; // safety - only delete our uploads
        var filePath = Path.Combine(_env.WebRootPath, relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); }
            catch (Exception ex) { _log.LogWarning(ex, "No se pudo borrar {Path}", filePath); }
        }
    }
}
