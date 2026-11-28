using System.Security.Cryptography;
using Cenvo.Infrastructure.Security;

namespace Cenvo.Tests.Unit;

public class PersonalDataCipherTests
{
    private static AesGcmPersonalDataCipher Cipher(byte fill = 7) => new(Enumerable.Repeat(fill, 32).ToArray());

    [Fact]
    public void Round_trips_and_never_stores_the_plain_value()
    {
        var stored = Cipher().Encrypt("079123456789");
        Assert.DoesNotContain("079123456789", stored);
        Assert.True(stored.Length <= EncryptedPropertyExtensions.StoredLength(12));
        Assert.Equal("079123456789", Cipher().Decrypt(stored));
    }

    [Fact]
    public void Same_value_encrypts_differently_each_time() =>
        Assert.NotEqual(Cipher().Encrypt("0901234567"), Cipher().Encrypt("0901234567"));

    [Fact]
    public void Tampered_value_or_wrong_key_is_rejected()
    {
        var stored = Cipher().Encrypt("079123456789");
        var bytes = Convert.FromBase64String(stored[3..]);
        bytes[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => Cipher().Decrypt("v1:" + Convert.ToBase64String(bytes)));
        Assert.ThrowsAny<CryptographicException>(() => Cipher(fill: 8).Decrypt(stored));
        Assert.ThrowsAny<CryptographicException>(() => Cipher().Decrypt("079123456789")); // plain text in the column
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64!")]
    [InlineData("AAAA")]
    public void Missing_or_bad_key_stops_the_app(string? key) =>
        Assert.Throws<InvalidOperationException>(() => AesGcmPersonalDataCipher.FromBase64(key));
}
