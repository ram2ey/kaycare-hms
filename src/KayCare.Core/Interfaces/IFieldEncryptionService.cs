namespace KayCare.Core.Interfaces;

/// <summary>
/// Authenticated column-level encryption for PII/PHI fields that are never used in a SQL
/// WHERE/ORDER BY (search, sort, and uniqueness all require either plaintext or a separate
/// deterministic blind-index column, neither of which this covers - see DB17/DB18 tracker notes).
/// </summary>
public interface IFieldEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}
