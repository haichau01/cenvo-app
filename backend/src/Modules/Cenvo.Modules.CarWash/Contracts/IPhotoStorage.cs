namespace Cenvo.Modules.CarWash.Contracts;

/// <summary>Where vehicle condition photos are kept (the Api implements it — local disk today, object storage later).</summary>
public interface IPhotoStorage
{
    /// <returns>An opaque storage key to keep on the photo record.</returns>
    Task<string> Save(Stream content, string extension, CancellationToken ct);

    /// <returns>The file, or null when it no longer exists.</returns>
    Stream? Open(string storageKey);

    void Delete(string storageKey);
}
