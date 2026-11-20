using PortfolioAdmin.Web.Data;
using PortfolioAdmin.Web.Data.Entities;

namespace PortfolioAdmin.Web.Services;

public class MediaException(string message) : Exception(message);

/// <summary>
/// Stores uploaded images under wwwroot/uploads with random file names.
/// The file type is checked from its first bytes (not from the name or the browser's content type).
/// Swap this class for an S3 implementation later without changing callers.
/// </summary>
public class MediaStorage(IWebHostEnvironment env, AppDbContext db, IConfiguration config)
{
    public const long MaxBytes = 5 * 1024 * 1024;

    static readonly (string Ext, string Type, byte[] Magic)[] Signatures =
    [
        (".jpg", "image/jpeg", [0xFF, 0xD8, 0xFF]),
        (".png", "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        (".gif", "image/gif", "GIF8"u8.ToArray()),
        (".webp", "image/webp", "RIFF"u8.ToArray()), // + "WEBP" at offset 8, checked below
    ];

    public async Task<MediaFile> SaveImageAsync(IFormFile file, string? userId)
    {
        if (file.Length == 0) throw new MediaException("The file is empty.");
        if (file.Length > MaxBytes) throw new MediaException("Images must be 5 MB or smaller.");

        var head = new byte[12];
        await using (var s = file.OpenReadStream()) _ = await s.ReadAsync(head);

        var sig = Signatures.FirstOrDefault(x => head.AsSpan().StartsWith(x.Magic));
        if (sig.Ext is null || (sig.Ext == ".webp" && !head.AsSpan(8, 4).SequenceEqual("WEBP"u8)))
            throw new MediaException("Only JPG, PNG, GIF and WebP images are allowed.");

        var folder = DateTime.UtcNow.ToString("yyyy-MM");
        var dir = Path.Combine(env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + sig.Ext;
        await using (var outStream = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(outStream);

        var media = new MediaFile
        {
            OriginalName = Path.GetFileName(file.FileName)[..Math.Min(Path.GetFileName(file.FileName).Length, 200)],
            Path = $"/uploads/{folder}/{name}",
            ContentType = sig.Type,
            Size = file.Length,
            UploadedById = userId,
        };
        db.MediaFiles.Add(media);
        return media;
    }

    public void Delete(MediaFile media)
    {
        var full = Path.GetFullPath(Path.Combine(env.WebRootPath, media.Path.TrimStart('/')));
        var uploads = Path.GetFullPath(Path.Combine(env.WebRootPath, "uploads"));
        if (full.StartsWith(uploads, StringComparison.OrdinalIgnoreCase) && File.Exists(full)) File.Delete(full);
        db.MediaFiles.Remove(media);
    }

    /// <summary>Uploaded paths become absolute so the portfolio (on another host) can load them.</summary>
    public string? PublicUrl(string? path, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/uploads/")) return path;
        var baseUrl = config["App:PublicBaseUrl"]?.TrimEnd('/') ?? $"{request.Scheme}://{request.Host}";
        return baseUrl + path;
    }
}
