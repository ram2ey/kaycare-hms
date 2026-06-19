namespace KayCare.Core.Interfaces;

public interface IBlobStorageService
{
    /// <summary>Uploads a stream to the given container/blobPath. Creates the container if it doesn't exist.</summary>
    Task UploadAsync(string containerName, string blobPath, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Permanently deletes a blob.</summary>
    Task DeleteAsync(string containerName, string blobPath, CancellationToken ct = default);

    /// <summary>Returns a time-limited signed URI for reading a single blob.</summary>
    Task<Uri> GenerateSasUriAsync(string containerName, string blobPath, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Downloads blob content as bytes. Returns null if the blob does not exist.</summary>
    Task<byte[]?> DownloadAsync(string containerName, string blobPath, CancellationToken ct = default);
}
