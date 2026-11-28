using System.Text.RegularExpressions;
using Cenvo.Modules.CarWash.Contracts;

namespace Cenvo.Api.Storage;

public class PhotoStorageOptions
{
    /// <summary>Folder for uploaded photos; relative paths are under the app's content root. Not served as static files.</summary>
    public string Root { get; set; } = "App_Data/photos";
}

/// <summary>
/// Keeps photos on the local disk under server-generated keys (<c>yyyy/MM/guid.ext</c>).
/// Keys are validated before use, so a stored value can never point outside the root folder.
/// </summary>
public partial class LocalPhotoStorage(IWebHostEnvironment env, Microsoft.Extensions.Options.IOptions<PhotoStorageOptions> options)
    : IPhotoStorage
{
    private readonly string _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.Root));

    public async Task<string> Save(Stream content, string extension, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var key = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var path = PathOf(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public Stream? Open(string storageKey)
    {
        var path = PathOf(storageKey);
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    public void Delete(string storageKey)
    {
        var path = PathOf(storageKey);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathOf(string key)
    {
        if (!KeyFormat().IsMatch(key)) throw new InvalidOperationException("Khóa ảnh không hợp lệ.");
        return Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));
    }

    [GeneratedRegex(@"^\d{4}/\d{2}/[0-9a-f]{32}\.(jpg|png|webp)$")]
    private static partial Regex KeyFormat();
}
