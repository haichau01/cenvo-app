namespace Cenvo.Modules.CarWash.Domain.Calculations;

/// <summary>Recognises an image by its first bytes (never trust the file name or the client's content type).</summary>
public static class ImageFormat
{
    public const long MaxBytes = 5 * 1024 * 1024;
    /// <summary>Bytes needed to recognise every supported format.</summary>
    public const int HeaderLength = 12;

    public record Format(string ContentType, string Extension);

    private static readonly Format Jpeg = new("image/jpeg", ".jpg");
    private static readonly Format Png = new("image/png", ".png");
    private static readonly Format Webp = new("image/webp", ".webp");

    /// <returns>The format, or null when the bytes are not a JPEG, PNG or WebP image.</returns>
    public static Format? Detect(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF])) return Jpeg;
        if (header.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return Png;
        if (header.Length >= HeaderLength && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8)) return Webp;
        return null;
    }
}
