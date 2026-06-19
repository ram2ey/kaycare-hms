using KayCare.Core.Interfaces;
using Supabase.Storage;

namespace KayCare.Infrastructure.Services;

/// <summary>
/// Supabase Storage implementation of IBlobStorageService.
/// Each "containerName" maps to a Supabase Storage bucket (created on demand).
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly Supabase.Client _supabase;

    public BlobStorageService(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    // ── Upload ─────────────────────────────────────────────────────────────────

    public async Task UploadAsync(
        string containerName,
        string blobPath,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        await EnsureBucketAsync(containerName);

        // Read stream into bytes — Supabase C# SDK requires byte[]
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var options = new Supabase.Storage.FileOptions { ContentType = contentType, Upsert = true };
        await _supabase.Storage.From(containerName).Upload(bytes, blobPath, options);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    public async Task DeleteAsync(
        string containerName,
        string blobPath,
        CancellationToken ct = default)
    {
        await _supabase.Storage.From(containerName).Remove(new List<string> { blobPath });
    }

    // ── Download ───────────────────────────────────────────────────────────────

    public async Task<byte[]?> DownloadAsync(
        string containerName,
        string blobPath,
        CancellationToken ct = default)
    {
        try
        {
            return await _supabase.Storage.From(containerName).Download(blobPath, null);
        }
        catch
        {
            // Supabase throws when the object does not exist
            return null;
        }
    }

    // ── Signed URL ─────────────────────────────────────────────────────────────

    public async Task<Uri> GenerateSasUriAsync(
        string containerName,
        string blobPath,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var expiresInSeconds = (int)expiry.TotalSeconds;
        var signedUrl = await _supabase.Storage
            .From(containerName)
            .CreateSignedUrl(blobPath, expiresInSeconds);

        return new Uri(signedUrl);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Creates a private bucket if it does not already exist.</summary>
    private async Task EnsureBucketAsync(string bucketName)
    {
        try
        {
            await _supabase.Storage.CreateBucket(bucketName, new BucketUpsertOptions { Public = false });
        }
        catch
        {
            // Bucket already exists — safe to ignore
        }
    }
}
