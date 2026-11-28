using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cenvo.Infrastructure.Security;

/// <summary>Encrypts personal identifiers (national ID, phone) before they reach the database.</summary>
public interface IPersonalDataCipher
{
    string Encrypt(string plainText);
    string Decrypt(string stored);
}

/// <summary>
/// AES-256-GCM: random 96-bit nonce per value, 128-bit tag (tampering is detected). Stored as
/// <c>v1:</c> + base64(nonce | ciphertext | tag) — the version prefix leaves room for key rotation.
/// Encryption is not deterministic, so encrypted columns cannot be searched or indexed by value.
/// </summary>
public sealed class AesGcmPersonalDataCipher : IPersonalDataCipher
{
    public const int KeySize = 32;
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmPersonalDataCipher(byte[] key)
    {
        if (key.Length != KeySize) throw new ArgumentException($"Personal data key must be {KeySize} bytes.", nameof(key));
        _key = key;
    }

    /// <exception cref="InvalidOperationException">Missing or malformed key — the app must not start without it.</exception>
    public static AesGcmPersonalDataCipher FromBase64(string? base64)
    {
        try
        {
            return new AesGcmPersonalDataCipher(Convert.FromBase64String(base64 ?? ""));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            throw new InvalidOperationException(
                "Thiếu hoặc sai PersonalData:Key — cần 32 byte dạng base64 (tạo bằng: openssl rand -base64 32).", ex);
        }
    }

    public string Encrypt(string plainText)
    {
        var plain = Encoding.UTF8.GetBytes(plainText);
        var buffer = new byte[NonceSize + plain.Length + TagSize];
        var nonce = buffer.AsSpan(0, NonceSize);
        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, buffer.AsSpan(NonceSize, plain.Length), buffer.AsSpan(NonceSize + plain.Length, TagSize));
        return Prefix + Convert.ToBase64String(buffer);
    }

    public string Decrypt(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Personal data is not in the expected encrypted format.");
        var buffer = Convert.FromBase64String(stored[Prefix.Length..]);
        var length = buffer.Length - NonceSize - TagSize;
        if (length < 0) throw new CryptographicException("Encrypted personal data is truncated.");
        var plain = new byte[length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(buffer.AsSpan(0, NonceSize), buffer.AsSpan(NonceSize, length), buffer.AsSpan(NonceSize + length, TagSize), plain);
        return Encoding.UTF8.GetString(plain);
    }
}

public static class EncryptedPropertyExtensions
{
    /// <summary>Base64 of nonce + tag + up to this many plaintext bytes, plus the version prefix.</summary>
    public static int StoredLength(int maxPlainBytes) => 3 + (12 + maxPlainBytes + 16 + 2) / 3 * 4;

    /// <summary>Stores the column encrypted; null stays null. The value cannot be used in WHERE / ORDER BY.</summary>
    public static PropertyBuilder<string?> IsEncrypted(this PropertyBuilder<string?> property, IPersonalDataCipher cipher, int maxPlainBytes) =>
        property
            .HasConversion(new ValueConverter<string?, string?>(
                plain => plain == null ? null : cipher.Encrypt(plain),
                stored => stored == null ? null : cipher.Decrypt(stored)))
            .HasMaxLength(StoredLength(maxPlainBytes));
}
