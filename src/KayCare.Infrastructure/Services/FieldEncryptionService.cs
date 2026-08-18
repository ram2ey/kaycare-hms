using System.Security.Cryptography;
using System.Text;
using KayCare.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace KayCare.Infrastructure.Services;

/// <summary>
/// AES-256-GCM, one random 12-byte nonce per Encrypt() call (never reused - GCM security
/// depends on nonce uniqueness under a fixed key). Output is base64(nonce || ciphertext || tag).
/// Non-deterministic by design: the same plaintext encrypts differently every time, which is why
/// this is only ever applied to fields that are never compared/searched/sorted in SQL.
/// </summary>
public class FieldEncryptionService : IFieldEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize   = 16;

    private readonly byte[] _key;

    public FieldEncryptionService(IConfiguration config)
    {
        var keyBase64 = config["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyBase64) || keyBase64.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Encryption:Key is missing or is still the placeholder value. Set a real base64-encoded 32-byte key before starting.");
        }

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
        {
            throw new InvalidOperationException(
                $"Encryption:Key must decode to exactly 32 bytes (AES-256); got {_key.Length}.");
        }
    }

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce      = RandomNumberGenerator.GetBytes(NonceSize);
        var cipherBytes = new byte[plainBytes.Length];
        var tag         = new byte[TagSize];

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        var result = new byte[NonceSize + cipherBytes.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + cipherBytes.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext is too short to contain a nonce and tag.");
        }

        var nonce       = data.AsSpan(0, NonceSize);
        var cipherBytes = data.AsSpan(NonceSize, data.Length - NonceSize - TagSize);
        var tag         = data.AsSpan(data.Length - TagSize, TagSize);
        var plainBytes  = new byte[cipherBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
